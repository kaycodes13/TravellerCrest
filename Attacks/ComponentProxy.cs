using Silksong.UnityHelper.Extensions;
using System;
using UnityEngine;

namespace TravellerCrest.Attacks;

/// <summary>
/// Base class for adding groups of additional settings to descendants of <see cref="Needleforge.Attacks.GameObjectProxy"/>.
/// </summary>
/// <typeparam name="T">The component this type is proxying.</typeparam>
internal abstract class ComponentProxy<T> where T : Component {

	/// <summary>
	/// The component being proxied. All API properties should modify this in some way.
	/// </summary>
	protected T? component;

	/// <summary>
	/// Should initialize <see cref="component"/> with the values provided to the API
	/// properties, and any other setup it needs to function.
	/// </summary>
	protected abstract void Init();

	/// <summary>
	/// Use to initialize the proxied component when the <see cref="GameObject"/> it's
	/// part of is created.
	/// </summary>
	internal void Initialize(GameObject owner) {
		if (!owner)
			throw new InvalidOperationException($"Owner {nameof(GameObject)} must exist at the time of {GetType().Name} initialization.");
		component = owner.GetOrAddComponent<T>();
		Init();
	}

	/// <summary>
	/// Initializes the proxied component on the given <see cref="GameObject"/> provided
	/// it exists; does nothing otherwise. Used for safety in proxy property setters.
	/// </summary>
	internal void TryInitialize(GameObject? owner) {
		if (owner) Initialize(owner);
	}

}
