namespace ModelFlow.DataVirtualization.DataManagement;

using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Extensions;

internal static class DataSourceSyncManager
{
    public static IDisposable AutoManage<TViewModel, TModel>(
        this DataSource<TViewModel, TModel> dataSource,
        DataItem<TViewModel> item)
        where TViewModel : class
    {
        return new DataSourceSyncWrapper<TViewModel, TModel>(dataSource, item);
    }

    private class DataSourceSyncWrapper<TViewModel, TModel>
        : IDisposable, IDataManager
        where TViewModel : class
    {
        private readonly DataSource<TViewModel, TModel> _dataSource;
        private readonly DataItem<TViewModel> _item;
        private readonly IAutoSynchronize _viewModel;
        private readonly CompositeDisposable _subscriptions;
        private readonly Subject<Unit> _propertyChangedSubject;

        public DataSourceSyncWrapper(
            DataSource<TViewModel, TModel> dataSource,
            DataItem<TViewModel> item)
        {
            if (item.Item is not IAutoSynchronize viewModel)
                throw new NotSupportedException("Your viewmodel must implement IAutoSynchronize");

            if (viewModel.IsManaged)
            {
                throw new Exception(
                    $"Viewmodel: {viewModel.GetType()} is already wrapped. Cannot subscribe for database synchronisation more than once.");
            }

            _dataSource = dataSource;
            _item = item;
            _viewModel = viewModel;

            viewModel.IsManaged = true;
            viewModel.DataManager = this;

            _subscriptions = new CompositeDisposable();

            viewModel.PropertyChanged += OnViewModelOnPropertyChanged;

            _subscriptions.Add(Disposable.Create(() => viewModel.PropertyChanged -= OnViewModelOnPropertyChanged));
            
            _propertyChangedSubject = new Subject<Unit>();

            var uiThreadScheduler = VirtualizationManager.UiThreadScheduler ?? Scheduler.CurrentThread;
            
            IObservable<Unit> observeChanges = _propertyChangedSubject;

            if (VirtualizationManager.PropertySyncThrottleTime > TimeSpan.Zero)
            {
                observeChanges = observeChanges.Throttle(VirtualizationManager.PropertySyncThrottleTime)
                    .ObserveOn(uiThreadScheduler);
            }

            observeChanges.Do(OnUpdate)
                .Subscribe()
                .DisposeWith(_subscriptions);
        }

        void IDisposable.Dispose()
        {
            _viewModel.IsManaged = false;
            _subscriptions.Dispose();
        }
        
        public Task SaveAsync()
        {
            return _dataSource.UpdateAsync(_item.Item);
        }

        public async Task DeleteAsync()
        {
            await _dataSource.DeleteAsync(_item);
        }

        private async void OnUpdate(Unit unit)
        {
            if (_viewModel.CanSave)
            {
                await _dataSource.UpdateAsync(_item.Item);
            }
        }

        private void OnViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
        {
            _propertyChangedSubject.OnNext(Unit.Default);
        }
    }
}