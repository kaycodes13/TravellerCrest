using HutongGames.PlayMaker;
using Needleforge.Attacks;
using Silksong.FsmUtil;
using System.Collections.Generic;
using UnityEngine;

namespace TravellerCrest.Utils;

/// <summary>
/// Utilities for making attack code, particularly FSM edits, less wordy.
/// Because wordiness in code irritates me.
/// </summary>
/// <remarks>
/// Note to self: Consider adding similar utilities to Needleforge directly, at some point.
/// </remarks>
internal static class MiscUtil {

	#region Needleforge Objects

	/// <summary>
	/// Returns the first <typeparamref name="C"/> on this proxy's <see cref="GameObject"/>,
	/// or <see langword="null"/> if the GameObject or component doesn't exist.
	/// </summary>
	public static C Get<C>(this GameObjectProxy gp) where C : Component
		=> (bool)gp.GameObject ? gp.GameObject.GetComponent<C>() : null!;

	/// <summary>
	/// Tries to find the first <typeparamref name="C"/> on this proxy's <see cref="GameObject"/>,
	/// returns a <see langword="bool"/> indicating if it succeeded or not.
	/// </summary>
	public static bool TryGet<C>(this GameObjectProxy gp, out C c) where C : Component {
		c = null!;
		return (bool)gp.GameObject && gp.GameObject.TryGetComponent(out c);
	}

	#endregion

	#region FSMs

	/// <summary>
	/// Add multiple transitions to an FsmState at once. Easier on my eyes.
	/// </summary>
	public static void AddTransitions(this FsmState state, params (FsmState dest, string evt)[] transitions) {
		foreach (var (dest, evt) in transitions)
			state.AddTransition(evt, dest.name);
	}

	/// <summary>
	/// Add multiple transitions to an FsmState at once. Easier on my eyes.
	/// </summary>
	public static void AddTransitions(this FsmState state, params (FsmState dest, string[] evts)[] transitions) {
		foreach (var (dest, evts) in transitions)
			foreach(var e in evts)
				state.AddTransition(e, dest.name);
	}

	#endregion

}
