﻿namespace Smartstore.PriceBuilder.Models.Public
{
    public partial class BlogPostTagListModel : ModelBase
    {
        public List<BlogPostTagModel> Tags { get; set; } = new();
    }
}
