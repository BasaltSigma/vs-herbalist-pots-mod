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
        /// How many in-game hours between each roll check for growth
        /// </summary>
        public double calendarTimeIntervalHours;
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
            tickInterval = 10000,
            calendarTimeIntervalHours = 24.0,
            baseGrowChance = 0.5f,
            growChanceIncrement = 0.1f,
            maxGrownStackSize = 4
        };
    }
}
