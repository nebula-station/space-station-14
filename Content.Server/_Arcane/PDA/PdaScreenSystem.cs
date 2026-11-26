// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Server/PDA/PdaScreenSystem.cs

using Content.Shared.CartridgeLoader;
using Content.Shared.PDA;
using Content.Shared._Arcane.PDA;
using Robust.Shared.Prototypes;

namespace Content.Server._Arcane.PDA;

/// <summary>
/// Keeps the PDA screen in sync with its interface and active program, and handles the power-off button.
/// </summary>
public sealed partial class PdaScreenSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private CartridgeLoaderSystem _cartridgeLoader = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<PdaScreenComponent> ent, ref MapInitEvent args)
    {
        SetScreen(ent, ent.Comp.OffState);
    }

    [SubscribeLocalEvent]
    private void OnUiChanged(Entity<PdaScreenComponent> ent, ref BoundUIOpenedEvent args)
    {
        Update(ent, null);
    }

    [SubscribeLocalEvent]
    private void OnUiChanged(Entity<PdaScreenComponent> ent, ref BoundUIClosedEvent args)
    {
        Update(ent, null);
    }

    [SubscribeLocalEvent]
    private void OnPowerOff(Entity<PdaScreenComponent> ent, ref PdaPowerOffMessage args)
    {
        if (!PdaUiKey.Key.Equals(args.UiKey))
            return;

        if (TryComp<CartridgeLoaderComponent>(ent, out var loader) && loader.ActiveProgram is { } program)
            _cartridgeLoader.DeactivateProgram((ent, loader), program);

        _ui.CloseUi(ent.Owner, PdaUiKey.Key, args.Actor);
        SetScreen(ent, ent.Comp.OffState);
    }

    [SubscribeLocalEvent]
    private void OnProgramActivated(Entity<CartridgeComponent> ent, ref CartridgeActivatedEvent args)
    {
        if (TryComp<PdaScreenComponent>(args.Loader, out var screen))
            Update((args.Loader, screen), ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnProgramDeactivated(Entity<CartridgeComponent> ent, ref CartridgeDeactivatedEvent args)
    {
        if (TryComp<PdaScreenComponent>(args.Loader, out var screen))
            Update((args.Loader, screen), EntityUid.Invalid);
    }

    /// <param name="program">The active program, <see cref="EntityUid.Invalid"/> for none, or null to read it from the loader.</param>
    private void Update(Entity<PdaScreenComponent> ent, EntityUid? program)
    {
        program ??= CompOrNull<CartridgeLoaderComponent>(ent)?.ActiveProgram ?? EntityUid.Invalid;

        string state;
        if (program.Value.IsValid())
        {
            var proto = MetaData(program.Value).EntityPrototype?.ID;
            state = proto != null && ent.Comp.ProgramScreens.TryGetValue(proto, out var programState)
                ? programState
                : ent.Comp.ProgramState;
        }
        else
        {
            state = _ui.IsUiOpen(ent.Owner, PdaUiKey.Key) ? ent.Comp.MenuState : ent.Comp.OffState;
        }

        SetScreen(ent, state);
    }

    private void SetScreen(EntityUid uid, string state)
    {
        _appearance.SetData(uid, PdaScreenVisuals.State, state);
    }
}
