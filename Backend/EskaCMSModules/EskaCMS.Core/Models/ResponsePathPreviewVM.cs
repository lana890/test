using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace EskaCMS.Core.Models
{
    /// <summary>
    /// Asks what a path reads out of a parent API response, so an APIMapperParamChild.ResponsePath can be
    /// picked and checked before the child link is saved.
    /// Either ApiMapperId (the parent API is invoked) or RawResponse (a response body at hand) is given.
    /// </summary>
    public class ResponsePathPreviewRequest
    {
        /// <summary>
        /// The parent API to read the value from. It is invoked through the normal pipeline, so its own
        /// response shaping (output params, ResponsePath) applies - exactly what a child link sees at runtime.
        /// </summary>
        public long ApiMapperId { get; set; }

        /// <summary>
        /// A response body to use instead of invoking the API, to try a path against a sample.
        /// </summary>
        public string RawResponse { get; set; }

        /// <summary>
        /// The path to read: data.customer.id, items[0].id, $.data.id. Optional - when it is empty only the
        /// response and the paths it offers are returned.
        /// </summary>
        public string ResponsePath { get; set; }

        /// <summary>
        /// Context for the parent's own input params, the same object the invoker takes.
        /// </summary>
        public JObject obj { get; set; }
    }

    /// <summary>
    /// What a path reads out of a parent API response, plus every path that response offers.
    /// </summary>
    public class ResponsePathPreviewVM
    {
        public long ApiMapperId { get; set; }

        public string ResponsePath { get; set; }

        /// <summary>
        /// True when the path read a value out of the response. False means the child link would fall back to
        /// the parent output param name (and then to its alias) at runtime.
        /// </summary>
        public bool Matched { get; set; }

        public string Value { get; set; }

        /// <summary>
        /// Object, Array, String, Integer, Null ... the JSON type of the value the path read.
        /// </summary>
        public string ValueType { get; set; }

        /// <summary>
        /// The parent response the path was read from, after the parent's own shaping.
        /// </summary>
        public JToken Response { get; set; }

        /// <summary>
        /// Every path in that response, in document order, with the value each one reads.
        /// </summary>
        public List<ResponsePathVM> Paths { get; set; }

        /// <summary>
        /// True when the response offers more paths than the returned list holds.
        /// </summary>
        public bool PathsTruncated { get; set; }
    }

    public class ResponsePathVM
    {
        public string Path { get; set; }

        /// <summary>
        /// Object, Array, String, Integer, Null ... the JSON type at that path.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The value at that path, shortened when it is long. A path of type Object or Array has no value here,
        /// it is the one to use for a param that takes a whole object or a body array.
        /// </summary>
        public string Value { get; set; }
    }
}
