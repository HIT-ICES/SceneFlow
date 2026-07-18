// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Exit Games GmbH">
//   Exit Games GmbH, 2012
// </copyright>
// <summary>
//   TimeKeeper Helper. See class description.
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace ExitGames.Client.DemoParticle
{
    /// <summary>
    ///     A utility class that turns it's ShouldExecute property to true after a set interval time has passed.
    /// </summary>
    /// <remarks>
    ///     TimeKeepers can be useful to execute tasks in a certain interval within a game loop (integrating a recurring task
    ///     into a certain thread).
    ///     An interval can be overridden, when you set ShouldExecute to true.
    ///     Call Reset after execution of whatever you do to re-enable the TimeKeeper (ShouldExecute becomes false until
    ///     interval passed).
    ///     Being based on Environment.TickCount, this is not very precise but cheap.
    /// </remarks>
    public class TimeKeeper
    {
        private int lastExecutionTime = Environment.TickCount;
        private bool shouldExecute;

        /// <summary>
        ///     Creates a new, enabled TimeKeeper and sets it's interval.
        /// </summary>
        /// <param name="interval"></param>
        public TimeKeeper(int interval)
        {
            IsEnabled = true;
            Interval = interval;
        }

        /// <summary>Interval in which ShouldExecute should be true (and something is executed).</summary>
        public int Interval { get; set; }

        /// <summary>A disabled TimeKeeper never turns ShouldExecute to true. Reset won't affect IsEnabled!</summary>
        public bool IsEnabled { get; set; }

        /// <summary>Turns true of the time interval has passed (after reset or creation) or someone set ShouldExecute manually.</summary>
        /// <remarks>Call Reset to start a new interval.</remarks>
        public bool ShouldExecute
        {
            get => IsEnabled && (shouldExecute || Environment.TickCount - lastExecutionTime > Interval);
            set => shouldExecute = value;
        }

        /// <summary>ShouldExecute becomes false and the time interval is refreshed for next execution.</summary>
        /// <remarks>Does not affect IsEnabled.</remarks>
        public void Reset()
        {
            shouldExecute = false;
            lastExecutionTime = Environment.TickCount;
        }
    }
}