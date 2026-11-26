// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/045e1f70c57558b76595316d8f399599f85bc753/Content.Server/_Orion/Ghost/GhostReturnToRoundSystem.cs

using Content.Shared.Administration;
using Content.Shared.Ghost.Components;
using Robust.Shared.Console;

namespace Content.Server._Orion.Ghost;

[AnyCommand]
public sealed partial class ReturnToRoundCommand : LocalizedEntityCommands
{
    [Dependency] private GhostReturnToRoundSystem _returnToRound = default!;

    public override string Command => "returntoround";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player?.AttachedEntity is not { } ghost || !EntityManager.TryGetComponent<GhostComponent>(ghost, out var ghostComponent))
        {
            shell.WriteError(Loc.GetString("ghost-respawn-command-no-entity"));
            return;
        }

        _returnToRound.TryGhostReturnToRound((ghost, ghostComponent));
    }
}
