using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace HerbPots
{
    public class HerbPotsModSystem : ModSystem
    {
        public static HerbPotsModSystem Instance { get; private set; }
        public static string MOD_ID = "herbalistpots";

        internal const string MOD_CONFIG_FILENAME = "herbalistpots.json";

        public static HerbalistPotGrowthProps GrowthProps { get; private set; }
        public static JsonObject GrowthTemps { get; private set; }

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            Instance = this;

            GrowthProps = api.LoadModConfig<HerbalistPotGrowthProps>(MOD_CONFIG_FILENAME);
            if (GrowthProps == null)
            {
                api.StoreModConfig(HerbalistPotGrowthProps.DefaultValues, MOD_CONFIG_FILENAME);
                GrowthProps = api.LoadModConfig<HerbalistPotGrowthProps>(MOD_CONFIG_FILENAME);
            }
            ValidateConfigValues();

            // block entity registries
            api.RegisterBlockEntityClass("BlockEntityHerbalistPot", typeof(BlockEntityHerbalistPot));

            // block registeries
            api.RegisterBlockClass("BlockHerbalistPot", typeof(BlockHerbalistPot));
        }

        public override void AssetsLoaded(ICoreAPI api)
        {
            base.AssetsLoaded(api);

            // get temps from asset config (use default temp range if null)
            GrowthTemps = api.Assets.TryGet(new AssetLocation("herbalistpots:config/climate"))?.ToObject<JsonObject>();
            if (GrowthTemps == null)
            {
                GrowthProps.useDefaultTemperatureRange = true;
            }
        }

        /// <summary>
        /// Ensure realistic values from mod config for a sanity check
        /// To guard against unexpected or invalid values (e.g. negative numbers or very large numbers)
        /// </summary>
        private void ValidateConfigValues()
        {
            int fourYearsInHours = 24 * 7 * 12 * 4;
            int dayInMs = 60_000 * 60 * 24;
            GrowthProps.calendarTimeIntervalHours = Math.Clamp(GrowthProps.calendarTimeIntervalHours, 1, fourYearsInHours);
            GrowthProps.tickInterval = Math.Clamp(GrowthProps.tickInterval, 250, dayInMs);
            GrowthProps.baseGrowChance = Math.Clamp(GrowthProps.baseGrowChance, 0.01f, 1.0f);
            GrowthProps.growChanceIncrement = Math.Clamp(GrowthProps.growChanceIncrement, 0.01f, 1.0f);
            GrowthProps.overrideDefaultMinTemp = Math.Clamp(GrowthProps.overrideDefaultMinTemp, -273, 2000);
            GrowthProps.overrideDefaultMaxTemp = Math.Clamp(GrowthProps.overrideDefaultMaxTemp, -273, 2000);
        }
    }
}
