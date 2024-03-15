namespace DataGridAsyncDemoMVVM
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Avalonia.Controls;
    using Avalonia.Controls.Models.TreeDataGrid;
    using ViewModels;
    using System.Linq.Dynamic.Core;
    using VitalElement.DataVirtualization;
    using VitalElement.DataVirtualization.Management;
    using VitalElement.DataVirtualization.Pageing;


    public class RemoteOrDbDataSource : DataSource<RemoteOrDbDataItem>
    {
        private readonly RemoteOrDbDataSourceEmulation _remoteDatas;

        private readonly Random _rand = new Random();
        
        public IQueryable<TSource> FilterAndSort<TSource>(IQueryable<TSource> source)
        {
            var result = source;
            
            if (!string.IsNullOrWhiteSpace(FilterQuery))
            {
                result = source.Where(FilterQuery);
            }

            if (!string.IsNullOrWhiteSpace(SortQuery))
            {
                result = result.OrderBy(SortQuery);
            }

            return result;
        }
        
        public RemoteOrDbDataSource()
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

        protected override Task<int> GetCountAsync()
        {
            return Task.Run(() =>
            {
                Task.Delay(20 + (int) Math.Round(_rand.NextDouble() * 30)).Wait(); // Just to slow it down !
                return FilterAndSort(_remoteDatas.Items.AsQueryable()).Count();
            });
        }

        protected override Task<PagedSourceItemsPacket<RemoteOrDbDataItem>> GetItemsAtAsync(int offset, int count)
        {
            return Task.Run(() =>
            {
                Task.Delay(50 + (int) Math.Round(_rand.NextDouble() * 100)).Wait(); // Just to slow it down !
                return new PagedSourceItemsPacket<RemoteOrDbDataItem>
                {
                    LoadedAt = DateTime.Now,
                    Items = (from items in FilterAndSort(_remoteDatas.Items.AsQueryable()) select items).Skip(offset).Take(count)
                };
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
            
            source.Columns.Add(new TextColumn<RemoteOrDbDataItem, string>(new NameHeaderViewModel(dataSource, "Name"), x => x.Name, options: new TextColumnOptions<RemoteOrDbDataItem>
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