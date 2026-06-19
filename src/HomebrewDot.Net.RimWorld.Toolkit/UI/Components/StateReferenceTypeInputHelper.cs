using System;
using System.Linq;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.UI.Components;
using RimWorld;
using Verse;

namespace HomebrewDot.Net.Rimworld.UI.Components
{
    /// <summary>
    /// Input helper for <see cref="StatReferenceType"/>.
    /// </summary>
    public class StateReferenceTypeInputHelper : IReferenceTypeInputHelper
    {
        /// <summary>
        /// Singleton instance of the <see cref="StateReferenceTypeInputHelper"/>.
        /// </summary>
        public static StateReferenceTypeInputHelper Instance { get; } = new StateReferenceTypeInputHelper();

        private StateReferenceTypeInputHelper()
        {
        }

        public Window GetInputWindow(string name, IReferenceType referenceType, Action<string> onSelected)
        {
            var allStats = DefDatabase<StatDef>.AllDefsListForReading
                .OrderBy(s => s.label)
                .ToList();

            var optionsGrid = new Grid<StatDef>(
                drawContent: (cellRect, value) => Widgets.Label(cellRect.ContractedBy(4f), value.LabelCap),
                getTooltip: x => x.defName,
                cellWidth: 300f,
                cellHeight: 32f,
                cellGap: 4f);

            var selectedGrid = new Grid<StatDef>(
                drawContent: (cellRect, value) => Widgets.Label(cellRect.ContractedBy(4f), value.LabelCap),
                getTooltip: x => x.defName,
                cellWidth: 300f,
                cellHeight: 32f,
                cellGap: 4f);

            return new SelectionWindow<StatDef>(
                title: "Select Stat",
                options: allStats,
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
