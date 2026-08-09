using System;
using System.Linq;
using HomebrewDot.Net.Rimworld.Referencing;
using Verse;

namespace HomebrewDot.Net.Rimworld.UI.Components
{
    /// <summary>
    /// Input helper for <see cref="Referencing.Components.DefReferenceType{T}"/>. Provides a selection window for picking a def of type <typeparamref name="T"/> from the <see cref="DefDatabase{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of def to select.</typeparam>
    public class DefReferenceTypeInputHelper<T> : IReferenceTypeInputHelper where T : Def
    {
        /// <summary>
        /// Singleton instance of the <see cref="DefReferenceTypeInputHelper{T}"/>.
        /// </summary>
        public static DefReferenceTypeInputHelper<T> Instance { get; } = new DefReferenceTypeInputHelper<T>();

        private DefReferenceTypeInputHelper()
        {
        }

        /// <inheritdoc/>
        public Window GetInputWindow(string name, IReferenceType referenceType, Action<string> onSelected)
        {
            var allDefs = DefDatabase<T>.AllDefsListForReading
                .OrderBy(d => d.label)
                .ToList();

            var optionsGrid = new Grid<T>(
                drawContent: (cellRect, value) => Widgets.Label(cellRect.ContractedBy(4f), value.LabelCap),
                getTooltip: x => x.defName,
                cellWidth: 300f,
                cellHeight: 32f,
                cellGap: 4f);

            var selectedGrid = new Grid<T>(
                drawContent: (cellRect, value) => Widgets.Label(cellRect.ContractedBy(4f), value.LabelCap),
                getTooltip: x => x.defName,
                cellWidth: 300f,
                cellHeight: 32f,
                cellGap: 4f);

            return new SelectionWindow<T>(
                title: $"Select {typeof(T).Name}",
                options: allDefs,
                optionsGrid: optionsGrid,
                selectedGrid: selectedGrid,
                onConfirm: selected =>
                {
                    if (selected != null && selected.Count > 0)
                    {
                        onSelected?.Invoke(selected[0].defName);
                    }
                },
                allowMultipleSelection: false,
                enableFiltering: true,
                getFilterStrings: x => new[] { x.defName, x.LabelCap.ToString() });
        }
    }
}
