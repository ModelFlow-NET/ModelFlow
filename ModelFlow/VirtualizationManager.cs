namespace ModelFlow.DataVirtualization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Threading.Tasks;
    using Actions;
    using Interfaces;

    public class VirtualizationManager
    {
        private readonly object _actionLock = new object();

        private readonly List<IVirtualizationAction> _actions = new List<IVirtualizationAction>();

        private bool _processing;

        private Func<Action, Task>? _uiThreadExcecuteAction;

        public static VirtualizationManager Instance { get; } = new VirtualizationManager();

        public static bool IsInitialized { get; private set; }
        
        public static IScheduler? UiThreadScheduler { get; set; }

        public static TimeSpan PropertySyncThrottleTime { get; set; } = TimeSpan.FromMilliseconds(400);

        public Func<Action, Task>? UiThreadExcecuteAction
        {
            get => _uiThreadExcecuteAction;
            set
            {
                _uiThreadExcecuteAction = value;
                IsInitialized = true;
            }
        }

        internal void AddAction(IVirtualizationAction action)
        {
            lock (_actionLock)
            {
                _actions.Add(action);
            }
        }

        internal void AddAction(Action action)
        {
            AddAction(new ActionVirtualizationWrapper(action));
        }

        public void ProcessActions()
        {
            if (_processing) return;

            _processing = true;

            List<IVirtualizationAction> lst;
            lock (_actionLock)
            {
                lst = _actions.ToList();
            }

            foreach (var action in lst)
            {
                var bdo = true;

                if (action is IRepeatingVirtualizationAction)
                {
                    bdo = (action as IRepeatingVirtualizationAction).IsDueToRun();
                }

                if (!bdo) continue;
                switch (action.ThreadModel)
                {
                    case VirtualActionThreadModelEnum.UseUIThread:
                        if (UiThreadExcecuteAction == null) // PLV
                            throw new Exception(
                                "VirtualizationManager isn’t already initialized !  set the VirtualizationManager’s UIThreadExcecuteAction (VirtualizationManager.Instance.UIThreadExcecuteAction = a => Dispatcher.Invoke( a );)");
                        UiThreadExcecuteAction.Invoke(() => action.DoAction());
                        break;
                    case VirtualActionThreadModelEnum.Background:
                        Task.Run(() => action.DoAction()).ConfigureAwait(false);
                        break;
                    default:
                        break;
                }

                if (action is IRepeatingVirtualizationAction)
                {
                    if ((action as IRepeatingVirtualizationAction).KeepInActionsList()) continue;
                    lock (_actionLock)
                    {
                        _actions.Remove(action);
                    }
                }
                else
                {
                    lock (_actionLock)
                    {
                        _actions.Remove(action);
                    }
                }
            }

            _processing = false;
        }

        private void RunOnUi(IVirtualizationAction action)
        {
            if (UiThreadExcecuteAction == null) // PLV
                throw new Exception(
                    "VirtualizationManager isn’t already initialized !  set the VirtualizationManager’s UIThreadExcecuteAction (VirtualizationManager.Instance.UIThreadExcecuteAction = a => Dispatcher.Invoke( a );)");
            UiThreadExcecuteAction.Invoke(action.DoAction);
        }

        internal void RunOnUi(Action action)
        {
            RunOnUi(new ActionVirtualizationWrapper(action));
        }
        
        internal async Task RunOnUiAsync(IVirtualizationAction action)
        {
            if (UiThreadExcecuteAction == null) // PLV
                throw new Exception(
                    "VirtualizationManager isn’t already initialized !  set the VirtualizationManager’s UIThreadExcecuteAction (VirtualizationManager.Instance.UIThreadExcecuteAction = a => Dispatcher.Invoke( a );)");
            
            await UiThreadExcecuteAction.Invoke(action.DoAction);
        }
        
    }
}