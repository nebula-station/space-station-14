// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/93847a07a4b0b5890003c074e4768c5ba828543a/Content.Arcane.Shared/Medical/Concussion/SharedConcussionSystem.cs

using Content.Shared._Arcane.Medical.Concussion;
using Content.Trauma.Server.Blob;
using Content.Trauma.Server.Language;
using Content.Trauma.Server.Zombies;
using Content.Trauma.Shared.Language.Events;

namespace Content.Trauma.Server._Arcane.Medical.Concussion;

public sealed partial class ConcussionLanguageSystem : EntitySystem
{
    [SubscribeLocalEvent(after: [typeof(LanguageSystem), typeof(TranslatorSystem), typeof(TranslatorImplantSystem), typeof(TraumaBlobSystem), typeof(TraumaZombieSystem)])]
    private void OnLanguages(Entity<ConcussedComponent> ent, ref DetermineEntityLanguagesEvent args)
    {
        args.UnderstoodLanguages.Clear();
    }
}
