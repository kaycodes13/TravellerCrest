using GlobalEnums;
using Needleforge.Data;
using Silksong.UnityHelper.Extensions;
using System.Collections;
using TravellerCrest.Components;
using TravellerCrest.Data;
using UnityEngine;
using UnityEngine.Rendering;
using static TravellerCrest.Utils.AssetUtil;
using static UnityEngine.ParticleSystem;

namespace TravellerCrest.Mechanics;

internal static class HUD {

	static HudFrameData Hud => SifCrest.HudFrame;

		const string path = $"{nameof(TravellerCrest)}.Assets.Sprites";

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


		Hud.ProfileIcon = LoadSprite($"{path}.hud_profile.png", ppu: 100);
		Hud.SteelProfileIcon = LoadSprite($"{path}.hud_ss_profile.png", ppu: 100);

		Hud.OnRootCreated += BuildRoot;
		Hud.Coroutine = MeterCoro;
	}

	private static void BuildRoot() {
		GameObject root = Hud.Root!;
		AddSortingGroup(root, 0);
		root.transform.localPosition = new(-1.54f, 0.15f, 0);

		var (meterObj, meterRend)
			= NewHudElement<SpriteRenderer>("meter", order: 0);
		meterRend.sprite = LoadSprite($"{path}.hud_meter.png");
		meterRend.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

		var (maskObj, maskRend)
			= NewHudElement<SpriteMask>("mask", order: 1);
		maskRend.sprite = LoadSprite($"{path}.hud_meter_mask.png");
		maskObj.transform.localScale = Vector3.zero;

		var (burstGo, _)
			= NewHudElement<MeshRenderer>("burst", order: 10);
		burstGo.SetActive(false);
		var burstDeac = burstGo.AddComponent<DeactivateAfter2dtkAnimation>();
		burstDeac.animators = [burstGo.AddComponent<tk2dSpriteAnimator>()];
		burstGo.AddComponent<tk2dSprite>();
		burstGo.transform.localScale = 0.6f * Vector3.one;

		HeroController.instance.InvokeNextFrame(FixPositionAndLayering);
	}

	private static IEnumerator MeterCoro(BindOrbHudFrame hudInstance) {
		PlayerData pd = PlayerData.instance;
		GameObject
			mask = Hud.Root!.FindChild("mask")!,
			burst = Hud.Root!.FindChild("burst")!;

		var burstanim = burst.GetOrAddComponent<tk2dSpriteAnimator>();
		burstanim.library = hudInstance.animator.library;
		burstanim.DefaultClipId = hudInstance.animator.library.GetClipIdByName("Soul Burst Q");

		while (true) {
			if (HeroController.instance.IsPaused()) {
				yield return null;
				continue;
			}
			int masksMissing = pd.maxHealth - pd.health;
			if (masksMissing == 9) {
				if (mask.transform.localScale != Vector3.one) {
					burst.SetActive(false);
					burst.SetActive(true);
					hudInstance.wandererHarpAppearAudio.SpawnAndPlayOneShot(
						GlobalSettings.Audio.DefaultUIAudioSourcePrefab,
						hudInstance.transform.position
					);
				}
				mask.transform.localScale = Vector3.one;
			}
			else {
				mask.transform.localScale = (masksMissing / 10f) * Vector3.one;
			}
			yield return null;
		}
	}

	#region Local Utilities

	private static SortingGroup AddSortingGroup(GameObject go, int order) {
		var group = go.AddComponent<SortingGroup>();
		group.sortingLayerName = "Over";
		group.sortingOrder = order;
		return group;
	}

	private static GameObject NewSortingGroup(
		string name, int order, GameObject? group = null
	) {
		GameObject elt = new(name);

		if (group)
			elt.transform.SetParent(group!.transform);
		else
			elt.transform.SetParent(Hud.Root!.transform);

		elt.transform.localScale = Vector3.one;
		elt.transform.localPosition = Vector3.zero;
		elt.layer = (int)PhysLayers.UI;

		AddSortingGroup(elt, order);

		return elt;
	}

	private static (GameObject, T) NewHudElement<T>(
		string name, int order, GameObject? group = null
	) where T : Renderer {
		GameObject elt = new(name);

		if (group)
			elt.transform.SetParent(group!.transform);
		else
			elt.transform.SetParent(Hud.Root!.transform);

		elt.transform.localScale = Vector3.one;
		elt.transform.localPosition = Vector3.zero;
		elt.layer = (int)PhysLayers.UI;

		T renderer = elt.AddComponent<T>();
		renderer.sortingLayerName = "Over";
		renderer.sortingOrder = order;

		return (elt, renderer);
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

		var activator = rootParent.gameObject.GetOrAddComponent<HudRootActivator>();
		activator.hudroot = root.gameObject;
		activator.crest = SifCrest;

		root.localScale = Vector3.one;

		// critical for layering the hud frame, meter, and full-silk-orb correctly
		float bindOrbZ = hudFrame.position.z;
		Vector3 rootPos = root.position;
		root.position = new(rootPos.x, rootPos.y, bindOrbZ - 0.00001f);
	}

	#endregion

}
