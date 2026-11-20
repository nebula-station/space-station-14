// SPDX-License-Identifier: AGPL-3.0-or-later

// Ported from https://github.com/Goob-Station/Goob-Station/blob/707e36693d1d9289a4f04921c5e070b1e4195589/Content.Shared/UserInterface/ActivatableUISystem.Goob.cs

using Content.Shared.Interaction;

namespace Content.Shared.UserInterface;

public sealed partial class ActivatableUISystem
{
    [Dependency] private SharedInteractionSystem _interaction = default!;

    public bool InteractUI(EntityUid user, EntityUid uiEntity, ActivatableUIComponent aui, Enum? key = null)
    {
        var interactionParticle = false;
        return InteractUI(user, uiEntity, aui, key, ref interactionParticle);
    }

    private void InteractUI(EntityUid user, Entity<ActivatableUIComponent> ui)
    {
        var interactionParticle = false;
        InteractUI(user, ui, ui, null, ref interactionParticle);
        _interaction.DoContactInteraction(user, ui, null, true, interactionParticles: interactionParticle);
    }
}
