namespace VitalElement.DataVirtualization.DataManagement;

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

internal interface IDataItem { }

public class DataItem<T> : IDataItem, INotifyPropertyChanged
{
    private T _item;

    public DataItem(T item)
    {
        _item = item;
    }

    public T Item
    {
        get => _item;
        set
        {
            if (EqualityComparer<T>.Default.Equals(value, _item)) return;
            _item = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}