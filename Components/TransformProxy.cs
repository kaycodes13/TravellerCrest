using System;
using UnityEngine;

namespace TravellerCrest.Components;

/// <summary>
/// Provides an API for changing the position and rotation of an attack.
/// </summary>
internal class TransformProxy : ComponentProxy<Transform> {

	#region API

	public Vector3 Position {
		get => _pos;
		set {
			_pos = value;
			if (component) component.localPosition = value;
		}
	}
	private Vector3 _pos = Vector3.zero;

	public Quaternion Rotation {
		get => _rot;
		set {
			_rot = value;
			if (component) component.localRotation = value;
		}
	}
	private Quaternion _rot = Quaternion.identity;

	#endregion

	protected override void Init()
		=> component!.SetLocalPositionAndRotation(Position, Rotation);
}
