using System.Linq;
using Content.Client._Nebula.Ghost.Roles;
using Content.Client.Eui;
using Content.Client.Players.PlayTimeTracking;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.UserInterface.Systems.Ghost.Controls.Roles
{
    [UsedImplicitly]
    public sealed class GhostRolesEui : BaseEui
    {
        private readonly NebulaGhostRolesWindow _window; // Nebula edit
        private NebulaGhostRoleRulesWindow? _windowRules = null; // Nebula edit
        private uint _windowRulesId = 0;

        public GhostRolesEui()
        {
            _window = new NebulaGhostRolesWindow(); // Nebula edit

            _window.OnRoleRequestButtonClicked += info =>
            {
                _windowRules?.Close();

                if (info.Kind == GhostRoleKind.RaffleJoined)
                {
                    SendMessage(new LeaveGhostRoleRaffleMessage(info.Identifier));
                    return;
                }

                _windowRules = new NebulaGhostRoleRulesWindow(info.Rules, _ => // Nebula edit
                {
                    SendMessage(new RequestGhostRoleMessage(info.Identifier));

                    // if raffle role, close rules window on request, otherwise do
                    // old behavior of waiting for the server to close it
                    if (info.Kind != GhostRoleKind.FirstComeFirstServe)
                        _windowRules?.Close();
                });
                _windowRulesId = info.Identifier;
                _windowRules.OnClose += () =>
                {
                    _windowRules = null;
                };
                _windowRules.OpenCentered();
            };

            _window.OnRoleFollow += info =>
            {
                SendMessage(new FollowGhostRoleMessage(info.Identifier));
            };

            _window.OnClose += () =>
            {
                SendMessage(new CloseEuiMessage());
            };
        }

        public override void Opened()
        {
            base.Opened();
            _window.OpenCentered();
        }

        public override void Closed()
        {
            base.Closed();
            _window.Close();
            _windowRules?.Close();
        }

        public override void HandleState(EuiStateBase state)
        {
            base.HandleState(state);

            if (state is not GhostRolesEuiState ghostState)
                return;

            // We must save BodyVisible state, so all Collapsible boxes will not close
            // on adding new ghost role.
            // Save the current state of each Collapsible box being visible or not
            _window.SaveCollapsibleBoxesStates();

            // Clearing the container before adding new roles
            _window.ClearEntries();

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var sysManager = entityManager.EntitySysManager;
            var spriteSystem = sysManager.GetEntitySystem<SpriteSystem>();
            var requirementsManager = IoCManager.Resolve<JobRequirementsManager>();

            // Grouping roles
            var groupedRoles = ghostState.GhostRoles.GroupBy(
                role => (
                    role.Name,
                    role.Description,
                    //  Check the prototypes for role requirements and bans
                    requirementsManager.IsAllowed(role.RolePrototypes.Item1, role.RolePrototypes.Item2, null, out var reason),
                    reason));

            // Add a new entry for each role group
            foreach (var group in groupedRoles)
            {
                var reason = group.Key.reason;
                var name = group.Key.Name;
                var description = group.Key.Description;
                var prototypesAllowed = group.Key.Item3;

                // Adding a new role
                _window.AddEntry(name, description, prototypesAllowed, reason, group, spriteSystem);
            }

            // Restore the Collapsible box state if it is saved
            _window.RestoreCollapsibleBoxesStates();

            // Close the rules window if it is no longer needed
            var closeRulesWindow = ghostState.GhostRoles.All(role => role.Identifier != _windowRulesId);
            if (closeRulesWindow)
                _windowRules?.Close();
        }
    }
}
