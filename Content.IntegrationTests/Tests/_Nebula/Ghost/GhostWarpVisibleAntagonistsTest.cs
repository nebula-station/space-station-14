// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.Mind.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Nebula.Ghost;

/// <summary>
/// Ghost warps flag only antagonists recognizable on sight, hidden ones stay unmarked.
/// </summary>
[TestFixture]
[TestOf(typeof(GhostSystem))]
public sealed class GhostWarpVisibleAntagonistsTest : GameTest
{
    [SidedDependency(Side.Server)]
    private readonly GhostSystem _ghost = null!;

    [SidedDependency(Side.Server)]
    private readonly MindSystem _minds = null!;

    [SidedDependency(Side.Server)]
    private readonly RoleSystem _roles = null!;

    [Test]
    [RunOnSide(Side.Server)]
    public void GhostWarpVisibleAntagonists()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_ghost.IsVisibleAntagonist(CreateMind("MindRoleNukeops")), Is.True);
            Assert.That(_ghost.IsVisibleAntagonist(CreateMind("MindRoleTraitor")), Is.False);
            Assert.That(_ghost.IsVisibleAntagonist(CreateMind("MindRoleTraitorReinforcement")), Is.False);
            Assert.That(_ghost.IsVisibleAntagonist(CreateMind(null)), Is.False);
        }
    }

    private EntityUid CreateMind(string? roleProto)
    {
        var mind = _minds.CreateMind(null);

        var owned = SSpawn(null);
        SEntMan.EnsureComponent<MindContainerComponent>(owned);
        _minds.TransferTo(mind.Owner, owned);

        if (roleProto != null)
            _roles.MindAddRole(mind.Owner, roleProto, silent: true);

        return mind.Owner;
    }
}
