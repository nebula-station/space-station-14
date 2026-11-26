// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Server/Fax/FaxSystem.cs

using Content.Goobstation.Shared.Fax;
using Content.Shared.Fax;
using Content.Shared.Fax.Components;

namespace Content.Server.Fax;

public sealed partial class FaxSystem
{
    /// <summary>
    /// Lets the copied item handle the copy itself, e.g. a painting that needs a photocopier.
    /// </summary>
    private bool CopyHandledByItem(Entity<FaxMachineComponent> fax, FaxCopyMessage args)
    {
        if (fax.Comp.PaperSlot.Item is not { } paper)
            return false;

        var copiedEv = new GettingFaxedCopiedEvent(fax, args);
        RaiseLocalEvent(paper, ref copiedEv);
        return copiedEv.Handled;
    }
}
