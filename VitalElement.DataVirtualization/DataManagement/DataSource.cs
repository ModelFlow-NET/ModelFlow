namespace VitalElement.DataVirtualization.DataManagement;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Actions;
using VitalElement.DataVirtualization.Interfaces;
using VitalElement.DataVirtualization.Pageing;

public class DataSource
{
    public static IDataSourceCallbacks? DataSourceCallbacks;
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
        SyncRoot = new object();
        _autoSyncEnabled = autoSync;
        _selector = selector;
        _collection = new VirtualizingObservableCollection<DataItem<TViewModel>>(
            new PaginationManager<DataItem<TViewModel>>(this, pageSize: pageSize, maxPages: maxPages));

        SortDescriptionList = new SortDescriptionList();

        SortDescriptionList.CollectionChanged += (_, _) => { Invalidate(); };
    }

    void IPagedSourceProviderAsync<DataItem<TViewModel>>.Replace (DataItem<TViewModel> old, DataItem<TViewModel> newItem)
    {
        old.Item = newItem.Item;
    }

    /// <summary>
    /// Indicates that the count property has been accessed at least once.
    /// This means that a DataGrid or List has connected to the datasource.
    /// </summary>
    public bool IsInitialised { get; private set; }

    /// <summary>
    /// Indicates if the datasource is Threadsafe.
    /// </summary>
    public bool IsSynchronized => false;

    /// <summary>
    /// An object used to synchronise access with the underlying collection.
    /// </summary>
    public object SyncRoot { get; }

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
    public void SetFilterQuery(Func<IQueryable<TModel>, IQueryable<TModel>>? filterQuery)
    {
        _filterQuery = filterQuery;
    }

    /// <summary>
    /// Retrieves the underlying virtualized observable collection.
    /// This is exposed as an IReadOnlyCollection, as the user should use the DataSource api to modify its contents.
    /// The collection is an VirtualizedObservableCollection meaning that only the items accessed are materialized
    /// via the datasource.
    /// </summary>
    public IReadOnlyObservableCollection<DataItem<TViewModel>> Collection => _collection;

    /// <summary>
    /// A list of sort descriptions that can be changed.
    /// Invalidation of the datasource is automatic when this is modified.
    /// </summary>
    public SortDescriptionList SortDescriptionList { get; }

    /// <summary>
    /// Called when the datasource is reset.
    /// </summary>
    /// <param name="count">The number of items in the datasource.</param>
    protected abstract void OnReset(int count);

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
    /// <returns></returns>
    protected abstract TViewModel GetPlaceHolder(int index, int page, int offset);

    protected abstract TModel? GetModelForViewModel(TViewModel viewModel);

    protected abstract bool ModelsEqual(TModel a, TModel b);

    Task<int> IPagedSourceProviderAsync<DataItem<TViewModel>>.IndexOfAsync(DataItem<TViewModel> item) =>
        IndexOfAsync(item.Item);
    
    protected virtual async Task<int> IndexOfAsync(TViewModel item,
        Func<IQueryable<TModel>, IQueryable<TModel>> filterSortQuery)
    {
        var model = GetModelForViewModel(item);

        if (model is null)
        {
            return -1;
        }

        var count = await GetCountAsync(BuildFilterQuery); // use last count to reduce calls.
        
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
    /// This will be called when a ViewModel is materialized. It is called AFTER any async initialisation has occurred.
    /// This can be used to subscribe to model changes. After this is called, Database synchronisation will be active.
    /// </summary>
    /// <param name="item">The ViewModel that was created.</param>
    protected virtual void OnMaterialized(TViewModel item)
    {
        if (_autoSyncEnabled && item is IAutoSynchronize { IsManaged: false } vm)
        {
            this.AutoManage(vm);
        }
    }
    
    /// <summary>
    /// Triggers a request of the first page. This is intended to be used to allow combobox controls to work where your data source is typically smaller
    /// than a page size. If your datasource is larger than a page, then you may need to use a more suitable control that doesnt rely on knowing all the indexes
    /// to manage selection. 
    /// </summary>
    public async Task RequestFirstPage()
    {
        if (Collection is VirtualizingObservableCollection<TViewModel> col && col.Provider is PaginationManager<TViewModel> pgr)
        {
            // Trigger the first page to be retrieved.
            await Task.Run(async () =>
            {
                var count = pgr.Count;

                while (!IsInitialised)
                {
                    await Task.Delay(100);
                }

                count = pgr.Count;

                if (count > 0)
                {
                    pgr.GetAt(0, col);
                }
            });
            // Calls after this called from the ui thread will have the first page loaded.
            // Because GetAt schedules the actual page request on the ui thread.
        }
    }
    
    public async Task<bool> CreateAsync(TViewModel viewModel)
    {
        try
        {
            if (DataSourceCallbacks is { })
            {
                if (!await DataSourceCallbacks.OnBeforeCreateOperation(viewModel))
                {
                    return false;
                }
            }

            bool isSuccess = await DoCreateAsync(viewModel);

            if (isSuccess)
            {
                // to do, maintain a dictionary, so we dont get duplicates when retrieved from source.
                var index = await IndexOfAsync(viewModel);

                if (index >= 0)
                {
                    _collection.Insert(index, viewModel);
                }
                else
                {
                    _collection.Add(viewModel);   
                }

                await ProcessMaterializedItem(viewModel);
            }
            
            if (DataSourceCallbacks is { })
            {
                await DataSourceCallbacks.OnCreateOperationCompleted(viewModel, isSuccess);
            }

            return isSuccess;
        }
        catch (Exception e)
        {
            if (DataSourceCallbacks is { })
            {
                await DataSourceCallbacks.OnCreateException(viewModel, e);
            }
        }

        return false;
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

    public async Task<bool> DeleteAsync(TViewModel viewModel)
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

            bool isSuccess = await DoDeleteAsync(viewModel);

            if (isSuccess)
            {
                _collection.Remove(viewModel);
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
    

    void IBaseSourceProvider<DataItem<TViewModel>>.OnReset(int count)
    {
        OnReset(count);
    }

    Task<bool> IPagedSourceProviderAsync<DataItem<TViewModel>>.ContainsAsync(DataItem<TViewModel> item) => ContainsAsync(item.Item);

    async Task<int> IPagedSourceProviderAsync<DataItem<TViewModel>>.GetCountAsync()
    {
        var result = await GetCountAsync(BuildFilterQuery);
        IsInitialised = true;
        return result;
    }

    /// <summary>
    /// Gets the count from the database, applying any filters.
    /// Note: this public GetCountAsync is for end users only.
    /// it is not the one called by the data virtualization.
    /// </summary>
    /// <returns>int - the number of rows.</returns>
    public async Task<int> GetCountAsync() => await GetCountAsync(BuildFilterQuery);

    public async Task<TViewModel?> GetViewModelAsync(Expression<Func<TModel, bool>> predicate)
    {
        TViewModel? result = null;

        var item = await GetItemAsync(predicate);

        if (item != null)
        {
            var completionSource = new TaskCompletionSource<bool>();
            
            await VirtualizationManager.Instance.RunOnUiAsync(new ActionVirtualizationWrapper(async () =>
            {
                if (result is INeedsInitializationAsync toInitialize)
                {
                    await toInitialize.InitializeAsync();
                }

                result = await Materialize(item);
                
                completionSource.SetResult(true);
            }));

            await completionSource.Task;
        }

        return result;
    }

    private async Task<TViewModel> Materialize(TModel item)
    {
        var result = _selector(item);

        await ProcessMaterializedItem(result);

        return result;
    }

    private async Task ProcessMaterializedItem(TViewModel viewModel)
    {
        if (viewModel is INeedsInitializationAsync toInitialize)
        {
            await toInitialize.InitializeAsync();
        }

        OnMaterialized(viewModel);
    }

    async Task<IEnumerable<DataItem<TViewModel>>> IPagedSourceProviderAsync<DataItem<TViewModel>>.GetItemsAtAsync(int offset, int count)
    {
        var items = (await GetItemsAtAsync(offset, count, BuildFilterSortQuery)).ToList();

        List<TViewModel> result = new List<TViewModel>();

        var completionSource = new TaskCompletionSource<bool>();

        await VirtualizationManager.Instance.RunOnUiAsync(new ActionVirtualizationWrapper(async () =>
        {
            foreach (var item in items)
            {
                result.Add(await Materialize(item));
            }

            completionSource.SetResult(true);
        }));

        await completionSource.Task;

        return result.Select(x => new DataItem<TViewModel>(x));
    }

    DataItem<TViewModel> IPagedSourceProviderAsync<DataItem<TViewModel>>.GetPlaceHolder(int index, int page, int offset) =>
        new DataItem<TViewModel>(GetPlaceHolder(index, page, offset));

    
    public async Task<int> IndexOfAsync(TViewModel item)
    {
        return await IndexOfAsync(item, BuildFilterSortQuery);
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

    public IQueryable<TModel> BuildSortQuery(IQueryable<TModel> queryable)
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

    private IQueryable<TModel> BuildFilterSortQuery(IQueryable<TModel> queryable)
    {
        if (_filterQuery is not null)
        {
            queryable = _filterQuery(queryable);
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
}