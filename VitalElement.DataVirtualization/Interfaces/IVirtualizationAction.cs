namespace VitalElement.VirtualizingCollection.Interfaces
{
    using Actions;

    public interface IVirtualizationAction
    {
        VirtualActionThreadModelEnum ThreadModel { get; }

        void DoAction();
    }
}