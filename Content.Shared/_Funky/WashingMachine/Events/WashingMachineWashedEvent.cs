// SPDX-FileCopyrightText: 2025 Doctor-Cpu <77215380+Doctor-Cpu@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GabyChangelog <agentepanela2@gmail.com>
// SPDX-FileCopyrightText: 2025 Will-Oliver-Br <164823659+Will-Oliver-Br@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Funky.WashingMachine.Events;

public sealed partial class WashingMachineWashedEvent(EntityUid washingMachine, HashSet<EntityUid> items) : EntityEventArgs
{
    public EntityUid WashingMachine = washingMachine;
    public HashSet<EntityUid> Items = items;
}
