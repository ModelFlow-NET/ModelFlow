namespace DataGridAsyncDemoMVVM;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DynamicData;
using VitalElement.DataVirtualization.DataManagement;
using VitalElement.DataVirtualization.Extensions;

public class RemoteOrDbDataSource : DataSource<RemoteItemViewModel, RemoteOrDbDataItem>
{
    private readonly IQueryable<RemoteOrDbDataItem> _remoteDatas;

    private readonly Random _rand = new();
    
    public RemoteOrDbDataSourceEmulation Emulation { get; }
        
    public RemoteOrDbDataSource() : base (x=> new RemoteItemViewModel(x), 50, 4)
    {
        this.AddSortDescription(x => x.Id, ListSortDirection.Ascending);
        
        Emulation = new RemoteOrDbDataSourceEmulation(1025);

        _remoteDatas = Emulation.Items.AsQueryable();
    }

    protected override Task<int> IndexOfAsync(RemoteItemViewModel item, Func<IQueryable<RemoteOrDbDataItem>, IQueryable<RemoteOrDbDataItem>> filterSortQuery)
    {
        return Task.FromResult(Emulation.Items.IndexOf(item.Model));
    }

    protected override Task<bool> DoCreateAsync(RemoteItemViewModel item)
    {
        Emulation.Items.Add(item.Model);

        return Task.FromResult(true);
    }

    protected override Task<bool> DoUpdateAsync(RemoteItemViewModel viewModel)
    {
        throw new NotImplementedException();
    }

    protected override Task<bool> DoDeleteAsync(RemoteItemViewModel item)
    {
        return Task.FromResult(Emulation.Items.Remove(item.Model));
    }

    protected override void OnReset(int count)
    {
        // Do nothing.
    }

    protected override Task<bool> ContainsAsync(RemoteItemViewModel item)
    {
        throw new NotImplementedException();
    }

    public override Task<RemoteOrDbDataItem?> GetItemAsync(Expression<Func<RemoteOrDbDataItem, bool>> predicate)
    {
        return _remoteDatas.GetRowAsync(predicate);
    }

    protected override async Task<int> GetCountAsync(Func<IQueryable<RemoteOrDbDataItem>, IQueryable<RemoteOrDbDataItem>> filterQuery)
    {
        //await Task.Delay(1000 + (int)Math.Round(_rand.NextDouble() * 30));

        return await _remoteDatas.GetRowCountAsync(filterQuery);
    }

    protected override async Task<IEnumerable<RemoteOrDbDataItem>> GetItemsAtAsync(int offset, int count, Func<IQueryable<RemoteOrDbDataItem>, IQueryable<RemoteOrDbDataItem>> filterSortQuery)
    {
        if (count > 5)
        {
            await Task.Delay(1500 + (int)Math.Round(_rand.NextDouble() * 100));
        }

        return await _remoteDatas.GetRowsAsync(offset, count, filterSortQuery);
    }

    protected override RemoteItemViewModel? GetPlaceHolder(int index, int page, int offset)
    {
        return new RemoteItemViewModel(new RemoteOrDbDataItem(-1, "", "loading...", "", index, offset));
    }

    protected override bool ModelsEqual(RemoteOrDbDataItem a, RemoteOrDbDataItem b)
    {
        return a.Id == b.Id;
    }

    protected override RemoteOrDbDataItem? GetModelForViewModel(RemoteItemViewModel viewModel)
    {
        return viewModel.Model;
    }
}