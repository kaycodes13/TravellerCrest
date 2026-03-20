using GlobalSettings;
using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Silksong.FsmUtil;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TravellerCrest.Mechanics;

[HarmonyPatch]
internal static class Bind {

	private const string
		ADDING_LIFEBLOOD_VARNAME = $"{Id} Adding Lifeblood",
		SPAWNED_FLASH_VARNAME = $"{Id} Spawned Flash",
		SPAWNED_BUBBLES_VARNAME = $"{Id} Spawned Bubbles";

	private static readonly Color
		blueFlash = new(0.55f, 0.9f, 1f),
		blueBubble = new(0.196f, 0.824f, 1f);

	private static ToolItem.UsageOptions FleaBrew {
		get {
			if (_fleaBrew == null || !_fleaBrew.Value.ThrowPrefab)
				_fleaBrew =
					((ToolItemStatesLiquid)ToolItemManager.GetToolByName("Flea Brew"))
					.usableEmptyState.Usage;
			return _fleaBrew.Value;
		}
	}
	private static ToolItem.UsageOptions? _fleaBrew;

	private static GameObject? BubbleWhitePrefab {
		get {
			if (!_bubblesWhite)
				_bubblesWhite = CreateBubblePrefab(Color.white, $"{SifName} Bubbles Silk");
			return _bubblesWhite;
		}
	}
	private static GameObject? BubbleBluePrefab {
		get {
			if (!_bubblesBlue)
				_bubblesBlue = CreateBubblePrefab(blueBubble, $"{SifName} Bubbles Lifeblood");
			return _bubblesBlue;
		}
	}
	private static GameObject? _bubblesWhite, _bubblesBlue;

	/// <summary>
	/// Handler for <see cref="Needleforge.Data.CrestData.BindEvent"/>.
	/// </summary>
	internal static void OnBindStart(FsmInt masks, FsmInt numBinds, FsmFloat time, PlayMakerFSM fsm) {
		time.Value = 0.7f;
		if (Gameplay.MultibindTool.IsEquipped) {
			numBinds.Value = 2;
			time.Value = 0.8f;
		}
	}

	/// <summary>
	/// Edits to the bind FSM which control the number of masks/lifeblood healed in
	/// finer detail, and change the visual effects for binding.
	/// </summary>
	[HarmonyPatch(typeof(HeroController), nameof(HeroController.Start))]
	[HarmonyPrefix]
	private static void BindFSMEdits(HeroController __instance) {
		PlayMakerFSM fsm = __instance.gameObject.GetFsmPreprocessed("Bind")!;

		FsmBool
			addingLifeblood = fsm.GetBoolVariable(ADDING_LIFEBLOOD_VARNAME),
			isFirstLoop = fsm.FindBoolVariable("Is First Loop")!;
		FsmGameObject
			spawnedFlash = fsm.GetGameObjectVariable(SPAWNED_FLASH_VARNAME),
			spawnedBubbles = fsm.GetGameObjectVariable(SPAWNED_BUBBLES_VARNAME);
		FsmInt
			numBinds = fsm.FindIntVariable("Bind Amount")!,
			masks = fsm.FindIntVariable("Heal Amount")!;

		// Silk effects are now flea brew bubbles, recoloured according to whether
		// we're adding lifeblood or healing masks

		fsm.GetState("Bind Ground")!.AddMethod(ReplaceSilkEffects);
		fsm.GetState("Bind Air")!.AddMethod(ReplaceSilkEffects);

		void ReplaceSilkEffects() {
			addingLifeblood.Value = SifCrest.IsEquipped && PlayerData.instance.healthBlue < 2;

			if (!SifCrest.IsEquipped)
				return;

			__instance.transform
				.Find("Bind Effects/Bind Silk").gameObject
				.SetActive(false);

			GameObject? bubblePrefab = addingLifeblood.Value
				? BubbleBluePrefab : BubbleWhitePrefab;

			StopBubbles(spawnedBubbles.Value);
			spawnedBubbles.Value = SpawnBubbles(bubblePrefab!, __instance.gameObject);
		}

		// Any form of bind cancelling also cancels bubbles
		fsm.GetState("Cancel All")!.InsertMethod(0, () => StopBubbles(spawnedBubbles.Value));

		// Healing stops the bubble effect, throw an empty bottle on the last bind,
		// and heals different amount of lifeblood/masks depending on how many binds are
		// occurring and the player's prior lifeblood value.

		FsmState healState = fsm.GetState("Heal")!;

		int healIndex = healState.IndexFirstActionMatching(
			x => x is CallMethodProper action
				&& action.methodName.Value == nameof(HeroController.AddHealth)
		);

		healState.InsertMethod(healIndex, () => {
			if (!SifCrest.IsEquipped)
				return;

			int healthBlue = PlayerData.instance.healthBlue;

			if (addingLifeblood.Value) {
				masks.Value = 0;
				GameManager.instance.QueuedBlueHealth = 2 - healthBlue;
				EventRegister.SendEvent(EventRegisterEvents.AddQueuedBlueHealth);
				__instance.SpriteFlash.flashHealBlue();
			}
			else if (isFirstLoop.Value)
				masks.Value = 2;
			else
				masks.Value = 1;

			StopBubbles(spawnedBubbles.Value);

			if(numBinds.Value <= 1)
				ThrowTonicBottle();
		});

		// The screen flash after binding should be lifeblood blue if we healed lifeblood.

		int flashSpawnIndex = healState.IndexFirstActionMatching(
			x => x is SpawnObjectFromGlobalPool action
				&& action.gameObject.Value.name.Contains("White Flash")
		);

		healState.GetAction<SpawnObjectFromGlobalPool>(flashSpawnIndex)!
			.storeObject = spawnedFlash;

		healState.InsertMethod(flashSpawnIndex + 1, () => {
			var fader = spawnedFlash.Value.GetComponent<SimpleSpriteFade>();
			Color colour = (SifCrest.IsEquipped && addingLifeblood.Value)
				? blueFlash : Color.white;
			fader.fadeInColor = colour with { a = fader.fadeInColor.a };
			fader.normalColor = colour with { a = fader.normalColor.a };
		});
	}


