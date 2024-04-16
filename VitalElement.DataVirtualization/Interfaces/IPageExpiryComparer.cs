namespace VitalElement.DataVirtualization.Interfaces
{
    internal interface IPageExpiryComparer
    {
        bool IsUpdateValid(object pageUpdateAt, object updateAt);
    }
}