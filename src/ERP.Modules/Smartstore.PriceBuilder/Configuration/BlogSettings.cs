﻿using Smartstore.Core.Configuration;

namespace Smartstore.PriceBuilder
{
    public class PriceBuilderSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether blog is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the page size for posts.
        /// </summary>
        public int PostsPageSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets a value indicating whether not registered user can leave comments.
        /// </summary>
        public bool AllowNotRegisteredUsersToLeaveComments { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to notify about new blog comments.
        /// </summary>
        public bool NotifyAboutNewBlogComments { get; set; }

        /// <summary>
        /// Gets or sets a number of blog tags that appear in the tag cloud.
        /// </summary>
        public int NumberOfTags { get; set; } = 15;

        /// <summary>
        /// The maximum age of blog items (in days) for RSS feed.
        /// </summary>
        public int MaxAgeInDays { get; set; } = 180;
    }
}
