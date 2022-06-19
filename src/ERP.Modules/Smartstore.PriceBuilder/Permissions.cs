using Smartstore.Core.Identity;
using Smartstore.Core.Security;

namespace Smartstore.PriceBuilder
{
    internal static class PriceBuilderPermissions
    {
        public const string Self = "product.price";
        public const string Read = Self + ".read";
        public const string Update = Self + ".update";
        public const string Create = Self + ".create";
        public const string Delete = Self + ".delete";
        public const string EditPriceType = Self + ".editpricetype";
        public const string Compute = Self + ".compute";
    }

    internal class BlogPermissionProvider : IPermissionProvider
    {
        public IEnumerable<PermissionRecord> GetPermissions()
        {
            // Get all permissions from above static class.
            var permissionSystemNames = PermissionHelper.GetPermissions(typeof(PriceBuilderPermissions));
            var permissions = permissionSystemNames.Select(x => new PermissionRecord { SystemName = x });

            return permissions;
        }

        public IEnumerable<DefaultPermissionRecord> GetDefaultPermissions()
        {
            // Allow root permission for admin by default.
            return new[]
            {
                new DefaultPermissionRecord
                {
                    CustomerRoleSystemName = SystemCustomerRoleNames.Administrators,
                    PermissionRecords = new[]
                    {
                        new PermissionRecord { SystemName = PriceBuilderPermissions.Self }
                    }
                }
            };
        }
    }
}
