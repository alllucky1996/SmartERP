using System;
using Smartstore.Domain;

namespace Smartstore.PriceBuilder.Domain
{
    /// <summary>
    /// Represents a blog post tag.
    /// </summary>
    public partial class PriceAttribuite : BaseEntity, IAuditable, IActivatable
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }
        public string Value { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOnUtc { get; set; }
        public bool IsActive { get; set; }
    }
}
