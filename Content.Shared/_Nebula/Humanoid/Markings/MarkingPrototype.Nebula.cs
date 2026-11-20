// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Humanoid.Markings;

/// <summary>
/// Nebula - WolfGate. Genital art comes in size steps and in a skin-toned variant, so the drawn state is picked per marking.
/// </summary>
public sealed partial class MarkingPrototype
{
    /// <summary>
    /// Picks the RSI state of each sprite from <see cref="Marking.Size"/> and the organ's art tone.
    /// The states in <see cref="Sprites"/> are only defaults for previews and layer keys.
    /// </summary>
    [DataField]
    public MarkingSizedStates? SizedStates { get; private set; }

    /// <summary>
    /// The size clamped to the steps of this marking, null when the marking has no sizes.
    /// </summary>
    public int? SanitizeSize(int? size)
    {
        if (SizedStates is not { } sized || size is not { } value)
            return null;

        return Math.Clamp(value, 0, sized.Steps.Count - 1);
    }
}

[DataDefinition]
public sealed partial class MarkingSizedStates
{
    /// <summary>
    /// Tokens put in place of <c>{size}</c>, smallest first.
    /// </summary>
    [DataField(required: true)]
    public List<string> Steps = new();

    /// <summary>
    /// Index of the step used by a marking without a size.
    /// </summary>
    [DataField]
    public int Default;

    /// <summary>
    /// One state template per sprite. <c>{size}</c> becomes the step token,
    /// <c>{tone}</c> the <see cref="MarkingsGroupPrototype.ArtTone"/> of the organ.
    /// </summary>
    [DataField(required: true)]
    public List<string> States = new();

    public int StepIndex(int? size)
    {
        return Math.Clamp(size ?? Default, 0, Steps.Count - 1);
    }

    public string? State(int sprite, int? size, string tone)
    {
        if (sprite >= States.Count || Steps.Count == 0)
            return null;

        return States[sprite]
            .Replace("{size}", Steps[StepIndex(size)])
            .Replace("{tone}", tone);
    }
}
