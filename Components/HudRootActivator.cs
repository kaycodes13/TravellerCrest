using Needleforge.Data;
using System.Collections;
using UnityEngine;

namespace TravellerCrest.Components;

internal class HudRootActivator : MonoBehaviour {

	public GameObject hudroot;
	public CrestData crest;

	private Coroutine? coro;

	private void Start() => OnEnable();

	private void OnEnable() => coro ??= StartCoroutine(ManageHudRoot());

	private void OnDisable() => StopCoroutine(coro);

	private IEnumerator ManageHudRoot() {
		var hc = HeroController.instance;
		while (true) {
			if (hc.IsPaused() || crest == null || !hudroot) {
				yield return null;
				continue;
			}

			bool equipped = crest.IsEquipped;

			if (equipped ^ hudroot.activeSelf)
				hudroot.SetActive(equipped);

			yield return null;
		}
	}

}
