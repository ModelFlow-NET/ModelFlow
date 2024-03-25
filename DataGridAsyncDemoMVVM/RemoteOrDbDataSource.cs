namespace DataGridAsyncDemoMVVM;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VitalElement.DataVirtualization.DataManagement;

public class RemoteOrDbDataSource : DataSource<RemoteOrDbDataItem, RemoteOrDbDataItem>
{
    private readonly RemoteOrDbDataSourceEmulation _remoteDatas;

    private readonly Random _rand = new Random();
        
    public RemoteOrDbDataSource() : base (x=>x, 100, 5)
    {
        _remoteDatas = new RemoteOrDbDataSourceEmulation(100000);
    }

    protected override void OnMaterialized(RemoteOrDbDataItem item)
    {
        // do nothing.
    }

    protected override Task<bool> DoCreateAsync(RemoteOrDbDataItem item)
    {
        throw new NotImplementedException();
    }

    protected override Task<bool> DoUpdateAsync(RemoteOrDbDataItem viewModel)
    {
        throw new NotImplementedException();
    }

    protected override Task<bool> DoDeleteAsync(RemoteOrDbDataItem item)
    {
        throw new NotImplementedException();
    }

    protected override void OnReset(int count)
    {
        // Do nothing.
    }

    protected override Task<bool> ContainsAsync(RemoteOrDbDataItem item)
    {
        throw new NotImplementedException();
    }

    public override Task<RemoteOrDbDataItem?> GetItemAsync(Expression<Func<RemoteOrDbDataItem, bool>> predicate)
    {
        return Task.FromResult((RemoteOrDbDataItem?)null);
    }

    protected override Task<int> GetCountAsync(Func<IQueryable<RemoteOrDbDataItem>, IQueryable<RemoteOrDbDataItem>> filterQuery)
    {
        return Task.Run(() =>
        {
            Task.Delay(1000 + (int) Math.Round(_rand.NextDouble() * 30)).Wait(); // Just to slow it down !
            return filterQuery(_remoteDatas.Items.AsQueryable()).Count();
        });
    }

    protected override Task<IEnumerable<RemoteOrDbDataItem>> GetItemsAtAsync(int offset, int count, Func<IQueryable<RemoteOrDbDataItem>, IQueryable<RemoteOrDbDataItem>> query)
    {
        return Task.Run(() =>
        {
            Task.Delay(1500 + (int) Math.Round(_rand.NextDouble() * 100)).Wait(); // Just to slow it down !
            return (from items in query(_remoteDatas.Items.AsQueryable()) select items).Skip(offset)
                .Take(count).AsEnumerable();
        });   
    }

    protected override RemoteOrDbDataItem GetPlaceHolder(int index, int page, int offset)
    {
        return new RemoteOrDbDataItem {Name = "Waiting [" + page + "/" + offset + "]"};
    }

    protected override Task<int> IndexOfAsync(RemoteOrDbDataItem item)
    {
        return Task.FromResult(-1);
    }
}