using System.Collections.Generic;

using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

using Nest;
using Xunit;

using NCI.OCPL.Utils.Testing;

using NCI.OCPL.Api.SiteWideSearch.Controllers;

namespace NCI.OCPL.Api.SiteWideSearch.Tests.AutoSuggestControllerTests
{
    /// <summary>
    /// Defines Tests for the AutosuggestController class
    /// <remarks>
    /// The AutosuggestController class requires an IElasticClient, which is how
    /// the controller queries an ElasticSearch server.  As these are unit tests, we
    /// will not be connecting to a ES server.  So we are using the Moq framework for
    /// mocking up the methods in an IElasticClient.
    ///
    ///
    /// The primary method we use is the SearchTemplate method.  This calls an ElasticSearch
    /// template (which is like a stored procedure).  Most of the tests will be for validating
    /// the parameters passed into the SearchTemplate method.  In order for the Nest library to
    /// provide a fluent interface in defining queries and parameters for templates, most methods
    /// will take in an anonymous function for defining the parameters.  These functions usually
    /// return an object that defines the request the client should send to the server.
    ///
    /// I note all of this since the class names are quite long and the code may start to get
    /// funky looking.
    /// </remarks>
    /// </summary>


    /// <summary>
    /// Defines a class with all of the data mapping tests to ensure we are able to correctly
    /// map the responses from ES into the correct response from the AutosuggestController
    /// </summary>
    public class Get_DataMapTests : AutosuggestTests_Base
    {

        /// <summary>
        /// Helper method to build a SearchTemplateRequest for testing purposes.
        /// </summary>
        /// <param name="index">The index to fetch from</param>
        /// <param name="fileName">The template fileName to use</param>
        /// <param name="term">The search term we are looking for</param>
        /// <param name="size">The result set size</param>
        /// <param name="fields">The fields we are requesting</param>
        /// <param name="site">The sites to filter the results by</param>
        /// <returns>A SearchTemplateRequest</returns>
        private SearchTemplateRequest<T> GetSearchRequest<T>(
            string index,
            string fileName,
            string term,
            int size,
            string fields,
            string site
            ) where T : class {

            // ISearchTemplateRequest.File is obsolete.
            // Refactoring to remove this dependency is recorded as issue #28
            // https://github.com/NCIOCPL/sitewide-search-api/issues/28
#pragma warning disable CS0618
            SearchTemplateRequest<T> expReq = new SearchTemplateRequest<T>(index){
                File = fileName
            };
#pragma warning restore CS0618

            expReq.Params = new Dictionary<string, object>();
            expReq.Params.Add("searchstring", term);
            expReq.Params.Add("my_size", size);

            return expReq;
        }

        [Fact]
        /// <summary>
        /// Test that the list of results exists.
        /// </summary>
        public void Check_Results_Exist()
        {
            string testFile = "AutoSuggest.CGov.En.BreastCancer.json";

            IOptions<AutosuggestIndexOptions> config = GetMockedAutosuggestIndexOptions();
            AutosuggestController ctrl = new AutosuggestController(
                ElasticTools.GetInMemoryElasticClient(testFile),
                config,
                NullLogger<AutosuggestController>.Instance
            );

            //Parameters don't matter in this case...
            Suggestions results = ctrl.Get(
                "cgov",
                "en",
                "breast cancer"
            );

            Assert.NotEmpty(results.Results);
        }

        [Fact]
        /// <summary>
        /// Test that the search results at arbitrary offsets
        /// in the collection are present
        /// </summary>
        public void Check_Results_Present()
        {
            string testFile = "AutoSuggest.CGov.En.BreastCancer.json";

            IOptions<AutosuggestIndexOptions> config = GetMockedAutosuggestIndexOptions();
            AutosuggestController ctrl = new AutosuggestController(
                ElasticTools.GetInMemoryElasticClient(testFile),
                config,
                NullLogger<AutosuggestController>.Instance
            );

            //Parameters don't matter in this case...
            Suggestions results = ctrl.Get(
                "cgov",
                "en",
                "breast cancer"
            );

            Assert.All(results.Results, item => Assert.NotNull(item));
        }

        [Fact]
        /// <summary>
        /// Test that the list of returned results has the right number of items.
        /// </summary>
        public void Check_Result_Count()
        {
            string testFile = "AutoSuggest.CGov.En.BreastCancer.json";

            IOptions<AutosuggestIndexOptions> config = GetMockedAutosuggestIndexOptions();
            AutosuggestController ctrl = new AutosuggestController(
                ElasticTools.GetInMemoryElasticClient(testFile),
                config,
                NullLogger<AutosuggestController>.Instance
            );

            //Parameters don't matter in this case...
            Suggestions results = ctrl.Get(
                "cgov",
                "en",
                "breast cancer"
            );

            Assert.Equal(20, results.Results.Length);
        }


