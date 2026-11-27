// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Goobstation.Shared/Fax/GettingFaxedCopiedEvent.cs

using Content.Shared.Fax;
using Content.Shared.Fax.Components;

namespace Content.Goobstation.Shared.Fax;

[ByRefEvent]
public record struct GettingFaxedCopiedEvent(Entity<FaxMachineComponent> Fax, FaxCopyMessage Args, bool Handled = false);
