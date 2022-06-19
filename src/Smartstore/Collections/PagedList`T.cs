﻿using System.Collections.ObjectModel;

namespace Smartstore.Collections
{
    public class PagedList<T> : Pageable<T>, IPagedList<T>
    {
        /// <param name="pageIndex">The 0-based page index</param>
        public PagedList(IEnumerable<T> source, int pageIndex, int pageSize)
            : base(source, pageIndex, pageSize)
        {
        }

        /// <param name="pageIndex">The 0-based page index</param>
        public PagedList(IEnumerable<T> source, int pageIndex, int pageSize, int totalCount)
            : base(source, pageIndex, pageSize, totalCount)
        {
        }

        public IPagedList<T> ModifyQuery(Func<IQueryable<T>, IQueryable<T>> modifier)
        {
            var result = modifier?.Invoke(SourceQuery);
            SourceQuery = result ?? throw new InvalidOperationException("The '{0}' delegate must not return NULL.".FormatInvariant(nameof(modifier)));

            return this;
        }

        public IPagedList<T> Load(bool force = false)
        {
            // Returns instance for chaining.
            if (force && List != null)
            {
                List.Clear();
                List = null;
            }

            EnsureIsLoaded();

            return this;
        }

        public async Task<IPagedList<T>> LoadAsync(bool force = false, CancellationToken cancelToken = default)
        {
            // Returns instance for chaining.
            if (force && List != null)
            {
                List.Clear();
                List = null;
            }

            await EnsureIsLoadedAsync(cancelToken);

            return this;
        }

        #region IList<T> Members

        public void Add(T item)
        {
            EnsureIsLoaded();
            List.Add(item);
        }

        public void Clear()
        {
            if (List != null)
            {
                List.Clear();
                List = null;
            }
        }

        public bool Contains(T item)
        {
            EnsureIsLoaded();
            return List.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            EnsureIsLoaded();
            List.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            if (List != null)
            {
                return List.Remove(item);
            }

            return false;
        }

        public int Count
        {
            get
            {
                EnsureIsLoaded();
                return List.Count;
            }
        }

        public bool IsReadOnly
        {
            get => false;
        }

        public int IndexOf(T item)
        {
            EnsureIsLoaded();
            return List.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            EnsureIsLoaded();
            List.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            if (List != null)
            {
                List.RemoveAt(index);
            }
        }

        public T this[int index]
        {
            get
            {
                EnsureIsLoaded();
                return List[index];
            }
            set
            {
                EnsureIsLoaded();
                List[index] = value;
            }
        }

        #endregion

        #region Utils

        public void AddRange(IEnumerable<T> collection)
        {
            EnsureIsLoaded();
            List.AddRange(collection);
        }

        public ReadOnlyCollection<T> AsReadOnly()
        {
            EnsureIsLoaded();
            return List.AsReadOnly();
        }

        #endregion
    }
}
