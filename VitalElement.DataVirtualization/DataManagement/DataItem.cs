namespace VitalElement.DataVirtualization.DataManagement;

using System.ComponentModel;
using System.Runtime.CompilerServices;

public interface IDataItem
{
    object Item { get; internal set; }
    
    bool IsLoading { get; internal set; }
}

public interface IDataItem<T> : IDataItem where T : class
{
    public new T Item { get; }
}

public static class DataItem
{
    public static IDataItem<T> Create<T>(T item) where T : class
    {
        return new DataItem<T>(item, false);
    }
    
    internal static IDataItem<T> Create<T>(T item, bool isPlaceholder) where T : class
    {
        return new DataItem<T>(item, isPlaceholder);
    }
}

internal class DataItem<T> : IDataItem<T>, INotifyPropertyChanged where T : class
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