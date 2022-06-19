using System;
using System.ComponentModel.DataAnnotations.Schema;
using Smartstore.Domain;
using Smartstore.PriceBuilder.Domain.Enums;

namespace Smartstore.PriceBuilder.Domain
{
    /// <summary>
    /// Represents a blog post tag.
    /// </summary>
    public partial class PriceCompute : BaseEntity, IAuditable, IDisplayOrder
    {
        public int PriceTypeId { get; set; }
        public int PriceAttribuiteId { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOnUtc { get; set; }
        public int DisplayOrder { get; set; }

        public EnumMathExpresstion EnumMathExpresstion { get; set; }

        [ForeignKey("PriceTypeId")]
        public virtual PriceType PriceType { get; set; }
        [ForeignKey("PriceAttribuiteId")]
        public virtual PriceAttribuite PriceAttribuite { get; set; }
    }
}
