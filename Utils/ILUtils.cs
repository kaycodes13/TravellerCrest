using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

namespace TravellerCrest.Utils;

/// <summary>
/// Instruction matching predicates for making IL patches more readable.
/// </summary>
internal static class ILUtils {
	/// <summary>
	/// True if the opcode is <see cref="OpCodes.Ldfld"/> and the field name matches.
	/// </summary>
	internal static bool Ldfld(CodeInstruction x, string name)
		=> x.opcode == OpCodes.Ldfld && x.operand is FieldInfo f && f.Name == name;

	/// <summary>
	/// True if the opcode is <see cref="OpCodes.Call"/> and the method name matches.
	/// </summary>
	internal static bool Call(CodeInstruction x, string name)
		=> x.opcode == OpCodes.Call && x.operand is MethodInfo m && m.Name == name;

	/// <summary>
	/// True if the opcode is <see cref="OpCodes.Callvirt"/> and the method name matches.
	/// </summary>
	internal static bool Callvirt(CodeInstruction x, string name)
		=> x.opcode == OpCodes.Callvirt && x.operand is MethodInfo m && m.Name == name;
}
