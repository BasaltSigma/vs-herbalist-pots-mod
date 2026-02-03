using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace HerbPots
{
    public class BlockHerbalistPot : Block
    {
        private WorldInteraction[] interactions = Array.Empty<WorldInteraction>();
        public HerbalistPotGrowthProps GrowthProps => Attributes["growBehavior"].AsObject<HerbalistPotGrowthProps>();

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            List<ItemStack> flowerList = new List<ItemStack>();
            foreach (Block block in api.World.Blocks)
            {
                JsonObject attributes = block.Attributes;
                if (!block.IsMissing && attributes != null && attributes["plantContainable"].Exists && block.Code.FirstCodePart().Equals("flower"))
                {
                    flowerList.Add(new ItemStack(block));
                }
            }

            List<ItemStack> shearsList = new List<ItemStack>();
            foreach (Item item in api.World.Items)
            {
                if (item.Tool == EnumTool.Shears)
                {
                    shearsList.Add(new ItemStack(item));
                }
            }

            interactions = new WorldInteraction[3]
            {
                new WorldInteraction
                {
                    ActionLangCode = "herbalistpots:blockhelp-herbalist-pot-plant",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = flowerList.ToArray()
                },
                new WorldInteraction
                {
                    ActionLangCode = "herbalistpots:blockhelp-herbalist-pot-harvest",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = shearsList.ToArray()
                },
                new WorldInteraction
                {
                    ActionLangCode = "herbalistpots:blockhelp-herbalist-pot-clear",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift"
                }
            };
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            bool num = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
            if (num && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityHerbalistPot blockEntityHerbalistPot)
            {
                BlockPos blockPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
                double y = byPlayer.Entity.Pos.X - ((double)blockPos.X + blockSel.HitPosition.X);
                double x = (double)(float)byPlayer.Entity.Pos.Z - ((double)blockPos.Z + blockSel.HitPosition.Z);
                float num2 = (float)Math.Atan2(y, x);
                float num3 = (float)Math.PI / 8f;
                float meshAngle = (float)(int)Math.Round(num2 / num3) * num3;
                blockEntityHerbalistPot.MeshAngle = meshAngle;
            }
            return num;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
            BlockEntityHerbalistPot potContainer = world.BlockAccessor.GetBlockEntity<BlockEntityHerbalistPot>(pos);
            if (potContainer != null && potContainer.Contents != null)
            {
                ItemStack contentsToDrop = potContainer.Contents.Clone();
                contentsToDrop.StackSize += potContainer.StoredProducts;
                world.SpawnItemEntity(contentsToDrop, pos);
            }
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityHerbalistPot potContainer = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityHerbalistPot;
            IPlayerInventoryManager inventoryManager = byPlayer.InventoryManager;
            if (inventoryManager != null && potContainer != null)
            {
                bool isHoldingItem = !inventoryManager.ActiveHotbarSlot?.Empty ?? false;
                if (potContainer.Contents == null && isHoldingItem)
                {
                    return potContainer.TryPutContents(inventoryManager.ActiveHotbarSlot, byPlayer);
                }
                else
                {
                    if (isHoldingItem && inventoryManager.ActiveHotbarSlot.Itemstack.Item?.Tool  == EnumTool.Shears)
                    {
                        bool transferred = potContainer.TryTakeProducts(byPlayer, out int amount);
                        DamageItem(world, byPlayer.Entity, inventoryManager.ActiveHotbarSlot, amount + 1);
                        return transferred;
                    }
                    else if (byPlayer.Entity.Controls.ShiftKey)
                    {
                        return potContainer.TryRemoveContents(byPlayer);
                    }
                }
            }
            return false;
        }

        public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos)
        {
            base.OnDecalTesselation(world, decalMesh, pos);
            if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityHerbalistPot blockEntityHerbalistPot)
            {
                decalMesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, blockEntityHerbalistPot.MeshAngle, 0f);
            }
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            BlockEntityHerbalistPot potContainer = world.BlockAccessor.GetBlockEntity<BlockEntityHerbalistPot>(selection.Position);
            List<WorldInteraction> activeInteractions = new List<WorldInteraction>();
            if (potContainer != null)
            {
                if (potContainer.Inventory[0].Empty)
                {
                    activeInteractions.Add(interactions[0]);
                }
                else if (!potContainer.Inventory[0].Empty && potContainer.StoredProducts == 0)
                {
                    activeInteractions.Add(interactions[2]);
                }
                else
                {
                    activeInteractions.Add(interactions[1]);
                    activeInteractions.Add(interactions[2]);
                }
            }
            return activeInteractions.ToArray().Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
    }
}
