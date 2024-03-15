namespace DataGridAsyncDemoMVVM.ViewModels;

using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;
using VitalElement.DataVirtualization.Management;

public partial class NameHeaderViewModel : ViewModelBase
{
    private readonly RemoteOrDbDataSource _dataSource;
    private ListSortDirection _sortDirection;

    [ObservableProperty]
    private string filter;

    public NameHeaderViewModel(RemoteOrDbDataSource dataSource, string propertyName)
    {
        _dataSource = dataSource;

        this.ToggleSortCommand = ReactiveCommand.Create(() =>
        {
            _sortDirection = _sortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;

            dataSource.SortDescriptionList.Add(new SortDescription(propertyName, _sortDirection));
        });
        
        this.WhenAnyValue(x => x.Filter)
            .Throttle(TimeSpan.FromMilliseconds(400))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(x =>
            {
                dataSource.FilterDescriptionList.Add(new FilterDescription(propertyName, x));
            })
            .Subscribe();
    }
    
    public ICommand ToggleSortCommand { get; }
}