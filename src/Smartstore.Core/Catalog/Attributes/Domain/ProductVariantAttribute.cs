﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Catalog.Attributes
{
    internal class ProductVariantAttributeMap : IEntityTypeConfiguration<ProductVariantAttribute>
    {
        public void Configure(EntityTypeBuilder<ProductVariantAttribute> builder)
        {
            builder.HasOne(c => c.Product)
                .WithMany(c => c.ProductVariantAttributes)
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.ProductAttribute)
                .WithMany()
                .HasForeignKey(c => c.ProductAttributeId);
        }
    }

    /// <summary>
    /// Represents a product attribute mapping.
    /// </summary>
    [Table("Product_ProductAttribute_Mapping")]
    [Index(nameof(AttributeControlTypeId), Name = "IX_AttributeControlTypeId")]
    [Index(nameof(ProductId), nameof(DisplayOrder), Name = "IX_Product_ProductAttribute_Mapping_ProductId_DisplayOrder")]
    public partial class ProductVariantAttribute : BaseEntity, ILocalizedEntity, IDisplayOrder
    {
        public ProductVariantAttribute()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ProductVariantAttribute(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the product identifier.
        /// </summary>
        public int ProductId { get; set; }

        private Product _product;
        /// <summary>
        /// Gets or sets the product.
        /// </summary>
        [JsonIgnore]
        public Product Product
        {
            get => _product ?? LazyLoader.Load(this, ref _product);
            set => _product = value;
        }

        /// <summary>
        /// Gets or sets the product attribute identifier.
        /// </summary>
        public int ProductAttributeId { get; set; }

        private ProductAttribute _productAttribute;
        /// <summary>
        /// Gets or sets the product attribute.
        /// </summary>
        public ProductAttribute ProductAttribute
        {
            get => _productAttribute ?? LazyLoader.Load(this, ref _productAttribute);
            set => _productAttribute = value;
        }

        /// <summary>
        /// Gets or sets a value a text prompt.
        /// </summary>
        [StringLength(4000)]
        public string TextPrompt { get; set; }

        /// <summary>
        /// Gets or sets any custom data.
        /// It's not used by Smartstore but is being passed to the choice partial view.
        /// </summary>
        [MaxLength]
        public string CustomData { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is required.
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Gets or sets the attribute control type identifier.
        /// </summary>
        public int AttributeControlTypeId { get; set; }

        /// <summary>
        /// Gets or sets the attribute control type.
        /// </summary>
		[NotMapped]
        public AttributeControlType AttributeControlType
        {
            get => (AttributeControlType)AttributeControlTypeId;
            set => AttributeControlTypeId = (int)value;
        }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the selection of multiple values is supported.
        /// </summary>
        [NotMapped]
        public bool IsMultipleChoice => AttributeControlType == AttributeControlType.Checkboxes;

        /// <summary>
        /// Gets a value indicating whether the attribute has a list of values.
        /// </summary>
        public bool IsListTypeAttribute()
        {
            return AttributeControlType switch
            {
                AttributeControlType.TextBox or
                AttributeControlType.MultilineTextbox or
                AttributeControlType.Datepicker or
                AttributeControlType.FileUpload => false,
                _ => true,  // All other attribute control types support values.
            };
        }

        private ICollection<ProductVariantAttributeValue> _productVariantAttributeValues;
        /// <summary>
        /// Gets or sets the product variant attribute values.
        /// </summary>
        public ICollection<ProductVariantAttributeValue> ProductVariantAttributeValues
        {
            get => _productVariantAttributeValues ?? LazyLoader.Load(this, ref _productVariantAttributeValues) ?? (_productVariantAttributeValues ??= new HashSet<ProductVariantAttributeValue>());
            protected set => _productVariantAttributeValues = value;
        }
    }
}
