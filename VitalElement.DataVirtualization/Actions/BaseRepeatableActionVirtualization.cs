namespace VitalElement.DataVirtualization.Actions
{
    using System;
    using Interfaces;

    /// <summary>
    ///     Base class there the Action repeats on a periodic basis (the RepeatingSchedule) like BaseActionVirtualization
    ///     simply implement the DoAction method.
    /// </summary>
    internal abstract class BaseRepeatableActionVirtualization : BaseActionVirtualization, IRepeatingVirtualizationAction
    {
        protected DateTime LastRun = DateTime.MinValue;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseRepeatableActionVirtualization" /> class.
        /// </summary>
        /// <param name="threadModel">The thread model.</param>
        /// <param name="isRepeating">if set to <c>true</c> [is repeating].</param>
        /// <param name="repeatingSchedule">The repeating schedule.</param>
        public BaseRepeatableActionVirtualization(
            VirtualActionThreadModelEnum threadModel = VirtualActionThreadModelEnum.UseUIThread,
            bool isRepeating = false, TimeSpan? repeatingSchedule = null)
            : base(threadModel)
        {
            IsRepeating = isRepeating;
            if (repeatingSchedule.HasValue)
            {
                RepeatingSchedule = repeatingSchedule.Value;
            }
        }

        /// <summary>
        ///     Gets or sets the repeating schedule.
        /// </summary>
        /// <value>
        ///     The repeating schedule.
        /// </value>
        public TimeSpan RepeatingSchedule { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        ///     Gets or sets a value indicating whether [is repeating].
        /// </summary>
        /// <value>
        ///     <c>true</c> if [is repeating]; otherwise, <c>false</c>.
        /// </value>
        protected bool IsRepeating { get; set; }

        /// <summary>
        ///     Determines whether [is due to run].
        /// </summary>
        /// <returns></returns>
        public virtual bool IsDueToRun()
        {
            if (DateTime.Now >= LastRun.Add(RepeatingSchedule))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     check to see if the action should be kept.
        /// </summary>
        /// <returns></returns>
        public virtual bool KeepInActionsList()
        {
            return IsRepeating;
        }
    }
}