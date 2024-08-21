namespace ModelFlow.DataVirtualization.Interfaces
{
    internal interface IReclaimableService
    {
        void RunClaim(string sectionContext);
    }
}