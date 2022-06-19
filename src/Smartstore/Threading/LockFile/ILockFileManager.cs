﻿namespace Smartstore.Threading
{
    /// <summary>
    /// Abstraction for application-wide lock files creation.
    /// </summary>
    /// <remarks>
    /// All virtual paths passed in or returned are relative to "/App_Data/Tenants/{Tenant}".
    /// </remarks>
    public interface ILockFileManager
    {
        /// <summary>
        /// Attempts to acquire an exclusive lock file.
        /// </summary>
        /// <param name="subpath">The filename of the lock file to create relative to current tenant root.</param>
        /// <param name="lockFile">A reference to the lock file object if the lock is granted.</param>
        /// <returns><c>true</c> if the lock is granted; otherwise, <c>false</c>.</returns>
        bool TryAcquireLock(string subpath, out ILockFile lockFile);

        /// <summary>
        /// Wether a lock file is already existing.
        /// </summary>
        /// <param name="subpath">The filename of the lock file to test.</param>
        /// <returns><c>true</c> if the lock file exists; otherwise, <c>false</c>.</returns>
        bool IsLocked(string subpath);

        /// <summary>
        /// Wether a lock file is already existing.
        /// </summary>
        /// <param name="subpath">The filename of the lock file to test.</param>
        /// <returns><c>true</c> if the lock file exists; otherwise, <c>false</c>.</returns>
        Task<bool> IsLockedAsync(string subpath);
    }
}
