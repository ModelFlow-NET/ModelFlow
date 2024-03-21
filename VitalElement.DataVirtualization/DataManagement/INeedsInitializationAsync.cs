namespace VitalElement.DataVirtualization.DataManagement;

using System.Threading.Tasks;

public interface INeedsInitializationAsync
{
    Task InitializeAsync();
}