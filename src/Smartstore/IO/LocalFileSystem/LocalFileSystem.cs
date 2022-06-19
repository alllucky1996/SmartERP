﻿using System.Diagnostics;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;

namespace Smartstore.IO
{
    /// <summary>
    /// Looks up files using the local disk file system
    /// </summary>
    /// <remarks>
    /// When the environment variable "DOTNET_USE_POLLING_FILE_WATCHER" is set to "1" or "true", calls to
    /// <see cref="Watch(string)" /> will use <see cref="PollingFileChangeToken" />.
    /// </remarks>
    [DebuggerDisplay("LocalFileSystem - Root: {Root}, UseActivePolling: {UseActivePolling}, UsePollingFileWatcher: {UsePollingFileWatcher}")]
    public class LocalFileSystem : FileSystemBase, IFileProvider
    {
        private readonly PhysicalFileProvider _provider;
        private readonly ExclusionFilters _filters;

        /// <summary>
        /// Initializes a new instance of a LocalFileSystem with the given physical root path.
        /// </summary>
        public LocalFileSystem(string root)
        {
            Guard.NotEmpty(root, nameof(root));

            _filters = ExclusionFilters.Sensitive;
            _provider = new PhysicalFileProvider(root, _filters);
        }

        #region IFileProvider

        public override IFileInfo GetFileInfo(string subpath)
        {
            return _provider.GetFileInfo(subpath);
        }

        public override IDirectoryContents GetDirectoryContents(string subpath)
        {
            return _provider.GetDirectoryContents(subpath);
        }

        public override IChangeToken Watch(string filter)
        {
            return _provider.Watch(filter);
        }

        #endregion

        public override string Root
        {
            get => _provider.Root;
        }

        public bool UseActivePolling
        {
            get => _provider.UseActivePolling;
            set => _provider.UseActivePolling = value;
        }

        public bool UsePollingFileWatcher
        {
            get => _provider.UsePollingFileWatcher;
            set => _provider.UsePollingFileWatcher = value;
        }

        public override string MapPath(string subpath)
        {
            var fullPath = MapPathInternal(ref subpath, false);
            return fullPath == null
                ? null
                : Path.GetFullPath(fullPath);
        }

        public override bool FileExists(string subpath)
        {
            var fullPath = MapPathInternal(ref subpath, false);
            if (string.IsNullOrEmpty(fullPath))
            {
                return false;
            }

            return File.Exists(fullPath);
        }

        public override bool DirectoryExists(string subpath)
        {
            var fullPath = MapPathInternal(ref subpath, false);
            if (string.IsNullOrEmpty(fullPath))
            {
                return false;
            }

            return Directory.Exists(fullPath);
        }

        public override IFile GetFile(string subpath)
        {
            var fullPath = MapPathInternal(ref subpath, false);
            return fullPath.HasValue()
                ? new LocalFile(subpath, new FileInfo(fullPath), this)
                : new NotFoundFile(subpath, this);
        }

        public override IDirectory GetDirectory(string subpath)
        {
            var fullPath = MapPathInternal(ref subpath, false);
            return fullPath.HasValue()
                ? new LocalDirectory(subpath, new DirectoryInfo(fullPath), this)
                : new NotFoundDirectory(subpath, this);
        }

        #region IDisposable

        protected override void OnDispose(bool disposing)
        {
            _provider.Dispose();
        }

        #endregion      

        #region Utils

        internal string MapPathInternal(ref string subpath, bool throwOnFailure)
        {
            if (string.IsNullOrEmpty(subpath))
                return Root;

            subpath = PathUtility.NormalizeRelativePath(subpath);

            var mappedPath = Path.Combine(Root, subpath);

            // Verify that the resulting path is inside the root file system path.
            if (!IsUnderneathRoot(mappedPath))
            {
                if (throwOnFailure)
                {
                    throw new FileSystemException($"The path '{subpath}' resolves to a physical path outside the file system store root.");
                }
                else
                {
                    return null;
                }
            }

            return Path.GetFullPath(mappedPath);
        }

        private static bool IsExcluded(FileSystemInfo fileSystemInfo, ExclusionFilters filters) 
            => filters != ExclusionFilters.None && (fileSystemInfo.Name.StartsWith(".", StringComparison.Ordinal) && (filters & ExclusionFilters.DotPrefixed) != ExclusionFilters.None || fileSystemInfo.Exists && ((fileSystemInfo.Attributes & FileAttributes.Hidden) != (FileAttributes)0 && (filters & ExclusionFilters.Hidden) != ExclusionFilters.None || (fileSystemInfo.Attributes & FileAttributes.System) != (FileAttributes)0 && (filters & ExclusionFilters.System) != ExclusionFilters.None));

        private bool IsUnderneathRoot(string fullPath)
        {
            return fullPath.StartsWith(Root, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}