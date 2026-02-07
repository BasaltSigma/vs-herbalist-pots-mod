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
    }
}
