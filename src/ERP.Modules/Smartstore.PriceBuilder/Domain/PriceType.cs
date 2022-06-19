using System;
using Smartstore.Domain;

namespace Smartstore.PriceBuilder.Domain
{
    /// <summary>
    /// Represents a blog post tag.
    /// </summary>
    public partial class PriceType : BaseEntity, IAuditable, ISoftDeletable, IDisplayOrder, IActivatable
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime? LastComputed { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOnUtc { get; set; }
        public bool Deleted { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; }
    }
}