        [Fact]
        /// <summary>
        /// Test that the first result contains the expected string.
        /// </summary>
        public void Check_First_Result()
        {
            string testFile = "AutoSuggest.CGov.En.BreastCancer.json";

            IOptions<AutosuggestIndexOptions> config = GetMockedAutosuggestIndexOptions();
            AutosuggestController ctrl = new AutosuggestController(
                ElasticTools.GetInMemoryElasticClient(testFile),
                config,
                NullLogger<AutosuggestController>.Instance
            );

            //Parameters don't matter in this case...
            Suggestions results = ctrl.Get(
                "cgov",
                "en",
                "breast cancer"
            );

            Assert.Equal("breast cancer", results.Results[0].Term);
        }

        [Theory]
        [InlineData(0, "breast cancer")]
        [InlineData(3, "metastatic breast cancer")]
        [InlineData(17, "breast cancer risk assessment")]
        [InlineData(19, "breast cancer symptoms")]
        /// <summary>
        /// Test that the suggested search strings from arbitrary offsets
        /// in the collection have the correct values
        /// </summary>
        /// <param name="offset">Offset into the list of results of the item to check.</param>
        /// <param name="expectedTerm">The expected term text</param>
        public void Check_Arbitrary_Result(int offset, string expectedTerm)
        {
            string testFile = "AutoSuggest.CGov.En.BreastCancer.json";

            IOptions<AutosuggestIndexOptions> config = GetMockedAutosuggestIndexOptions();
            AutosuggestController ctrl = new AutosuggestController(
                ElasticTools.GetInMemoryElasticClient(testFile),
                config,
                NullLogger<AutosuggestController>.Instance
            );

            //Parameters don't matter in this case...
            Suggestions results = ctrl.Get(
                "cgov",
                "en",
                "breast cancer"
            );

            Assert.Equal(expectedTerm, results.Results[offset].Term);
        }

        [Fact]
        /// <summary>
        /// Test for Breast Cancer search string and ensures Total is mapped correctly.
        /// </summary>
        public void Has_Correct_Total()
        {
            string testFile = "AutoSuggest.CGov.En.BreastCancer.json";

            IOptions<AutosuggestIndexOptions> config = GetMockedAutosuggestIndexOptions();
            AutosuggestController ctrl = new AutosuggestController(
                ElasticTools.GetInMemoryElasticClient(testFile),
                config,
                NullLogger<AutosuggestController>.Instance
            );

            //Parameters don't matter in this case...
            Suggestions results = ctrl.Get(
                "cgov",
                "en",
                "breast cancer"
            );

            Assert.Equal(222, results.Total);
        }

        // TODO: Add tests for varying the various parameters.
        // TODO: Move Check_For_Correct_Request_Data() and variants
        //       to a separate class

        [Fact]
        /// <summary>
        /// Verify that the request sent to ES for a single term is being set up correctly.
        /// </summary>
        public void Check_For_Correct_Request_Data()
        {
            string term = "Breast Cancer";

            ISearchTemplateRequest actualReq = null;

            //Setup the client with the request handler callback to be executed later.
            IElasticClient client =
                ElasticTools.GetMockedSearchTemplateClient<Suggestion>(
                    req => actualReq = req,
                    resMock => {
                        //Make sure we say that the response is valid.
                        resMock.Setup(res => res.IsValid).Returns(true);
                    } // We don't care what the response looks like.
                );

            IOptions<AutosuggestIndexOptions> config = GetMockedAutosuggestIndexOptions();
            AutosuggestController controller = new AutosuggestController(
                client,
                config,
                NullLogger<AutosuggestController>.Instance
            );

            //NOTE: this is when actualReq will get set.
            controller.Get(
                "cgov",
                "en",
                term
            );

            SearchTemplateRequest<Suggestion> expReq = GetSearchRequest<Suggestion>(
                "cgov",                 // Search index to look in.
                "autosg_suggest_cgov_en",  // Template name, preceded by the name of the directory it's stored in.
                term,                   // Search term
                10,                     // Max number of records to retrieve.
                "\"url\", \"title\", \"metatag.description\", \"metatag.dcterms.type\"",
                "all"
            );

            Assert.Equal(
                expReq,
                actualReq,
                new ElasticTools.SearchTemplateRequestComparer()
            );
        }

    }


}
