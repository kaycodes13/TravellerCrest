using Coffee.UISoftMask;
using UnityEngine;

namespace TravellerCrest.Components;

/// <summary>
/// When the screen resolution/size changes, SoftMaskable objects completely disappear
/// because they aren't updated properly in this version of the library. Attaching this
/// to a SoftMask manually updates them in that case, though they may flicker a little.
/// </summary>
/// <remarks>
/// Is this a smart solution to the problem? Not in the slightest. But it DOES work.
/// </remarks>
[RequireComponent(typeof(SoftMask))]
internal class SoftMaskFixer : MonoBehaviour {

	private SoftMask? mask;
	private int screenWidth = Screen.width, screenHeight = Screen.height;
	private Resolution screenRes = Screen.currentResolution;

	private void Update() {
		if (
			screenWidth != Screen.width || screenHeight != Screen.height
			|| screenRes.width != Screen.currentResolution.width
			|| screenRes.height != Screen.currentResolution.height
		) {
			screenWidth = Screen.width;
			screenHeight = Screen.height;
			screenRes = Screen.currentResolution;
			OnRectTransformDimensionsChange();
		}
	}

	private void OnRectTransformDimensionsChange() {
		if (!mask)
			mask = GetComponent<SoftMask>();

		if (gameObject.activeInHierarchy) {
			foreach (Transform child in transform) {
				child.gameObject.SetActive(false);
				child.gameObject.SetActive(true);
			}
			mask.enabled = false;
			mask.enabled = true;
		}
	}

}
