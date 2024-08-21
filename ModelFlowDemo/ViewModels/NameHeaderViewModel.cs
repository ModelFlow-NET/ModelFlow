namespace DataGridAsyncDemoMVVM.ViewModels;

using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Windows.Input;
using ReactiveUI;
using ModelFlow.DataVirtualization.Extensions;

public partial class NameHeaderViewModel : ViewModelBase
{
    private ListSortDirection _sortDirection;

    public NameHeaderViewModel(RemoteOrDbDataSource dataSource, Expression<Func<RemoteOrDbDataItem, string>> property)
    {
        ToggleSortCommand = ReactiveCommand.Create(() =>
        {
            _sortDirection = _sortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;

            dataSource.AddSortDescription(property, _sortDirection);
        });
    }
    
    public ICommand ToggleSortCommand { get; }
}