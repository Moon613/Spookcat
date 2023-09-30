using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using static Spookcat.Spookcat;
using Debug = UnityEngine.Debug;

namespace Spookcat;
class SpookyCat
{
    internal static void Apply() {
        On.Player.Die += Player_Die;
        On.Player.ctor += Player_ctor;
        On.Player.AddFood += Player_AddFood;
        On.Player.MovementUpdate += Player_MovementUpdate;
        On.Player.SpitOutOfShortCut += Player_SpitOutOfShortCut;
        IL.Player.Stun += IL_Player_Stun;
        IL.Creature.LoseAllGrasps += IL_Creature_LoseAllGrasps;
        On.Player.Update += Player_Update;
    }
    static void Player_Die(On.Player.orig_Die orig, Player self) {
        if (SpookyCWT.TryGetValue(self, out SpookcatEx spookcatEx)) {
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
                    room.PlaySound(SoundID.Rock_Hit_Creature, self.mainBodyChunk.pos);
                }
            }
            if (spookcatEx.hurtLevel >= 1f || (self.bodyChunks[0].pos.y < -self.bodyChunks[0].restrictInRoomRange + 1f && ((!self.Template.canFly && !self.room.water) || self.dead || !self.Template.canSwim || !self.room.water)))
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
        if (!SpookyCWT.TryGetValue(self, out SpookcatEx _)) {
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
                spookcatEx.spookIllness = new SpookIllness(self, 17);
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
        }
        orig(self, eu);
    }
}