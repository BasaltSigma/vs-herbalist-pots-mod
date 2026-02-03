using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HerbPots
{
    public class HerbalistPotGrowthProps
    {
        /// <summary>
        /// Interval in milliseconds between block ticks
        /// </summary>
        public int tickInterval;
        /// <summary>
        /// Growth chance of the flower on the first tick of this growth period
        /// </summary>
        public float baseGrowChance;
        /// <summary>
        /// How much the chance of growth increases for every failed growth tick
        /// </summary>
        public float growChanceIncrement;
        /// <summary>
        /// Max output stack size
        /// </summary>
        public int maxGrownStackSize;

        public static HerbalistPotGrowthProps DefaultValues = new HerbalistPotGrowthProps
        {
            tickInterval = 60_000,
            baseGrowChance = 0.03f,
            growChanceIncrement = 0.01f,
            maxGrownStackSize = 4
        };
    }
}
