// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Client.UserInterface.Controls; // Nebula - WolfGate

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

    // Nebula start - WolfGate
    private Slider? _sizeSlider;
    private Label? _sizeLabel;

    /// <summary>
    /// A size slider under markings with more than one size step, e.g. breasts. Shown even when the colors are forced.
    /// </summary>
    private void UpdateSizeControl(bool selected)
    {
        if (_markingPrototype.SizedStates is not { Steps.Count: > 1 } sized)
            return;

        SizeContainer.Visible = selected && _interactive;
        if (!SizeContainer.Visible)
            return;

        if (_sizeSlider == null || _sizeLabel == null)
        {
            _sizeLabel = new Label { MinWidth = 96 };
            _sizeSlider = new Slider
            {
                MinValue = 0,
                MaxValue = sized.Steps.Count - 1,
                Rounded = true,
                HorizontalExpand = true,
            };
            _sizeSlider.OnValueChanged += range =>
                _markingsModel.TrySetMarkingSize(_organ, _layer, _markingPrototype.ID, (int) range.Value);

            SizeContainer.AddChild(_sizeLabel);
            SizeContainer.AddChild(_sizeSlider);
        }

        var step = sized.StepIndex(_markingsModel.GetMarking(_organ, _layer, _markingPrototype.ID)?.Size);
        _sizeSlider.SetValueWithoutEvent(step);
        _sizeLabel.Text = Loc.GetString("markings-size", ("size", sized.Steps[step]));
    }
    // Nebula end
}
