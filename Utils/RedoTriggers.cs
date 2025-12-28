using System;
using System.Linq;
using UnityEngine;

namespace TravellerCrest.Utils;

#if DEBUG
/// <summary>
/// For tweaking trigger timings with unity explorer
/// </summary>
internal class RedoTriggers : MonoBehaviour {

	tk2dSpriteAnimation lib;

	private void Awake() {
		lib = GetComponent<tk2dSpriteAnimation>();
	}

	public void Redo(string name, string indexes) {
		var clip = lib.GetClipByName(name);
		if (clip is null)
			return;

		int[] nums = [.. indexes.Split(',').Select(x => int.Parse(x.Trim()))];

		for (int i = 0; i < clip.frames.Length; i++) {
			clip.frames[i].triggerEvent = nums.Contains(i);
		}
	}

}
#endif
