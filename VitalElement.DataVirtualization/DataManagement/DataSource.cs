namespace VitalElement.DataVirtualization.DataManagement;

using System.Threading.Tasks;
using VitalElement.DataVirtualization.Extensions;
using VitalElement.DataVirtualization.Interfaces;
using VitalElement.DataVirtualization.Pageing;

public abstract class DataSource<T> : IPagedSourceProviderAsync<T>, IFilteredSortedSourceProviderAsync
        where T : class
    {
        private readonly VirtualizingObservableCollection<T> _collection;
        
        public FilterDescriptionList FilterDescriptionList { get; }

        public SortDescriptionList SortDescriptionList { get; }
        
        public string? FilterQuery { get; private set; }
        
        public string? SortQuery { get; private set; }

        public DataSource(int pageSize, int maxPages)
        {
            _collection = new VirtualizingObservableCollection<T>(
                new PaginationManager<T>(this, pageSize: pageSize, maxPages: maxPages));

            FilterDescriptionList = new FilterDescriptionList();
            SortDescriptionList = new SortDescriptionList();

            FilterDescriptionList.CollectionChanged += (_, _) =>
            {
                _collection.Clear();
                FilterQuery = FilterDescriptionList.ToQueryString();
            };
            
            SortDescriptionList.CollectionChanged += (_, _) =>
            {
                _collection.Clear();
                SortQuery = SortDescriptionList.ToQueryString();
            };
        }

        public VirtualizingObservableCollection<T> Collection => _collection;
        
        protected abstract void OnReset(int count);

        protected abstract Task<bool> ContainsAsync(T item);

        protected abstract Task<int> GetCountAsync();

        protected abstract Task<PagedSourceItemsPacket<T>> GetItemsAtAsync(int offset, int count);

        protected abstract T GetPlaceHolder(int index, int page, int offset);

        protected abstract Task<int> IndexOfAsync(T item);

        void IBaseSourceProvider<T>.OnReset(int count)
        {
            OnReset(count);
        }

        Task<bool> IPagedSourceProviderAsync<T>.ContainsAsync(T item) => ContainsAsync(item);

        Task<int> IPagedSourceProviderAsync<T>.GetCountAsync() => GetCountAsync();

        Task<PagedSourceItemsPacket<T>> IPagedSourceProviderAsync<T>.GetItemsAtAsync(int offset, int count) => GetItemsAtAsync(offset, count);

        T IPagedSourceProviderAsync<T>.GetPlaceHolder(int index, int page, int offset) =>
            GetPlaceHolder(index, page, offset);

        Task<int> IPagedSourceProviderAsync<T>.IndexOfAsync(T item) => IndexOfAsync(item);
        
        public bool IsSynchronized { get; }
        
        public object SyncRoot { get; }
    }