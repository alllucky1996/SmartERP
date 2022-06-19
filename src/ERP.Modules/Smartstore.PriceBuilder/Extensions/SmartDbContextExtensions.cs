using Smartstore.Core.Data;

namespace Smartstore.PriceBuilder
{
    public static class SmartDbContextExtensions
    {
        public static DbSet<BlogPost> BlogPosts(this SmartDbContext db)
            => db.Set<BlogPost>();

        public static DbSet<BlogComment> BlogComments(this SmartDbContext db)
            => db.Set<BlogComment>();
    }
}
