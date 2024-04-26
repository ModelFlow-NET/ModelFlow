namespace VitalElement.DataVirtualization.DataManagement;

using System.ComponentModel;

public interface IAutoSynchronize : INotifyPropertyChanged
{
    bool CanSave { get; set; }
    
    IDataManager? DataManager { get; set; }
    
    bool IsManaged { get; set; }
}