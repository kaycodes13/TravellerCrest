namespace TravellerCrest.Utils;

/// <summary>
/// Extensions and utilities for working with probabilities.
/// </summary>
internal static class ProbabilityUtils {

	/// <summary>
	/// Returns a random true/false using the given probability of a true value.
	/// </summary>
	internal static bool GetRandomBool(float trueProbability = 0.5f)
		=> Probability.GetRandomItemByProbability<ProbabilityVar<bool>, bool>([
			new() { Value = false, Probability = 1 - trueProbability },
			new() { Value = true,  Probability = trueProbability },
		]);

}
