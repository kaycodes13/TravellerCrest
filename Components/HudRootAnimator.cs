using Needleforge.Data;
using System.Collections;
using UnityEngine;

namespace TravellerCrest.Components;

internal class HudRootAnimator : MonoBehaviour {

	public GameObject hudroot;
	public CrestData crest;
	
	const float TIME = 0.2f;
	BindOrbHudFrame hud;
	Coroutine? coro;

	void Start() {
		hud = FindAnyObjectByType<BindOrbHudFrame>();
		OnEnable();
	}

	void OnEnable() => coro ??= StartCoroutine(ManageHudRoot());

	void OnDisable() => StopCoroutine(coro);

	IEnumerator ManageHudRoot() {
		bool prevEquipped = false;
		while (true) {
			if (HeroController.instance.IsPaused() || crest == null || !hudroot) {
				yield return null;
				continue;
			}

			bool equipped = crest.IsEquipped;

			if (equipped != prevEquipped) {
				if (!equipped) {
					hudroot.transform.ScaleTo(this, Vector3.zero, TIME);
					prevEquipped = equipped;
				}
				else {
					if (hud.animator.IsPlaying(crest.HudFrame.Idle!.name)) {
						hudroot.transform.ScaleTo(this, Vector3.one, TIME);
						prevEquipped = equipped;
					}
				}
			}

			yield return null;
		}
	}

}
