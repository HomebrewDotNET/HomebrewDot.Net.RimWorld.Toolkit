using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace HomebrewDot.Net.Rimworld.UI.Components
{
    /// <summary>
    /// Generic selection window supporting single or multiple selection modes.
    /// </summary>
    /// <typeparam name="T">The item type to select.</typeparam>
    public sealed class SelectionWindow<T> : Window
    {
        private readonly string _title;
        private readonly List<T> _allOptions;
        private readonly Grid<T> _optionsGrid;
        private readonly Grid<T> _selectedGrid;
        private readonly bool _allowMultipleSelection;
        private readonly bool _enableFiltering;
        private readonly Func<T, IEnumerable<string>> _getFilterStrings;
        private readonly Func<T, string, bool> _filterPredicate;
        private readonly Action<IReadOnlyList<T>> _onConfirm;

        private readonly List<T> _selectedOptions = new List<T>();
        private readonly List<T> _filteredOptions = new List<T>();

        private Vector2 _optionsScroll = Vector2.zero;
        private Vector2 _selectedScroll = Vector2.zero;
        private string _search = string.Empty;

        /// <summary>
        /// Creates a new selection window.
        /// </summary>
        /// <param name="title">Window title.</param>
        /// <param name="options">Options to choose from.</param>
        /// <param name="optionsGrid">Grid used to draw selectable options.</param>
        /// <param name="selectedGrid">Grid used to draw selected options.</param>
        /// <param name="onConfirm">Callback invoked with selected options on confirm.</param>
        /// <param name="allowMultipleSelection">Whether multiple options may be selected.</param>
        /// <param name="enableFiltering">Whether search/filtering controls are shown and applied.</param>
        /// <param name="getFilterStrings">Optional filter strings provider. Defaults to ToString().</param>
        /// <param name="filterPredicate">Optional custom filter predicate.</param>
        /// <param name="initialSelection">Optional initial selection.</param>
        public SelectionWindow(
            string title,
            IEnumerable<T> options,
            Grid<T> optionsGrid,
            Grid<T> selectedGrid,
            Action<IReadOnlyList<T>> onConfirm,
            bool allowMultipleSelection = true,
            bool enableFiltering = true,
            Func<T, IEnumerable<string>> getFilterStrings = null,
            Func<T, string, bool> filterPredicate = null,
            IEnumerable<T> initialSelection = null)
        {
            _title = title ?? string.Empty;
            _allOptions = options?.ToList() ?? new List<T>();
            _optionsGrid = optionsGrid ?? throw new ArgumentNullException(nameof(optionsGrid));
            _selectedGrid = selectedGrid ?? throw new ArgumentNullException(nameof(selectedGrid));
            _onConfirm = onConfirm;
            _allowMultipleSelection = allowMultipleSelection;
            _enableFiltering = enableFiltering;
            _getFilterStrings = getFilterStrings;
            _filterPredicate = filterPredicate;

            if (initialSelection != null)
            {
                foreach (var selected in initialSelection)
                {
                    if (_allowMultipleSelection)
                    {
                        if (!_selectedOptions.Contains(selected))
                        {
                            _selectedOptions.Add(selected);
                        }
                    }
                    else
                    {
                        _selectedOptions.Clear();
                        _selectedOptions.Add(selected);
                        break;
                    }
                }
            }

            _optionsGrid.OnClick += AddSelection;
            _selectedGrid.OnClick += RemoveSelection;

            closeOnClickedOutside = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
            forcePause = true;
            doCloseButton = false;

            RebuildFilteredOptions();
        }

        /// <inheritdoc />
        public override Vector2 InitialSize => new Vector2(980f, 720f);

        /// <inheritdoc />
        public override void DoWindowContents(Rect inRect)
        {
            var cursorY = inRect.y;

            Widgets.Label(new Rect(inRect.x, cursorY, inRect.width, 26f), _title);
            cursorY += 30f;

            if (_enableFiltering)
            {
                Widgets.Label(new Rect(inRect.x, cursorY, 64f, 24f), "Search:");
                var searchRect = new Rect(inRect.x + 68f, cursorY - 2f, Mathf.Max(120f, inRect.width - 68f), 28f);
                var nextSearch = Widgets.TextField(searchRect, _search ?? string.Empty);
                if (!string.Equals(nextSearch, _search, StringComparison.Ordinal))
                {
                    _search = nextSearch;
                    RebuildFilteredOptions();
                }
                cursorY += 32f;
            }

            var buttonsHeight = 36f;
            var selectedHeight = Mathf.Max(110f, inRect.height * 0.28f);
            var selectedLabelRect = new Rect(inRect.x, inRect.yMax - buttonsHeight - selectedHeight - 28f, inRect.width, 22f);
            var selectedRect = new Rect(inRect.x, selectedLabelRect.yMax + 4f, inRect.width, selectedHeight);

            var optionsLabelRect = new Rect(inRect.x, cursorY, inRect.width, 22f);
            Widgets.Label(optionsLabelRect, "Options");
            var optionsRect = new Rect(inRect.x, optionsLabelRect.yMax + 4f, inRect.width, Mathf.Max(80f, selectedLabelRect.y - (optionsLabelRect.yMax + 8f)));

            Widgets.DrawMenuSection(optionsRect);
            var optionsContentRect = optionsRect.ContractedBy(6f);
            _optionsGrid.Draw(optionsContentRect, ref _optionsScroll, _filteredOptions);

            Widgets.Label(selectedLabelRect, _allowMultipleSelection ? "Selected" : "Selected (single)");
            Widgets.DrawMenuSection(selectedRect);
            var selectedContentRect = selectedRect.ContractedBy(6f);
            _selectedGrid.Draw(selectedContentRect, ref _selectedScroll, _selectedOptions);

            DrawButtons(new Rect(inRect.x, inRect.yMax - buttonsHeight, inRect.width, buttonsHeight));
        }

        private void DrawButtons(Rect rect)
        {
            var buttonWidth = 120f;
            var cancelRect = new Rect(rect.x, rect.y, buttonWidth, rect.height);
            var confirmRect = new Rect(cancelRect.xMax + 8f, rect.y, buttonWidth, rect.height);

            Widgets.DrawMenuSection(cancelRect);
            if (Widgets.ButtonInvisible(cancelRect))
            {
                Close();
            }
            Widgets.Label(cancelRect.ContractedBy(4f), "Cancel");

            Widgets.DrawMenuSection(confirmRect);
            if (Widgets.ButtonInvisible(confirmRect))
            {
                _onConfirm?.Invoke(_selectedOptions.ToList());
                Close();
            }
            Widgets.Label(confirmRect.ContractedBy(4f), "Confirm");
        }

        private void AddSelection(T option)
        {
            if (_allowMultipleSelection)
            {
                if (!_selectedOptions.Contains(option))
                {
                    _selectedOptions.Add(option);
                }
                return;
            }

            _selectedOptions.Clear();
            _selectedOptions.Add(option);
        }

        private void RemoveSelection(T option)
        {
            _selectedOptions.Remove(option);
        }

        private void RebuildFilteredOptions()
        {
            _filteredOptions.Clear();

            if (!_enableFiltering || string.IsNullOrWhiteSpace(_search))
            {
                _filteredOptions.AddRange(_allOptions);
                return;
            }

            var query = _search.Trim();

            for (var i = 0; i < _allOptions.Count; i++)
            {
                var option = _allOptions[i];
                var include = _filterPredicate != null
                    ? _filterPredicate(option, query)
                    : DefaultFilter(option, query);

                if (include)
                {
                    _filteredOptions.Add(option);
                }
            }
        }

        private bool DefaultFilter(T option, string query)
        {
            var values = _getFilterStrings != null
                ? _getFilterStrings(option)
                : new[] { option?.ToString() ?? string.Empty };

            foreach (var value in values)
            {
                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }

                if (value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
