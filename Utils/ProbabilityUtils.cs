namespace TravellerCrest.Utils;

/// <summary>
/// Extensions and utilities for working with probabilities.
/// </summary>
internal static class ProbabilityUtils {

	/// <summary>
	/// Returns a random true/false using the given probability of a true value.
	/// </summary>
	internal static bool GetRandomBool(float trueProbability = 0.5f)
		=> Probability.GetRandomItemByProbability<Var<bool>, bool>([
			new() { Value = false, Probability = 1 - trueProbability },
			new() { Value = true,  Probability = trueProbability },
		]);
	
	#pragma warning disable CS8618 // Non-nullable field must contain value
	/// <summary>
	/// A <see cref="Probability.ProbabilityInt"/> for any type of value.
	/// </summary>
	internal class Var<T> : Probability.ProbabilityBase<T> {
		public T Value;
		public override T Item => Value;
	}

}
