namespace VirtualizingCollection.Tests
{
    using VitalElement.DataVirtualization.Interfaces;

    internal class ItemSourceProvider<T> : IItemSourceProvider<T>
    {
        private readonly IList<T> _source;

        public ItemSourceProvider(IEnumerable<T> source)
        {
            _source = source.ToList();
        }

        public void OnReset(int count)
        {
            _source.Clear();
            ;
        }

        public bool Contains(T item)
        {
            return _source.Contains(item);
        }

        public T GetAt(int index, object voc)
        {
            return _source[index];
        }

        public int GetCount(bool asyncOk)
        {
            return _source.Count;
        }

        public int IndexOf(T item)
        {
            return _source.IndexOf(item);
        }

        public bool IsSynchronized { get; } = false;
        public object SyncRoot => this;
    }
}