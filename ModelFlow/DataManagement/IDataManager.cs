namespace ModelFlow.DataVirtualization.DataManagement;

using System.Threading.Tasks;

public interface IDataManager
{
    Task SaveAsync();

    Task DeleteAsync();
}