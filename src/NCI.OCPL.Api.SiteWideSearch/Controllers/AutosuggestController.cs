using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

using Nest;

namespace NCI.OCPL.Api.SiteWideSearch.Controllers
{
    /// <summary>
    /// A Controller for handling requests for SiteWideSearch Suggestions
    /// </summary>
    [Route("[controller]")]
    public class AutosuggestController : Controller
    {
        // Static to limit to a single instance (can't do const for non-scalar types)
        static readonly string[] validLanguages = {"en", "es"};

        private readonly IElasticClient _elasticClient;
        private readonly AutosuggestIndexOptions _indexConfig;
        private readonly ILogger<AutosuggestController> _logger;

        public const string HEALTHY_STATUS = "alive!";

        public AutosuggestController(IElasticClient elasticClient,
            IOptions<AutosuggestIndexOptions> config,
            ILogger<AutosuggestController> logger)
        {
            _elasticClient = elasticClient;
            _indexConfig = config.Value;
            _logger = logger;
        }

        // GET autosuggset/cgov_en/lung+cancer
        /// <summary>
        /// Retrieves a collection of potential search terms based on the value passed as term.
        /// </summary>
        /// <param name="collection">The search collection/strategy to use. This defines the ES template to use.</param>
        /// <param name="language">The language to use. Only "en" and "es" are currently supported.</param>
        /// <param name="term">The search term to use as a basis for search terms</param>
        /// <param name="size">The maximum number of results to return.</param>
        /// <returns>A Suggestions collection of Suggestion objects.</returns>
        /// <remarks>
        /// Collection is of the form {sitename}_{lang_code}.  Currently, {sitename} is always "cgov" and {lang_code} may
        /// be either "en" (English) or "es" (Español).
        /// </remarks>
        [HttpGet("{collection}/{language}/{*term}")]

        public Suggestions Get(
            string collection,
            string language,
            string term,
            [FromQuery] int size = 10
            )
        {
            if (string.IsNullOrWhiteSpace(collection))
                throw new APIErrorException(400, "You must supply a language and term");

            if(!validLanguages.Contains(language))
                throw new APIErrorException(400, "Not a valid language code.");

            if (string.IsNullOrWhiteSpace(term))
                throw new APIErrorException(400, "You must supply a search term");

            // Term comes from from a catch-all parameter, so make sure it's been decoded.
            term = WebUtility.UrlDecode(term);

            // Setup our template name based on the collection name.  Template name is the directory the
            // file is stored in, an underscore, the template name prefix (search), an underscore,
            // the name of the collection (only "cgov" at this time), another underscore and then
            // the language code (either "en" or "es").
            string templateName = String.Format("autosg_suggest_{0}_{1}", collection, language);


            //TODO: Catch Exception
            var response = _elasticClient.SearchTemplate<Suggestion>(sd => sd
                .Index(_indexConfig.AliasName)
                .File(templateName)
                .Params(pd => pd
                    .Add("searchstring", term)
                    .Add("my_size", 10)
                )
            );

            if (response.IsValid) {
                return new Suggestions(
                    response.Total,
                    response.Documents
                );

            } else {
                throw new APIErrorException(500, "Error connecting to search servers");
            }
        }


        /// <summary>
        /// Provides an endpoint for checking that the various services which make up the API
        /// (and thus the API itself) are all in a state where they can return information.
        /// </summary>
        /// <returns>The contents of SearchController.HEALTHY_STATUS ('alive!') if
        /// all services are running. If unhealthy services are found, APIErrorException is thrown
        /// with HTTPStatusCode set to 500.</returns>
        [HttpGet("status")]
        public string GetStatus()
        {
            // Use the cluster health API to verify that the Best Bets index is functioning.
            // Maps to https://ncias-d1592-v.nci.nih.gov:9299/_cluster/health/bestbets?pretty (or other server)
            //
            // References:
            // https://www.elastic.co/guide/en/elasticsearch/reference/master/cluster-health.html
            // https://github.com/elastic/elasticsearch/blob/master/rest-api-spec/src/main/resources/rest-api-spec/api/cluster.health.json#L20
            IClusterHealthResponse response = _elasticClient.ClusterHealth(hd =>
            {
                hd = hd
                    .Index(_indexConfig.AliasName);

                return hd;
            });

            if (!response.IsValid)
            {
                _logger.LogError("Error checking ElasticSearch health.");
                _logger.LogError("Returned debug info: {0}.", response.DebugInformation);
                throw new APIErrorException(500, "Errors Occurred.");
            }

            if (response.Status != "green"
                && response.Status != "yellow")
            {
                _logger.LogError("Elasticsearch not healthy. Index status is '{0}'.", response.Status);
                throw new APIErrorException(500, "Service not healthy.");
            }

            return HEALTHY_STATUS;
        }
    }
}
