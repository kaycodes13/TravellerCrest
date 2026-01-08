using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TravellerCrest.Components;

/// <summary>
/// Locks an element in a <see cref="Canvas"/> to its preferred size,
/// with an optional multiplier on that size.
/// </summary>
/// <remarks>
/// I tried to do this the "proper" unity UI way.
/// I did not figure it out before getting very tired of the problem.
/// </remarks>
internal class LockToPreferredSize : UIBehaviour {

	/// <summary>
	/// A multiplier on the object's preferred size which will be used to calculate
	/// the final size. Default is 1.
	/// </summary>
	public float Scale { get; set; } = 1;

	private RectTransform rect;

	protected override void Awake() {
		rect = (RectTransform)transform;
	}

	private void Update() {
		if (!rect || !IsActive())
			return;

		Vector2 preferredSize = new(
			LayoutUtility.GetPreferredWidth(rect),
			LayoutUtility.GetPreferredHeight(rect)
		);

		Vector2 finalSize = preferredSize * Scale;

		if (rect.sizeDelta != finalSize)
			rect.sizeDelta = finalSize;
	}

}
