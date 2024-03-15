namespace VitalElement.DataVirtualization.Pageing
{
    using System;
    using System.Collections.Generic;

    internal class PagedSourceItemsPacket<T>
    {
        public PagedSourceItemsPacket(IEnumerable<T> items)
        {
            Items = items;
            LoadedAt = DateTime.Now;
        }

        public IEnumerable<T> Items { get; }

        public object LoadedAt { get; }
    }
}