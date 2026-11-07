// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Shared/_Orion/DocumentPrinter/SharedDocumentPrinterSystem.cs

using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._Orion.DocumentPrinter;

public sealed partial class SharedDocumentPrinterSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;

    [SubscribeLocalEvent]
    private void OnGetAlternativeVerbs(Entity<DocumentPrinterComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;
        var enabled = !ent.Comp.IsOnAutocomplete;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString(enabled ? "printer-autofill-on" : "printer-autofill-off"),
            Act = () => TrySetAutocomplete(ent.AsNullable(), enabled, user),
        });
    }

    public bool TrySetAutocomplete(Entity<DocumentPrinterComponent?> ent, bool enabled, EntityUid user)
    {
        if (!Resolve(ent, ref ent.Comp, false) || ent.Comp.IsOnAutocomplete == enabled)
            return false;

        ent.Comp.IsOnAutocomplete = enabled;
        Dirty(ent);
        _audio.PlayPredicted(ent.Comp.SwitchSound, ent, user);
        return true;
    }
}
