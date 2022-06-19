﻿namespace Smartstore.Admin.Models.Maintenance
{
    public class MaintenanceModel : ModelBase
    {
        public DeleteGuestsModel DeleteGuests { get; set; } = new();
        public DeleteExportedFilesModel DeleteExportedFiles { get; set; } = new();
        public DeleteImageCacheModel DeleteImageCache { get; set; } = new();

        public bool CanExecuteSql { get; set; }
        public bool CanCreateBackup { get; set; }

        [LocalizedDisplay("Admin.System.Maintenance.SqlQuery")]
        public string SqlQuery { get; set; }

        [LocalizedDisplay("Admin.System.Maintenance.DeleteGuests.")]
        public class DeleteGuestsModel : ModelBase
        {
            [LocalizedDisplay("*StartDate")]
            public DateTime? StartDate { get; set; }

            [LocalizedDisplay("*EndDate")]
            public DateTime? EndDate { get; set; }

            [LocalizedDisplay("*OnlyWithoutShoppingCart")]
            public bool OnlyWithoutShoppingCart { get; set; }
        }

        [LocalizedDisplay("Admin.System.Maintenance.DeleteExportedFiles.")]
        public class DeleteExportedFilesModel : ModelBase
        {
            [LocalizedDisplay("*StartDate")]
            public DateTime? StartDate { get; set; }

            [LocalizedDisplay("*EndDate")]
            public DateTime? EndDate { get; set; }

            public int? NumDeletedFiles { get; set; }
            public int? NumDeletedDirectories { get; set; }
        }

        [LocalizedDisplay("Admin.System.Maintenance.DeleteImageCache.")]
        public class DeleteImageCacheModel : ModelBase
        {
            [LocalizedDisplay("*FileCount")]
            public long NumFiles { get; set; }

            [LocalizedDisplay("*TotalSize")]
            public string TotalSize { get; set; }
        }
    }
}
