using Avalonia.Controls;

namespace DataGridAsyncDemoMVVM.Views;

using System;
using Avalonia.Threading;
using VitalElement.DataVirtualization;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        if (!VirtualizationManager.IsInitialized)
        {
            //set the VirtualizationManager’s UIThreadExcecuteAction. In this case
            //we’re using Dispatcher.Invoke to give the VirtualizationManager access
            //to the dispatcher thread, and using a DispatcherTimer to run the background
            //operations the VirtualizationManager needs to run to reclaim pages and manage memory.
            VirtualizationManager.Instance.UiThreadExcecuteAction = a => Dispatcher.UIThread.Post	(a);

            DispatcherTimer.Run(() =>
            {
                 VirtualizationManager.Instance.ProcessActions();
                 return true;
            }, TimeSpan.FromMilliseconds(10), DispatcherPriority.Background	);
        }
        
        InitializeComponent();
        
        
    }
}