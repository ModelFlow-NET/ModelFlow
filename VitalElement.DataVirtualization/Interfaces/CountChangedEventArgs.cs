namespace VitalElement.DataVirtualization.Interfaces
{
    using System;

    internal class CountChangedEventArgs : EventArgs
    {
        public int Count { get; set; }
        public bool NeedsReset { get; set; }
    }
}