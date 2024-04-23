namespace VirtualizingCollection.Tests
{
    using System.ComponentModel;
    using System.Reactive.Linq;
    using DataGridAsyncDemoMVVM;
    using FluentAssertions;
    using ReactiveUI;
    using VitalElement.DataVirtualization;

    public class VirtualizingObservableCollectionTest
    {
        private readonly RemoteOrDbDataSource _dataSource;

        public VirtualizingObservableCollectionTest()
        {
            VirtualizationManager.Instance.UiThreadExcecuteAction = UiThreadExcecuteAction;

            _dataSource = new();
        }

        private Task UiThreadExcecuteAction(Action arg)
        {
            arg();
            return Task.CompletedTask;
        }

        [Fact]
        public async Task _Count_100()
        {
            // Trigger datasource loading.
            Assert.Equal(0, _dataSource.Collection.Count);

            await Task.Delay(1000);

            Assert.Equal(1025, _dataSource.Collection.Count);

            var item = _dataSource.Collection[^1];

            Assert.True(item.IsLoading);

            await Observable.FromEventPattern<PropertyChangedEventArgs>(item, nameof(item.PropertyChanged))
                .Do(x => { })
                .FirstAsync();

            Assert.False(item.IsLoading);

            await _dataSource.DeleteAsync(item);

            Assert.Equal(1024, _dataSource.Collection.Count);

            Assert.DoesNotContain(item, _dataSource.Collection);
        }

        [Fact]
        public async Task Item_Is_Set_When_DataItem_IsLoading_Changes()
        {
            // Trigger datasource loading.
            Assert.Equal(0, _dataSource.Collection.Count);

            await Task.Delay(1000);

            Assert.Equal(1025, _dataSource.Collection.Count);

            var item = _dataSource.Collection[^1];
            Assert.True(item.IsLoading);

            using var monitoredItem = item.Monitor();
            
            item.WhenAnyValue(x => x.IsLoading)
                .Skip(1)
                .Do(x =>
                {
                    Assert.False(item.IsLoading);
                    Assert.Same(_dataSource.Emulation.Items[^1], item.Item.Model);
                })
                .Subscribe();
            
            item.WhenAnyValue(x => x.Item)
                .Skip(1)
                .Do(x =>
                {
                    Assert.False(item.IsLoading);
                    Assert.Same(_dataSource.Emulation.Items[^1], item.Item.Model);
                })
                .Subscribe();

            await Task.Delay(500);

            monitoredItem.Should().RaisePropertyChangeFor(x => x.IsLoading);
            monitoredItem.Should().RaisePropertyChangeFor(x => x.Item);

            Assert.False(item.IsLoading);
            Assert.Same(_dataSource.Emulation.Items[^1], item.Item);
        }
    }
}