using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NCI.OCPL.Api.SiteWideSearch
{
    /// <summary>
    /// Options for the SiteWideSearch indices
    /// </summary>
    public class AutosuggestIndexOptions
    {
        /// <summary>
        /// Gets or sets the name of the search index.
        /// </summary>
        /// <value>The name of the index to use for search.</value>
        public string AliasName { get; set; }
    }
}
