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
    /// This controller handles requests for performaning site wide searches.
    /// </summary>
    [Route("[controller]")]
    public class SearchController : Controller
    {
        // Static to limit to a single instance (can't do const for non-scalar types)
        static readonly string[] validLanguages = { "en", "es" };

        private readonly IElasticClient _elasticClient;
        private readonly SearchIndexOptions _indexConfig;
        private readonly ILogger<SearchController> _logger;

        /// <summary>
        /// Message to return for a "healthy" status.
        /// </summary>
        public const string HEALTHY_STATUS = "alive!";

        /// <summary>
        /// Creates a new instance of a Search Controller
        /// </summary>
        /// <param name="elasticClient">An instance of an IElasticClient to use for connecting to the ElasticSearch cluster.</param>
        /// <param name="config">The search configuration.</param>
        /// <param name="logger">An instance of a ILogger to use for logging messages.</param>
        public SearchController(IElasticClient elasticClient,
            IOptions<SearchIndexOptions> config,
            ILogger<SearchController> logger)
        {
            _elasticClient = elasticClient;
            _indexConfig = config.Value;
            _logger = logger;
        }

        // GET search/cgov/en/lung+cancer
        /// <summary>
        /// Gets the results of a search
        /// </summary>
        /// <param name="collection">The search collection/strategy to use.  This defines the ES template to use.</param>
        /// <param name="language">The language to use. Only "en" and "es" are currently supported.</param>
        /// <param name="term">The search term to search for</param>
        /// <param name="from">The offset of results to retrieve</param>
        /// <param name="size">The number of items to retrieve</param>
        /// <param name="site">An optional parameter used to limit the number of items returned based on site.</param>
        /// <returns>A SiteWideSearchResults collection object</returns>
        [HttpGet("{collection}/{language}/{*term}")]
        public SiteWideSearchResults Get(
            string collection,
            string language,
            string term = null,
            [FromQuery] int from = 0,
            [FromQuery] int size = 10,
            [FromQuery] string site = "all"
            )
        {

            if (string.IsNullOrWhiteSpace(collection))
                throw new APIErrorException(400, "You must supply a collection name");

            if (!validLanguages.Contains(language))
                throw new APIErrorException(400, "Not a valid language code.");

            // The catch-all parameter can match the empty string, effectively giving us
            // the {collection}/{language} route as well. We want to handle searches
            // for "nothing" by returning nothing, no need to invoke Elasticsearch.
            if(string.IsNullOrWhiteSpace(term))
            {
                return new SiteWideSearchResults(
                    0,
                    new SiteWideSearchResult[0]
                );
            }

            //TODO: Access Logging with params
            //_logger.LogInformation("Search Request -- Term: {0}, Page{1} ", term, pagenum);

            // Term comes from from a catch-all parameter, so make sure it's been decoded.
            term = WebUtility.UrlDecode(term);

            // Setup our template name based on the collection name.  Template name is the directory the
            // file is stored in, an underscore, the template name prefix (search), an underscore,
            // the name of the collection (only "cgov" or "doc" at this time), another underscore and then
            // the language code (either "en" or "es").
            string templateName = String.Format("cgov_search_{0}_{1}", collection, language);

            //TODO: Make this a parameter that can take in a list of fields and turn them
            //into this string.
            // Setup the list of fields we want ES to return.
            string fields = "\"url\", \"title\", \"metatag.description\", \"metatag.dcterms.type\"";

            //thios Can throw exception
            var response = _elasticClient.SearchTemplate<SiteWideSearchResult>(sd => sd
                .Index(_indexConfig.AliasName)
                .File(templateName)
                .Params(pd => pd
                    .Add("my_value", term)
                    .Add("my_size", size)
                    .Add("my_from", from)
                    .Add("my_fields", fields)
                    .Add("my_site", site)
                )
            );

            if (response.IsValid) {
                return new SiteWideSearchResults(
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
                    .Index("autosg");

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
