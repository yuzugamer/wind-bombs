using System.Security;
using System.Security.Permissions;
using BepInEx;
using UnityEngine;
using Noise;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Linq;
using MoreSlugcats;
using System;
using MonoMod.RuntimeDetour;
using System.Reflection;
using WindBombs;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace WindBombs;

[BepInPlugin("yuzugamer.windbombs", "Wind Bomb", "1.1.0")]

public partial class WindBombMod : BaseUnityPlugin
{
    private static bool init;
    public static AbstractPhysicalObject.AbstractObjectType WindBomb = new("WindBomb", true);
    public static AbstractPhysicalObject.AbstractObjectType GravityBomb = new("GravityBomb", true);
    public static MultiplayerUnlocks.SandboxUnlockID ItemUnlockWindBomb = new("WindBomb", true);
    public static SLOracleBehaviorHasMark.MiscItemType MiscItemTypeWindBomb = new("WindBomb", true);

    private void OnEnable()
    {
        init = false;
        On.RainWorld.OnModsInit += On_RainWorld_PostModsInit;
        On.RainWorld.OnModsDisabled += On_RainWorld_OnModsDisabled;
    }

    private static void On_RainWorld_PostModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        MachineConnector.SetRegisteredOI("yuzugamer.windbombs", new ModOptions());
        if (init) return;
        init = true;
        On.AbstractPhysicalObject.Realize += On_AbstractPhysicalObject_Realize;
        On.ScavengerBomb.Explode += On_ScavengerBomb_Explode;
        IL.ScavengerBomb.DrawSprites += IL_ScavengerBomb_DrawSprites;
        On.ScavengerAI.RealWeapon += On_ScavengerAI_RealWeapon;
        On.ScavengerAI.CollectScore_PhysicalObject_bool += On_ScavengerAI_CollectScore;
        IL.ScavengerAI.IUseARelationshipTracker_UpdateDynamicRelationship += IL_ScavengerAI_IUseARelationshipTracker_UpdateDynamicRelationship;
        IL.ScavengerAbstractAI.InitGearUp += IL_ScavengerAbstractAI_InitGearUp;
        IL.ScavengerAbstractAI.TradeItem += IL_ScavengerAbstractAI_TradeItem;
        On.ScavengerAI.WeaponScore += On_ScavengerAI_WeaponScore;
        IL.ScavengerTreasury.ctor += IL_ScavengerTreasury_ctor;
        On.ItemSymbol.ColorForItem += On_ItemSymbol_ColorForItem;
        On.ItemSymbol.SpriteNameForItem += On_ItemSymbol_SpriteNameForItem;
        On.SLOracleBehaviorHasMark.TypeOfMiscItem += On_SLOracleBehaviorHasMark_TypeOfMiscItem;
        On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += On_SLOracleBehaviorHasMark_MoonConversation_AddEvents;
        if (ModManager.MSC)
        {
            //On.MoreSlugcats.GourmandCombos.InitCraftingLibrary += On_GourmandCombos_InitCraftingLibrary;
            On.MoreSlugcats.SlugNPCAI.LethalWeaponScore += On_SlugNPCAI_LethalWeaponScore;
            new Hook(typeof(ScavengerBomb).GetInterfaceMap(typeof(IProvideWarmth)).TargetMethods.First(method => method.Name == "MoreSlugcats.IProvideWarmth.get_warmth"), typeof(WindBombMod).GetMethod(nameof(On_ScavengerBomb_MoreSlugcats_IProvideWarmth_get_warmth), BindingFlags.Static | BindingFlags.NonPublic));
        }
        if (!MultiplayerUnlocks.ItemUnlockList.Contains(ItemUnlockWindBomb))
            MultiplayerUnlocks.ItemUnlockList.Add(ItemUnlockWindBomb);
        if (ModManager.ActiveMods.Any(mod => mod.id == "henpemaz_rainmeadow"))
        {
            MeadowCompat.MeadowEnabled = true;
        }
    }

    private static void On_RainWorld_OnModsDisabled(On.RainWorld.orig_OnModsDisabled orig, RainWorld self, ModManager.Mod[] newlyDisabledMods)
    {
        if (newlyDisabledMods.Any(mod => mod.id == "yuzugamer.windbombs"))
        {
            if (MultiplayerUnlocks.ItemUnlockList.Contains(ItemUnlockWindBomb))
            {
                MultiplayerUnlocks.ItemUnlockList.Remove(ItemUnlockWindBomb);
            }
            if (WindBomb != null)
            {
                WindBomb.Unregister();
                WindBomb = null;
            }
            if (GravityBomb != null)
            {
                GravityBomb.Unregister();
                GravityBomb = null;
            }
            if (ItemUnlockWindBomb != null)
            {
                ItemUnlockWindBomb.Unregister();
                ItemUnlockWindBomb = null;
            }
            if (MiscItemTypeWindBomb != null)
            {
                MiscItemTypeWindBomb.Unregister();
                MiscItemTypeWindBomb = null;
            }
        }
    }

    private static void On_AbstractPhysicalObject_Realize(On.AbstractPhysicalObject.orig_Realize orig, AbstractPhysicalObject self)
    {
        orig(self);
        if (self.realizedObject == null)
        {
            if (self.type == WindBomb || self.type == GravityBomb)
            {
                self.realizedObject = new WindBomb(self, self.world);
            }
        }
    }

    private static void On_ScavengerBomb_Explode(On.ScavengerBomb.orig_Explode orig, ScavengerBomb self, BodyChunk hitChunk)
    {
        if (!(self is WindBomb wind))
        {
            orig(self, hitChunk);
            return;
        }
        wind.WindExplode(hitChunk);
    }

    private static Color BombColor(Color color, ScavengerBomb self) => self is WindBomb ? self.explodeColor : color;

    private static void IL_ScavengerBomb_DrawSprites(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(x => x.MatchStloc(2)))
        {
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(BombColor);
        }
    }

    private static int On_ScavengerAI_CollectScore(On.ScavengerAI.orig_CollectScore_PhysicalObject_bool orig, ScavengerAI self, PhysicalObject obj, bool weaponFiltered)
    {
        if (obj is WindBomb)
        {
            if (!self.scavenger.room.world.singleRoomWorld)
            {
                var region = self.scavenger.room.world.region.name;
                if (region == "SI" || region == "CC")
                {
                    return 3;
                }
            }
            return 2;
        }
        return orig(self, obj, weaponFiltered);
    }

    private static int On_ScavengerAI_WeaponScore(On.ScavengerAI.orig_WeaponScore orig, ScavengerAI self, PhysicalObject obj, bool pickupDropInsteadOfWeaponSelection, bool reallyWantsSpear = false)
    {
        if (obj is WindBomb)
        {
            if (self.currentViolenceType == ScavengerAI.ViolenceType.NonLethal)
            {
                foreach (var grasp in self.scavenger.grasps)
                {
                    if (grasp == null)
                    {
                        return 4;
                    }
                }
            }
            if (self.currentViolenceType == ScavengerAI.ViolenceType.ForFun)
            {
                return 5;
            }
            if (!self.scavenger.room.world.singleRoomWorld)
            {
                var region = self.scavenger.room.world.region.name;
                if (region == "SI")
                {
                    return 4;
                }
                else if (region == "CC")
                {
                    return 3;
                }
            }
            return 2;
        }
        return orig(self, obj, pickupDropInsteadOfWeaponSelection, reallyWantsSpear);
    }

    private static bool On_ScavengerAI_RealWeapon(On.ScavengerAI.orig_RealWeapon orig, ScavengerAI self, PhysicalObject obj)
    {
        return orig(self, obj) && !(obj is WindBomb && self.scavenger.room.deathFallGraphic == null);
    }

    private static int SpawnWindBomb(ScavengerAbstractAI self, int count)
    {
        if (ModOptions.NaturalWindBombSpawns.Value && count >= 0 && UnityEngine.Random.value < ((self.parent.creatureTemplate.type == CreatureTemplate.Type.Scavenger ? 0.08f : 0.06f) * ModOptions.WindBombSpawnMult.Value))
        {
            var windBomb = new AbstractPhysicalObject(self.world, WindBomb, null, self.parent.pos, self.world.game.GetNewID());
            self.world.GetAbstractRoom(self.parent.pos).AddEntity(windBomb);
            new AbstractPhysicalObject.CreatureGripStick(self.parent, windBomb, count, true);
            count--;
        }
        return count;
    }

    private static bool IsNotWindBomb(ScavengerBomb bomb, Creature creature, int i) => bomb != null && (!(creature.grasps[i].grabbed is WindBomb) || creature.room.deathFallGraphic != null);

    private static void IL_ScavengerAI_IUseARelationshipTracker_UpdateDynamicRelationship(ILContext il)
    {
        var c = new ILCursor(il);
        if(c.TryGotoNext(
            MoveType.After,
            x => x.MatchIsinst<ScavengerBomb>()
        ))
        {
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldloc, 4);
            c.Emit(OpCodes.Ldloc, 7);
            c.EmitDelegate(IsNotWindBomb);
        }
    }

    private static void IL_ScavengerAbstractAI_InitGearUp(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(
            x => x.MatchLdloc(0),
            x => x.MatchLdcI4(0),
            x => x.MatchBlt(out var _),
            x => x.MatchCallOrCallvirt(out var _),
            x => x.MatchLdcR4(0.08f),
            x => x.MatchBgeUn(out var _)
        ))
        {
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc_0);
            c.EmitDelegate(SpawnWindBomb);
            c.Emit(OpCodes.Stloc_0);
        }
    }

    private static AbstractPhysicalObject WindBombTrade(AbstractSpear orig, ScavengerAbstractAI self) 
    {
        if (ModOptions.NaturalWindBombSpawns.Value)
        {
            float chance = 0.1f;
            if (!self.world.singleRoomWorld)
            {
                var region = self.world.region.name;
                if (region == "SI")
                {
                    chance = 0.25f;
                }
                else if (region == "CC" || region == "UW")
                {
                    chance = 0.15625f;
                }
            }
            if (UnityEngine.Random.value < (chance * ModOptions.WindBombSpawnMult.Value))
            {
                return new AbstractPhysicalObject(self.world, WindBomb, null, self.parent.pos, self.world.game.GetNewID());
            }
        }
        return orig;
    }

    private static void IL_ScavengerAbstractAI_TradeItem(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(
            MoveType.After,
            x => x.MatchNewobj<AbstractSpear>()
        ))
        {
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(WindBombTrade);
        }
    }

    public static AbstractPhysicalObject TreasuryWindBomb(AbstractPhysicalObject orig, ScavengerTreasury self, int i)
    {
        if (ModOptions.NaturalWindBombSpawns.Value)
        {
            var chance = 0.05f;
            if (!self.room.world.singleRoomWorld)
            {
                var region = self.room.world.region.name;
                if (region == "SI")
                {
                    chance = 0.1f;
                }
                else if (region == "CC" || region == "UW")
                {
                    chance = 0.7f;
                }
            }
            if (UnityEngine.Random.value < (chance * ModOptions.WindBombSpawnMult.Value))
            {
                return new AbstractPhysicalObject(self.room.world, WindBomb, null, self.room.GetWorldCoordinate(self.tiles[i]), self.room.game.GetNewID());
            }
        }
        return orig;
    }

    private static void IL_ScavengerTreasury_ctor(ILContext il)
    {
        var c = new ILCursor(il);
        //var skip = c.DefineLabel();
        var skip2 = c.DefineLabel();
        if(c.TryGotoNext(
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<ScavengerTreasury>("property"),
            x => x.MatchLdloc(8),
            x => x.MatchCallOrCallvirt(out var _)
        ))
        {
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldloc, 8);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, 7);
            c.EmitDelegate(TreasuryWindBomb);
            c.Emit(OpCodes.Stloc, 8);
        }
    }

    private static Color On_ItemSymbol_ColorForItem(On.ItemSymbol.orig_ColorForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
    {
        if (itemType == WindBomb)
            return new Color32(100, 100, 188, 255);
        return orig(itemType, intData);
    }

    private static string On_ItemSymbol_SpriteNameForItem(On.ItemSymbol.orig_SpriteNameForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
    {
        if (itemType == WindBomb)
            return "Symbol_StunBomb";
        return orig(itemType, intData);
    }

    private static SLOracleBehaviorHasMark.MiscItemType On_SLOracleBehaviorHasMark_TypeOfMiscItem(On.SLOracleBehaviorHasMark.orig_TypeOfMiscItem orig, SLOracleBehaviorHasMark self, PhysicalObject testItem)
    {
        if (testItem is WindBomb)
        {
            return MiscItemTypeWindBomb;
        }
        return orig(self, testItem);
    }

    private static void On_SLOracleBehaviorHasMark_MoonConversation_AddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
    {
        orig(self);
        if (self.id == Conversation.ID.Moon_Misc_Item)
        {
            if (self.describeItem == MiscItemTypeWindBomb)
            {
                self.events.Add(new Conversation.TextEvent(self, 10, self.Translate("It's some kind of thick membrane filled with highly pressurized air.<LINE>Were the membrane to be damaged, the air inside would be rapidly released, causing a small but forceful explosion.<LINE>I would be very careful around ledges with this!"), 0));
                return;
            }
        }
    }

    /*private static void On_GourmandCombos_InitCraftingLibrary(On.MoreSlugcats.GourmandCombos.orig_InitCraftingLibrary orig)
    {
        orig();
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[WindBomb], GourmandCombos.objectsLibrary[AbstractPhysicalObject.AbstractObjectType.FirecrackerPlant], 0, AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[WindBomb], GourmandCombos.objectsLibrary[AbstractPhysicalObject.AbstractObjectType.SporePlant], 0, AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[WindBomb], GourmandCombos.objectsLibrary[AbstractPhysicalObject.AbstractObjectType.Mushroom], 0, AbstractPhysicalObject.AbstractObjectType.PuffBall, null);

    }*/

    private static float On_SlugNPCAI_LethalWeaponScore(On.MoreSlugcats.SlugNPCAI.orig_LethalWeaponScore orig, SlugNPCAI self, PhysicalObject obj, Creature target)
    {
        if (obj is WindBomb)
        {
            if (!self.creature.realizedCreature.room.world.singleRoomWorld)
            {
                var region = self.creature.realizedCreature.room.world.region.name;
                if (region == "SI" || region == "CC" || self.creature.realizedCreature.room.deathFallGraphic != null)
                {
                    return 4f;
                }
            }
            return 2f;
        }
        return orig(self, obj, target);
    }

    private static float On_ScavengerBomb_MoreSlugcats_IProvideWarmth_get_warmth(Func<ScavengerBomb, float> orig, ScavengerBomb self)
    {
        if (self is WindBomb) return 0f;
        return orig(self);
    }
}