using System.Collections.Generic;
using System.Linq;

namespace NCI.OCPL.Api.SiteWideSearch
{
    /// <summary>
    /// Container for the the list of potential search terms returned
    /// by the SiteWideSearch.AutosuggestController.
    /// </summary>
    public class Suggestions
    {
        /// <summary>
        /// The set of potential search items.
        /// </summary>
        /// <value>Possibly empty list of suggestions.</value>
        public Suggestion[] Results { get; private set; } = new Suggestion[] { };

        /// <summary>
        /// The total number of matching search terms available.
        /// </summary>
        public long Total { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="totalResults">The total number of suggestions available.</param>
        /// <param name="results">Array of Suggestions</param>
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