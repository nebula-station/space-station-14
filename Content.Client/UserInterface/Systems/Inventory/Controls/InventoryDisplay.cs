using System.Numerics;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Inventory.Controls;

public sealed class InventoryDisplay : LayoutContainer
{
    private float _columns = 0; // Lua
    private float _rows = 0; // Lua
    private const int MarginThickness = 10;
    private const int ButtonSpacing = 5;
    private const int ButtonSize = 75;
    private readonly Control resizer;

    // Lua start
    public int CellSize { get; set; } = ButtonSize;
    public int CellSpacing { get; set; } = ButtonSpacing;
    public bool InvertY { get; set; }
    // Lua end

    private readonly Dictionary<string, (SlotControl, Vector2, float)> _buttons = new(); // Lua

    public InventoryDisplay()
    {
        resizer = new Control();
        AddChild(resizer);
    }

    public SlotControl AddButton(SlotControl newButton, Vector2i buttonOffset)
    {
        return AddButton(newButton, new Vector2(buttonOffset.X, buttonOffset.Y)); // Lua
    }

    // Lua
    public SlotControl AddButton(SlotControl newButton, Vector2 buttonOffset, float scale = 1f)
    {
        AddChild(newButton);
        HorizontalExpand = true;
        VerticalExpand = true;
        InheritChildMeasure = true;
        if (!_buttons.TryAdd(newButton.SlotName, (newButton, buttonOffset, scale))) // Lua
            Logger.Warning("Tried to add button without a slot!");
        UpdateSizeData(buttonOffset, scale); // Lua
        Relayout(); // Lua
        return newButton;
    }

    public SlotControl? GetButton(string slotName)
    {
        return !_buttons.TryGetValue(slotName, out var foundButton) ? null : foundButton.Item1;
    }

    private void UpdateSizeData(Vector2 buttonOffset, float scale = 1f) // Lua
    {
        // Lua start
        var x = buttonOffset.X + scale - 1;
        if (x > _columns)
            _columns = x;
        var y = buttonOffset.Y + scale - 1;
        if (y > _rows)
            _rows = y;
        resizer.SetHeight = (_rows + 1) * (CellSize + CellSpacing);
        resizer.SetWidth = (_columns + 1) * (CellSize + CellSpacing);
        // Lua end
    }

    // Lua start
    public void Relayout()
    {
        foreach (var (_, (button, offset, scale)) in _buttons)
        {
            var y = InvertY ? _rows + 1 - offset.Y - scale : offset.Y;
            SetPosition(button, new Vector2(offset.X, y) * CellSize + new Vector2(CellSpacing, CellSpacing));
        }
    }
    // Lua end

    public bool TryGetButton(string slotName, out SlotControl? button)
    {
        var success = _buttons.TryGetValue(slotName, out var buttonData);
        button = buttonData.Item1;
        return success;
    }

    public void RemoveButton(string slotName)
    {
        // Lua start
        if (!_buttons.Remove(slotName, out var removed))
            return;
        RemoveChild(removed.Item1);
        //recalculate the size of the control when a slot is removed
        _columns = 0;
        _rows = 0;
        foreach (var (_, (_, buttonOffset, scale)) in _buttons) // Lua
        {
            UpdateSizeData(buttonOffset, scale); // Lua
        }
        Relayout();
        // Lua end
    }

    public void ClearButtons()
    {
        // Lua start
        foreach (var (_, (button, _, _)) in _buttons)
        {
            RemoveChild(button);
        }

        _buttons.Clear();
        _columns = 0;
        _rows = 0;
        // Lua end
    }
}
