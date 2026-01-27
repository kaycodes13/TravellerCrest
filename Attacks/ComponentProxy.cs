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
	/// Gets the first component of the specified type attached to the
	/// <see cref="GameObject"/> that <see cref="component"/> is attached to.
	/// </summary>
	protected C GetOwnerComponent<C>() where C : Component
		=> component!.gameObject.GetComponent<C>();

	/// <summary>
	/// Use to initialize the proxied component when the
	/// <see cref="GameObject"/> it's part of is created.
	/// </summary>
	internal void Initialize(GameObject owner) {
		if (!owner)
			throw new InvalidOperationException($"Owner {nameof(GameObject)} must exist at the time of {GetType().Name} initialization.");
		component = owner.GetOrAddComponent<T>();
		Init();
	}

}
