using System;
using UnityEngine;
using RWCustom;
using Random = UnityEngine.Random;
using static Spookcat.Spookcat;

namespace Spookcat;
class SpookyGraphics
{
    internal static void Apply() {
        On.PlayerGraphics.ctor += PlayerGraphics_ctor;
        On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
        On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;
        On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
        On.PlayerGraphics.Update += PlayerGraphics_Update;
    }
    static void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow) {
        orig(self, ow);
        if (SpookyCWT.TryGetValue(self.player, out var spookcatEx)) {
            spookcatEx.graphics.rag = new Vector2[Random.Range(4, Random.Range(4, 5)), 6];
        }
    }
    static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) {
        orig(self, sLeaser, rCam);
        if (SpookyCWT.TryGetValue(self.player, out var spookcatEx)) {
            spookcatEx.graphics.ragSpriteIndex = sLeaser.sprites.Length;
            Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length+1);
            sLeaser.sprites[spookcatEx.graphics.ragSpriteIndex] = TriangleMesh.MakeLongMesh(spookcatEx.graphics.rag.GetLength(0), false, false);
            sLeaser.sprites[spookcatEx.graphics.ragSpriteIndex].shader = rCam.game.rainWorld.Shaders["JaggedSquare"];
            sLeaser.sprites[spookcatEx.graphics.ragSpriteIndex].alpha = rCam.game.SeededRandom((int)Random.value);
            self.AddToContainer(sLeaser, rCam, null);
        }
    }
    static void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer) {
        orig(self, sLeaser, rCam, newContainer);
        if (SpookyCWT.TryGetValue(self.player, out var spookcatEx) && sLeaser.sprites.Length > 13) {
            rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[spookcatEx.graphics.ragSpriteIndex]);
            sLeaser.sprites[1].MoveBehindOtherNode(sLeaser.sprites[4]);
        }
    }
    static void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) {
        orig(self, sLeaser, rCam, palette);
        if (SpookyCWT.TryGetValue(self.player, out var spookcatEx)) {
            sLeaser.sprites[2].color = new Color(0f, 0f, 0f);
            sLeaser.sprites[spookcatEx.graphics.ragSpriteIndex].color = Color.Lerp(new Color(1f, 0.05f, 0.04f), palette.blackColor, (0.1f + 0.8f * palette.darkness) * 0.4f);
            Color a = Custom.HSL2RGB(0.249f, 0.5f, 0.45f);
            sLeaser.sprites[9].color = Color.Lerp(a, palette.blackColor, (0.1f + 0.8f * palette.darkness) * 0.4f);
        }
    }
    static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos) {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        if (SpookyCWT.TryGetValue(self.player, out var spookcatEx)) {
            float num = 0f;
            Vector2 a = spookcatEx.graphics.TongueAttachPos(timeStacker, self);
            for (int i = 0; i < spookcatEx.graphics.rag.GetLength(0); i++)
            {
                float f = (float)i / (float)(spookcatEx.graphics.rag.GetLength(0) - 1);
                Vector2 vector = Vector2.Lerp(spookcatEx.graphics.rag[i, 1], spookcatEx.graphics.rag[i, 0], timeStacker);
                float num2 = (2f + 2f * Mathf.Sin(Mathf.Pow(f, 2f) * 3.1415927f)) * Vector3.Slerp(spookcatEx.graphics.rag[i, 4], spookcatEx.graphics.rag[i, 3], timeStacker).x;
                Vector2 normalized = (a - vector).normalized;
                Vector2 a2 = Custom.PerpendicularVector(normalized);
                float d = Vector2.Distance(a, vector) / 5f;
                (sLeaser.sprites[spookcatEx.graphics.ragSpriteIndex] as TriangleMesh)?.MoveVertice(i * 4, a - normalized * d - a2 * (num2 + num) * 0.5f - camPos);
                (sLeaser.sprites[spookcatEx.graphics.ragSpriteIndex] as TriangleMesh)?.MoveVertice(i * 4 + 1, a - normalized * d + a2 * (num2 + num) * 0.5f - camPos);
                (sLeaser.sprites[spookcatEx.graphics.ragSpriteIndex] as TriangleMesh)?.MoveVertice(i * 4 + 2, vector + normalized * d - a2 * num2 - camPos);
                (sLeaser.sprites[spookcatEx.graphics.ragSpriteIndex] as TriangleMesh)?.MoveVertice(i * 4 + 3, vector + normalized * d + a2 * num2 - camPos);
                a = vector;
                num = num2;
            }
            sLeaser.sprites[9].element = Futile.atlasManager.GetElementWithName("FaceDead");
            sLeaser.sprites[2].isVisible = false;
        }
    }
    static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self) {
        orig(self);
        if (SpookyCWT.TryGetValue(self.player, out var spookcatEx)) {
            spookcatEx.graphics.lastRotation = spookcatEx.graphics.rotation;
            float ang = Custom.AimFromOneVectorToAnother(self.drawPositions[1, 1], self.head.pos);
            spookcatEx.graphics.rotation = Custom.DegToVec(ang);
            for (int i = 0; i < spookcatEx.graphics.rag.GetLength(0); i++)
            {
                float t = (float)i / (float)(spookcatEx.graphics.rag.GetLength(0) - 1);
                spookcatEx.graphics.rag[i, 1] = spookcatEx.graphics.rag[i, 0];
                spookcatEx.graphics.rag[i, 0] += spookcatEx.graphics.rag[i, 2];
                spookcatEx.graphics.rag[i, 2] -= spookcatEx.graphics.rotation * Mathf.InverseLerp(1f, 0f, (float)i) * 0.8f;
                spookcatEx.graphics.rag[i, 4] = spookcatEx.graphics.rag[i, 3];
                spookcatEx.graphics.rag[i, 3] = (spookcatEx.graphics.rag[i, 3] + spookcatEx.graphics.rag[i, 5] * Custom.LerpMap(Vector2.Distance(spookcatEx.graphics.rag[i, 0], spookcatEx.graphics.rag[i, 1]), 1f, 18f, 0.05f, 0.3f)).normalized;
                spookcatEx.graphics.rag[i, 5] = (spookcatEx.graphics.rag[i, 5] + Custom.RNV() * Random.value * Mathf.Pow(Mathf.InverseLerp(1f, 18f, Vector2.Distance(spookcatEx.graphics.rag[i, 0], spookcatEx.graphics.rag[i, 1])), 0.3f)).normalized;
                if (self.owner.room.PointSubmerged(spookcatEx.graphics.rag[i, 0]))
                {
                    spookcatEx.graphics.rag[i, 2] *= Custom.LerpMap(spookcatEx.graphics.rag[i, 2].magnitude, 1f, 10f, 1f, 0.5f, Mathf.Lerp(1.4f, 0.4f, t));
                    spookcatEx.graphics.rag[i, 2].y += 0.05f;
                    spookcatEx.graphics.rag[i, 2] += Custom.RNV() * 0.1f;
                }
                else
                {
                    spookcatEx.graphics.rag[i, 2] *= Custom.LerpMap(Vector2.Distance(spookcatEx.graphics.rag[i, 0], spookcatEx.graphics.rag[i, 1]), 1f, 6f, 0.999f, 0.7f, Mathf.Lerp(1.5f, 0.5f, t));
                    spookcatEx.graphics.rag[i, 2].y -= self.owner.room.gravity * Custom.LerpMap(Vector2.Distance(spookcatEx.graphics.rag[i, 0], spookcatEx.graphics.rag[i, 1]), 1f, 6f, 0.6f, 0f);
                    if (i % 3 == 2 || i == spookcatEx.graphics.rag.GetLength(0) - 1)
                    {
                        SharedPhysics.TerrainCollisionData terrainCollisionData = new SharedPhysics.TerrainCollisionData(spookcatEx.graphics.rag[i, 0], spookcatEx.graphics.rag[i, 1], spookcatEx.graphics.rag[i, 2], 1f, new IntVector2(0, 0), false);
                        terrainCollisionData = SharedPhysics.HorizontalCollision(self.owner.room, terrainCollisionData);
                        terrainCollisionData = SharedPhysics.VerticalCollision(self.owner.room, terrainCollisionData);
                        terrainCollisionData = SharedPhysics.SlopesVertically(self.owner.room, terrainCollisionData);
                        spookcatEx.graphics.rag[i, 0] = terrainCollisionData.pos;
                        spookcatEx.graphics.rag[i, 2] = terrainCollisionData.vel;
                        if (terrainCollisionData.contactPoint.x != 0)
                        {
                            spookcatEx.graphics.rag[i, 2].y *= 0.6f;
                        }
                        if (terrainCollisionData.contactPoint.y != 0)
                        {
                            spookcatEx.graphics.rag[i, 2].x *= 0.6f;
                        }
                    }
                }
            }
            for (int j = 0; j < spookcatEx.graphics.rag.GetLength(0); j++)
            {
                if (j > 0)
                {
                    Vector2 normalized = (spookcatEx.graphics.rag[j, 0] - spookcatEx.graphics.rag[j - 1, 0]).normalized;
                    float num = Vector2.Distance(spookcatEx.graphics.rag[j, 0], spookcatEx.graphics.rag[j - 1, 0]);
                    float d = (num <= spookcatEx.graphics.conRad) ? 0.25f : 0.5f;
                    spookcatEx.graphics.rag[j, 0] += normalized * (spookcatEx.graphics.conRad - num) * d;
                    spookcatEx.graphics.rag[j, 2] += normalized * (spookcatEx.graphics.conRad - num) * d;
                    spookcatEx.graphics.rag[j - 1, 0] -= normalized * (spookcatEx.graphics.conRad - num) * d;
                    spookcatEx.graphics.rag[j - 1, 2] -= normalized * (spookcatEx.graphics.conRad - num) * d;
                    if (j > 1)
                    {
                        normalized = (spookcatEx.graphics.rag[j, 0] - spookcatEx.graphics.rag[j - 2, 0]).normalized;
                        spookcatEx.graphics.rag[j, 2] += normalized * 0.2f;
                        spookcatEx.graphics.rag[j - 2, 2] -= normalized * 0.2f;
                    }
                    if (j < spookcatEx.graphics.rag.GetLength(0) - 1)
                    {
                        spookcatEx.graphics.rag[j, 3] = Vector3.Slerp(spookcatEx.graphics.rag[j, 3], (spookcatEx.graphics.rag[j - 1, 3] * 2f + spookcatEx.graphics.rag[j + 1, 3]) / 3f, 0.1f);
                        spookcatEx.graphics.rag[j, 5] = Vector3.Slerp(spookcatEx.graphics.rag[j, 5], (spookcatEx.graphics.rag[j - 1, 5] * 2f + spookcatEx.graphics.rag[j + 1, 5]) / 3f, Custom.LerpMap(Vector2.Distance(spookcatEx.graphics.rag[j, 1], spookcatEx.graphics.rag[j, 0]), 1f, 8f, 0.05f, 0.5f));
                    }
                }
                else
                {
                    spookcatEx.graphics.rag[j, 0] = spookcatEx.graphics.TongueAttachPos(1f, self);
                    spookcatEx.graphics.rag[j, 2] *= 0f;
                }
            }
        }
    }
}