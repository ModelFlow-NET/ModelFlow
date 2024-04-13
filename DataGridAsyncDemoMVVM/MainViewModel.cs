namespace DataGridAsyncDemoMVVM
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Input;
    using Avalonia.Controls;
    using Avalonia.Controls.Models.TreeDataGrid;
    using Avalonia.Threading;
    using CommunityToolkit.Mvvm.ComponentModel;
    using ReactiveUI;
    using ViewModels;
    using VitalElement.DataVirtualization;
    using VitalElement.DataVirtualization.DataManagement;
    using VitalElement.DataVirtualization.Pageing;


    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private DataItem<RemoteOrDbDataItem>? _selectedItem;

        [ObservableProperty]
        private int _randomIndex;

        public MainViewModel()
        {
            var dataSource = new RemoteOrDbDataSource();

            Items = dataSource.Collection;

            var source = new FlatTreeDataGridSource<DataItem<RemoteOrDbDataItem>>(dataSource.Collection);
            
            source.Columns.Add(new TextColumn<DataItem<RemoteOrDbDataItem>, string>(new NameHeaderViewModel(dataSource, x=>x.Name), x => x.Item.Name, options: new TextColumnOptions<DataItem<RemoteOrDbDataItem>>
            {
                CanUserSortColumn = false
            }));
            source.Columns.Add(new TextColumn<DataItem<RemoteOrDbDataItem>, string>("String 1", x => x.Item.Str1, options: new TextColumnOptions<DataItem<RemoteOrDbDataItem>>
            {
                CanUserSortColumn = false
            }));
            source.Columns.Add(new TextColumn<DataItem<RemoteOrDbDataItem>, string>("String 2", x => x.Item.Str2, options: new TextColumnOptions<DataItem<RemoteOrDbDataItem>>
            {
                CanUserSortColumn = false
            }));
            
            ItemSource = source;

            SelectRandomCommand = ReactiveCommand.Create(() =>
            {
                var rand = new Random((int)DateTime.Now.Ticks);

                var index = rand.Next(0, dataSource.Emulation.Items.Count);

                RandomIndex = index;

                SelectedItem = DataItem.Create(dataSource.Emulation.Items[index]);
            });
            
            Dispatcher.UIThread.Post(async () =>
            {
                SelectedItem = DataItem.Create(dataSource.Emulation.Items[500]); 
            });
        }

        public IReadOnlyCollection<DataItem<RemoteOrDbDataItem>> Items { get; }
        
        public ITreeDataGridSource ItemSource { get; }
        
        public ICommand SelectRandomCommand { get; }
    }
}