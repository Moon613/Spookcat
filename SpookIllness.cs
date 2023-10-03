using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Spookcat;
// This is basically a copy-paste of RedsIllness, but with a few changes. Would probably be easier to keep like this, instead of inheriting.
public class SpookIllness
{
	public SpookIllness(Player player)
	{
		this.player = player;
	}
	public float Severity
	{
		get
		{
			return Mathf.Lerp(0.3f, 0.75f, Mathf.InverseLerp(2720f, 120f, (float)counter) * 0.5f);
		}
	}
	public float CurrentFitIntensity
	{
		get
		{
			return Mathf.Pow(Mathf.Clamp01(Mathf.Sin(fit * Mathf.PI) * 1.2f), 1.6f) * fitSeverity;
		}
	}
	public void Update()
	{
		if (player.room == null)
		{
			return;
		}
		// if (!init) {
		// 	init = true;
			// Change here, doesn't set the player to be malnourished
		// }
		counter++;
		if (fit > 0f)
		{
			fit += 1f / fitLength;
			player.aerobicLevel = Mathf.Max(player.aerobicLevel, Mathf.Pow(CurrentFitIntensity, 1.5f));
			if (CurrentFitIntensity > 0.7f)
			{
				player.Blink(6);
			}
			if (fit > 1f)
			{
				fit = 0f;
			}
		}
		// Change here, uses curedTemporal instead of curedForTheCycle
		else if (!curedTemporal && 0.5 < (double)(1f / (60f + Mathf.Lerp(0.1f, 0.001f, Severity) * (float)counter)))
		{
			fitSeverity = Custom.SCurve(Mathf.Pow(Random.value, Mathf.Lerp(3.4f, 0.4f, Severity)), 0.7f);
			fitLength = Mathf.Lerp(80f, 240f, Mathf.Pow(Random.value, Mathf.Lerp(1.6f, 0.4f, (fitSeverity + Severity) / 2f)));
			fitSeverity = Mathf.Pow(fitSeverity, Mathf.Lerp(1.4f, 0.4f, Severity));
			fit += 1f / fitLength;
		}
		if (effect == null && SpookIllnessEffect.CanShowPlayer(player))
		{
			effect = new SpookIllnessEffect(this, player.room);
			player.room.AddObject(effect);
			return;
		}
		if (effect != null && (!SpookIllnessEffect.CanShowPlayer(player) || effect.slatedForDeletetion))
		{
			effect = null;
		}
	}
	public float TimeFactor
	{
		get
		{
			return 1f - 0.9f * Mathf.Max(Mathf.Max(0f, Mathf.InverseLerp(40f * Mathf.Lerp(12f, 21f, Severity), 40f, (float)counter) * Mathf.Lerp(0.2f, 0.5f, Severity)), CurrentFitIntensity * 0.5f);
		}
	}
	public void AbortFit()
	{
		fit = 0f;
	}
	public Player player;
	// public bool init;
	public int counter;
	private bool curedTemporal;
	public SpookIllnessEffect? effect;
	public float fit;
	public float fitLength;
	public float fitSeverity;
    public class SpookIllnessEffect : CosmeticSprite
	{
		public SpookIllnessEffect(SpookIllness illness, Room room)
		{
			this.illness = illness;
			this.room = room;
			rotDir = ((Random.value >= 0.5f) ? 1f : -1f);
		}
		// Completely Changed from original
		public float Intensity()
		{
            // Might potentially Error, if this causes problems then look here.
            Spookcat.SpookyCWT.TryGetValue(illness.player, out SpookcatEx spookcatEx);
			return Mathf.Lerp(0f, 0.65f, Mathf.SmoothStep(0f, 1f, spookcatEx.hurtLevel));
		}
		// Completely changed from original, but might be a game update thing.
		public static bool CanShowPlayer(Player player)
		{
			// Removes two checks, if the player is not in a shortcut, and not dead.
			return player.room != null && player.room.ViewedByAnyCamera(player.firstChunk.pos, 100f);
		}
		public override void Update(bool eu)
		{
			base.Update(eu);
			if (room == null)
			{
				return;
			}
			lastFade = fade;
			lastViableFade = viableFade;
			lastRot = rot;
			sin += 1f / Mathf.Lerp(120f, 30f, fluc3);
			fluc = Custom.LerpAndTick(fluc, fluc1, 0.02f, 0.016666668f);
			fluc1 = Custom.LerpAndTick(fluc1, fluc2, 0.02f, 0.016666668f);
			fluc2 = Custom.LerpAndTick(fluc2, fluc3, 0.02f, 0.016666668f);
			if (Mathf.Abs(fluc2 - fluc3) < 0.01f)
			{
				fluc3 = Random.value;
			}
			fade = Mathf.Pow(illness.CurrentFitIntensity * (0.85f + 0.15f * Mathf.Sin(sin * Mathf.PI * 2f)), Mathf.Lerp(1.5f, 0.5f, fluc));
			rot += rotDir * fade * (0.5f + 0.5f * fluc) * 7f * (0.1f + 0.9f * Mathf.InverseLerp(1f, 4f, Vector2.Distance(illness.player.firstChunk.lastLastPos, illness.player.firstChunk.pos)));
			if (!CanShowPlayer(illness.player) || illness.player.room != room || illness.effect != this)
			{
				viableFade = Mathf.Max(0f, viableFade - 0.033333335f);
				if (viableFade <= 0f && lastViableFade <= 0f)
				{
					illness.AbortFit();
					Destroy();
				}
			}
			else
			{
				viableFade = Mathf.Min(1f, viableFade + 0.033333335f);
				pos = (room.game.Players[0].realizedCreature.firstChunk.pos * 2f + room.game.Players[0].realizedCreature.bodyChunks[1].pos) / 3f;
			}
			if (fade == 0f && lastFade > 0f)
			{
				rotDir = ((Random.value >= 0.5f) ? 1f : -1f);
			}
			if (soundLoop == null && fade > 0f)
			{
				soundLoop = new DisembodiedDynamicSoundLoop(this);
				soundLoop.sound = SoundID.Reds_Illness_LOOP;
				soundLoop.VolumeGroup = 1;
				return;
			}
			if (soundLoop != null)
			{
				soundLoop.Update();
				soundLoop.Volume = Custom.LerpAndTick(soundLoop.Volume, Mathf.Pow((fade + illness.CurrentFitIntensity) / 2f, 0.5f), 0.06f, 0.14285715f);
			}
		}
		public override void Destroy()
		{
			base.Destroy();
			if (soundLoop != null && soundLoop.emitter != null)
			{
				soundLoop.emitter.slatedForDeletetion = true;
			}
		}
		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			base.InitiateSprites(sLeaser, rCam);
			sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite("Futile_White", true){ shader = rCam.game.rainWorld.Shaders["RedsIllness"] };
            AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("GrabShaders"));
		}
		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
			float num = Intensity();
			if (num == 0f)
			{
				sLeaser.sprites[0].isVisible = false;
				return;
			}
			sLeaser.sprites[0].isVisible = true;
			sLeaser.sprites[0].x = Mathf.Clamp(Mathf.Lerp(lastPos.x, pos.x, timeStacker) - camPos.x, 0f, rCam.sSize.x);
			sLeaser.sprites[0].y = Mathf.Clamp(Mathf.Lerp(lastPos.y, pos.y, timeStacker) - camPos.y, 0f, rCam.sSize.y);
			sLeaser.sprites[0].rotation = Mathf.Lerp(lastRot, rot, timeStacker);
			sLeaser.sprites[0].scaleX = (rCam.sSize.x * (6f - 3f * num) + 2f) / 16f;
			sLeaser.sprites[0].scaleY = (rCam.sSize.x * (6f - 3f * num) + 2f) / 16f;
			// Change here, moved the num's right one and made r=0
			sLeaser.sprites[0].color = new Color(0f, num, num, 0f);
		}
		private SpookIllness illness;
		public float fade;
		public float lastFade;
		public float viableFade;
		public float lastViableFade;
		private float rot;
		private float lastRot;
		private float rotDir;
		private float sin;
		public float fluc;
		public float fluc1;
		public float fluc2;
		public float fluc3;
		public DisembodiedDynamicSoundLoop? soundLoop;
	}
}
