using HutongGames.PlayMaker;
using Needleforge.Attacks;
using Silksong.FsmUtil;
using System;
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

	/// <summary>
	/// Safe way to tell an attack proxy to start slashing.
	/// </summary>
	public static void StartAtk(this AttackBase atk)
		=> atk.CallAtkFn("StartSlash", x => { x.OnSlashStarting(); x.OnPlaySlash(); });

	/// <summary>
	/// Safe way to tell an attack proxy to stop slashing.
	/// </summary>
	public static void CancelAtk(this AttackBase atk)
		=> atk.CallAtkFn("CancelAttack", x => x.OnCancelAttack());

	/// <summary>
	/// Calls a function on the proxy's <see cref="NailAttackBase"/>, if the function
	/// exists on its actual type; or else calls the provided fallback function.
	/// </summary>
	/// <remarks>
	/// I'm so annoyed that Team Cherry didn't define StartSlash() and CancelAttack()
	/// on the base class. What is this object hierarchy.
	/// </remarks>
	private static void CallAtkFn(this AttackBase atk, string fnName, Action<NailAttackBase>? fallback = null) {
		if (!atk.GameObject)
			return;

		var nab = atk.Get<NailAttackBase>();
		var type = nab.GetType();
		var fn = type.GetMethod(fnName, []);

		if (fn != null)
			fn.Invoke(nab, []);
		else
			fallback?.Invoke(nab);
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
