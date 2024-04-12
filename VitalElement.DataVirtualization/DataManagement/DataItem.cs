namespace VitalElement.DataVirtualization.DataManagement;

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public interface IDataItem
{
    object Item { get; internal set; }
}

public class DataItem<T> : IDataItem, INotifyPropertyChanged where T : class
{
    private T _item;
    private bool _isLoading = true;

    public DataItem(T item)
    {
        _item = item;
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (value == _isLoading) return;
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    object IDataItem.Item
    {
        get => Item;
        set
        {
            IsLoading = false;
            Item = (T)value;
        }
    }

    public T Item
    {
        get => _item;
        private set
        {
            _item = value;
            OnPropertyChanged();
        }
    }

    public bool IsInUse
    {
        get { return PropertyChanged != null; }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class DataItem1<T> : IDataItem, INotifyPropertyChanged
{
    private T _item;

    public DataItem1(T item)
    {
        _item = item;
    }

    object IDataItem.Item
    {
        get => Item;
        set => Item = (T)value;
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