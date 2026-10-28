using Content.Server._Nebula.Discord;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Ban)]
    public sealed partial class PardonCommand : LocalizedCommands
    {
        [Dependency] private IServerDbManager _dbManager = default!;
        [Dependency] private NebulaWebhooks _webhooks = default!; // Nebula

        public override string Command => "pardon";

        public override async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;

            if (args.Length != 1)
            {
                shell.WriteLine(Help);
                return;
            }

            if (!int.TryParse(args[0], out var banId))
            {
                shell.WriteLine(Loc.GetString($"cmd-pardon-unable-to-parse", ("id", args[0]), ("help", Help)));
                return;
            }

            var ban = await _dbManager.GetBanAsync(banId);

            // Nebula edit start
            var scope = await _dbManager.GetRecordScopeServerId();
            if (ban == null || !RecordScope.Matches(ban.ServerId, scope))
            // Nebula edit end
            {
                shell.WriteLine($"No ban found with id {banId}");
                return;
            }

            if (ban.Unban != null)
            {
                if (ban.Unban.UnbanningAdmin != null)
                {
                    shell.WriteLine(Loc.GetString($"cmd-pardon-already-pardoned-specific",
                        ("admin", ban.Unban.UnbanningAdmin.Value),
                        ("time", ban.Unban.UnbanTime)));
                }

                else
                    shell.WriteLine(Loc.GetString($"cmd-pardon-already-pardoned"));

                return;
            }

            var unbanDef = new UnbanDef(banId, player?.UserId, DateTimeOffset.Now);
            await _dbManager.AddUnbanAsync(unbanDef);
            _webhooks.SendUnban(unbanDef, ban); // Nebula

            shell.WriteLine(Loc.GetString($"cmd-pardon-success", ("id", banId)));
        }
    }
}
