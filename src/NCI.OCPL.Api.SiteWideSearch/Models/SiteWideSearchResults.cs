using System.Collections.Generic;
using System.Linq;

namespace NCI.OCPL.Api.SiteWideSearch
{
    public class SiteWideSearchResults
    {

        public SiteWideSearchResult[] Results { get; private set; } = new SiteWideSearchResult[] { };
        public long TotalResults { get; private set; }

        public SiteWideSearchResults(long totalResults, IEnumerable<SiteWideSearchResult> results)
        {
            if (results != null)
            {
                Results = results.ToArray();
            }
            TotalResults = totalResults;
        }


    }
}