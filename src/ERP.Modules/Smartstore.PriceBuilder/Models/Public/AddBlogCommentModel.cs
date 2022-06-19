﻿namespace Smartstore.PriceBuilder.Models.Public
{
    public partial class AddBlogCommentModel : EntityModelBase
    {
        [LocalizedDisplay("Blog.Comments.CommentText")]
        [SanitizeHtml]
        public string CommentText { get; set; }

        public bool DisplayCaptcha { get; set; }
    }
}
