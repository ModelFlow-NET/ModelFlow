﻿namespace ModelFlow.DataVirtualization.Interfaces
{
    using Actions;

    internal interface IVirtualizationAction
    {
        VirtualActionThreadModelEnum ThreadModel { get; }

        void DoAction();
    }
}