namespace VitalElement.DataVirtualization.Actions
{
    using DataManagement;

    public struct PlaceholderReplaceWA
    {
        private IDataItem _newValue;
        private IDataItem _oldValue;

        public PlaceholderReplaceWA(IDataItem oldValue, IDataItem newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public void Execute()
        {
            _oldValue.Item = _newValue.Item;
        }
    }
}