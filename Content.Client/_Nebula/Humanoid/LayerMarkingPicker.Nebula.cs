// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._Nebula.CCVar;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Humanoid;

public sealed partial class LayerMarkingPicker
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private MarkingManager _markingManager = default!;
    [Dependency] private ILocalizationManager _loc = default!;

    private const string FilterOwn = "own";
    private const string FilterAll = "all";
    private const string FilterCommon = "common";

    private readonly List<string> _filters = [];
    private readonly List<LayerMarkingItem> _baseOrder = [];
    private ProtoId<MarkingsGroupPrototype> _group;
    private string _filter = FilterAll;
    private bool _built;
    private bool _resetQueued;

    /// <summary>
    /// Fills the filter with the markings groups of this layer and applies the remembered filter.
    /// </summary>
    public void SetRecommendationGroup(ProtoId<MarkingsGroupPrototype> group)
    {
        IoCManager.InjectDependencies(this);

        _group = group;

        var hasOwn = false;
        var hasCommon = false;
        HashSet<ProtoId<MarkingsGroupPrototype>> others = [];
        foreach (var marking in _allMarkings.Keys)
        {
            var groups = _markingManager.GetRecommendedGroups(marking);
            if (groups.Count == 0)
                hasCommon = true;

            foreach (var markingGroup in groups)
            {
                if (markingGroup == group)
                    hasOwn = true;
                else
                    others.Add(markingGroup);
            }
        }

        if (hasOwn)
            _filters.Add(FilterOwn);

        _filters.Add(FilterAll);

        if (hasCommon && (hasOwn || others.Count > 0))
            _filters.Add(FilterCommon);

        _filters.AddRange(others.Select(g => g.Id).OrderBy(GroupName));

        for (var i = 0; i < _filters.Count; i++)
        {
            FilterButton.AddItem(FilterName(_filters[i]), i);
        }

        FilterButton.Visible = _filters.Count > 1;
        FilterButton.OnItemSelected += args =>
        {
            _cfg.SetCVar(NCCVars.MarkingsFilter, _filters[args.Id]);
            _cfg.SaveToFile();
            SelectFilter(args.Id);
        };

        var remembered = _cfg.GetCVar(NCCVars.MarkingsFilter);
        var index = _filters.IndexOf(remembered);
        if (index < 0)
            index = _filters.IndexOf(FilterOwn);

        SelectFilter(Math.Max(index, 0));

        SearchBar.OnTextChanged += _ => UpdateVisibility();
        ReorderButton.OnPressed += _ => FilterButton.Visible = !ReorderButton.Pressed && _filters.Count > 1;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_built)
        {
            _built = true;
            _resetQueued = false;
            Build();
            return;
        }

        if (!_resetQueued)
            return;

        _resetQueued = false;
        UpdateCount();
        RefreshItems();
        RefreshFilter();
    }

    private void Build()
    {
        OrderingItems.AddChild(new LayerMarkingOrderer(_markingsModel, _organ, _layer));
        UpdateMarkings();

        foreach (var child in Items.Children)
        {
            if (child is LayerMarkingItem item)
                _baseOrder.Add(item);
        }

        UpdateCount();

        if (_filters.Count > 0)
            SelectFilter(Math.Max(_filters.IndexOf(_filter), 0));
    }

    private void QueueReset()
    {
        _resetQueued = true;
    }

    private void RefreshItems()
    {
        foreach (var item in _baseOrder)
        {
            item.RefreshSelection();
        }
    }

    private void SelectFilter(int index)
    {
        FilterButton.SelectId(index);
        _filter = _filters[index];

        var position = 0;
        foreach (var item in _baseOrder.OrderByDescending(IsSelected))
        {
            item.SetPositionInParent(position++);
        }

        UpdateVisibility();
    }

    /// <summary>
    /// Re-sorts and re-filters the items after the whole set of selected markings was replaced.
    /// </summary>
    private void RefreshFilter()
    {
        if (_filters.Count == 0)
            return;

        SelectFilter(_filters.IndexOf(_filter));
    }

    private void UpdateVisibility()
    {
        if (_filters.Count == 0)
            return;

        var query = SearchBar.Text.Trim();
        foreach (var item in _baseOrder)
        {
            item.Visible = item.CheckMatchesSearch(query) && (IsSelected(item) || MatchesFilter(item.MarkingId));
        }
    }

    private bool MatchesFilter(string marking)
    {
        var groups = _markingManager.GetRecommendedGroups(marking);
        return _filter switch
        {
            FilterAll => true,
            FilterOwn => groups.Contains(_group),
            FilterCommon => groups.Count == 0,
            _ => groups.Contains(new ProtoId<MarkingsGroupPrototype>(_filter)),
        };
    }

    private bool IsSelected(LayerMarkingItem item)
    {
        return _markingsModel.IsMarkingSelected(_organ, _layer, item.MarkingId);
    }

    private string FilterName(string filter)
    {
        return filter switch
        {
            FilterOwn or FilterAll or FilterCommon => Loc.GetString($"markings-filter-{filter}"),
            _ => GroupName(filter),
        };
    }

    private string GroupName(string group)
    {
        return _loc.TryGetString($"marking-group-{group}", out var name) ? name : group;
    }
}
