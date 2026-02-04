using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;

namespace HerbPots
{
    public class BlockEntityHerbalistPot : BlockEntityContainer, ITexPositionSource, IRotatable
    {
        private InventoryGeneric inv;
        private ICoreClientAPI capi;
        private PlantContainerProps curContProps;
        private ITexPositionSource contentTexSource;
        private Dictionary<string, AssetLocation> shapeTextures;
        private MeshData potMesh;
        private MeshData contentMesh;
        public HerbalistPotGrowthProps growthProps = HerbalistPotGrowthProps.DefaultValues;

        public virtual float MeshAngle { get; protected set; }
        public virtual double LastHourTimestamp { get; protected set; }
        public virtual float GrowthChance { get; protected set; }
        public virtual int StoredProducts { get; protected set; }

        public override InventoryBase Inventory => inv;
        public Size2i AtlasSize => capi.BlockTextureAtlas.Size;
        public override string InventoryClassName => "herbalistpot";

        public ItemStack Contents
        {
            get
            {
                return inv[0].Itemstack;
            }
        }

        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                AssetLocation value = null;
                if (curContProps.Textures != null && curContProps.Textures.TryGetValue(textureCode, out var value2))
                {
                    value = value2.Base;
                }
                if (value == null && shapeTextures != null)
                {
                    shapeTextures.TryGetValue(textureCode, out value);
                }
                int textureSubId;
                if (value != null)
                {
                    TextureAtlasPosition texPos = capi.BlockTextureAtlas[value];
                    if (texPos == null)
                    {
                        BitmapRef bmp2 = capi.Assets.TryGet(value.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"))?.ToBitmap(capi);
                        if (bmp2 != null)
                        {
                            capi.BlockTextureAtlas.GetOrInsertTexture(value, out textureSubId, out texPos, () => bmp2);
                            bmp2.Dispose();
                        }
                    }
                    return texPos;
                }
                if (Contents.Class == EnumItemClass.Item)
                {
                    value = Contents.Item.Textures[textureCode].Base;
                    BitmapRef bmp = capi.Assets.TryGet(value.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"))?.ToBitmap(capi);
                    if (bmp != null)
                    {
                        capi.BlockTextureAtlas.GetOrInsertTexture(value, out textureSubId, out var texPos2, () => bmp);
                        bmp.Dispose();
                        return texPos2;
                    }
                }
                return contentTexSource[textureCode];
            }
        }

        public BlockEntityHerbalistPot()
        {
            inv = new InventoryGeneric(1, null, null);
            inv.OnAcquireTransitionSpeed += slotTransitionSpeed;
            GrowthChance = growthProps.baseGrowChancePerDay;
        }

        private float slotTransitionSpeed(EnumTransitionType transitionType, ItemStack stack, float mulByConfig)
        {
            return 0f;
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            capi = api as ICoreClientAPI;
            if (api.Side == EnumAppSide.Client && potMesh == null)
            {
                GenerateMeshes();
                MarkDirty(redrawOnClient: true);
            }
            if (Block is BlockHerbalistPot herbalistPotBlock)
            {
                growthProps = herbalistPotBlock.Attributes["growBehavior"]?.AsObject<HerbalistPotGrowthProps>(HerbalistPotGrowthProps.DefaultValues);
            }
            RegisterGameTickListener(CheckCanGrow, growthProps.tickInterval);
            LastHourTimestamp = api.World.Calendar.ElapsedHours;
        }

        protected virtual void CheckCanGrow(float delta)
        {
            if (inv[0].Empty || StoredProducts >= growthProps.maxGrownStackSize)
            {
                return;
            }
            double elapsedHours = Api.World.Calendar.ElapsedHours;
            if ((elapsedHours - LastHourTimestamp) > growthProps.calendarTimeIntervalHours)
            {
                float rand = Api.World.Rand.NextSingle();
                if (rand < GrowthChance)
                {
                    StoredProducts++;
                    GrowthChance = growthProps.baseGrowChancePerDay;
                    LastHourTimestamp = elapsedHours;
                    MarkDirty(redrawOnClient: true);
                }
                else
                {
                    GrowthChance += growthProps.growChanceIncrement;
                }
            }
        }

