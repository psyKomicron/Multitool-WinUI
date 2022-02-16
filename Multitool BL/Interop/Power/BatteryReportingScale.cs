namespace Multitool.Interop
{
    /// <summary>
    /// 
    /// </summary>
    public class BatteryReportingScale
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="granularity"></param>
        /// <param name="capacity"></param>
        public BatteryReportingScale(uint granularity, uint capacity)
        {
            Granularity = granularity;
            Capacity = capacity;
        }

        /// <summary>
        /// 
        /// </summary>
        public uint Granularity { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public uint Capacity { get; private set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return nameof(Granularity) + " " + Granularity + ", " + nameof(Capacity) + " " + Capacity;
        }
    }
}
