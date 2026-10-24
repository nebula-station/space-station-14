// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface.Shapes;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._Nebula.UserInterface.Controls;

/// <summary>
/// Button drawn as a <see cref="ShapeStyle"/>. While <see cref="BaseButton.Pressed"/> or <see cref="IsActive"/>
/// it uses the active colors and can pulse between them.
/// </summary>
[Virtual]
public class ShapedButton : BaseButton
{
    public const string StylePropertyIdleColor = "shapedButtonIdleColor";
    public const string StylePropertyHoverColor = "shapedButtonHoverColor";
    public const string StylePropertyPressedColor = "shapedButtonPressedColor";
    public const string StylePropertyDisabledColor = "shapedButtonDisabledColor";
    public const string StylePropertyActiveColor = "shapedButtonActiveColor";
    public const string StylePropertyActivePulseColor = "shapedButtonActivePulseColor";
    public const string StylePropertyBorderColor = "shapedButtonBorderColor";
    public const string StylePropertyActiveBorderColor = "shapedButtonActiveBorderColor";
    public const string StylePropertyTextColor = "shapedButtonTextColor";
    public const string StylePropertyActiveTextColor = "shapedButtonActiveTextColor";
    public const string StylePropertyPulseEnabled = "shapedButtonPulseEnabled";
    public const string StylePropertyShape = "shapedButtonShape";

    private readonly Label _label;
    private readonly TextureRect _icon;
    private readonly BoxContainer _content;
    private readonly ShapeOutline _outline = new();
    private float _pulseTime;
    private bool _isActive;

    public float BorderThickness { get; set; } = 2f;

    private Color? _textColorOverride;

    /// <summary>
    /// Idle fill computed in code instead of the style, hover and pressed are mixed towards the text color.
    /// Replaces the active look too.
    /// </summary>
    public Color? ColorOverride { get; set; }

    public Color? TextColorOverride
    {
        get => _textColorOverride;
        set
        {
            _textColorOverride = value;
            UpdateTextColor();
        }
    }

    public float PulsePeriod { get; set; } = 1.4f;

    /// <summary>
    /// Forces the active look without toggling the button.
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
            UpdateTextColor();
        }
    }

    public string? Text
    {
        get => _label.Text;
        set
        {
            _label.Text = value;
            _label.Visible = !string.IsNullOrEmpty(value);
            UpdatePadding();
        }
    }

    public Label Label => _label;

    /// <summary>
    /// Row holding the icon and the label, extra controls can be added to it.
    /// </summary>
    public BoxContainer Content => _content;

    /// <summary>
    /// Monochrome icon drawn before the text and tinted with the text color.
    /// </summary>
    public Texture? Icon
    {
        get => _icon.Texture;
        set
        {
            _icon.Texture = value;
            _icon.Visible = value != null;
            UpdatePadding();
        }
    }

    /// <summary>
    /// Size of <see cref="Icon"/> in virtual pixels.
    /// </summary>
    public float IconSize
    {
        get => _icon.SetWidth;
        set => _icon.SetSize = new Vector2(value, value);
    }

    private bool Active => IsActive || Pressed;

    public ShapedButton()
    {
        MinHeight = 28;

        _icon = new TextureRect
        {
            Visible = false,
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            SetSize = new Vector2(18, 18),
            VerticalAlignment = VAlignment.Center,
        };

        _label = new Label
        {
            Visible = false,
            VerticalAlignment = VAlignment.Center,
        };

        _content = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            SeparationOverride = 6,
        };

        _content.AddChild(_icon);
        _content.AddChild(_label);
        AddChild(_content);
        UpdatePadding();
    }

    protected override void StylePropertiesChanged()
    {
        base.StylePropertiesChanged();
        UpdateTextColor();
    }

    protected override void DrawModeChanged()
    {
        base.DrawModeChanged();
        UpdateTextColor();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!Active || !this.GetStyle(StylePropertyPulseEnabled, false))
        {
            _pulseTime = 0f;
            return;
        }

        _pulseTime += args.DeltaSeconds;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (PixelSize.X <= 0 || PixelSize.Y <= 0)
            return;

        Color fill;
        Color border;

        if (Active && !Disabled && ColorOverride == null)
        {
            var pulse = _pulseTime > 0f
                ? 0.5f - 0.5f * MathF.Cos(_pulseTime * MathF.Tau / MathF.Max(0.1f, PulsePeriod))
                : 0f;

            var active = this.GetStyle(StylePropertyActiveColor, Color.White);
            fill = Color.InterpolateBetween(active, this.GetStyle(StylePropertyActivePulseColor, active), pulse);

            var activeBorder = this.GetStyle(StylePropertyActiveBorderColor, Color.Transparent);
            border = activeBorder.WithAlpha(activeBorder.A * (0.35f + 0.65f * pulse));
        }
        else if (ColorOverride is { } color)
        {
            var text = _textColorOverride ?? this.GetStyle(StylePropertyTextColor, Color.White);

            fill = DrawMode switch
            {
                DrawModeEnum.Pressed => NebulaPalette.Mix(color, text, 0.8f),
                DrawModeEnum.Hover => NebulaPalette.Mix(color, text, 0.9f),
                DrawModeEnum.Disabled => color.WithAlpha(color.A * 0.38f),
                _ => color,
            };

            border = Color.Transparent;
        }
        else
        {
            fill = DrawMode switch
            {
                DrawModeEnum.Pressed => this.GetStyle(StylePropertyPressedColor, Color.Gray),
                DrawModeEnum.Hover => this.GetStyle(StylePropertyHoverColor, Color.Gray),
                DrawModeEnum.Disabled => this.GetStyle(StylePropertyDisabledColor, Color.DarkGray),
                _ => this.GetStyle(StylePropertyIdleColor, Color.Gray),
            };

            border = this.GetStyle(StylePropertyBorderColor, Color.Transparent);
        }

        var thickness = BorderThickness * UIScale;
        _outline.Update(PixelSizeBox, this.GetStyle(StylePropertyShape, ShapeStyle.Pill), UIScale, thickness);

        if (thickness <= 0f || border.A <= 0f)
        {
            ShapeGeometry.DrawFill(handle, _outline.Outer, fill);
            return;
        }

        ShapeGeometry.DrawFill(handle, _outline.Inner, fill);
        ShapeGeometry.DrawRing(handle, _outline.Outer, _outline.Inner, border);
    }

    private void UpdatePadding()
    {
        var iconOnly = _icon.Visible && !_label.Visible;
        _content.Margin = new Thickness(iconOnly ? 5 : 12, 0);
    }

    private void UpdateTextColor()
    {
        var color = _textColorOverride ?? this.GetStyle(StylePropertyTextColor, Color.White);

        if (Active && !Disabled && ColorOverride == null)
            color = this.GetStyle(StylePropertyActiveTextColor, color);

        _label.FontColorOverride = color;
        _icon.ModulateSelfOverride = color;
    }
}
