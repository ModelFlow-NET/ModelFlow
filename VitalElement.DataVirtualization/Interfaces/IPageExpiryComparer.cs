namespace VitalElement.VirtualizingCollection.Interfaces
{
    public interface IPageExpiryComparer
    {
        bool IsUpdateValid(object pageUpdateAt, object updateAt);
    }
}