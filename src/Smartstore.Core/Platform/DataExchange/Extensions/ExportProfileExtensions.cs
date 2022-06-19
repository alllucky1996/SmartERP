﻿using System.Globalization;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Utilities;

namespace Smartstore.Core.DataExchange.Export
{
    public static partial class ExportProfileExtensions
    {
        /// <summary>
        /// Resolves the file name pattern for an export profile.
        /// </summary>
        /// <param name="profile">Export profile.</param>
        /// <param name="store">Store.</param>
        /// <param name="fileIndex">One based file index.</param>
        /// <param name="maxFileNameLength">The maximum length of the file name.</param>
        /// <param name="fileNamePattern">
        /// The file name pattern to be resolved. 
        /// <see cref="ExportProfile.FileNamePattern"/> of <paramref name="profile"/> is used if <c>null</c>.
        /// </param>
        /// <returns>Resolved file name pattern.</returns>
        public static string ResolveFileNamePattern(
            this ExportProfile profile,
            Store store,
            int fileIndex,
            int maxFileNameLength,
            string fileNamePattern = null)
        {
            fileNamePattern ??= profile.FileNamePattern;

            using var psb = StringBuilderPool.Instance.Get(out var sb);
            sb.Append(fileNamePattern);

            sb.Replace("%Profile.Id%", profile.Id.ToString());
            sb.Replace("%Profile.FolderName%", profile.FolderName);
            sb.Replace("%Store.Id%", store.Id.ToString());
            sb.Replace("%File.Index%", fileIndex.ToString("D4"));

            if (fileNamePattern.Contains("%Profile.SeoName%"))
            {
                sb.Replace("%Profile.SeoName%", SeoHelper.BuildSlug(profile.Name, true, false, false).Replace("-", ""));
            }
            if (fileNamePattern.Contains("%Store.SeoName%"))
            {
                sb.Replace("%Store.SeoName%", profile.PerStore ? SeoHelper.BuildSlug(store.Name, true, false, true) : "allstores");
            }
            if (fileNamePattern.Contains("%Random.Number%"))
            {
                sb.Replace("%Random.Number%", CommonHelper.GenerateRandomInteger().ToString());
            }
            if (fileNamePattern.Contains("%Timestamp%"))
            {
                sb.Replace("%Timestamp%", DateTime.UtcNow.ToString("s", CultureInfo.InvariantCulture));
            }

            var result = sb.ToString()
                .ToValidFileName(string.Empty)
                .Truncate(maxFileNameLength);

            return result;
        }
    }
}
