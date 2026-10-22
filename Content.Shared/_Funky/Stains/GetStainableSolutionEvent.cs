// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Components;

namespace Content.Shared._Funky.Stains;

public sealed partial class GetStainableSolutionEvent(EntityUid stained) : HandledEntityEventArgs
{
    public EntityUid Stained = stained;
    public Solution? Solution;
}
