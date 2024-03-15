using System;

namespace VitalElement.VirtualizingCollection.Interfaces
{
    public interface IObservableReactiveCollection<T> : IObservableCollection<T>, IObservable<T>
    {
        new int Count { get; }
        new bool Remove(object item);
    }
}