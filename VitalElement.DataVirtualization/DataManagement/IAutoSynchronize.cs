namespace VitalElement.DataVirtualization.DataManagement;

using System;
using System.ComponentModel;
using System.Reactive;

public interface IAutoSynchronize : INotifyPropertyChanged
{
    bool IsManaged { get; set; }
    
    IObservable<Unit> OnDeleteRequested { get; }
}