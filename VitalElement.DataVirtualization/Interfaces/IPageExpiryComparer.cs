namespace VitalElement.DataVirtualization.Interfaces
{
    public interface IPageExpiryComparer
    {
        bool IsUpdateValid(object pageUpdateAt, object updateAt);
    }
}