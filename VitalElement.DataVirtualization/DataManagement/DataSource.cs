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

public abstract class DataSource<TDestination, T> : IPagedSourceProviderAsync<TDestination>
    where TDestination : class
{
    private Func<IQueryable<T>, IQueryable<T>>? filterQuery;
    private readonly VirtualizingObservableCollection<TDestination> _collection;
    private readonly Func<T, TDestination> _selector;

    public DataSource(Func<T, TDestination> selector, int pageSize, int maxPages)
    {
        _selector = selector;
        _collection = new VirtualizingObservableCollection<TDestination>(
            new PaginationManager<TDestination>(this, pageSize: pageSize, maxPages: maxPages));

        SortDescriptionList = new SortDescriptionList();

        SortDescriptionList.CollectionChanged += (_, _) => { _collection.Clear(); };
    }
    
    public bool IsInitialised { get; private set; }

    public void InvalidateFilters()
    {
        _collection.Clear();
    }

    public void SetFilterQuery(Func<IQueryable<T>, IQueryable<T>>? filterQuery)
    {
        this.filterQuery = filterQuery;
    }

    public VirtualizingObservableCollection<TDestination> Collection => _collection;

    public SortDescriptionList SortDescriptionList { get; }

    protected abstract void OnReset(int count);

    protected abstract Task<bool> ContainsAsync(TDestination item);

    protected abstract Task<int> GetCountAsync(Func<IQueryable<T>, IQueryable<T>> filterQuery);

    protected abstract Task<IEnumerable<T>> GetItemsAtAsync(int offset, int count,
        Func<IQueryable<T>, IQueryable<T>> filterSortQuery);

    protected abstract TDestination GetPlaceHolder(int index, int page, int offset);

    protected abstract Task<int> IndexOfAsync(TDestination item);

    void IBaseSourceProvider<TDestination>.OnReset(int count)
    {
        OnReset(count);
    }

    Task<bool> IPagedSourceProviderAsync<TDestination>.ContainsAsync(TDestination item) => ContainsAsync(item);

    async Task<int> IPagedSourceProviderAsync<TDestination>.GetCountAsync()
    {
        var result = await GetCountAsync(BuildFilterQuery);
        IsInitialised = true;
        return result;
    }

    async Task<IEnumerable<TDestination>> IPagedSourceProviderAsync<TDestination>.GetItemsAtAsync(int offset, int count)
    {
        var items = await GetItemsAtAsync(offset, count, BuildFilterSortQuery);

        IEnumerable<TDestination> result = Enumerable.Empty<TDestination>();

        await VirtualizationManager.Instance.RunOnUiAsync(new ActionVirtualizationWrapper(() =>
        {
            result = items.Select(_selector).ToList();
        }));

        return result;
    }

    TDestination IPagedSourceProviderAsync<TDestination>.GetPlaceHolder(int index, int page, int offset) =>
        GetPlaceHolder(index, page, offset);

    Task<int> IPagedSourceProviderAsync<TDestination>.IndexOfAsync(TDestination item) => IndexOfAsync(item);

    public bool IsSynchronized { get; }

    public object SyncRoot { get; }

    private IQueryable<T> AddSorting(IQueryable<T> query, ListSortDirection sortDirection, string propertyName)
    {
        var param = Expression.Parameter(typeof(T));
        var prop = Expression.PropertyOrField(param, propertyName);
        var sortLambda = Expression.Lambda(prop, param);

        Expression<Func<IOrderedQueryable<T>>>? sortMethod = null;

        switch (sortDirection)
        {
            case ListSortDirection.Ascending when query.Expression.Type == typeof(IOrderedQueryable<T>):
                sortMethod = () => ((IOrderedQueryable<T>)query).ThenBy<T, object?>(k => null);
                break;
            case ListSortDirection.Ascending:
                sortMethod = () => query.OrderBy<T, object?>(k => null);
                break;
            case ListSortDirection.Descending when query.Expression.Type == typeof(IOrderedQueryable<T>):
                sortMethod = () => ((IOrderedQueryable<T>)query).ThenByDescending<T, object?>(k => null);
                break;
            case ListSortDirection.Descending:
                sortMethod = () => query.OrderByDescending<T, object?>(k => null);
                break;
        }

        var methodCallExpression = sortMethod?.Body as MethodCallExpression;
        if (methodCallExpression == null)
        {
            throw new Exception("MethodCallExpression null");
        }

        var method = methodCallExpression.Method.GetGenericMethodDefinition();
        var genericSortMethod = method.MakeGenericMethod(typeof(T), prop.Type);

        return ((IOrderedQueryable<T>)genericSortMethod.Invoke(query, new object[] { query, sortLambda }) !) !;
    }

    private IQueryable<T> BuildFilterQuery(IQueryable<T> queryable)
    {
        if (filterQuery is not null)
        {
            queryable = filterQuery(queryable);
        }

        return queryable;
    }

    private IQueryable<T> BuildFilterSortQuery(IQueryable<T> queryable)
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
}