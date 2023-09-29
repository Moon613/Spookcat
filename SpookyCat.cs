using System.Diagnostics;
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
        On.Player.Stun += Player_Stun;
        On.Creature.LoseAllGrasps += Creature_LoseAllGrasps;
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
        Debug.Log($"SpookCat: StaticWorld Template: {StaticWorld.creatureTemplates[1].type}");
        orig(self, abstractCreature, world);
        if (!SpookyCWT.TryGetValue(self, out SpookcatEx _)) {
            SpookyCWT.Add(self, new SpookcatEx());
        }
        if (SpookyCWT.TryGetValue(self, out var spookcatEx)) {
            self.abstractCreature.creatureTemplate.wormGrassImmune = true;  // This could be done with a shrimple IL to the two places this is used instead of modifying the template.
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
    // These two can be made better, into ILHooks at the very start of the method
    static void Player_Stun(On.Player.orig_Stun orig, Player self, int st) {
        if (!SpookyCWT.TryGetValue(self, out var _)) {
            orig(self, st);
        }
    }
    // These two can be made better, into ILHooks at the very start of the method
    static void Creature_LoseAllGrasps(On.Creature.orig_LoseAllGrasps orig, Creature self) {
        if (self is Player player && SpookyCWT.TryGetValue(player, out var _)) {
            return;
        }
        orig(self);
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