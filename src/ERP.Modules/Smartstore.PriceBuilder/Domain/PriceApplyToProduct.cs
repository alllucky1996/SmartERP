using System;
using System.ComponentModel.DataAnnotations.Schema;
using Smartstore.Core.Catalog.Products;
using Smartstore.Domain;

namespace Smartstore.PriceBuilder.Domain
{
    /// <summary>
    /// Represents a blog post tag.
    /// </summary>
    public partial class PriceApplyToProduct : BaseEntity, IAuditable
    {
        public int ProductId { get; set; }
        public int PriceTypeId { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOnUtc { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
        [ForeignKey("PriceTypeId")]
        public virtual PriceType PriceType { get; set; }
    }
}
