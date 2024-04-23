namespace VirtualizingCollection.Tests
{
    public class TestItem
    {
        private static int _root = -1;
        public int Index { get; set; } = Interlocked.Increment(ref _root);
    }

    public class TestItemViewModel
    {
        private readonly TestItem _model;
        
        public TestItemViewModel(TestItem model)
        {
            _model = model;
        }

        public int Index => _model.Index;
    }
}