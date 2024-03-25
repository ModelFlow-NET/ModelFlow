namespace DataGridAsyncDemoMVVM
{
    using System.Collections.Generic;
    using System.Windows.Input;
    using Avalonia.Controls;
    using Avalonia.Controls.Models.TreeDataGrid;
    using ViewModels;
    using VitalElement.DataVirtualization;
    using VitalElement.DataVirtualization.Pageing;


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

        public IReadOnlyCollection<RemoteOrDbDataItem> Items { get; }
        
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