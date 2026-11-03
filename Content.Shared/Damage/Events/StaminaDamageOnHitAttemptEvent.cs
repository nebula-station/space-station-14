namespace Content.Shared.Damage.Events;

/// <summary>
/// Attempting to apply stamina damage on entity.
/// </summary>
[ByRefEvent] // Nebula add user
public record struct StaminaDamageOnHitAttemptEvent(bool LightAttack, bool Cancelled, EntityUid? User = null); // Goob - added LightAttack
