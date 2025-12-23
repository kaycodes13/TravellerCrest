namespace TravellerCrest.Utils;

/// <summary>
/// A <see cref="Probability.ProbabilityInt"/> that can be used with values of any type.
/// </summary>
internal class ProbabilityVar<T> : Probability.ProbabilityBase<T> {
	#pragma warning disable CS8618 // Non-nullable field must contain a non-null value
	public T Value;
	#pragma warning restore CS8618

	public override T Item => Value;
}
