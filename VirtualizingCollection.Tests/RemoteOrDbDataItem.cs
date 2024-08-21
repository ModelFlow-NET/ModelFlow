namespace DataGridAsyncDemoMVVM
{
    using System;
    using System.Reactive;
    using System.Reactive.Subjects;
    using System.Windows.Input;
    using ReactiveUI;
    using ViewModels;
    using VitalElement.DataVirtualization.DataManagement;

    public class RemoteItemViewModel : ViewModelBase, IAutoSynchronize, INeedsInitializationAsync
    {
        private readonly RemoteOrDbDataItem _model;
        
        public RemoteItemViewModel(RemoteOrDbDataItem model)
        {
            _model = model;
            DeleteCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (DataManager != null)
                {
                    await DataManager.DeleteAsync();
                }
            });
        }

        public RemoteOrDbDataItem Model => _model;

        public int Id => Model.Id;
        public double Double1 => Model.Double1;
        public int Int1 => Model.Int1;
        public string Name => Model.Name;
        public string Str1 => Model.Str1;
        public string Str2 => Model.Str2;

        public bool CanSave { get; set; }
        
        public IDataManager? DataManager { get; set; }
        
        public bool IsManaged { get; set; }
        
        public ICommand DeleteCommand { get; }
        
        public Task InitializeAsync()
        {
            OnInitialized?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public event EventHandler? OnInitialized;
    }
    
    public class RemoteOrDbDataItem
    {
        public RemoteOrDbDataItem(int id, string name, string str1, string str2, int int1, double double1)
        {
            
            Id = id;
            Name = name;
            Str1 = str1;
            Str2 = str2;
            Int1 = int1;
            Double1 = double1;
        }
        
        public int Id { get; }
        public double Double1 { get; set; }
        public int Int1 { get; set; }
        public string Name { get; set; }
        public string Str1 { get; set; }
        public string Str2 { get; set; }
    }
}