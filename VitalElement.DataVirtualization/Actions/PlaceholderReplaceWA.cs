namespace VitalElement.DataVirtualization.Actions
{
    using DataManagement;

    internal struct PlaceholderReplaceWA
    {
        private IDataItem _newValue;
        private IMutateDataItem _oldValue;

        public PlaceholderReplaceWA(IMutateDataItem oldValue, IDataItem newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public void Execute()
        {
            _oldValue.SetIsLoading(false);
            _oldValue.SetItem(_newValue.Item);
        }
    }
}