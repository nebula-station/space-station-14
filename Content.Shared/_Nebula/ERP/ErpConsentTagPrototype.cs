// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.ERP;

/// <summary>
/// OOC consent tag set in the character editor and shown by the ERP preferences examine verb.
/// </summary>
[Prototype]
public sealed partial class ErpConsentTagPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Tooltip explaining what the tag allows.
    /// </summary>
    [DataField(required: true)]
    public LocId Description;

    /// <summary>
    /// Whether <see cref="ErpConsent.Ask"/> can be picked, otherwise the tag is only yes or no.
    /// </summary>
    [DataField]
    public bool AllowAsk = true;

    /// <summary>
    /// Value used when the player never set this tag.
    /// </summary>
    [DataField]
    public ErpConsent Default = ErpConsent.No;

    /// <summary>
    /// Tag that has to be <see cref="ErpConsent.Yes"/> for this one to be anything but <see cref="ErpConsent.No"/>.
    /// </summary>
    [DataField]
    public ProtoId<ErpConsentTagPrototype>? Requires;

    /// <summary>
    /// Tags are ordered by this, lowest first.
    /// </summary>
    [DataField]
    public int Order;
}

[Serializable, NetSerializable]
public enum ErpConsent : byte
{
    No,
    Ask,
    Yes
}
