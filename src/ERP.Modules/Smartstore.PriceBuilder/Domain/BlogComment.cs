﻿using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Identity;

namespace Smartstore.PriceBuilder.Domain
{
    internal class BlogCommentMap : IEntityTypeConfiguration<BlogComment>
    {
        public void Configure(EntityTypeBuilder<BlogComment> builder)
        {
            builder.HasOne(c => c.BlogPost)
                .WithMany(c => c.BlogComments)          // INFO: Important! Must be set in this case else CustomerContent retrieval of type BlogComment will fail.
                .HasForeignKey(c => c.BlogPostId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    /// <summary>
    /// Represents a blog comment.
    /// </summary>
    [Table("BlogComment")] // Enables EF TPT inheritance
    public partial class BlogComment : CustomerContent
    {
        //public BlogComment()
        //{
        //}

        //[SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        //private BlogComment(ILazyLoader lazyLoader)
        //        : base(lazyLoader)
        //{
        //}

        /// <summary>
        /// Gets or sets the comment text.
        /// </summary>
        [MaxLength]
        public string CommentText { get; set; }

        /// <summary>
        /// Gets or sets the blog post identifier.
        /// </summary>
        public int BlogPostId { get; set; }

        [ForeignKey("BlogPostId")]
        public virtual BlogPost BlogPost { get; set; }

        //private BlogPost _blogPost;
        ///// <summary>
        ///// Gets or sets the blog post.
        ///// </summary>
        //public BlogPost BlogPost
        //{
        //    get => _blogPost ?? LazyLoader.Load(this, ref _blogPost);
        //    set => _blogPost = value;
        //}
    }
}
