namespace ModelFlow.DataVirtualization.DataManagement;

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Actions;
using Interfaces;
using Pageing;

public abstract class DataSource : INotifyPropertyChanged
{
    public static IDataSourceCallbacks? DataSourceCallbacks;
    private bool _isInitialised;
    private bool _isActive;

    /// <summary>
    /// Indicates that the count property has been accessed at least once.
    /// This means that a DataGrid or List has connected to the datasource.
    /// </summary>
    public bool IsInitialised
    {
        get => _isInitialised;
        protected internal set
        {
            if (value == _isInitialised) return;
            _isInitialised = value;
            OnPropertyChanged();
        }
    }

    public bool IsActive
    {
        get => _isActive;
        protected internal set
        {
            if(value == _isActive) return;
            _isActive = value;
            OnPropertyChanged();
        }
    }
    
    public abstract IEnumerable DataCollection { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? FilterQueryCleared;

    protected void RaiseFilterQueryCleared()
    {
        FilterQueryCleared?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public abstract class DataSource<TViewModel> : DataSource<TViewModel, TViewModel> where TViewModel : class
{
    protected DataSource(int pageSize, int maxPages, bool autoSync = true) : base(x => x, pageSize, maxPages, autoSync)
    {
    }

    protected override TViewModel? GetModelForViewModel(TViewModel viewModel)
    {
        return viewModel;
    }
}

public abstract class DataSource<TViewModel, TModel> : DataSource, IPagedSourceProviderAsync<DataItem<TViewModel>>
    where TViewModel : class
{
    private Func<IQueryable<TModel>, IQueryable<TModel>>? _filterQuery;
    private readonly VirtualizingObservableCollection<DataItem<TViewModel>> _collection;
    private readonly Func<TModel, TViewModel> _selector;
    private readonly bool _autoSyncEnabled;

    public DataSource(Func<TModel, TViewModel> selector, int pageSize, int maxPages, bool autoSync = true)
    {
        _autoSyncEnabled = autoSync;
        _selector = selector;
        _collection = new (
            new PaginationManager<DataItem<TViewModel>>(this, ()=>_collection, pageSize: pageSize, maxPages: maxPages));

        SortDescriptionList = new SortDescriptionList();

        SortDescriptionList.CollectionChanged += (_, _) => Invalidate();
    }

    /// <summary>
    /// Indicates if the datasource is Threadsafe.
    /// </summary>
    public bool IsSynchronized => false;

    /// <summary>
    /// An object used to synchronise access with the underlying collection.
    /// </summary>
    object ISynchronized.SyncRoot { get; } = new();

    /// <summary>
    /// Invalidates the datasource, causing the count and items to be retrieved again.
    /// This should be called if the filter query needs to be re-evaluated. Changing the sort descriptions
    /// this will be called automatically.
    /// </summary>
    public void Invalidate()
    {
        _collection.Clear();
    }

    /// <summary>
    /// Sets a filter query on the datasource. All accesses to the datasource will be filtered according to this query.
    /// </summary>
    /// <param name="filterQuery">A Func that retrieves the query.</param>
    /// <param name="invalidate">If the datasource should be invalidated when setting the query. (Default true)</param>
    public void SetFilterQuery(Func<IQueryable<TModel>, IQueryable<TModel>>? filterQuery, bool invalidate = true)
    {
        if (_filterQuery != filterQuery)
        {
            _filterQuery = filterQuery;

            if (invalidate)
            {
                Invalidate();
            }

            if (filterQuery == null)
            {
                RaiseFilterQueryCleared();
            }
        }
    }

    /// <summary>
    /// Retrieves the underlying virtualized observable collection.
    /// This is exposed as an IReadOnlyCollection, as the user should use the DataSource api to modify its contents.
    /// The collection is an VirtualizedObservableCollection meaning that only the items accessed are materialized
    /// via the datasource.
    /// </summary>
    public IReadOnlyObservableCollection<DataItem<TViewModel>> Collection => _collection;

    public override IEnumerable DataCollection => Collection;

    /// <summary>
    /// A list of sort descriptions that can be changed.
    /// Invalidation of the datasource is automatic when this is modified.
    /// </summary>
    public SortDescriptionList SortDescriptionList { get; }

    /// <summary>
    /// Called when the datasource is reset.
    /// </summary>
    /// <param name="count">The number of items in the datasource.</param>
    protected virtual void OnReset(int count)
    {
        // do nothing.
    }

    /// <summary>
    /// Determines if the datasource contains a viewmodel.
    /// </summary>
    /// <param name="item">The viewmodel to check</param>
    /// <returns>true if the datasource contains the model, false otherwise.</returns>
    protected abstract Task<bool> ContainsAsync(TViewModel item);

    /// <summary>
    /// Gets the total number of TModel in the DataSource when filtered.
    /// </summary>
    /// <param name="filterQuery">The filter query to filter the datasource.</param>
    /// <returns>the number of items as an integer.</returns>
    protected abstract Task<int> GetCountAsync(Func<IQueryable<TModel>, IQueryable<TModel>> filterQuery);

    /// <summary>
    /// Gets a range of items within a datasource's sequence.
    /// </summary>
    /// <param name="offset">The offset of the first item to retrieve.</param>
    /// <param name="count">The number of items to retrieve.</param>
    /// <param name="filterSortQuery">A query to run on the datasource that filters and then sorts the data.</param>
    /// <returns>returns an IEnumerable of <see cref="TModel"/>s</returns>
    protected abstract Task<IEnumerable<TModel>> GetItemsAtAsync(int offset, int count,
        Func<IQueryable<TModel>, IQueryable<TModel>> filterSortQuery);

    /// <summary>
    /// Retrieves a single row from the datasource.
    /// </summary>
    /// <param name="predicate">The predicate to select the row.</param>
    /// <returns>The model representing the row, or null if no row matches the predicate.</returns>
    public abstract Task<TModel?> GetItemAsync(Expression<Func<TModel, bool>> predicate);

    /// <summary>
    /// Gets a placeholder to represent the viewmodel whilst data is yet to be retrieved.
    /// It is recommended to return a singleton here, to save memory usage.
    /// </summary>
    /// <param name="index">The index of the item.</param>
    /// <param name="page">The page index of the item.</param>
    /// <param name="offset">The offset of the item.</param>
    /// <returns>Returns a placeholder or null if not needed.</returns>
    protected abstract TViewModel GetPlaceHolder(int index, int page, int offset);

    /// <summary>
    /// Given an instance of a viewmodel, retrieve the model.
    /// Implementing this allows the DataSource to support selection, using its inbuilt implementation of
    /// <see cref="IndexOfAsync(DataItem{T})"/>
    /// If this is not implmented or returns null then the inbuilt indexof will not allow supporting selection.
    /// </summary>
    /// <param name="viewModel">The viewmodel to get the model from.</param>
    /// <returns>The model.</returns>
    protected abstract TModel? GetModelForViewModel(TViewModel viewModel);

    /// <summary>
    /// Are 2 models equal or equivalent.
    /// For example if the model is a database object, this should compare the primary key
    /// to check if they are representing the same row in the database or other source.
    /// Note: they may not be the same instance!
    /// This method is needed to support selection which performs a binary search.
    /// </summary>
    /// <param name="a">Model A</param>
    /// <param name="b">Model B</param>
    /// <returns>true or false</returns>
    protected abstract bool ModelsEqual(TModel a, TModel b);

    /// <summary>
    /// Get the Index of a ViewModel inside the datasource.
    /// Note: this will not work unless there is at least 1 SortDescription set on the datasource.
    /// The default implementation performs a binary search which can lead to several calls to the datasource.
    /// If your datasource is suitable to query the index of an item, you may override this method, to implement
    /// a more efficient solution.
    /// </summary>
    /// <param name="item">The viewmodel to get the index of.</param>
    /// <param name="filterSortQuery">A query that filters and sorts the datasource.</param>
    /// <returns>the index of the item in the datasource or -1 if it can not be found.</returns>
    protected virtual async Task<int> IndexOfAsync(TViewModel item,
        Func<IQueryable<TModel>, IQueryable<TModel>> filterSortQuery)
    {
        var model = GetModelForViewModel(item);

        if (model is null)
        {
            return -1;
        }

        var count = await GetCountAsync(BuildFilterQuery); // use last count to reduce calls.

        if (count <= 0)
        {
            return -1;
        }

        // Initialize start and end indices for binary search
        int start = 0;
        int end = count - 1;

        while (start <= end)
        {
            int mid = start + ((end - start) / 2);

            var sample = (await GetItemsAtAsync(mid, 1, filterSortQuery)).FirstOrDefault();

            if (ModelsEqual(sample, model))
            {
                // contains... index.
                return mid;
            }
            else
            {
                var compareItems = new[] { model, sample };

                var query = BuildSortQuery(compareItems.AsQueryable());

                var sorted = query.ToList();

                if (ModelsEqual(sorted[0], model))
                {
                    // Item is lower, search in the left half
                    end = mid - 1;
                }
                else
                {
                    // Item is higher, search in the right half
                    start = mid + 1;
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// This ensures your datasource count is initialised. You need this to happen usually before selecting an item.
    /// </summary>
    public async Task EnsureInitialisedAsync()
    {
        if (!IsInitialised)
        {
            var count = Collection.Count;

            while (!IsInitialised)
            {
                await Task.Delay(10);
            }
        }
    }

    /// <summary>
    /// Creates the viewmodel in the datasource.
    /// i.e. Add a new model to the datasource based on the viewmodel.
    /// </summary>
    /// <param name="viewModel"></param>
    /// <returns>true if it was a success.</returns>
    public async Task<(bool success, int index, DataItem<TViewModel>? item)> CreateAsync(TViewModel viewModel)
    {
        // Ensure the datasource is initialised, otherwise it can cause a race condition
        // The insert will want to ask for the count in a sync fashion.
        await EnsureInitialisedAsync();

        SetFilterQuery(null, true);

        try
        {
            if (DataSourceCallbacks is { })
            {
                if (!await DataSourceCallbacks.OnBeforeCreateOperation(viewModel))
                {
                    return (false, -1, null);
                }
            }

            bool isSuccess = await DoCreateAsync(viewModel);
            int index = -1;
            DataItem<TViewModel>? item = null;

            if (isSuccess)
            {
                // to do, maintain a dictionary, so we dont get duplicates when retrieved from source.
                index = await IndexOfAsync(viewModel);

                item = DataItem.Create(viewModel);

                if (index >= 0 && _collection.Count > index)
                {
                    _collection.Insert(index, item);
                }
                else
                {
                    _collection.Add(item);
                }

                await Materialize(item, item.Item);
            }

            if (DataSourceCallbacks is { })
            {
                await DataSourceCallbacks.OnCreateOperationCompleted(viewModel, isSuccess);
            }

            return (isSuccess, index, item);
        }
        catch (Exception e)
        {
            if (DataSourceCallbacks is { })
            {
                await DataSourceCallbacks.OnCreateException(viewModel, e);
            }
        }

        return (false, -1, null);
    }

    /// <summary>
    /// Creates an entry in the datasource to store the ViewModel 
    /// </summary>
    /// <param name="item">The viewmodel.</param>
    /// <returns>True if the operation was a success or false if it failed.</returns>
    protected abstract Task<bool> DoCreateAsync(TViewModel item);

    /// <summary>
    /// Updates an entry in the datasource. 
    /// </summary>
    /// <param name="item">The viewmodel.</param>
    /// <returns>True if the operation was a success or false if it failed.</returns>
    protected abstract Task<bool> DoUpdateAsync(TViewModel viewModel);

    /// <summary>
    /// Update the viewmodel in the datasource.
    /// </summary>
    /// <param name="viewModel">the viewmodel to save.</param>
    public async Task UpdateAsync(TViewModel viewModel)
    {
        try
        {
            if (DataSourceCallbacks is { })
            {
                if (!await DataSourceCallbacks.OnBeforeUpdateOperation(viewModel))
                {
                    return;
                }
            }

            bool isSuccess = await DoUpdateAsync(viewModel);

            if (DataSourceCallbacks is { })
            {
                await DataSourceCallbacks.OnUpdateOperationCompleted(viewModel, isSuccess);
            }
        }
        catch (Exception e)
        {
            if (DataSourceCallbacks is { } callbacks)
            {
                await callbacks.OnUpdateException(viewModel, e);
            }
        }
    }

    /// <summary>
    /// Deletes an entry in the datasource. 
    /// </summary>
    /// <param name="item">The viewmodel.</param>
    /// <returns>True if the operation was a success or false if it failed.</returns>
    protected abstract Task<bool> DoDeleteAsync(TViewModel item);

    /// <summary>
    /// Delete an item from the datasource.
    /// </summary>
    /// <param name="viewModel">The viewmodel that will be deleted.</param>
    /// <returns>true if the delete was a success.</returns>
    public async Task<bool> DeleteAsync(DataItem<TViewModel> viewModel)
    {
        try
        {
            if (DataSourceCallbacks is { })
            {
                if (!await DataSourceCallbacks.OnBeforeDeleteOperation(viewModel))
                {
                    return false;
                }
            }

            var index = _collection.IndexOf(viewModel);

            bool isSuccess = await DoDeleteAsync(viewModel.Item);

            if (isSuccess && index >= 0)
            {
                _collection.RemoveAt(index);
            }

            if (DataSourceCallbacks is { })
            {
                await DataSourceCallbacks.OnDeleteOperationCompleted(viewModel, isSuccess);
            }

            return isSuccess;
        }
        catch (Exception e)
        {
            if (DataSourceCallbacks is { })
            {
                await DataSourceCallbacks.OnDeleteException(viewModel, e);
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the count from the database, applying any filters.
    /// Note: this public GetCountAsync is for end users only.
    /// it is not the one called by the data virtualization.
    /// </summary>
    /// <returns>int - the number of rows.</returns>
    public async Task<int> GetCountAsync() => await GetCountAsync(BuildFilterQuery);

    public Task<IEnumerable<TModel>> GetModelsAtAsync(int offset, int count)
    {
        return GetItemsAtAsync(offset, count, x => BuildFilterSortQuery(x, _filterQuery));
    }

    /// <summary>
    /// Get the first viewmodel that matches the predicate.
    /// </summary>
    /// <param name="predicate">A predicate that selects a <see cref="TModel"/></param>
    /// <returns>An instance of <see cref="TViewModel"/> or null.</returns>
    public async Task<TViewModel?> GetViewModelAsync(Expression<Func<TModel, bool>> predicate)
    {
        DataItem<TViewModel>? result = null;

        var item = await GetItemAsync(predicate);

        if (item != null)
        {
            var completionSource = new TaskCompletionSource<bool>();

            await VirtualizationManager.Instance.RunOnUiAsync(new ActionVirtualizationWrapper(async () =>
            {
                result = await Materialize(item);

                completionSource.SetResult(true);
            }));

            await completionSource.Task;
        }

        return result?.Item;
    }

    private async Task<DataItem<TViewModel>> Materialize(ISourcePage<DataItem<TViewModel>> page, int pageIndex, TModel item)
    {
        return await Materialize(page.GetAt(pageIndex), item);
    }

    private async Task<DataItem<TViewModel>> Materialize(TModel item)
    {
        return await Materialize(null, item);
    }
    
    private async Task<DataItem<TViewModel>> Materialize(DataItem<TViewModel>? wrapper, TModel model)
    {
        var materialized = _selector(model);

        return await Materialize(wrapper, materialized);
    }

    private async Task<DataItem<TViewModel>> Materialize(DataItem<TViewModel>? wrapper, TViewModel materialized)
    {
        await InitializeItemAsync(materialized);

        if (wrapper == null)
        {
            wrapper = DataItem.Create(materialized);
        }
        
        wrapper.SetItem(materialized);
        
        OnMaterializedInternal(wrapper);

        return wrapper;
    }

    private async Task InitializeItemAsync(TViewModel viewModel)
    {
        if (viewModel is INeedsInitializationAsync toInitialize)
        {
            await toInitialize.InitializeAsync();
        }
    }
    
    private void OnMaterializedInternal(DataItem<TViewModel> item)
    {
        if (_autoSyncEnabled && item.Item is IAutoSynchronize { IsManaged: false })
        {
            this.AutoManage(item);
        }
        
        OnMaterialized(item);
    }
    
    /// <summary>
    /// This will be called when a ViewModel is materialized. It is called AFTER any async initialisation has occurred.
    /// This can be used to subscribe to model changes. After this is called, Database synchronisation will be active.
    /// </summary>
    /// <param name="item">The ViewModel that was created.</param>
    protected virtual void OnMaterialized(DataItem<TViewModel> item)
    {
    }

    /// <summary>
    /// Gets the index of a viewmodel in the datasource.
    /// Note: this requires your datasource to implement certain methods:
    /// <see cref="IndexOfAsync(DataItem{T})"/>
    /// <seealso cref="GetModelForViewModel"/>
    /// <seealso cref="ModelsEqual"/>
    /// </summary>
    /// <param name="item">The item to get the index of.</param>
    /// <returns>the index or -1 if an item was not in the datasource.</returns>
    public async Task<int> IndexOfAsync(TViewModel item)
    {
        return await IndexOfAsync(item, x => BuildFilterSortQuery(x, _filterQuery));
    }

    private IQueryable<TModel> AddSorting(IQueryable<TModel> query, ListSortDirection sortDirection,
        string propertyName)
    {
        var param = Expression.Parameter(typeof(TModel));
        var prop = Expression.PropertyOrField(param, propertyName);
        var sortLambda = Expression.Lambda(prop, param);

        Expression<Func<IOrderedQueryable<TModel>>>? sortMethod = null;

        switch (sortDirection)
        {
            case ListSortDirection.Ascending when query.Expression.Type == typeof(IOrderedQueryable<TModel>):
                sortMethod = () => ((IOrderedQueryable<TModel>)query).ThenBy<TModel, object?>(k => null);
                break;
            case ListSortDirection.Ascending:
                sortMethod = () => query.OrderBy<TModel, object?>(k => null);
                break;
            case ListSortDirection.Descending when query.Expression.Type == typeof(IOrderedQueryable<TModel>):
                sortMethod = () => ((IOrderedQueryable<TModel>)query).ThenByDescending<TModel, object?>(k => null);
                break;
            case ListSortDirection.Descending:
                sortMethod = () => query.OrderByDescending<TModel, object?>(k => null);
                break;
        }

        var methodCallExpression = sortMethod?.Body as MethodCallExpression;
        if (methodCallExpression == null)
        {
            throw new Exception("MethodCallExpression null");
        }

        var method = methodCallExpression.Method.GetGenericMethodDefinition();
        var genericSortMethod = method.MakeGenericMethod(typeof(TModel), prop.Type);

        return ((IOrderedQueryable<TModel>)genericSortMethod.Invoke(query, new object[] { query, sortLambda }) !) !;
    }

    private IQueryable<TModel> BuildFilterQuery(IQueryable<TModel> queryable)
    {
        if (_filterQuery is not null)
        {
            queryable = _filterQuery(queryable);
        }

        return queryable;
    }

    private IQueryable<TModel> BuildSortQuery(IQueryable<TModel> queryable)
    {
        var sorting = SortDescriptionList;

        if (sorting.Any())
        {
            foreach (var sort in sorting)
            {
                if (sort.Direction != null)
                {
                    queryable = AddSorting(queryable, sort.Direction.Value, sort.PropertyName);
                }
            }
        }

        return queryable;
    }

    private IQueryable<TModel> BuildFilterSortQuery(IQueryable<TModel> queryable, Func<IQueryable<TModel>, IQueryable<TModel>>? filterQuery)
    {
        if (filterQuery is not null)
        {
            queryable = filterQuery(queryable);
        }

        var sorting = SortDescriptionList;

        if (sorting.Any())
        {
            foreach (var sort in sorting)
            {
                if (sort.Direction != null)
                {
                    queryable = AddSorting(queryable, sort.Direction.Value, sort.PropertyName);
                }
            }
        }

        return queryable;
    }

    Task<int> IPagedSourceProviderAsync<DataItem<TViewModel>>.IndexOfAsync(DataItem<TViewModel> item)
    {
        return IndexOfAsync(item.Item);
    }

    void IBaseSourceProvider.OnReset(int count)
    {
        OnReset(count);
    }

    async Task<IEnumerable<DataItem<TViewModel>>> IPagedSourceProviderAsync<DataItem<TViewModel>>.GetItemsAtAsync(ISourcePage<DataItem<TViewModel>> page,
        int offset, int count, Action? signal)
    {
        StartOperation();
        var filter = _filterQuery;
        signal?.Invoke();
        var items = (await GetItemsAtAsync(offset, count, x => BuildFilterSortQuery(x, filter))).ToList();

        if (items.Count != count)
        {
            throw new Exception(
                "The number of items returned from the data source is different than expected. This has caused an inconsistent state. Check the DataSource implementation." +
                this.GetType().FullName);
        }

        var result = new List<DataItem<TViewModel>>();

        var completionSource = new TaskCompletionSource<bool>();

        await VirtualizationManager.Instance.RunOnUiAsync(new ActionVirtualizationWrapper(async () =>
        {
            for (int i = 0; i < items.Count; i++)
            {
                result.Add(await Materialize(page, i, items[i]));
            }

            completionSource.SetResult(true);
        }));

        await completionSource.Task;
        
        EndOperation();

        return result;
    }

    DataItem<TViewModel> IPagedSourceProviderAsync<DataItem<TViewModel>>.
        GetPlaceHolder(int index, int page, int offset) =>
        DataItem.Create(GetPlaceHolder(index, page, offset), true);

    void IPagedSourceProviderAsync<DataItem<TViewModel>>.Replace(DataItem<TViewModel> old, DataItem<TViewModel> newItem)
    {
        // DO NOTHING
    }

    Task<bool> IPagedSourceProviderAsync<DataItem<TViewModel>>.ContainsAsync(DataItem<TViewModel> item)
    {
        return ContainsAsync(item.Item);
    }

    async Task<int> IPagedSourceProviderAsync<DataItem<TViewModel>>.GetCountAsync()
    {
        StartOperation();
        var result = await GetCountAsync(BuildFilterQuery);
        IsInitialised = true;
        EndOperation();
        return result;
    }

    private int _operationCount;

    private void StartOperation()
    {
        var operations = Interlocked.Increment(ref _operationCount);

        if (operations > 0)
        {
            IsActive = true;
        }
    }

    private void EndOperation()
    {
        var operations = Interlocked.Decrement(ref _operationCount);

        if (operations <= 0)
        {
            IsActive = false;
        }
    }
}

public abstract class SelectableReadOnlyDataSource<TViewModel, TModel> : DataSource<TViewModel, TModel> where TViewModel : class
{
    protected SelectableReadOnlyDataSource(Func<TModel, TViewModel> selector, int pageSize, int maxPages, bool autoSync = true) : base(selector, pageSize, maxPages, autoSync)
    {
    }

    public sealed override Task<TModel?> GetItemAsync(Expression<Func<TModel, bool>> predicate)
    {
        throw new NotImplementedException();
    }

    protected sealed override Task<bool> DoCreateAsync(TViewModel item)
    {
        throw new NotImplementedException();
    }

    protected sealed override Task<bool> DoUpdateAsync(TViewModel viewModel)
    {
        throw new NotImplementedException();
    }

    protected sealed override Task<bool> DoDeleteAsync(TViewModel item)
    {
        throw new NotImplementedException();
    }
}

public abstract class ReadOnlyDataSource<TViewModel, TModel> : DataSource<TViewModel, TModel> where TViewModel : class
{
    public ReadOnlyDataSource(Func<TModel, TViewModel> selector, int pageSize, int maxPages, bool autoSync = true) : base(selector, pageSize, maxPages, autoSync)
    {
    }

    protected sealed override Task<bool> ContainsAsync(TViewModel item)
    {
        throw new NotImplementedException();
    }

    public sealed override Task<TModel?> GetItemAsync(Expression<Func<TModel, bool>> predicate)
    {
        throw new NotImplementedException();
    }

    protected sealed override TModel? GetModelForViewModel(TViewModel viewModel)
    {
        throw new NotImplementedException();
    }

    protected sealed override bool ModelsEqual(TModel a, TModel b)
    {
        throw new NotImplementedException();
    }

    protected sealed override Task<bool> DoCreateAsync(TViewModel item)
    {
        throw new NotImplementedException();
    }

    protected sealed override Task<bool> DoUpdateAsync(TViewModel viewModel)
    {
        throw new NotImplementedException();
    }

    protected sealed override Task<bool> DoDeleteAsync(TViewModel item)
    {
        throw new NotImplementedException();
    }
}