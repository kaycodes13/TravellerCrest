using GlobalSettings;
using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Silksong.FsmUtil;
using System;

namespace TravellerCrest.Mechanics;

[HarmonyPatch]
internal static class Bind {

	internal static void OnBindStart(FsmInt masks, FsmInt numBinds, FsmFloat time, PlayMakerFSM fsm) {
		if (Gameplay.MultibindTool.IsEquipped) {
			numBinds.Value = 2;
			time.Value = 0.8f;
		}
	}

	[HarmonyPatch(typeof(HeroController), nameof(HeroController.Start))]
	[HarmonyPostfix]
	private static void AdjustHealAmount(HeroController __instance) {
		PlayMakerFSM fsm = __instance.gameObject.GetFsmPreprocessed("Bind")!;
		FsmState Heal = fsm.GetState("Heal")!;
		int index =
			Array.FindIndex(
				Heal.Actions,
				x => x is CallMethodProper action
					&& action.methodName.Value == nameof(HeroController.AddHealth)
			);

		Heal.InsertLambdaMethod(index, finished => {
			if (SifCrest.IsEquipped) {
				FsmInt masks = fsm.GetIntVariable("Heal Amount");
				int healthBlue = PlayerData.instance.healthBlue;

				if (healthBlue < 2) {
					masks.Value = 0;
					GameManager.instance.QueuedBlueHealth = 2 - healthBlue;
					EventRegister.SendEvent(EventRegisterEvents.AddQueuedBlueHealth);
				}
				else {
					masks.Value = 2;
				}
			}
			finished();
		});
	}

}
