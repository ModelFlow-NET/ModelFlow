namespace VitalElement.DataVirtualization.DataManagement;

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public interface IDataItem
{
    object Item { get; internal set; }
    
    bool IsLoading { get; internal set; }
}

public interface IDataItem<T> : IDataItem
{
    public T Item { get; }
}

public static class DataItem
{
    public static DataItem<T> Create<T>(T item)
    {
        return new DataItem<T>(item, false);
    }
}

public class DataItem<T> : IDataItem<T>, IDataItem, INotifyPropertyChanged
{
    private T _item;
    private bool _isLoading;

    internal DataItem(T item, bool isPlaceholder)
    {
        _item = item;
        _isLoading = isPlaceholder;
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

    bool IDataItem.IsLoading
    {
        get => IsLoading;
        set => IsLoading = value;
    }

    object IDataItem.Item
    {
        get => Item;
        set => Item = (T)value;
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

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}