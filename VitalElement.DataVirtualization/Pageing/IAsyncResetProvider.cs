namespace VitalElement.VirtualizingCollection.Pageing
{
    using System.Threading.Tasks;

    public interface IAsyncResetProvider
    {
        Task<int> GetCountAsync();
    }
}