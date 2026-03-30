using GlobalEnums;
using Needleforge.Data;
using Silksong.UnityHelper.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TravellerCrest.Components;
using TravellerCrest.Data;
using TravellerCrest.Utils;
using UnityEngine;

namespace TravellerCrest.Mechanics;

internal static class HUD {

	static HudFrameData Hud => SifCrest.HudFrame;
	const string path = $"{nameof(TravellerCrest)}.Assets.Sprites.HUD";

	static readonly Dictionary<int, MeterSprites> MetersByMaxHP = [];
	static MeterCenterFill? meter;
	static SimpleSpriteFade? glowFader;
	static GameObject? meterGo, glowGo, burstGo;

	record struct MeterSprites(Sprite Fill, Sprite Line);

	internal static void Setup() {
		tk2dSpriteAnimation
			mainLib = AnimationManager.MainLib,
			steelLib = AnimationManager.libraries["steel"];

		Hud.Appear = mainLib.GetClipByName("Traveller HUD Appear");
		Hud.Idle = mainLib.GetClipByName("Traveller HUD Idle");
		Hud.Disappear = mainLib.GetClipByName("Traveller HUD Disappear");

		Hud.SteelAppear = steelLib.GetClipByName("Traveller HUD Appear");
		Hud.SteelIdle = steelLib.GetClipByName("Traveller HUD Idle");
		Hud.SteelDisappear = steelLib.GetClipByName("Traveller HUD Disappear");

		if (!Hud.ProfileIcon) {
			Hud.ProfileIcon = AssetUtil.LoadSprite($"{path}.hud_profile.png", ppu: 100);
			Hud.SteelProfileIcon = AssetUtil.LoadSprite($"{path}.hud_ss_profile.png", ppu: 100);
		}

		if (MetersByMaxHP.Count == 0) {
			for (int i = 5; i <= 10; i++)
				MetersByMaxHP.Add(i, new(
					AssetUtil.LoadSprite($"{path}.{i}hp_fill.png"),
					AssetUtil.LoadSprite($"{path}.{i}hp_line.png")
				));
		}

		Hud.OnRootCreated += BuildRoot;
		Hud.Coroutine = MeterCoro;
	}

	static void BuildRoot() {
		GameObject root = Hud.Root!;
		root.transform.SetScale2D(Vector2.zero);
		root.transform.localPosition = new(-1.525f, 0.15f);

		var rootAnim = root.GetOrAddComponent<HudRootAnimator>();
		rootAnim.hudroot = root;
		rootAnim.crest = SifCrest;

		burstGo = NewGO("Burst", 0.6f * Vector3.one, new(0, 0, -0.0002f));
		glowGo = NewGO("Glow", Vector3.one, new(0, 0, -0.0001f));
		meterGo = NewGO("Meter", Vector3.one, Vector3.zero);

		GameObject NewGO(string name, Vector3 scale, Vector3 position) {
			var go = new GameObject(name) { layer = (int)PhysLayers.UI };
			go.transform.SetParent(root.transform);
			go.transform.localScale = scale;
			go.transform.localPosition = position;
			return go;
		}

		burstGo.SetActive(false);
		burstGo.AddComponent<tk2dSprite>();
		var burstDeac = burstGo.AddComponent<DeactivateAfter2dtkAnimation>();
		burstDeac.animators = [burstGo.AddComponent<tk2dSpriteAnimator>()];
		burstGo.AddComponent<MeshSortingOrder>().layerName = "Over";

		Shader blendModeScreen =
			Resources.FindObjectsOfTypeAll<Shader>()
			.FirstOrDefault(x => x.name == "UI/BlendModes/Screen");

		var glowRend = glowGo.AddComponent<SpriteRenderer>();
		glowRend.sortingLayerName = "Over";
		glowRend.sprite = AssetUtil.LoadSprite($"{path}.glow.png");
		glowRend.color = new Color(0, 0, 0, 0);
		if (blendModeScreen)
			glowRend.material = new(blendModeScreen);
		glowFader = glowGo.AddComponent<SimpleSpriteFade>();
		glowFader.fadeInColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
		glowFader.normalColor = glowRend.color;
		glowFader.fadeDuration = 0.08f;

		int maxHp = PlayerData.instance.maxHealth,
			minKey = MetersByMaxHP.Keys.Min(),
			maxKey = MetersByMaxHP.Keys.Max(),
			key = Mathf.Clamp(maxHp, minKey, maxKey);

		MeterSprites meterSprites = MetersByMaxHP[key];

		meter = meterGo.AddComponent<MeterCenterFill>();
		meter.Fill = meterSprites.Fill;
		meter.Line = meterSprites.Line;
		meter.Backboard = AssetUtil.LoadSprite($"{path}.backboard.png");
		meter.FillMask = AssetUtil.LoadSprite($"{path}.fill_mask.png");
		meter.Min = 0;
		meter.Max = maxKey - 1;
		meter.Value = 0;
	}

	static IEnumerator MeterCoro(BindOrbHudFrame hudInstance) {
		var pd = PlayerData.instance;

		var burstanim = burstGo!.GetOrAddComponent<tk2dSpriteAnimator>();
		burstanim.library = hudInstance.animator.library;
		burstanim.DefaultClipId = hudInstance.animator.library.GetClipIdByName("Soul Burst Q");

		int minKey = MetersByMaxHP.Keys.Min(),
			maxKey = MetersByMaxHP.Keys.Max(),
			prevMax = pd.maxHealth,
			maxMissing = pd.maxHealth - 1,
			prevMissing = pd.maxHealth - pd.health;

		AudioSource sfxPrefab = GlobalSettings.Audio.DefaultUIAudioSourcePrefab;
		Vector3 hudPos = hudInstance.transform.position;
		AudioEvent
			appearAudio = hudInstance.wandererHarpAppearAudio,
			disappearAudio = hudInstance.wandererHarpDisappearAudio;

		meter!.ValueToScaleFn = (val, min, max) => {
			if (val <= min)
				return 0;
			else if (val >= max || val >= maxMissing)
				return 1.25f;
			else
				return val / 12f + 1 / 10f;
		};

		while (true) {
			if (HeroController.instance.IsPaused()) {
				yield return null;
				continue;
			}

			if (prevMax != pd.maxHealth) {
				prevMax = pd.maxHealth;
				prevMissing = -1;
				maxMissing = pd.maxHealth - 1;

				var entry = MetersByMaxHP[Mathf.Clamp(pd.maxHealth, minKey, maxKey)];
				meter!.Line = entry.Line;
				meter!.Fill = entry.Fill;
			}

			int missing = pd.maxHealth - pd.health;

			if (prevMissing != missing) {
				if (missing == maxMissing) {
					burstGo!.SetActive(false);
					burstGo!.SetActive(true);
					glowGo!.SetActive(true);
					appearAudio.SpawnAndPlayOneShot(sfxPrefab, hudPos);
					glowFader!.FadeIn();
				}
				else if (prevMissing == maxMissing) {
					disappearAudio.SpawnAndPlayOneShot(sfxPrefab, hudPos);
					glowFader!.FadeOut();
				}
				meter!.Value = prevMissing = missing;
			}

			yield return null;
		}
	}

}
