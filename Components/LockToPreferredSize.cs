using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TravellerCrest.Components;

/// <summary>
/// Locks an element in a Canvas to its preferred size, with an optional size multiplier.
/// </summary>
/// <remarks>
/// I tried to do this the proper way. I failed. I took a sledgehammer to the issue.
/// </remarks>
internal class LockToPreferredSize : UIBehaviour {
	/// <summary>
	/// Multiplier on the object's preferred size which will be used to calculate
	/// the final size. Default is 1.
	/// </summary>
	public float Scale { get; set; } = 1;

	private RectTransform rect;

	protected override void Awake()
		=> rect = (RectTransform)transform;

	void Update() {
		if (!rect || !IsActive())
			return;
		Vector2 size = new Vector2(
			LayoutUtility.GetPreferredWidth(rect),
			LayoutUtility.GetPreferredHeight(rect)
		) * Scale;
		rect.sizeDelta = size;
	}
}
