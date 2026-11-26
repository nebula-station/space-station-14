// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Client/ChameleonStamp/ChameleonStampUserInterface.cs

using Content.Shared._Arcane.ChameleonStamp;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Arcane.ChameleonStamp;

[UsedImplicitly]
public sealed class ChameleonStampUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ChameleonStampWindow? _window;

    public ChameleonStampUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ChameleonStampWindow>();

        _window.SetOwner(Owner);
        _window.OnClose += Close;
        _window.ApplyButton.OnButtonDown += ApplyButton_OnButtonDown;

        _window.OpenCentered();
    }

    public override void Update()
    {
        base.Update();

        // The component is the state the server accepted, so this replaces the draft in the window.
        _window?.UpdateVisuals();
    }

    private void ApplyButton_OnButtonDown(Robust.Client.UserInterface.Controls.BaseButton.ButtonEventArgs obj)
    {
        if (_window is null)
            return;

        if (!EntMan.TryGetComponent<ChameleonStampComponent>(Owner, out var chameleonStamp))
            return;

        var stampColorMeta = _window.StampColorOptionButton.GetItemMetadata(_window.StampColorOptionButton.SelectedId);
        var stampSpriteMeta = _window.StampSpriteOptionButton.GetItemMetadata(_window.StampSpriteOptionButton.SelectedId);
        var stampStateMeta = _window.StampStateOptionButton.GetItemMetadata(_window.StampStateOptionButton.SelectedId);

        SendMessage(new ChameleonStampApplySettingsMessage(
            stampColorMeta is not null ? (string) stampColorMeta : chameleonStamp.DefaultStampPrototype,
            _window.StampColorSelector.Color,
            _window.StampMetaDataName.Text == "" ? null : _window.StampMetaDataName.Text,
            _window.StampMetaDataDescription.Text == "" ? null : _window.StampMetaDataDescription.Text,
            _window.StampedNameLineEdit.Text == "" ? null : _window.StampedNameLineEdit.Text,
            stampSpriteMeta is not null ? (string) stampSpriteMeta : chameleonStamp.DefaultStampPrototype,
            stampStateMeta is not null ? (string) stampStateMeta : chameleonStamp.DefaultStampPrototype));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _window != null)
        {
            _window.OnClose -= Close;
            _window.ApplyButton.OnButtonDown -= ApplyButton_OnButtonDown;
            _window.Orphan();
            _window = null;
        }

        base.Dispose(disposing);
    }
}
