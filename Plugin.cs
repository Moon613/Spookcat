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
using Menu;

#pragma warning disable CS0618
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace Spookcat;

[BepInPlugin("moon.spookcat", "Spookcat", "1.0.1")]
class Plugin : BaseUnityPlugin
{
    static bool init;
    [AllowNull] new internal static ManualLogSource Logger;
    public static ConditionalWeakTable<Player, SpookcatEx> SpookyCWT = new ConditionalWeakTable<Player, SpookcatEx>();
    public static ConditionalWeakTable<MenuScene, MenuSceneEx> MenuCWT = new ConditionalWeakTable<MenuScene, MenuSceneEx>();
    public static SlugcatStats.Name SpookyName = new SlugcatStats.Name("spooky");
    static readonly MenuScene.SceneID MENUSCENENAME = new("spookslug");
    public Plugin() {
        Logger = base.Logger;
    }
    public void OnEnable()
    {
        SpookyGraphics.Apply();
        SpookyCat.Apply();
        ScareEverything.Apply();    // Makes everything scared of Spookcat, which I think was part of the original intention of changing the Slugcat's template?
        IL.MiniFly.ViableForBuzzaround += IL_MiniFly_ViableForBuzzaround;
        On.RoomCamera.LoadPalette += RoomCamera_LoadPalette;
        IL.Room.Loaded += IL_Room_Loaded;
        IL.WormGrass.WormGrassPatch.Update += IL_WormGrass_WormGrassPatch_Update;
        On.Menu.MenuScene.Update += Menu_MenuScene_Update;
        On.Menu.MenuScene.ctor += Menu_MenuScene_ctor;
        On.RainWorld.OnModsInit += RainWorld_OnModsInit;
        IL.HUD.FoodMeter.UpdateShowCount += IL_FoodMeter_UpdateShowCount;
    }
    static void Menu_MenuScene_ctor(On.Menu.MenuScene.orig_ctor orig, MenuScene self, Menu.Menu menu, MenuObject owner, MenuScene.SceneID sceneID) {
        orig(self, menu, owner, sceneID);
        if (!MenuCWT.TryGetValue(self, out var _) && sceneID == MENUSCENENAME) {
            MenuCWT.Add(self, new());
        }
    }
    static void Menu_MenuScene_Update(On.Menu.MenuScene.orig_Update orig, MenuScene self) {
        orig(self);
        if (!MenuCWT.TryGetValue(self, out var menuEx) && !self.depthIllustrations.Exists(i => i.fileName.Contains("bkg5")))
        {
            return;
        }
        menuEx.timer++;
        menuEx.soundCooldown--;
        if (self.menu is SlugcatSelectMenu menu) {
            int index = self.depthIllustrations.FindIndex(i => i.fileName.Contains("bkg5"));
            if (index < 0) { return; }
            float green = SpookyOptions.FlashingLights.Value? Mathf.Max(-0.1f,  1f - (2 * Mathf.Abs( Mathf.Sin( 2f * Mathf.PI * Mathf.Cos( ( menuEx.timer + 4 * Mathf.Sin(1.25f * menuEx.timer)) / 128f)) ))) : 1f;
            self.depthIllustrations[index].sprite.color = new Color(0f, green, 0f, 1f);
            if (green >= 0.99f && menuEx.soundCooldown <= 0 && menu.slugcatPages.FindIndex(page => page.slugcatNumber == SpookyName) == menu.slugcatPageIndex) {
                self.menu.PlaySound(SoundID.Thunder_Close, 0f, 0.7f, 0.7f);
                menuEx.soundCooldown = SpookyOptions.FlashingLights.Value? 40 : 600;
            }
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
    static void RoomCamera_LoadPalette(On.RoomCamera.orig_LoadPalette orig, RoomCamera self, int pal, ref Texture2D texture) {
        orig(self, (self.game?.session is StoryGameSession session && session.saveStateNumber == SpookyName)? 56 : pal, ref texture);
    }
    static void IL_FoodMeter_UpdateShowCount(ILContext il) {
        var cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.After, i => i.MatchLdcI4(40), i => i.MatchStfld(out _))) {
            Logger.LogDebug("Spookcat: Failed to match Food IL");
            return;
        }
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate((FoodMeter self) => {
            try {
                if (self.hud.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.session is StoryGameSession session && session.saveStateNumber == SpookyName) {
                    self.eatCircleDelay = 20;
                }
            } catch (Exception err) {
                Logger.LogDebug($"Spookcat Exception!:\n{err}");
                Debug.Log(err);
            }
        });
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
                if (self.game.session is StoryGameSession session && session.saveStateNumber == SpookyName) {
                    if (self.lightning == null && SpookyOptions.FlashingLights.Value)
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
                }
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
    static void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self) {
        orig(self);
        if (!init) {
            MachineConnector.SetRegisteredOI("moon.spookcat", new SpookyOptions());
            init = true;
        }
    }
}
class MenuSceneEx
{
    public int timer;
    public int soundCooldown;
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
    }
}