	/// <summary>
	/// Makes Hornet throw a sized-down empty Flea Brew bottle over her shoulder.
	/// </summary>
	private static void ThrowTonicBottle() {
		GameObject bottle =
			FleaBrew.ThrowPrefab.Spawn(HeroController.instance.transform.position);

		Vector2 velocity = FleaBrew.ThrowVelocity;

		// hornet throwing it behind her is funny imo, don't invert this
		if (HeroController.instance.cState.facingRight)
			velocity = velocity with { x = -velocity.x };

		float
			velMag = velocity.magnitude * 0.7f,
			velAngle = velocity.normalized.DirectionToAngle(),
			randAngle = UnityEngine.Random.Range(-30, 30);

		velocity = velMag * (velAngle + randAngle).AngleToDirection();

		bottle.GetComponent<Rigidbody2D>().linearVelocity = velocity;
		bottle.transform.localScale = 0.75f * Vector3.one;
	}

	/// <summary>
	/// Spawns, and begins playing the particle systems of,
	/// a GameObject with a PlayParticleEffects component.
	/// </summary>
	private static GameObject SpawnBubbles(GameObject go, GameObject parent) {
		PlayParticleEffects bubbles = go.GetComponent<PlayParticleEffects>().Spawn();
		bubbles.transform.SetParent(parent.transform);
		bubbles.transform.SetLocalPosition2D(Vector2.zero);
		bubbles.PlayParticleSystems();
		return bubbles.gameObject;
	}

	/// <summary>
	/// Ends the particle systems of a GameObject with a PlayParticleEffects component.
	/// </summary>
	private static void StopBubbles(GameObject? go) {
		if (go && go.TryGetComponent<PlayParticleEffects>(out var ppe))
			ppe.StopParticleSystems();
	}

	/// <summary>
	/// Creates a prefab for the flea brew bubble particle effect, recoloured to the
	/// given colour. Only includes the continuous floating bubbles, not the burst.
	/// </summary>
	private static GameObject? CreateBubblePrefab(Color colour, string name) {
		if (HeroController.instance is HeroController hc) {
			GameObject go = UnityEngine.Object.Instantiate(hc.quickeningEffectPrefab.gameObject);
			go.SetActive(false);
			go.name = name;
			UnityEngine.Object.DontDestroyOnLoad(go);
			go.hideFlags = HideFlags.HideAndDontSave;

			var burst = go.transform.Find("Quickening_Burst").gameObject;
			UnityEngine.Object.DestroyImmediate(burst);

			RecolourParticles(go, colour);

			var partPlayer = go.GetComponent<PlayParticleEffects>();
			partPlayer.particleEffects = [.. partPlayer.particleEffects.Where(x => x)];

			return go;
		}
		return null;
	}

	/// <summary>
	/// Recolours all particle systems on all descendants of the given GameObject.
	/// </summary>
	private static GameObject RecolourParticles(GameObject go, Color colour) {
		foreach(Transform t in Descendants(go)) {
			if (!t.TryGetComponent<ParticleSystem>(out var partSystem))
				continue;

			var colourModule = partSystem.colorOverLifetime;
			if (!colourModule.enabled)
				continue;

			GradientColorKey[] colorkeys = [..
				colourModule.color.gradient.colorKeys
					.Select(x => new GradientColorKey(colour, x.time))
			];

			partSystem.colorOverLifetime.color.gradient
				.SetKeys(
					colorkeys,
					colourModule.color.gradient.alphaKeys
				);
		}
		return go;
	}

	/// <summary>
	/// Enumerates all the descendants of a GameObject.
	/// </summary>
	private static IEnumerable<Transform> Descendants(GameObject go) {
		foreach (Transform t in go.transform) {
			yield return t;
			foreach (Transform descendant in Descendants(t.gameObject))
				yield return descendant;
		}
	}

}
