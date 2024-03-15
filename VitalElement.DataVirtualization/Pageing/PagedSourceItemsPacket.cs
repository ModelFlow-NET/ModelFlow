namespace VitalElement.DataVirtualization.Pageing
{
    using System;
    using System.Collections.Generic;

    public class PagedSourceItemsPacket<T>
    {
        public IEnumerable<T> Items { get; set; }

        public object LoadedAt { get; set; } = DateTime.Now;
    }
}