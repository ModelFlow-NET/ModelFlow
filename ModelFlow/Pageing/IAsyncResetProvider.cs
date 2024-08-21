namespace ModelFlow.DataVirtualization.Pageing
{
    using System.Threading.Tasks;

    internal interface IAsyncResetProvider
    {
        Task<int> GetCountAsync();
    }
}