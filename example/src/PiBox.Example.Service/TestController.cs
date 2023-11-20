using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Vogen;

namespace PiBox.Example.Service
{
    [ApiController, Route("test")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public Entry Get()
        {
            return new Entry() { Name = "test" };
        }

        /// <summary>
        /// Create some temp resource
        /// </summary>
        /// <param name="entry">the entry to manipulate the id</param>
        /// <param name="idToSet">the id to set</param>
        /// <returns>the same object, with the id 123</returns>
        [HttpPost]
        [SwaggerResponse(200, "the manipulated object", typeof(Entry))]
        public Entry Create([FromBody] Entry entry, [FromQuery] EntryId idToSet)
        {
            entry.Id = idToSet;
            return entry;
        }
    }

    /// <summary>
    /// The value object
    /// </summary>
    [ValueObject<int>]
    public partial class EntryId
    {
        private static Validation Validate(int input) => input < 1 ? Validation.Invalid("lower as one") : Validation.Ok;
    }

    public class Entry
    {
        /// <summary>
        /// The identifier
        /// </summary>
        [Required, NotNull]
        public EntryId Id { get; set; }

        /// <summary>
        /// The name
        /// </summary>
        [NotNull]
        public string Name { get; set; }

        public IList<EntryId> Ids { get; set; }

        public IList<Entry2> Dentries { get; set; }
    }

    public class Entry2
    {
        public IList<EntryId> OtherIds { get; set; }
    }
}
