// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Server/StationGoal/SendStationGoalCommand.cs

using System.Linq;
using Content.Shared._Arcane.StationGoal;
using Content.Server.Administration;
using Content.Server.Commands;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._Arcane.StationGoal;

[AdminCommand(AdminFlags.Fun)]
public sealed partial class SendStationGoalCommand : LocalizedEntityCommands
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private StationGoalSystem _stationGoal = default!;

    public override string Command => "sendstationgoal";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var stationNet) || !EntityManager.TryGetEntity(stationNet, out var station))
        {
            shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (!_proto.TryIndex<StationGoalPrototype>(args[1], out var goal))
        {
            shell.WriteError(Loc.GetString("cmd-sendstationgoal-no-goal", ("id", args[1])));
            return;
        }

        if (!_stationGoal.SendStationGoal(station.Value, goal))
            shell.WriteError(Loc.GetString("cmd-sendstationgoal-not-sent"));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(ContentCompletionHelper.StationIds(EntityManager),
                Loc.GetString("cmd-sendstationgoal-arg-station")),
            2 => CompletionResult.FromHintOptions(_proto.EnumeratePrototypes<StationGoalPrototype>().Select(p => new CompletionOption(p.ID)),
                Loc.GetString("cmd-sendstationgoal-arg-id")),
            _ => CompletionResult.Empty,
        };
    }
}
