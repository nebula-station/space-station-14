// Ported from Frontier (sector-frontier-14) by Nebula.

using Content.Shared.Speech.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._NF.Speech;

[RegisterComponent, NetworkedComponent]
[Access(typeof(GoblinAccentSystem))]
public sealed partial class GoblinAccentComponent : BaseAccentComponent;
