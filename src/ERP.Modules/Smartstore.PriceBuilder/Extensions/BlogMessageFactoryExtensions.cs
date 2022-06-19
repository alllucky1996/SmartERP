﻿using Smartstore.Core.Messaging;

namespace Smartstore.PriceBuilder.Messaging
{
    public static class BlogMessageFactoryExtensions
    {
        /// <summary>
        /// Sends a blog comment notification message to a store owner.
        /// </summary>
        public static Task<CreateMessageResult> SendBlogCommentNotificationMessage(this IMessageFactory factory, BlogComment blogComment, int languageId = 0)
        {
            Guard.NotNull(blogComment, nameof(blogComment));

            return factory.CreateMessageAsync(MessageContext.Create(MessageTemplateNames.BlogCommentStoreOwner, languageId, customer: blogComment.Customer), true, blogComment);
        }
    }
}
