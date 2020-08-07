using System;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace NCI.OCPL.Api.SiteWideSearch
{
    /// <summary>
    /// Converts a JSON element containing either a single string, or an array of strings, into
    /// a single string.
    /// </summary>
    class MetadataDescriptionConverter : JsonConverter<string>
    {
        /// <summary>
        /// Responsible for reading a JSON element containing either a single string or an array of strings
        /// and converting it into a single string by discarding all but the first one.
        /// </summary>
        /// <param name="reader">The JsonReader to read from.</param>
        /// <param name="objectType">Type of the destination object (always System.String).</param>
        /// <param name="existingValue">The existing value of the destination object</param>
        /// <param name="hasExistingValue">Boolean. Does the destination object already have a value?</param>
        /// <param name="serializer">The calling serializer</param>
        /// <returns></returns>
        public override string ReadJson(JsonReader reader, Type objectType, string existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            // If it's just a string, return the value.
            if(reader.ValueType == typeof(string)) {
                return (string)reader.Value;
            }
            else
            {
                // Otherwise, save the first value and skip past the rest.
                string value = reader.ReadAsString();
                while(reader.ReadAsString() != null);
                return value;
            }
        }

        /// <summary>
        /// Mark the converter as not being used for writing JSON.
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The JsonWriter to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, string value, JsonSerializer serializer)
        {
            throw new NotImplementedException("This converter not intended for writing.");
        }
   }
}