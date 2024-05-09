namespace VitalElement.DataVirtualization.Interfaces
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;

    public interface IReadOnlyObservableCollection<T> : INotifyCollectionChanged, INotifyPropertyChanged, IReadOnlyList<T>
    {
    }
    
    public interface IObservableCollection<T> : INotifyCollectionChanged, ICollection<T>, IObservableCollection
    {
        new int Count { get; }
        new bool Remove(object item);

        void MoveItem(int oldIndex, int newIndex);
    }

    public interface IObservableCollection : ICollection, INotifyCollectionChanged
    {
        void Add(object item);
        void Clear();
        bool Remove(object item);
    }
}