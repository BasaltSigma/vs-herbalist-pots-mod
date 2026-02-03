using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace HerbPots
{
    public class HerbPotsModSystem : ModSystem
    {
        public static HerbPotsModSystem Instance { get; private set; }
        public static string MOD_ID = "herbalistpots";

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            Instance = this;

            // block entity registries
            api.RegisterBlockEntityClass("BlockEntityHerbalistPot", typeof(BlockEntityHerbalistPot));

            // block registeries
            api.RegisterBlockClass("BlockHerbalistPot", typeof(BlockHerbalistPot));
        }
    }
}
