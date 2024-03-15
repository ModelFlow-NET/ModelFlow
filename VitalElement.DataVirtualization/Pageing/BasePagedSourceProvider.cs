namespace VitalElement.DataVirtualization.Pageing
{
    using System;
    using System.Collections.Generic;
    using Interfaces;

    public class BasePagedSourceProvider<T> : IPagedSourceProvider<T>
    {
        public BasePagedSourceProvider()
        {
        }

        public BasePagedSourceProvider(
            Func<int, int, IEnumerable<T>> funcGetItemsAt = null,
            Func<int> funcGetCount = null,
            Func<T, int> funcIndexOf = null,
            Func<T, bool> funcContains = null,
            Action<int> actionOnReset = null
        )
        {
            FuncGetItemsAt = funcGetItemsAt;
            FuncGetCount = funcGetCount;
            FuncIndexOf = funcIndexOf;
            FuncContains = funcContains;
            ActionOnReset = actionOnReset;
        }

        public Action<int> ActionOnReset { get; set; }
        public Func<T, bool> FuncContains { get; set; }

        public Func<int> FuncGetCount { get; set; }

        public Func<int, int, IEnumerable<T>> FuncGetItemsAt { get; set; }

        public Func<T, int> FuncIndexOf { get; set; }

        public virtual void OnReset(int count)
        {
            ActionOnReset?.Invoke(count);
        }

        public bool Contains(T item)
        {
            return FuncContains?.Invoke(item) ?? false;
        }

        public virtual int Count
        {
            get
            {
                var ret = 0;

                if (FuncGetCount != null) ret = FuncGetCount.Invoke();

                return ret;
            }
        }

        public virtual IEnumerable<T> GetItemsAt(int pageoffset, int count)
        {
            return FuncGetItemsAt?.Invoke(pageoffset, count);
        }

        public virtual int IndexOf(T item)
        {
            return FuncIndexOf?.Invoke(item) ?? -1;
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