﻿namespace ModelFlow.DataVirtualization.Interfaces
{
    using System.Collections.Generic;
    using Pageing;

    internal interface ISourcePage<T>
    {
        bool CanReclaimPage { get; }

        int ItemsCount { get; }

        int ItemsPerPage { get; set; }

        object LastTouch { get; set; }
        int Page { get; set; }

        PageFetchStateEnum PageFetchState { get; set; }

        List<SourcePagePendingUpdates> PendingUpdates { get; }

        object WiredDateTime { get; set; }

        int Append(T item, object updatedAt, IPageExpiryComparer comparer);

        T GetAt(int offset);

        int IndexOf(T item);

        void InsertAt(int offset, T item, object updatedAt, IPageExpiryComparer comparer);

        bool RemoveAt(int offset, object updatedAt, IPageExpiryComparer comparer);

        T ReplaceAt(int offset, T newValue, object updatedAt, IPageExpiryComparer comparer);

        T ReplaceAt(T oldValue, T newValue, object updatedAt, IPageExpiryComparer comparer);

        bool ReplaceNeeded(int offset);
    }
}