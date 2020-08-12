using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NCI.OCPL.Api.SiteWideSearch
{
    /// <summary>
    /// Options for the SiteWideSearch indices
    /// </summary>
    public class SearchIndexOptions
    {
        /// <summary>
        /// Gets or sets the alias name for the Elasticsearch Collection we will use.
        /// </summary>
        /// <value>The name of the alias.</value>
        public string AliasName { get; set; }
    }
}
