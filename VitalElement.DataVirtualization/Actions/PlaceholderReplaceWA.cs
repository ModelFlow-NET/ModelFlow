namespace VitalElement.DataVirtualization.Actions
{
    using DataManagement;

    internal struct PlaceholderReplaceWA
    {
        private DataItem _newValue;
        private DataItem _oldValue;

        public PlaceholderReplaceWA(DataItem oldValue, DataItem newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public void Execute()
        {
            _oldValue.ItemObject = _newValue.ItemObject;
            _oldValue.IsLoading = false;
        }
    }
}