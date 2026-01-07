using Needleforge.Data;
using System.Collections;
using UnityEngine;

namespace TravellerCrest.Components;

internal class HudRootAnimator : MonoBehaviour {

	public GameObject hudroot;
	public CrestData crest;
	private BindOrbHudFrame hud;
	float time = 0.2f;

	private Coroutine? coro;

	private void Start() {
		hud = FindAnyObjectByType<BindOrbHudFrame>();
		OnEnable();
	}

	private void OnEnable() => coro ??= StartCoroutine(ManageHudRoot());

	private void OnDisable() => StopCoroutine(coro);

	private IEnumerator ManageHudRoot() {
		bool prevEquipped = false;
		while (true) {
			if (HeroController.instance.IsPaused() || crest == null || !hudroot) {
				yield return null;
				continue;
			}

			bool equipped = crest.IsEquipped;

			if (equipped != prevEquipped) {

				if (!equipped) {
					hudroot.transform.ScaleTo(this, Vector3.zero, time);
					prevEquipped = equipped;
				}
				else {
					if (hud.animator.IsPlaying(crest.HudFrame.Idle!.name)) {
						hudroot.transform.ScaleTo(this, Vector3.one, time);
						prevEquipped = equipped;
					}
				}
			}

			yield return null;
		}
	}

}
