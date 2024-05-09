using System.Runtime.CompilerServices;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using static Spookcat.Plugin;

namespace Spookcat;
class SpookyCat
{
    static readonly ConditionalWeakTable<RainWorldGame, StrongBox<bool>> StartGame = new ConditionalWeakTable<RainWorldGame, StrongBox<bool>>();
    internal static void Apply() {
        On.Player.Die += Player_Die;
        On.Player.ctor += Player_ctor;
        On.Player.AddFood += Player_AddFood;
        On.Player.MovementUpdate += Player_MovementUpdate;
        On.Player.SpitOutOfShortCut += Player_SpitOutOfShortCut;
        IL.Player.Stun += IL_Player_Stun;
        IL.Creature.LoseAllGrasps += IL_Creature_LoseAllGrasps;
        On.Player.Update += Player_Update;
        On.RainWorldGame.ctor += RainWorldGame_ctor;
    }
    static void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager) {
        orig(self, manager);
        StartGame.Add(self, new StrongBox<bool>(false));
    }
    static void Player_Die(On.Player.orig_Die orig, Player self) {
        bool carriedByLizor = false;
        foreach (AbstractCreature crit in self.abstractCreature.Room.creatures) {
            if (crit.realizedCreature is Lizard || crit.realizedCreature is DropBug) {
                foreach (var stuck in crit.stuckObjects) {
                    if (((stuck.A is AbstractCreature aCrit && aCrit == self.abstractCreature) || (stuck.B is AbstractCreature bCrit && bCrit == self.abstractCreature)) && crit.InDen) {
                        carriedByLizor = true;
                    }
                }
            }
        }
        if (SpookyCWT.TryGetValue(self, out SpookcatEx spookcatEx) && !carriedByLizor) {
            spookcatEx.GetHurt(self);
            Room room = self.room;
            if (room == null)
            {
                room = self.abstractCreature.world.GetAbstractRoom(self.abstractCreature.pos).realizedRoom;
            }
            if (room != null)
            {
                if (room.game.setupValues.invincibility)
                {
                    return;
                }
                if (!self.dead)
                {
                    room.PlaySound(SoundID.UI_Slugcat_Die, self.mainBodyChunk.pos);
                }
            }
            float num = (self.bodyChunks[0].restrictInRoomRange == self.bodyChunks[0].defaultRestrictInRoomRange)? ((self.bodyMode == Player.BodyModeIndex.WallClimb)? -250f : -500f) : -self.bodyChunks[0].restrictInRoomRange + 1f;
            if (spookcatEx.hurtLevel >= 1f || 
                (self.bodyChunks[0].pos.y < num && 
                    (!self.room.water || self.room.waterInverted || self.room.defaultWaterLevel < -10)))
            {
                orig(self);
            }
        }
        else {
            orig(self);
        }
    }
    static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world) {
        orig(self, abstractCreature, world);
        if (self.slugcatStats.name == SpookyName && !SpookyCWT.TryGetValue(self, out SpookcatEx _)) {
            SpookyCWT.Add(self, new SpookcatEx());
        }
        if (SpookyCWT.TryGetValue(self, out var spookcatEx)) {
            if (self.slugcatStats != null)
            {
                self.slugcatStats.poleClimbSpeedFac = 2f;
                self.slugcatStats.corridorClimbSpeedFac = 2f;
            }
            spookcatEx.hurtLevel = 0f;
            self.redsIllness = null;
            if (!self.playerState.isGhost)
            {
                spookcatEx.spookIllness = new SpookIllness(self);
            }
        }
    }
    static void Player_AddFood(On.Player.orig_AddFood orig, Player self, int add) {
        if (SpookyCWT.TryGetValue(self, out var spookcatEx)) {
            if (spookcatEx.spookIllness != null)
            {
                spookcatEx.hurtLevel = 0f;
            }
        }
        orig(self, add);
    }
    static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu) {
        orig(self, eu);
        if (SpookyCWT.TryGetValue(self, out var _)) {
            self.airInLungs = 1f;
            self.dangerGraspLastThrowButton = false;
            if (self.dangerGraspTime > 20)
            {
                self.dangerGraspTime = 1;
            }
            if (self.bodyChunks[0].ContactPoint.x != 0 && self.bodyChunks[0].ContactPoint.x == self.input[0].x)
            {
                if (self.input[0].y > 0)
                {
                    self.customPlayerGravity = -0.1f;
                }
                else if (self.input[0].y < 0)
                {
                    self.customPlayerGravity = 0.5f;
                }
                else
                {
                    self.customPlayerGravity = 0.9f;
                }
            }
            else
            {
                self.customPlayerGravity = 0.9f;
            }
            if (self.standing)
            {
                self.slugcatStats.runspeedFac = 1.2f;
                return;
            }
            self.slugcatStats.runspeedFac = 1.78f;
        }
    }
    static void Player_SpitOutOfShortCut(On.Player.orig_SpitOutOfShortCut orig, Player self, IntVector2 pos, Room newRoom, bool spitOutAllSticks) {
        orig(self, pos, newRoom, spitOutAllSticks);
        if (SpookyCWT.TryGetValue(self, out var spookcatEx)) {
            spookcatEx.graphics.ResetRag((PlayerGraphics)self.graphicsModule);
        }
    }
    static void IL_Player_Stun(ILContext il) {
        var cursor = new ILCursor(il);
        var label = cursor.DefineLabel();
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate((Player self) => {
            if (SpookyCWT.TryGetValue(self, out var _)) {
                return true;
            }
            return false;
        });
        cursor.Emit(OpCodes.Brtrue, label);
        if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchRet())) {
            Logger.LogDebug("Spookcat match ret fail! Stun.");
            return;
        }
        cursor.MarkLabel(label);
    }
    static void IL_Creature_LoseAllGrasps(ILContext il) {
        var cursor = new ILCursor(il);
        var label = cursor.DefineLabel();
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate((Creature self) => {
            if (self is Player player && SpookyCWT.TryGetValue(player, out var _)) {
                return true;
            }
            return false;
        });
        cursor.Emit(OpCodes.Brtrue, label);
        if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchRet())) {
            Logger.LogDebug("Spookcat match ret fail! Grasps.");
            return;
        }
        cursor.MarkLabel(label);
    }
    static void Player_Update(On.Player.orig_Update orig, Player self, bool eu) {
        if (SpookyCWT.TryGetValue(self, out var spookcatEx)) {
            if (spookcatEx.spookIllness != null)
            {
                spookcatEx.spookIllness.Update();
            }
            if (self.dangerGraspTime > 0)
            {
                self.stun = 0;
                if (self.input[0].thrw) { self.ThrowToGetFree(eu); }
                if (self.input[0].pckp) { self.DangerGraspPickup(eu); }
            }
        }
        orig(self, eu);
        if (self.room != null && self.room.game.session is StoryGameSession && self.room.game.rainWorld.progression?.currentSaveState.saveStateNumber == SpookyName && self.room.game.rainWorld.progression.currentSaveState.cycleNumber == 0 && StartGame.TryGetValue(self.room.game, out StrongBox<bool> firstTime) && !firstTime.Value) {
            self.SuperHardSetPosition(self.room.MiddleOfTile(21, 7));
            firstTime.Value = true;
        }
    }
}