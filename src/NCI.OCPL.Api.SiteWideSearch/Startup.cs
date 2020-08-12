using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Cors.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Nest;
using Elasticsearch.Net;
using System.Text;
using NSwag.AspNetCore;
using System.Reflection;
using NJsonSchema;

namespace NCI.OCPL.Api.SiteWideSearch
{
    /// <summary>
    /// Defines the configuration for the Sitewide Search API.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:NCI.OCPL.Api.SiteWideSearch.Startup"/> class.
        /// </summary>
        /// <param name="configuration">configuration</param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Configures the services.
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <returns>The services.</returns>
        /// <param name="services">Services.</param>
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddLogging();

            //Turn on the OptionsManager that supports IOptions
            services.AddOptions();

            //Adding CORS service
            services.AddCors();

            // Add configuration mappings
            services.Configure<SearchIndexOptions>(Configuration.GetSection("SearchIndexOptions"));
            services.Configure<AutosuggestIndexOptions>(Configuration.GetSection("AutosuggestIndexOptions"));
            services.Configure<NSwagOptions>(Configuration.GetSection("NSwag"));

            // This will inject an IElasticClient using our configuration into any
            // controllers that take an IElasticClient parameter into its constructor.
            //
            // AddTransient means that it will instantiate a new instance of our client
            // for each instance of the controller.  So the function below will be called
            // on each request.
            services.AddTransient<IElasticClient>(p => {

                // Get the ElasticSearch servers that we will be connecting to.
                // Ideally, we'd load Configuration via the Dependency Injection framework,
                // but this will work for now.
                string username = Configuration["Elasticsearch:Userid"];
                string password = Configuration["Elasticsearch:Password"];

                List<Uri> uris = GetServerUriList();

                // Create the connection pool, the SniffingConnectionPool will
                // keep tabs on the health of the servers in the cluster and
                // probe them to ensure they are healthy.  This is how we handle
                // redundancy and load balancing.
                var connectionPool = new SniffingConnectionPool(uris);
                //var connectionPool = new StaticConnectionPool(uris);

                //Return a new instance of an ElasticClient with our settings
                ConnectionSettings settings = new ConnectionSettings(connectionPool)
                    .BasicAuthentication(username, password);

                if (Configuration.GetValue<bool>("Elasticsearch:EnableDebugging", false)) {
                    settings = settings.DisableDirectStreaming();
                }

                return new ElasticClient(settings);
            });

            // Add framework services.
            services.AddMvc();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="env">The hosting environment.</param>
        /// <param name="loggerFactory">Factory for creating loggers.</param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseStaticFiles();
            // Enable the Swagger UI middleware and the Swagger generator
            app.UseSwaggerUi3(typeof(Startup).GetTypeInfo().Assembly, settings =>
            {
                settings.GeneratorSettings.DefaultPropertyNameHandling = PropertyNameHandling.CamelCase;

                if(!string.IsNullOrEmpty(Configuration["NSwag:Title"]))
                {
                    settings.GeneratorSettings.Title = Configuration["NSwag:Title"];
                }

                if (!string.IsNullOrEmpty(Configuration["NSwag:Description"]))
                {
                    settings.GeneratorSettings.Description = Configuration["NSwag:Description"];
                }

                settings.SwaggerUiRoute = "";
                settings.PostProcess = document => {
                    document.Host = null;
                };
            });

            // Allow use from anywhere.
            app.UseCors(builder => builder.AllowAnyOrigin());

            // This is equivelant to the old Global.asax OnError event handler.
            // It will handle any unhandled exception and return a status code to the
            // caller.  IF the error is of type APIErrorException then we will also return
            // a message along with the status code.  (Otherwise we )
            app.UseExceptionHandler(errorApp => {
                errorApp.Run(async context => {
                    context.Response.StatusCode = 500; // or another Status accordingly to Exception Type
                    context.Response.ContentType = "application/json";

                    var error = context.Features.Get<IExceptionHandlerFeature>();

                    if (error != null)
                    {
                        var ex = error.Error;

                        //Unhandled exceptions may not be sanitized, so we will not
                        //display the issue.
                        string message = "Errors have occurred.  Type: " + ex.GetType().ToString();

                        //Our own exceptions should be sanitized enough.
                        if (ex is APIErrorException) {
                            context.Response.StatusCode = ((APIErrorException)ex).HttpStatusCode;
                            message = ex.Message;
                        }

                        byte[] contents = Encoding.UTF8.GetBytes(new ErrorMessage(){
                            Message = message
                        }.ToString());

                        // HACK: This is a fix for a bug in .NET Core and the CORS middleware
                        // When the pull request that fixes the timing of setting the CORS header (https://github.com/aspnet/CORS/pull/163) goes through,
                        // we should remove this and test to see if it works without the hack.
                        if (context.Request.Headers.ContainsKey("Origin"))
                        {
                            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                        }

                        await context.Response.Body.WriteAsync(contents, 0, contents.Length);
                    }
                });
            });

            app.UseMvc();
        }

        /// <summary>
        /// Retrieves a list of Elasticsearch server URIs from the configuration's Elasticsearch:Servers setting.
        /// </summary>
        /// <returns>Returns a list of one or more Uri objects representing the configured set of Elasticsearch servers</returns>
        /// <remarks>
        /// The configuration's Elasticsearch:Servers property is required to contain URIs for one or more Elasticsearch servers.
        /// Each URI must include a protocol (http or https), a server name, and optionally, a port number.
        /// Multiple URIs are separated by a comma.  (e.g. "https://fred:9200, https://george:9201, https://ginny:9202")
        ///
        /// Throws ConfigurationException if no servers are configured.
        ///
        /// Throws UriFormatException if any of the configured server URIs are not formatted correctly.
        /// </remarks>
        private List<Uri> GetServerUriList(){
            List<Uri> uris = new List<Uri>();

            string serverList = Configuration["Elasticsearch:Servers"];
            if(!String.IsNullOrWhiteSpace(serverList))
            {
                // Convert the list of servers into a list of Uris.
                string[] names = serverList.Split(',');
                uris.AddRange(names.Select(server => new Uri(server)));
            }
            else
            {
                throw new ConfigurationException("No servers configured");
            }

            return uris;
        }
    }
}
