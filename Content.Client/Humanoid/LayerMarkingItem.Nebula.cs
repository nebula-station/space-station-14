// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Humanoid;
using Content.Shared.Humanoid.Markings;

namespace Content.Client.Humanoid;

public sealed partial class LayerMarkingItem
{
    private MarkingGradientControl? _gradientControl;

    public void RefreshSelection()
    {
        UpdateSelection();
    }

    private void AddGradientControl(Marking marking)
    {
        _gradientControl = new MarkingGradientControl();
        _gradientControl.SetGradient(marking.Gradient, GradientRoots(marking));
        _gradientControl.SwapPressed += () => _markingsModel.TrySwapMarkingGradient(_organ, _layer, _markingPrototype.ID);
        _gradientControl.GradientChanged += gradient =>
        {
            var now = _timing.CurFrame;
            if (_lastColorUpdate == now)
                return;

            _lastColorUpdate = now;
            _markingsModel.TrySetMarkingGradient(_organ, _layer, _markingPrototype.ID, gradient);
        };

        ColorsContainer.AddChild(_gradientControl);
    }

    private static Color GradientRoots(Marking marking)
    {
        return marking.MarkingColors.Count > 0 ? marking.MarkingColors[0] : Color.White;
    }
}
