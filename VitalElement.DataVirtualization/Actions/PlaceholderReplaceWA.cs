namespace VitalElement.DataVirtualization.Actions
{
    using System;
    using DataManagement;
    using Pageing;

    public class PlaceholderReplaceWA<T> : BaseActionVirtualization where T : class
    {
        private readonly int _index;
        private readonly T _newValue;
        private readonly T _oldValue;

        readonly WeakReference _voc;

        public PlaceholderReplaceWA(VirtualizingObservableCollection<T> voc, T oldValue, T newValue, int index)
            : base(VirtualActionThreadModelEnum.UseUIThread)
        {
            _voc = new WeakReference(voc);
            _oldValue = oldValue;
            _newValue = newValue;
            _index = index;
        }

        public override void DoAction()
        {
            var voc = (VirtualizingObservableCollection<T>) _voc.Target;

            if (voc.Provider is PaginationManager<T> paginationManager && _newValue is IDataItem)
            {
                paginationManager.ProviderAsync.Replace(_oldValue, _newValue);
            }
            else
            {
                if (voc != null && _voc.IsAlive)
                {
                    voc.ReplaceAt(_index, _oldValue, _newValue, null);
                }
            }
        }
    }
}