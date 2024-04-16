namespace VitalElement.DataVirtualization.DataManagement;

using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Extensions;

internal static class DataSourceSyncManager
{
    public static IDisposable AutoManage<TViewModel, TModel>(
        this DataSource<TViewModel, TModel> dataSource,
        IAutoSynchronize viewModel)
        where TViewModel : class
    {
        return new DataSourceSyncWrapper<TViewModel, TModel>(dataSource, viewModel);
    }

    private class DataSourceSyncWrapper<TViewModel, TModel>
        : IDisposable
        where TViewModel : class
    {
        private readonly DataSource<TViewModel, TModel> _dataSource;
        private readonly IAutoSynchronize _viewModel;
        private readonly CompositeDisposable _subscriptions;
        private readonly Subject<Unit> _propertyChangedSubject;

        public DataSourceSyncWrapper(
            DataSource<TViewModel, TModel> dataSource,
            IAutoSynchronize viewModel)
        {
            if (viewModel.IsManaged)
            {
                throw new Exception(
                    $"Viewmodel: {viewModel.GetType()} is already wrapped. Cannot subscribe for database synchronisation more than once.");
            }

            _dataSource = dataSource;
            _viewModel = viewModel;

            viewModel.IsManaged = true;

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

            _viewModel.OnDeleteRequested
                .Do(OnDelete)
                .Subscribe()
                .DisposeWith(_subscriptions);
        }

        void IDisposable.Dispose()
        {
            _viewModel.IsManaged = false;
            _subscriptions.Dispose();
        }

        private async void OnUpdate(Unit unit)
        {
            if (_viewModel is TViewModel vm)
            {
                await _dataSource.UpdateAsync(vm);
            }
        }
        
        private async void OnDelete(Unit unit)
        {
            if (_viewModel is TViewModel vm)
            {
                await _dataSource.DeleteAsync(vm);
            }
        }

        private void OnViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
        {
            _propertyChangedSubject.OnNext(Unit.Default);
        }
    }
}