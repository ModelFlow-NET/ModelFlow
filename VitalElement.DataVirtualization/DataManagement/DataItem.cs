namespace VitalElement.DataVirtualization.DataManagement;

using System.ComponentModel;
using System.Runtime.CompilerServices;

public abstract class DataItem
{
    public static DataItem<T> Create<T>(T item) where T : class
    {
        return new DataItem<T>(item, false);
    }
    
    internal static DataItem<T> Create<T>(T item, bool isPlaceholder) where T : class
    {
        return new DataItem<T>(item, isPlaceholder);
    }
    
    internal abstract object ItemObject { get; set; }
    
    public abstract bool IsLoading { get;  internal set; }
}

public class DataItem<T> : DataItem, INotifyPropertyChanged where T : class
{
    private T _item;
    private bool _isLoading;

    internal DataItem(T item, bool isPlaceholder)
    {
        _item = item;
        _isLoading = isPlaceholder;
    }

    public override bool IsLoading
    {
        get => _isLoading;
        internal set
        {
            if (value == _isLoading) return;
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    internal override object ItemObject
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

    public static implicit operator T(DataItem<T> dataItem)
    {
        return dataItem.Item;
    }
    
    // Implicit conversion from T to DataItem<T>
    public static implicit operator DataItem<T>(T item)
    {
        return new DataItem<T>(item, false);
    }
}