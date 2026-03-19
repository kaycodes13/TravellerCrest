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

	private static HudFrameData Hud => SifCrest.HudFrame;

	private const string path = $"{nameof(TravellerCrest)}.Assets.Sprites.HUD";

	private static readonly Dictionary<int, MeterSprites> MetersByMaxHP = [];

	private record struct MeterSprites(Sprite fill, Sprite line);


	private static MeterCenterFill? meter;
	private static SimpleSpriteFade? glowFader;

	private static GameObject?
		meterGo,
		glowGo,
		burstGo;

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

		if (!Hud.ProfileIcon){
			Hud.ProfileIcon = AssetUtil.LoadSprite($"{path}.hud_profile.png", ppu: 100);
			Hud.SteelProfileIcon = AssetUtil.LoadSprite($"{path}.hud_ss_profile.png", ppu: 100);
		}

		if (MetersByMaxHP.Count == 0) {
			for (int i = 5; i <= 10; i++) {
				MetersByMaxHP.Add(i,
					new(
						AssetUtil.LoadSprite($"{path}.{i}hp_fill.png"),
						AssetUtil.LoadSprite($"{path}.{i}hp_line.png")
					)
				);
			}
		}

		Hud.OnRootCreated += BuildRoot;
		Hud.Coroutine = MeterCoro;
	}

	private static void BuildRoot() {
		HeroController.instance.InvokeNextFrame(FixPositionAndLayering);
		GameObject root = Hud.Root!;
		root.transform.SetScale2D(Vector2.zero);

		var activator = root.GetOrAddComponent<HudRootAnimator>();
		activator.hudroot = root;
		activator.crest = SifCrest;

		burstGo = new GameObject("Burst Anim") { layer = (int)PhysLayers.UI };
		burstGo.SetActive(false);
		burstGo.transform.parent = root.transform;
		burstGo.transform.localScale = 0.6f * Vector3.one;
		burstGo.transform.localPosition = Vector3.zero;
		burstGo.transform.SetLocalPositionZ(-0.0002f);
		burstGo.AddComponent<tk2dSprite>();
		var burstDeac = burstGo.AddComponent<DeactivateAfter2dtkAnimation>();
		burstDeac.animators = [burstGo.AddComponent<tk2dSpriteAnimator>()];
		var burstOrder = burstGo.AddComponent<MeshSortingOrder>();
		burstOrder.layerName = "Over";

		Shader blendModeScreen =
			Resources.FindObjectsOfTypeAll<Shader>()
			.FirstOrDefault(x => x.name == "UI/BlendModes/Screen");

		glowGo = new GameObject("Glow") { layer = (int)PhysLayers.UI };
		glowGo.transform.parent = root.transform;
		glowGo.transform.localScale = Vector3.one;
		glowGo.transform.localPosition = Vector3.zero;
		glowGo.transform.SetLocalPositionZ(-0.0001f);
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

		int
			maxHp = PlayerData.instance.maxHealth,
			minKey = MetersByMaxHP.Keys.Min(),
			maxKey = MetersByMaxHP.Keys.Max(),
			key = Mathf.Clamp(maxHp, minKey, maxKey);

		MeterSprites meterSprites = MetersByMaxHP[key];

		meterGo = new("Meter") { layer = (int)PhysLayers.UI };
		meterGo.transform.parent = root.transform;
		meterGo.transform.localScale = Vector3.one;
		meterGo.transform.localPosition = Vector3.zero;
		meter = meterGo.AddComponent<MeterCenterFill>();
		meter.Fill = meterSprites.fill;
		meter.Line = meterSprites.line;
		meter.Backboard = AssetUtil.LoadSprite($"{path}.backboard.png");
		meter.FillMask = AssetUtil.LoadSprite($"{path}.fill_mask.png");
		meter.Min = 0;
		meter.Max = maxKey - 1;
		meter.Value = 0;
	}

	/// <summary>
	/// Sets the scale and coordinates of the hud root and hud root container to 
	/// consistent values that are easy to work with. I really should be fixing these
	/// things in Needleforge, not here, but that's a task for another day.
	/// </summary>
	private static void FixPositionAndLayering() {
		Transform
			root = Hud.Root!.transform,
			rootParent = root.parent,
			hudFrame = rootParent.parent;

		rootParent.localScale = Vector3.one;
		rootParent.localPosition = Vector3.zero;

		root.SetLocalPosition2D(-1.525f, 0.15f);

		// critical for layering the hud frame, meter, and full-silk-orb correctly
		root.SetPositionZ(hudFrame.position.z - 0.0001f);
	}


	private static IEnumerator MeterCoro(BindOrbHudFrame hudInstance) {
		PlayerData pd = PlayerData.instance;

		var burstanim = burstGo!.GetOrAddComponent<tk2dSpriteAnimator>();
		burstanim.library = hudInstance.animator.library;
		burstanim.DefaultClipId = hudInstance.animator.library.GetClipIdByName("Soul Burst Q");

		int
			minKey = MetersByMaxHP.Keys.Min(),
			maxKey = MetersByMaxHP.Keys.Max(),
			prevMax = pd.maxHealth,
			maxMissing = pd.maxHealth - 1,
			prevMissing = pd.maxHealth - pd.health;

		AudioSource sfxPrefab = GlobalSettings.Audio.DefaultUIAudioSourcePrefab;
		Vector3 husPos = hudInstance.transform.position;
		AudioEvent
			appearAudio = hudInstance.wandererHarpAppearAudio,
			disappearAudio = hudInstance.wandererHarpDisappearAudio;

		meter!.ValueToScaleFn = (val, min, max) => {
			if (val <= min)
				return 0;
			else if (val >= max || val >= maxMissing)
				return 2;
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
				int key = Mathf.Clamp(pd.maxHealth, minKey, maxKey);

				var entry = MetersByMaxHP[key];

				meter!.Line = entry.line;
				meter!.Fill = entry.fill;
				maxMissing = pd.maxHealth - 1;
			}

			int missing = pd.maxHealth - pd.health;

			if (prevMissing != missing) {

				if (missing == maxMissing) {
					burstGo!.SetActive(false);
					burstGo!.SetActive(true);
					glowGo!.SetActive(true);
					appearAudio.SpawnAndPlayOneShot(sfxPrefab, husPos);
					glowFader!.FadeIn();
				}
				else if (prevMissing == maxMissing) {
					disappearAudio.SpawnAndPlayOneShot(sfxPrefab, husPos);
					glowFader!.FadeOut();
				}

				meter!.Value = prevMissing = missing;
			}

			yield return null;
		}
	}

}
