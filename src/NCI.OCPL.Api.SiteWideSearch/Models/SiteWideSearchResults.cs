using System.Collections.Generic;
using System.Linq;

namespace NCI.OCPL.Api.SiteWideSearch
{
    /// <summary>
    /// Represents the results of a sitewide search operation.
    /// </summary>
    public class SiteWideSearchResults
    {

        /// <summary>
        /// Array of SiteWideSearchResult objects matching the search. May be empty.
        /// </summary>
        public SiteWideSearchResult[] Results { get; private set; } = new SiteWideSearchResult[] { };

        /// <summary>
        /// The total number of results matching the search criteria.
        /// </summary>
        public long TotalResults { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="totalResults">A long containing the number of results matching the search.</param>
        /// <param name="results">Array of search results.</param>
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