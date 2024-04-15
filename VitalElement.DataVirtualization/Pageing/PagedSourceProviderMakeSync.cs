namespace VitalElement.DataVirtualization.Pageing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Interfaces;

    internal class PagedSourceProviderMakeSync<T> : IPagedSourceProviderAsync<T>, IProviderPreReset
    {
        public PagedSourceProviderMakeSync()
        {
        }

        public PagedSourceProviderMakeSync(
            Func<int, int, Task<IEnumerable<T>>> funcGetItemsAtAsync = null,
            Func<Task<int>> funcGetCountAsync = null,
            Func<T, int> funcIndexOf = null,
            Func<T, Task<int>> funcIndexOfAsync = null,
            Func<T, bool> funcContains = null,
            Func<T, Task<bool>> funcContainsAsync = null,
            Action<int> actionOnReset = null,
            Func<int, int, int, T> funcGetPlaceHolder = null,
            Action actionOnBeforeReset = null
        )
        {
            FuncGetItemsAtAsync = funcGetItemsAtAsync;
            FuncGetCountAsync = funcGetCountAsync;
            FuncIndexOf = funcIndexOf;
            FuncIndexOfAsync = funcIndexOfAsync;
            FuncContains = funcContains;
            FuncContainsAsync = funcContainsAsync;
            ActionOnReset = actionOnReset;
            FuncGetPlaceHolder = funcGetPlaceHolder;
            ActionOnBeforeReset = actionOnBeforeReset;
        }

        public Action ActionOnBeforeReset { get; set; }

        public Action<int> ActionOnReset { get; set; }
        public Func<T, bool> FuncContains { get; set; }
        public Func<T, Task<bool>> FuncContainsAsync { get; set; }

        public Func<Task<int>> FuncGetCountAsync { get; set; }

        public Func<int, int, Task<IEnumerable<T>>> FuncGetItemsAtAsync { get; set; }

        public Func<int, int, int, T> FuncGetPlaceHolder { get; set; }

        public Func<T, int> FuncIndexOf { get; set; }

        public Func<T, Task<int>> FuncIndexOfAsync { get; set; }

        public virtual void OnReset(int count)
        {
            ActionOnReset?.Invoke(count);
        }

        public bool Contains(T item)
        {
            var ret = false;

            if (FuncContains != null)
            {
                ret = FuncContains.Invoke(item);
            }
            else if (FuncContainsAsync != null)
            {
                ret = Task.Run(() => FuncContainsAsync.Invoke(item)).GetAwaiter().GetResult();
            }
            else
            {
                ret = Task.Run(() => FuncContainsAsync(item)).GetAwaiter().GetResult();
            }

            return ret;
        }

        public int Count => Task.Run(GetCountAsync).GetAwaiter().GetResult();

        public IEnumerable<T> GetItemsAt(int pageoffset, int count)
        {
            return Task.Run(() => GetItemsAtAsync(pageoffset, count)).GetAwaiter().GetResult();
        }

        public virtual int IndexOf(T item)
        {
            if (FuncIndexOf != null)
            {
                return FuncIndexOf.Invoke(item);
            }

            if (FuncIndexOfAsync != null)
            {
                return Task.Run(() => FuncIndexOfAsync.Invoke(item)).GetAwaiter().GetResult();
            }

            return Task.Run(() => IndexOfAsync(item)).GetAwaiter().GetResult();
        }

        public void Replace(T old, T newItem)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ContainsAsync(T item)
        {
            return FuncContainsAsync?.Invoke(item);
        }

        public virtual Task<int> GetCountAsync()
        {
            return FuncGetCountAsync?.Invoke();
        }

        public virtual Task<IEnumerable<T>> GetItemsAtAsync(int pageoffset, int count)
        {
            return FuncGetItemsAtAsync?.Invoke(pageoffset, count);
        }

        public virtual T GetPlaceHolder(int index, int page, int offset)
        {
            return FuncGetPlaceHolder != null ? FuncGetPlaceHolder.Invoke(index, page, offset) : default(T);
        }

        public virtual async Task<int> IndexOfAsync(T item)
        {
            return await Task.FromResult(-1);
        }

        public virtual void OnBeforeReset()
        {
            ActionOnBeforeReset?.Invoke();
        }

        /// <summary>
        ///     Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection" /> is synchronized
        ///     (thread safe).
        /// </summary>
        /// <returns>
        ///     true if access to the <see cref="T:System.Collections.ICollection" /> is synchronized (thread safe); otherwise,
        ///     false.
        /// </returns>
        public bool IsSynchronized { get; } = false;

        /// <summary>
        ///     Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.
        /// </summary>
        /// <returns>
        ///     An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.
        /// </returns>
        public object SyncRoot => this;

        /// <summary>
        ///     Copies the elements of the <see cref="T:System.Collections.ICollection" /> to an <see cref="T:System.Array" />,
        ///     starting at a particular <see cref="T:System.Array" /> index.
        /// </summary>
        /// <param name="array">
        ///     The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied
        ///     from <see cref="T:System.Collections.ICollection" />. The <see cref="T:System.Array" /> must have zero-based
        ///     indexing.
        /// </param>
        /// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins. </param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="array" /> is null. </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index" /> is less than zero. </exception>
        /// <exception cref="T:System.ArgumentException">
        ///     <paramref name="array" /> is multidimensional.-or- The number of elements
        ///     in the source <see cref="T:System.Collections.ICollection" /> is greater than the available space from
        ///     <paramref name="index" /> to the end of the destination <paramref name="array" />.-or-The type of the source
        ///     <see cref="T:System.Collections.ICollection" /> cannot be cast automatically to the type of the destination
        ///     <paramref name="array" />.
        /// </exception>
        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }
    }
}