        public bool TryPutContents(ItemSlot fromSlot, IPlayer player)
        {
            if (!inv[0].Empty || fromSlot.Empty)
            {
                return false;
            }
            if (!fromSlot.Itemstack.Block?.Code.FirstCodePart().Equals("flower") ?? true)
            {
                return false;
            }
            if (fromSlot.TryPutInto(Api.World, inv[0]) > 0)
            {
                if (Api.Side == EnumAppSide.Server)
                {
                    Api.World.PlaySoundAt(new AssetLocation("game:sounds/block/plant"), Pos, 0.0);
                }
                (player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                fromSlot.MarkDirty();
                StoredProducts = 0;
                GrowthChance = growthProps.baseGrowChancePerDay;
                LastHourTimestamp = Api.World.Calendar.ElapsedHours;
                MarkDirty(redrawOnClient: true);
                return true;
            }
            return false;
        }

        public bool TryTakeProducts(IPlayer player, out int takenStackSize)
        {
            if (StoredProducts > 0)
            {
                ItemStack stack = inv[0].Itemstack.Clone();
                stack.StackSize = StoredProducts;
                if (player.InventoryManager.TryGiveItemstack(stack))
                {
                    if (Api.Side == EnumAppSide.Server)
                    {
                        Api.World.PlaySoundAt(new AssetLocation("game:sounds/block/plant"), Pos, 0.0);
                    }
                    (player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                    takenStackSize = StoredProducts;
                    StoredProducts = 0;
                    LastHourTimestamp = Api.World.Calendar.ElapsedHours;
                    GrowthChance = growthProps.baseGrowChancePerDay;
                    MarkDirty(redrawOnClient: true);
                    return true;
                }
            }
            takenStackSize = 0;
            return false;
        }

        public bool TryRemoveContents(IPlayer player)
        {
            if (inv[0].Empty)
            {
                return false;
            }
            if (player.InventoryManager.TryGiveItemstack(inv[0].TakeOutWhole()))
            {
                if (Api.Side == EnumAppSide.Server)
                {
                    Api.World.PlaySoundAt(new AssetLocation("game:sounds/block/plant"), Pos, 0.0);
                }
                (player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                StoredProducts = 0;
                inv[0].MarkDirty();
                MarkDirty(redrawOnClient: true);
                return true;
            }
            return false;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (Contents != null)
            {
                dsc.AppendLine(Lang.Get("herbalistpots:info-planted-herb", Contents.GetName()));
                if (StoredProducts > 0)
                {
                    dsc.AppendLine(Lang.Get("herbalistpots:info-products-available", StoredProducts.ToString()));
                }
                else
                {
                    dsc.AppendLine(Lang.Get("herbalistpots:info-products-empty"));
                }
            }
            else
            {
                dsc.AppendLine(Lang.Get("herbalistpots:info-no-flower-set"));
            }
        }

        private void GenerateMeshes()
        {
            if (base.Block.Code == null)
            {
                return;
            }
            potMesh = GeneratePotMesh(capi.Tesselator);
            if (potMesh != null)
            {
                potMesh = potMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, MeshAngle, 0f);
            }
            MeshData[] array = GenerateContentMeshes(capi.Tesselator);
            if (array != null && array.Length != 0)
            {
                contentMesh = array[GameMath.MurmurHash3Mod(Pos.X, Pos.Y, Pos.Z, array.Length)];
                if (curContProps != null && curContProps.RandomRotate)
                {
                    float radY = (float)GameMath.MurmurHash3Mod(Pos.X, Pos.Y, Pos.Z, 16) * 22.5f * ((float)Math.PI / 180f);
                    contentMesh = contentMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, radY, 0f);
                }
                else
                {
                    contentMesh = contentMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, MeshAngle, 0f);
                }
            }
        }

