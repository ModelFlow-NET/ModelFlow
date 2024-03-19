namespace DataGridAsyncDemoMVVM
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Avalonia.Controls;
    using Avalonia.Controls.Models.TreeDataGrid;
    using ViewModels;
    using VitalElement.DataVirtualization;
    using VitalElement.DataVirtualization.DataManagement;
    using VitalElement.DataVirtualization.Pageing;


    public class RemoteOrDbDataSource : DataSource<RemoteOrDbDataItem, RemoteOrDbDataItem>
    {
        private readonly RemoteOrDbDataSourceEmulation _remoteDatas;

        private readonly Random _rand = new Random();
        
        public RemoteOrDbDataSource() : base (x=>x, 100, 5)
        {
            _remoteDatas = new RemoteOrDbDataSourceEmulation(100000);
        }

        protected override void OnReset(int count)
        {
            // Do nothing.
        }

        protected override Task<bool> ContainsAsync(RemoteOrDbDataItem item)
        {
            throw new NotImplementedException();
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
    
    internal class MainViewModel
    {
        public MainViewModel()
        {
            var dataSource = new RemoteOrDbDataSource();

            Items = dataSource.Collection;

            var source = new FlatTreeDataGridSource<RemoteOrDbDataItem>(dataSource.Collection);
            
            source.Columns.Add(new TextColumn<RemoteOrDbDataItem, string>(new NameHeaderViewModel(dataSource, x=>x.Name), x => x.Name, options: new TextColumnOptions<RemoteOrDbDataItem>
            {
                CanUserSortColumn = false
            }));
            source.Columns.Add(new TextColumn<RemoteOrDbDataItem, string>("String 1", x => x.Str1, options: new TextColumnOptions<RemoteOrDbDataItem>
            {
                CanUserSortColumn = false
            }));
            source.Columns.Add(new TextColumn<RemoteOrDbDataItem, string>("String 2", x => x.Str2, options: new TextColumnOptions<RemoteOrDbDataItem>
            {
                CanUserSortColumn = false
            }));
            
            ItemSource = source;
        }

        public VirtualizingObservableCollection<RemoteOrDbDataItem> Items { get; }
        
        public ITreeDataGridSource ItemSource { get; }

        public ICommand FilterCommand { get; }

        //public ICollectionView MyDataVirtualizedAsyncFilterSortObservableCollectionCollectionView { get; }

        public ICommand SortCommand { get; }

        /*private async Task Filter(MemberPathFilterText memberPathFilterText)
        {
            if (string.IsNullOrWhiteSpace(memberPathFilterText.FilterText))
                this._myRemoteOrDbDataSourceAsyncProxy.FilterDescriptionList.Remove(memberPathFilterText
                    .MemberPath);
            else
                this._myRemoteOrDbDataSourceAsyncProxy.FilterDescriptionList.Add(
                    new FilterDescription(memberPathFilterText.MemberPath, memberPathFilterText.FilterText));
            Interlocked.Increment(ref this._filterWaitingCount);
            await Task.Delay(500);
            if (Interlocked.Decrement(ref this._filterWaitingCount) != 0) return;
            this._myRemoteOrDbDataSourceAsyncProxy.FilterDescriptionList.OnCollectionReset();
            this._myDataVirtualizedAsyncFilterSortObservableCollection.Clear();
        }

        private async Task Sort(MemberPathSortingDirection memberPathSortingDirection)
        {
            while (this._filterWaitingCount != 0)
                await Task.Delay(500);
            var sortDirection = memberPathSortingDirection.SortDirection;
            var sortMemberPath = memberPathSortingDirection.MemberPath;
            switch (sortDirection)
            {
                case null:
                    this._myRemoteOrDbDataSourceAsyncProxy.SortDescriptionList.Remove(sortMemberPath);
                    break;
                case ListSortDirection.Ascending:
                    this._myRemoteOrDbDataSourceAsyncProxy.SortDescriptionList.Add(
                        new SortDescription(sortMemberPath, ListSortDirection.Ascending));
                    break;
                case ListSortDirection.Descending:
                    this._myRemoteOrDbDataSourceAsyncProxy.SortDescriptionList.Add(
                        new SortDescription(sortMemberPath, ListSortDirection.Descending));
                    break;
            }

            this._myRemoteOrDbDataSourceAsyncProxy.FilterDescriptionList.OnCollectionReset();
            this._myDataVirtualizedAsyncFilterSortObservableCollection.Clear();
        }*/
    }
}