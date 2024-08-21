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
    using VitalElement.DataVirtualization.DataManagement;
    using VitalElement.DataVirtualization.Extensions;


    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private DataItem<RemoteItemViewModel>? _selectedItem;

        [ObservableProperty]
        private int _randomIndex;

        public MainViewModel()
        {
            var dataSource = new RemoteOrDbDataSource();

            Items = dataSource.Collection;

            var source = new FlatTreeDataGridSource<DataItem<RemoteItemViewModel>>(dataSource.Collection);

            var dsource = dataSource.Collection.GetDataSource();
            
            source.Columns.Add(new TextColumn<DataItem<RemoteItemViewModel>, string>(new NameHeaderViewModel(dataSource, x=>x.Name), x => x.Item.Name, options: new TextColumnOptions<DataItem<RemoteItemViewModel>>
            {
                CanUserSortColumn = false
            }));
            source.AddAutoColumn("String1", x => x.Item.Str1);/*
            source.Columns.Add(new TextColumn<DataItem<RemoteItemViewModel>, string>("String 1", x => x.Item.Str1, options: new TextColumnOptions<DataItem<RemoteItemViewModel>>
            {
                CanUserSortColumn = false
            }));*/
            source.Columns.Add(new TextColumn<DataItem<RemoteItemViewModel>, string>("String 2", x => x.Item.Str2, options: new TextColumnOptions<DataItem<RemoteItemViewModel>>
            {
                CanUserSortColumn = false
            }));
            
            ItemSource = source;

            SelectRandomCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var rand = new Random((int)DateTime.Now.Ticks);

                var index = rand.Next(0, dataSource.Emulation.Items.Count);

                RandomIndex = index;

                if (await dataSource.GetViewModelAsync(x => x.Int1 == RandomIndex) is { } item)
                {
                    SelectedItem = DataItem.Create(item);
                }
            });

            CreateCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var i = dataSource.Emulation.Items.Count;
                var (success, index, item) = await dataSource.CreateAsync(new RemoteItemViewModel(new RemoteOrDbDataItem(i,
                    "Name_" + i.ToString("00000000000"), "Str1_" + i, "Str1_" + i, i, i)));

                if (success)
                {
                    SelectedItem = item;
                }
            });
            
            Dispatcher.UIThread.Post(async () =>
            {
                await dataSource.EnsureInitialisedAsync();
                if (await dataSource.GetViewModelAsync(x => x.Int1 == 5) is { } item)
                {
                    SelectedItem = DataItem.Create(item);
                }
            });
        }

        partial void OnSelectedItemChanged(DataItem<RemoteItemViewModel>? value)
        {
            if (value != null)
            {;
                RandomIndex = value.Item.Int1;
            }
            else
            {
                RandomIndex = -1;
            }
        }

        public IReadOnlyCollection<DataItem<RemoteItemViewModel>> Items { get; }
        
        public ITreeDataGridSource ItemSource { get; }
        
        public ICommand SelectRandomCommand { get; }
        
        public ICommand CreateCommand { get; }
    }
}