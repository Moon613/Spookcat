using System;
using System.Security;
using System.Security.Permissions;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using RWCustom;
using BepInEx;
using Random = UnityEngine.Random;
using System.Runtime.CompilerServices;
using HUD;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using BepInEx.Logging;

#pragma warning disable CS0618
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace Spookcat;

[BepInPlugin("moon.spookcat", "Spookcat", "1.0.0")]
class Spookcat : BaseUnityPlugin
{
    [AllowNull] new internal static ManualLogSource Logger;
    public static ConditionalWeakTable<Player, SpookcatEx> SpookyCWT = new ConditionalWeakTable<Player, SpookcatEx>();
    public Spookcat() {
        Logger = base.Logger;
    }
    public void OnEnable()
    {
        SpookyGraphics.Apply();
        SpookyCat.Apply();
        ScareEverything.Apply();
        On.HardmodeStart.Update += HardmodeStart_Update;    // Will become unneeded once it is it's own Slugbase scug
        IL.MiniFly.ViableForBuzzaround += IL_MiniFly_ViableForBuzzaround;
        On.RoomCamera.LoadPalette += RoomCamera_LoadPalette;
        On.HUD.FoodMeter.UpdateShowCount += FoodMeter_UpdateShowCount;
        IL.Room.Loaded += IL_Room_Loaded;
        IL.WormGrass.WormGrassPatch.Update += IL_WormGrass_WormGrassPatch_Update;
    }
    static void HardmodeStart_Update(On.HardmodeStart.orig_Update orig, HardmodeStart self, bool eu) {
		if (self.room.game.cameras[0].hud != null && self.room.game.cameras[0].hud.textPrompt != null && self.room.game.cameras[0].hud.textPrompt.subregionTracker != null)
		{
            // self if statement is also new. Put right before orig.
			self.room.game.cameras[0].hud.textPrompt.subregionTracker.counter = 0;
		}
		Player? player = self.room.game.Players[0].realizedCreature as Player;
        if (player != null && !SpookyCWT.TryGetValue(player, out SpookcatEx _)) {
            orig(self, eu);
            return;
        }
		if (self.phase == HardmodeStart.Phase.Init) {
			if (self.room.game.cameras[0].room == self.room)
			{
				if (self.room.game.cameras[0].currentCameraPosition == 7)
				{
					self.camPosCorrect = true;
				}
				else
				{
					self.room.game.cameras[0].MoveCamera(7);
				}
			}
			if (!self.mapAdded && self.camPosCorrect && player != null)
			{
				if (self.room.game.cameras[0].hud == null)
				{
					self.room.game.cameras[0].FireUpSinglePlayerHUD(player);
				}
				else if (self.room.game.cameras[0].hud.map != null && self.room.game.cameras[0].hud.map.discLoaded)
				{
					self.room.game.cameras[0].hud.map.AddDiscoveryTexture(Resources.Load("Illustrations/redsJourney") as Texture2D);
					self.mapAdded = true;
					self.room.game.cameras[0].hud.foodMeter.NewShowCount(player.FoodInStomach);
					self.room.game.cameras[0].hud.foodMeter.visibleCounter = 0;
					self.room.game.cameras[0].hud.foodMeter.fade = 0f;
					self.room.game.cameras[0].hud.foodMeter.lastFade = 0f;
				}
			}
			if (player != null)
			{
				if (!self.playerPosCorrect)
				{
					player.SuperHardSetPosition(self.room.MiddleOfTile(350, 13));
					self.playerPosCorrect = true;
					if (player.graphicsModule != null)
					{
						player.graphicsModule.Reset();
					}
					self.startController = new HardmodeStart.StartController(new HardmodeStart.HardmodePlayer(self, 0));
					player.controller = self.startController;
					player.standing = true;
					player.playerState.foodInStomach = 2;   // self is changed from base game: 5 -> 2. Easy IL Hook.
					player.objectInStomach = new DataPearl.AbstractDataPearl(self.room.world, AbstractPhysicalObject.AbstractObjectType.DataPearl, null, new WorldCoordinate(self.room.abstractRoom.index, -1, -1, 0), self.room.game.GetNewID(), -1, -1, null, DataPearl.AbstractDataPearl.DataPearlType.Red_stomach);
				}
				if (self.nshSwarmer != null && self.nshSwarmer.realizedObject != null)
				{
					self.nshSwarmer.realizedObject.firstChunk.HardSetPosition(player.mainBodyChunk.pos + new Vector2(-30f, 0f));
					player.SlugcatGrab(self.nshSwarmer.realizedObject, 0);
					self.nshSwarmer = null;
				}
				if (self.spear != null && self.spear.realizedObject != null)
				{
					if (player.spearOnBack != null)
					{
						self.spear.realizedObject.firstChunk.HardSetPosition(player.mainBodyChunk.pos + new Vector2(-30f, 0f));
						player.spearOnBack.SpearToBack(self.spear.realizedObject as Spear);
					}
					self.spear = null;
				}
			}
			if (self.camPosCorrect && self.playerPosCorrect && self.nshSwarmer == null && self.spear == null && self.mapAdded)
			{
				self.phase = HardmodeStart.Phase.PlayerRun;
				return;
			}
		}
		if (self.phase == HardmodeStart.Phase.PlayerRun) {
            foreach (var hardPlayer in self.hardmodePlayers){
			hardPlayer.playerMovGiveUpCounter++;
                if (hardPlayer.playerAction > 8 || hardPlayer.playerMovGiveUpCounter > 400)
                {
                    self.phase = HardmodeStart.Phase.End;
                    return;
                }
            }
		}
		if (self.phase == HardmodeStart.Phase.End) {
			if (player != null)
			{
				player.controller = null;
				self.room.game.cameras[0].followAbstractCreature = player.abstractCreature;
			}
			self.Destroy();
		}
    }
    static void IL_MiniFly_ViableForBuzzaround(ILContext il) {
        var cursor = new ILCursor(il);
        var label = cursor.DefineLabel();
        if (!cursor.TryGotoNext(MoveType.After, i => i.MatchIsinst(out _), i => i.MatchBrfalse(out _))) {
            Logger.LogDebug("Spookcat: Error matching IL 1");
            return;
        }
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.EmitDelegate((AbstractCreature crit) => {
            if (crit.realizedCreature is Player player && SpookyCWT.TryGetValue(player, out var _)) {
                return true;
            }
            return false;
        });
        cursor.Emit(OpCodes.Brtrue, label);
        if (!cursor.TryGotoNext(MoveType.After, i => i.MatchBrtrue(out _))) {
            Logger.LogDebug("Spookcat: Error matching IL 2");
            return;
        }
        cursor.MarkLabel(label);
    }
    // Make it check for worldState, once updated to SlugBase
    static void RoomCamera_LoadPalette(On.RoomCamera.orig_LoadPalette orig, RoomCamera self, int pal, ref Texture2D texture) {
        orig(self, 56, ref texture);
    }
    // Also needs to be converted into an IL Hook
    static void FoodMeter_UpdateShowCount(On.HUD.FoodMeter.orig_UpdateShowCount orig, FoodMeter self) {
        if (self.showCount < self.hud.owner.CurrentFood)
        {
            if (self.showCountDelay != 0)
            {
                self.showCountDelay--;
                return;
            }
            self.showCountDelay = 10;
            if (self.showCount >= 0 && self.showCount < self.circles.Count && !self.circles[self.showCount].foodPlopped)
            {
                self.circles[self.showCount].FoodPlop();
            }
            self.showCount++;
            if (self.quarterPipShower != null)
            {
                self.quarterPipShower.Reset();
                return;
            }
        }
        else if (self.showCount > self.hud.owner.CurrentFood)
        {
            if (self.eatCircleDelay == 0)
            {
                self.eatCircleDelay = 20;   // This is what was changed, from 40 -> 20. Might just be a 1.5 -> 1.9 game thing really.
            }
            self.eatCircleDelay--;
            if (self.eatCircleDelay < 1)
            {
                self.circles[self.showCount - 1].EatFade();
                self.showCount--;
                self.eatCircleDelay = 0;
            }
        }
    }
    static void IL_Room_Loaded(ILContext il) {
        var cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.After, i => i.MatchStfld<Room>(nameof(Room.dustStorm)))) {
            Logger.LogDebug("Spookcat: 1 IL Failed!");
            return;
        }
        if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchLdcI4(0))) {
            Logger.LogDebug("Spookcat: 2 IL Failed");
            return;
        }

        try {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate((Room self) => {
                if (self.lightning == null)
                {
                    self.lightning = new Lightning(self, 1f, true);
                    self.AddObject(self.lightning);
                }
                if (self.insectCoordinator == null)
                {
                    self.insectCoordinator = new InsectCoordinator(self);
                    self.AddObject(self.insectCoordinator);
                }
                self.insectCoordinator.AddEffect(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.Flies, 1.1f, false));
                // self.insectCoordinator.AddEffect(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.FireFlies, 1f, false));
                self.roomSettings.placedObjects.RemoveAll(i => i.type == PlacedObject.Type.Rainbow);
                // Meh just add the sounds in here, it Just Works
                self.roomSettings.ambientSounds.Add(new OmniDirectionalSound("AM_RAIN-Rumbles.ogg", false)
                {
                    volume=0.4f,
                    pitch=0.9f
                });
                self.roomSettings.ambientSounds.Add(new OmniDirectionalSound("SO_WAT-RainishDrips.ogg", false)
                {
                    volume=0.4f,
                    pitch=0.9f
                });
                self.roomSettings.ambientSounds.Add(new OmniDirectionalSound("AM_ENV-NatWind.ogg", false)
                {
                    volume=0.4f,
                    pitch=0.9f
                });
            });
        } catch (Exception err) {
            Debug.Log($"Spookcat: Error Applying IL Hook\n{err}");
        }
    }
    static void IL_WormGrass_WormGrassPatch_Update(ILContext il) {
        var cursor = new ILCursor(il);
        var label = cursor.DefineLabel();

        if (!cursor.TryGotoNext(MoveType.After, i => i.MatchStloc(out _))) {
            return;
        }
        cursor.Emit(OpCodes.Ldloc, 0);
        cursor.EmitDelegate((Creature crit) => {
            if (crit is Player player && SpookyCWT.TryGetValue(player, out var _)) {
                return true;
            }
            return false;
        });
        cursor.Emit(OpCodes.Brtrue, label);
        
        if (!cursor.TryGotoNext(MoveType.After, i => i.MatchNewobj(out _), i => i.MatchCallOrCallvirt(out _))) {
            return;
        }
        cursor.MarkLabel(label);
    }
}
public class SpookcatEx
{
    internal float hurtLevel;
    internal SpookIllness? spookIllness;
    internal Graphics graphics = new();
    internal void GetHurt(Player player)
    {
        if (player.playerState.foodInStomach == 0)
        {
            hurtLevel += Mathf.Lerp(0.55f, 1.3f, Random.value);
            return;
        }
        player.playerState.foodInStomach = Math.Max(player.playerState.foodInStomach - 1, 0);
    }
    internal class Graphics
    {
        internal void ResetRag(PlayerGraphics pg) {
            if (rag == null) { return; }
            Vector2 vector = TongueAttachPos(1f, pg);
            for (int i = 0; i < rag.GetLength(0); i++)
            {
                rag[i, 0] = vector;
                rag[i, 1] = vector;
                rag[i, 2] *= 0f;
            }
        }
        public Vector2 TongueAttachPos(float timeStacker, PlayerGraphics pg) {
            Vector2 vector = Vector2.Lerp(pg.head.lastPos, pg.head.pos, timeStacker);
            Vector2 p = Vector2.Lerp(pg.drawPositions[1, 1], pg.drawPositions[1, 0], timeStacker);
            return vector - Custom.DirVec(p, vector) - (Vector2)Vector3.Slerp(lastRotation, rotation, timeStacker) * 3f;
        }
        public float conRad = 7f;
        public Vector2 lastRotation;
        [AllowNull] public Vector2[,] rag;
        public Vector2 rotation;
        public int ragSpriteIndex;
        public int hipsSpriteIndex;
    }
}