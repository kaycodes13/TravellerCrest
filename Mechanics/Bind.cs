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
	[HarmonyPatch(typeof(HeroController), "Start")]
	[HarmonyPrefix]
	static void BindFSMEdits(HeroController __instance) {
		PlayMakerFSM fsm = __instance.gameObject.GetFsmPreprocessed("Bind")!;

		FsmBool
			doHealBlue = fsm.GetBoolVariable($"Adding Lifeblood {Id}"),
			isFirstLoop = fsm.FindBoolVariable("Is First Loop")!;
		FsmGameObject
			flashObj = fsm.GetGameObjectVariable($"Spawned Flash {Id}"),
			bubblesObj = fsm.GetGameObjectVariable($"Spawned Bubbles {Id}");
		FsmInt
			numBinds = fsm.FindIntVariable("Bind Amount")!,
			masks = fsm.FindIntVariable("Heal Amount")!;

		#region Silk Effects = Flea Brew Bubbles

		fsm.GetState("Bind Ground")!.AddMethod(ReplaceSilkEffects);
		fsm.GetState("Bind Air")!.AddMethod(ReplaceSilkEffects);

		void ReplaceSilkEffects() {
			doHealBlue.Value = SifCrest.IsEquipped && __instance.playerData.healthBlue < 2;
			if (!SifCrest.IsEquipped)
				return;

			__instance.transform.Find("Bind Effects/Bind Silk").gameObject.SetActive(false);
			StopBubbles(bubblesObj.Value);
			bubblesObj.Value = SpawnBubbles(
				doHealBlue.Value ? blueBubble : Color.white,
				__instance.gameObject
			);
		}

		fsm.GetState("Cancel All")!.InsertMethod(0, () => StopBubbles(bubblesObj.Value));

		#endregion

		#region Conditional HP/Lifeblood amounts, spawn thrown bottle on success

		FsmState healState = fsm.GetState("Heal")!;

		int healIndex = healState.IndexFirstActionMatching(
			x => x is CallMethodProper action
				&& action.behaviour.Value == "HeroController"
				&& action.methodName.Value == "AddHealth"
		);

		healState.InsertMethod(healIndex, () => {
			if (!SifCrest.IsEquipped)
				return;

			if (doHealBlue.Value) {
				masks.Value = 0;
				__instance.gm.QueuedBlueHealth = 2 - __instance.playerData.healthBlue;
				EventRegister.SendEvent(EventRegisterEvents.AddQueuedBlueHealth);
				__instance.SpriteFlash.flashHealBlue();
			}
			else if (isFirstLoop.Value)
				masks.Value = 2;
			else
				masks.Value = 1;

			StopBubbles(bubblesObj.Value);
			if(numBinds.Value <= 1)
				ThrowTonicBottle(__instance);
		});

		#endregion

		#region Recolour screen flash

		int flashSpawnIndex = healState.IndexFirstActionMatching(
			x => x is SpawnObjectFromGlobalPool action
				&& action.gameObject.Value.name.Contains("White Flash")
		);

		healState.GetAction<SpawnObjectFromGlobalPool>(flashSpawnIndex)!
			.storeObject = flashObj;

		healState.InsertMethod(flashSpawnIndex + 1, () => {
			var fader = flashObj.Value.GetComponent<SimpleSpriteFade>();
			Color colour = (SifCrest.IsEquipped && doHealBlue.Value)
				? blueFlash : Color.white;
			fader.fadeInColor = colour with { a = fader.fadeInColor.a };
			fader.normalColor = colour with { a = fader.normalColor.a };
		});

		#endregion
	}

	static readonly Color
		blueFlash = new(0.55f, 0.9f, 1f),
		blueBubble = new(0.196f, 0.824f, 1f);

	/// <summary>
	/// Makes Hornet throw a sized-down empty Flea Brew bottle over her shoulder.
	/// </summary>
	static void ThrowTonicBottle(HeroController hc) {
		var fleaBrew = ((ToolItemStatesLiquid)ToolItemManager.GetToolByName("Flea Brew"))
			.usableEmptyState.Usage;

		GameObject bottle = fleaBrew.ThrowPrefab.Spawn(hc.transform.position);

		Vector2 velocity = fleaBrew.ThrowVelocity;
		if (hc.cState.facingRight)
			velocity = velocity with { x = -velocity.x };

		float
			velMag = velocity.magnitude * 0.7f,
			velAngle = velocity.normalized.DirectionToAngle(),
			randAngle = Random.Range(-30, 30);

		velocity = velMag * (velAngle + randAngle).AngleToDirection();

		bottle.GetComponent<Rigidbody2D>().linearVelocity = velocity;
		bottle.transform.localScale = 0.75f * Vector3.one;
	}

	/// <summary>
	/// Spawns bubbles and plays their particle systems.
	/// </summary>
	static GameObject SpawnBubbles(Color colour, GameObject hc) {
		GameObject prefab = BubblePrefab(colour);
		PlayParticleEffects bubbles = prefab.GetComponent<PlayParticleEffects>().Spawn();
		bubbles.transform.SetParent(hc.transform);
		bubbles.transform.SetLocalPosition2D(Vector2.zero);
		bubbles.PlayParticleSystems();
		return bubbles.gameObject;
	}

	/// <summary>
	/// Ends the particle systems of a GameObject with a PlayParticleEffects component.
	/// </summary>
	static void StopBubbles(GameObject? go) {
		if (go && go.TryGetComponent<PlayParticleEffects>(out var ppe))
			ppe.StopParticleSystems();
	}

	/// <summary>
	/// Gets/Creates a prefab for recoloured flea brew bubbles, sans burst.
	/// </summary>
	static GameObject BubblePrefab(Color colour) {
		if (bubbles.TryGetValue(colour, out var prefab) && prefab)
			return prefab;

		if (!HeroController.instance)
			return null!;

		GameObject go = Object.Instantiate(HeroController.instance.quickeningEffectPrefab.gameObject);
		go.SetActive(false);
		go.name = $"{SifName} Bubbles {bubbles.Count + 1}";
		Object.DontDestroyOnLoad(go);
		go.hideFlags = HideFlags.HideAndDontSave;

		Object.DestroyImmediate(go.transform.Find("Quickening_Burst").gameObject);
		RecolourParticles(go, colour);
		var partPlayer = go.GetComponent<PlayParticleEffects>();
		partPlayer.particleEffects = [.. partPlayer.particleEffects.Where(x => x)];

		bubbles[colour] = go;
		return go;
	}
	static readonly Dictionary<Color, GameObject> bubbles = [];

	/// <summary>
	/// Recolours all particle systems on all descendants of the given GameObject.
	/// </summary>
	static GameObject RecolourParticles(GameObject go, Color colour) {
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

			partSystem.colorOverLifetime.color.gradient.SetKeys(
				colorkeys,
				colourModule.color.gradient.alphaKeys
			);
		}
		return go;
	}

	/// <summary>
	/// Enumerates all the descendants of a GameObject.
	/// </summary>
	static IEnumerable<Transform> Descendants(GameObject go) {
		foreach (Transform t in go.transform) {
			yield return t;
			foreach (Transform descendant in Descendants(t.gameObject))
				yield return descendant;
		}
	}

}
