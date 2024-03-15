namespace VirtualizingCollection.Tests
{
    using VitalElement.VirtualizingCollection;

    public class VirtualizingObservableCollectionTest
    {
        private readonly VirtualizingObservableCollection<TestItem> _vc;

        public VirtualizingObservableCollectionTest()
        {
            VirtualizationManager.Instance.UiThreadExcecuteAction = action => action();

            _vc = new VirtualizingObservableCollection<TestItem>(
                new ItemSourceProvider<TestItem>(Enumerable.Range(0, 100).Select(i => new TestItem())));
        }

        [Fact]
        public void _Count_100()
        {
            Assert.Equal(100, _vc.Count);
        }

        [Fact]
        public void _GetEnumerator_()
        {
            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(i, _vc[i].Index);
            }
        }
    }
}