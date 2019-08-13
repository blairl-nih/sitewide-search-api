using System.Collections.Generic;
using System.Linq;

namespace NCI.OCPL.Api.SiteWideSearch
{
    /// Container for the the list of potential search terms returned
    /// by the SiteWideSearch.AutosuggestController.
    public class Suggestions
    {
        // The set of potential search items.
        public Suggestion[] Results { get; private set; } = new Suggestion[] { };

        // The total number of matching search terms available. 
        public long Total { get; private set; }

        public Suggestions(long totalResults, IEnumerable<Suggestion> results)
        {
            if (results != null)
            {
                Results = results.ToArray();
            }

            Total = totalResults;

        }


    }
}