        private MeshData GeneratePotMesh(ITesselatorAPI tesselator)
        {
            Dictionary<string, MeshData> orCreate = ObjectCacheUtil.GetOrCreate(Api, "plantContainerMeshes", () => new Dictionary<string, MeshData>());
            string key = base.Block.Code.ToString() + (!inv[0].Empty ? "soil" : "empty");
            if (orCreate.TryGetValue(key, out var value))
            {
                return value;
            }
            if (!inv[0].Empty && base.Block.Attributes != null)
            {
                CompositeShape compositeShape = base.Block.Attributes["filledShape"].AsObject<CompositeShape>(null, base.Block.Code.Domain);
                Shape shape = null;
                if (compositeShape != null)
                {
                    shape = Shape.TryGet(Api, compositeShape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"));
                }
                if (shape == null)
                {
                    Api.World.Logger.Error("Herbalist pot container, asset {0} not found,", compositeShape?.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"));
                    return value;
                }
                tesselator.TesselateShape(base.Block, shape, out value);
            }
            else
            {
                value = capi.TesselatorManager.GetDefaultBlockMesh(base.Block);
            }
            return orCreate[key] = value;
        }

        private MeshData[] GenerateContentMeshes(ITesselatorAPI tesselator)
        {
            if (Contents == null)
            {
                return null;
            }
            Dictionary<string, MeshData[]> orCreate = ObjectCacheUtil.GetOrCreate(Api, "plantContainerContentMeshes", () => new Dictionary<string, MeshData[]>());
            float y = ((base.Block.Attributes == null) ? 0.4f : base.Block.Attributes["fillHeight"].AsFloat(0.4f));
            string containerSize = base.Block.Attributes["plantContainerSize"]?.AsString();
            string key = Contents?.ToString() + "-" + containerSize + "f" + y;
            if (orCreate.TryGetValue(key, out var value))
            {
                return value;
            }
            curContProps = GetProps(inv[0].Itemstack);
            if (curContProps == null)
            {
                return null;
            }
            CompositeShape compositeShape = curContProps.Shape;
            if (compositeShape == null)
            {
                compositeShape = ((Contents.Class == EnumItemClass.Block) ? Contents.Block.Shape.Clone() : Contents.Item.Shape.Clone());
            }
            ModelTransform modelTransform = curContProps.Transform;
            if (modelTransform == null)
            {
                modelTransform = new ModelTransform().EnsureDefaultValues();
                modelTransform.Translation.Y = y;
            }
            contentTexSource = ((Contents.Class == EnumItemClass.Block) ? capi.Tesselator.GetTextureSource(Contents.Block) : capi.Tesselator.GetTextureSource(Contents.Item));
            List<IAsset> list;
            if (compositeShape.Base.Path.EndsWith("*"))
            {
                list = Api.Assets.GetManyInCategory("shapes", compositeShape.Base.Path.Substring(0, compositeShape.Base.Path.Length - 1), compositeShape.Base.Domain);
            }
            else
            {
                list = new List<IAsset>();
                list.Add(Api.Assets.TryGet(compositeShape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json")));
            }
            if (list != null && list.Count > 0)
            {
                ShapeElement.locationForLogging = compositeShape.Base;
                value = new MeshData[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    Shape shape = list[i].ToObject<Shape>();
                    shapeTextures = shape.Textures;
                    MeshData modelData;
                    try
                    {
                        byte climateColorMapId = (byte)((Contents.Block?.ClimateColorMapResolved != null) ? ((byte)(Contents.Block.ClimateColorMapResolved.RectIndex + 1)) : 0);
                        byte seasonColorMapId = (byte)((Contents.Block?.SeasonColorMapResolved != null) ? ((byte)(Contents.Block.SeasonColorMapResolved.RectIndex + 1)) : 0);
                        tesselator.TesselateShape("plant container content shape", shape, out modelData, this, null, 0, climateColorMapId, seasonColorMapId);
                    }
                    catch (Exception ex)
                    {
                        Api.Logger.Error(string.Concat(ex.Message, " (when tesselating ", compositeShape.Base.WithPathPrefixOnce("shapes/"), ")"));
                        Api.Logger.Error(ex);
                        value = null;
                        break;
                    }
                    modelData.ModelTransform(modelTransform);
                    value[i] = modelData;
                }
            }
            else
            {
                Api.World.Logger.Error("Plant container, content asset {0} not found,", compositeShape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"));
            }
            return orCreate[key] = value;
        }

        public PlantContainerProps GetProps(ItemStack stack)
        {
            return stack?.Collectible.Attributes?["plantContainable"]?["largeContainer"]?.AsObject<PlantContainerProps>(null, stack.Collectible.Code.Domain);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            MeshAngle = tree.GetFloat("meshAngle", MeshAngle);
            GrowthChance = tree.GetFloat("growthChance", GrowthChance);
            StoredProducts = tree.GetInt("storedProducts", StoredProducts);
            LastHourTimestamp = tree.GetDouble("lastHourTimestamp", LastHourTimestamp);
            if (capi != null)
            {
                GenerateMeshes();
                MarkDirty(redrawOnClient: true);
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("meshAngle", MeshAngle);
            tree.SetFloat("growthChance", GrowthChance);
            tree.SetInt("storedProducts", StoredProducts);
            tree.SetDouble("lastHourTimestamp", LastHourTimestamp);
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if (potMesh == null)
            {
                return false;
            }
            mesher.AddMeshData(potMesh);
            if (contentMesh != null && !inv[0].Empty)
            {
                if (Api.World.BlockAccessor.GetDistanceToRainFall(Pos, 6, 2) >= 20)
                {
                    MeshData meshData = contentMesh.Clone();
                    meshData.ClearWindFlags();
                    mesher.AddMeshData(meshData);
                }
                else
                {
                    mesher.AddMeshData(contentMesh);
                }
            }
            return true;
        }

        public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
        {
            MeshAngle = tree.GetFloat("meshAngle");
            MeshAngle -= (float)degreeRotation * ((float)Math.PI / 180f);
            tree.SetFloat("meshAngle", MeshAngle);
        }
    }
}
