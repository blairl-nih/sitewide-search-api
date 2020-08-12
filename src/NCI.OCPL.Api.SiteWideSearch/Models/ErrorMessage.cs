using System;
using Newtonsoft.Json;

namespace NCI.OCPL.Api.SiteWideSearch
{
    /// <summary>
    /// Represents a Error Message to be returned to the client
    /// </summary>
    public class ErrorMessage
    {
        /// <summary>
        /// The message to display
        /// </summary>
        /// <returns></returns>
        public string Message { get; set; }

        /// <summary>
        /// Returns the error message as a JSON string.
        /// </summary>
        /// <returns>JSON structure.</returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}