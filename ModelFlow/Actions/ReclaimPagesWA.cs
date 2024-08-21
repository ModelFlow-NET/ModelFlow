namespace ModelFlow.DataVirtualization.Actions
{
    using System;
    using Interfaces;

    internal class ReclaimPagesWA : BaseRepeatableActionVirtualization
    {
        readonly string _sectionContext = "";

        readonly WeakReference _wrProvider;

        public ReclaimPagesWA(IReclaimableService provider, string sectionContext)
            : base(VirtualActionThreadModelEnum.Background, true, TimeSpan.FromMinutes(1))
        {
            _wrProvider = new WeakReference(provider);
        }

        public override void DoAction()
        {
            LastRun = DateTime.Now;

            var reclaimer = _wrProvider.Target as IReclaimableService;

            if (reclaimer != null)
            {
                reclaimer.RunClaim(_sectionContext);
            }
        }

        public override bool KeepInActionsList()
        {
            var ret = base.KeepInActionsList();

            if (!_wrProvider.IsAlive) ret = false;

            return ret;
        }
    }
}