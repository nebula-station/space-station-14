using Content.Client.FlavorText;
using Content.Shared._Nebula.DetailExaminable;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private bool _allowFlavorText;

    private FlavorText.FlavorText? _flavorText;

    /// <summary>
    /// Refreshes the flavor text editor status.
    /// </summary>
    public void RefreshFlavorText()
    {
        if (_allowFlavorText)
        {
            if (_flavorText != null)
                return;

            _flavorText = new FlavorText.FlavorText();
            TabContainer.AddChild(_flavorText);
            TabContainer.SetTabTitle(TabContainer.ChildCount - 1, Loc.GetString("humanoid-profile-editor-flavortext-tab"));

            _flavorText.OnTextChanged += OnFlavorFieldChanged; // Nebula edit
        }
        else
        {
            if (_flavorText == null)
                return;

            // Nebula start
            _flavorText.OnTextChanged -= OnFlavorFieldChanged;

            TabContainer.RemoveChild(_flavorText);
            _flavorText.Dispose();
            _flavorText = null;
            // Nebula end
        }
    }

    // Nebula start
    private void OnFlavorFieldChanged(FlavorTextField field, string text)
    {
        if (Profile == null)
            return;

        Profile = Profile.WithDetailExaminableData(Profile.DetailExaminableData.CopyWithChangedField(field, text));
        SetDirty(); ;
    }

    private void UpdateFlavorTextEdit()
    {
        _flavorText?.SetData(Profile?.DetailExaminableData ?? new DetailExaminableData());
    }
    // Nebula end
}
