using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Xunit;

using NCI.OCPL.Utils.Testing;

using NCI.OCPL.Api.SiteWideSearch.Controllers;

namespace NCI.OCPL.Api.SiteWideSearch.Tests.AutoSuggestControllerTests
{
    /// <summary>
    /// Tests for the Autosuggest Controller's healthcheck endpoint
    /// </summary>
    public class AutosuggestControllerTests_HealthCheck : AutosuggestTests_Base
    {
        [Theory]
        [InlineData("ESHealthData/green.json")]
        [InlineData("ESHealthData/yellow.json")]
        public void GetStatus_Healthy(string datafile)
        {
            IOptions<AutosuggestIndexOptions> config = GetMockedAutosuggestIndexOptions();
            AutosuggestController ctrl = new AutosuggestController(
                ElasticTools.GetInMemoryElasticClient(datafile),
                config,
                NullLogger<AutosuggestController>.Instance
            );

            string status = ctrl.GetStatus();
            Assert.Equal(AutosuggestController.HEALTHY_STATUS, status, ignoreCase: true);
        }

        [Theory]
        [InlineData("ESHealthData/red.json")]
        //[InlineData("ESHealthData/unexpected.json")]   // i.e. "Unexpected color" - it appears as if 5.6.x does not have unexpected
        public void GetStatus_Unhealthy(string datafile)
        {
            IOptions<AutosuggestIndexOptions> config = GetMockedAutosuggestIndexOptions();
            AutosuggestController ctrl = new AutosuggestController(
                ElasticTools.GetInMemoryElasticClient(datafile),
                config,
                NullLogger<AutosuggestController>.Instance
            );

            APIErrorException ex = Assert.Throws<APIErrorException>(() => ctrl.GetStatus());

            Assert.Equal(500, ex.HttpStatusCode);
        }
    }
}
