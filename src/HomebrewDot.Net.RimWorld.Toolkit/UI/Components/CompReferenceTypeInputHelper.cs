using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using Verse;

namespace HomebrewDot.Net.Rimworld.UI.Components
{
    /// <summary>
    /// Input helper for <see cref="Referencing.Components.CompReferenceType"/>. Provides a two-step selection window for picking a comp type (either a <see cref="ThingComp"/> or a <see cref="CompProperties"/> type) and, optionally, a property or field to resolve on that comp. The produced value is either the comp type name or <c>TypeName|MemberName</c> using the <see cref="CompReferenceType.PathSeparator"/>.
    /// </summary>
    public class CompReferenceTypeInputHelper : IReferenceTypeInputHelper
    {
        /// <summary>
        /// Singleton instance of the <see cref="CompReferenceTypeInputHelper"/>.
        /// </summary>
        public static CompReferenceTypeInputHelper Instance { get; } = new CompReferenceTypeInputHelper();

        private static readonly Lazy<Type[]> _compTypes = new Lazy<Type[]>(() => ScanCompTypes());

        private CompReferenceTypeInputHelper()
        {
        }

        /// <summary>
        /// Scans all loaded assemblies for concrete comp types (subclasses of <see cref="ThingComp"/> or <see cref="CompProperties"/>). The result is ordered by simple type name and cached.
        /// </summary>
        /// <returns>All concrete comp types found.</returns>
        internal static Type[] ScanCompTypes()
        {
            return Toolkit.Helpers.ScanForTypes(x =>
                    x.IsClass && !x.IsAbstract && !x.IsGenericTypeDefinition &&
                    (typeof(ThingComp).IsAssignableFrom(x) || typeof(CompProperties).IsAssignableFrom(x)))
                .OrderBy(x => x.Name)
                .ToArray();
        }

        /// <summary>
        /// Builds the reference value for the given comp type and optional member. Without a member the value is the comp type name; with a member it becomes <c>TypeName|MemberName</c> using the <see cref="CompReferenceType.PathSeparator"/>.
        /// </summary>
        /// <param name="compType">The comp type to reference.</param>
        /// <param name="member">Optional member to traverse on the resolved comp.</param>
        /// <returns>The reference value.</returns>
        internal static string BuildValue(Type compType, MemberInfo member)
        {
            if (compType == null) return null;
            return member == null
                ? compType.Name
                : $"{compType.Name}{CompReferenceType.PathSeparator}{member.Name}";
        }

        /// <inheritdoc/>
        public Window GetInputWindow(string name, IReferenceType referenceType, Action<string> onSelected)
        {
            var compOptions = _compTypes.Value
                .Select(x => new CompTypeOption(x))
                .OrderBy(x => x.KindLabel)
                .ThenBy(x => x.Label)
                .ToList();

            var optionsGrid = new Grid<CompTypeOption>(
                drawContent: (cellRect, value) => Widgets.Label(cellRect.ContractedBy(4f), value.Label),
                getTooltip: x => x.Tooltip,
                cellWidth: 300f,
                cellHeight: 32f,
                cellGap: 4f);

            var selectedGrid = new Grid<CompTypeOption>(
                drawContent: (cellRect, value) => Widgets.Label(cellRect.ContractedBy(4f), value.Label),
                getTooltip: x => x.Tooltip,
                cellWidth: 300f,
                cellHeight: 32f,
                cellGap: 4f);

            return new SelectionWindow<CompTypeOption>(
                title: "Select Comp",
                options: compOptions,
                optionsGrid: optionsGrid,
                selectedGrid: selectedGrid,
                onConfirm: selected =>
                {
                    var option = selected.FirstOrDefault();
                    if (option == null)
                    {
                        return;
                    }
                    OpenPropertyPicker(option, onSelected);
                },
                allowMultipleSelection: false,
                enableFiltering: true,
                getFilterStrings: x => new[] { x.Label, x.Type.FullName, x.KindLabel });
        }

        private void OpenPropertyPicker(CompTypeOption compOption, Action<string> onSelected)
        {
            var properties = new List<PropertyOption> { PropertyOption.WholeComp };
            properties.AddRange(Toolkit.Helpers.Traversing.GetMembers(compOption.Type)
                .OrderBy(m => m.Name)
                .Select(m => new PropertyOption(m)));

            var optionsGrid = new Grid<PropertyOption>(
                drawContent: (cellRect, value) => Widgets.Label(cellRect.ContractedBy(4f), value.Label),
                getTooltip: x => x.Tooltip,
                cellWidth: 300f,
                cellHeight: 32f,
                cellGap: 4f);

            var selectedGrid = new Grid<PropertyOption>(
                drawContent: (cellRect, value) => Widgets.Label(cellRect.ContractedBy(4f), value.Label),
                getTooltip: x => x.Tooltip,
                cellWidth: 300f,
                cellHeight: 32f,
                cellGap: 4f);

            Find.WindowStack.Add(new SelectionWindow<PropertyOption>(
                title: $"Select Property on {compOption.Type.Name}",
                options: properties,
                optionsGrid: optionsGrid,
                selectedGrid: selectedGrid,
                onConfirm: selected =>
                {
                    var property = selected.FirstOrDefault();
                    if (property == null)
                    {
                        return;
                    }
                    onSelected?.Invoke(BuildValue(compOption.Type, property.Member));
                },
                allowMultipleSelection: false,
                enableFiltering: true,
                getFilterStrings: x => new[] { x.Label, x.Member?.Name, x.Member?.MemberType.ToString() }));
        }

        private sealed class CompTypeOption
        {
            public CompTypeOption(Type type)
            {
                Type = type;
                IsThingComp = typeof(ThingComp).IsAssignableFrom(type);
            }

            public Type Type { get; }
            public bool IsThingComp { get; }
            public string KindLabel => IsThingComp ? "ThingComp" : "CompProperties";
            public string Label => $"{Type.Name} ({KindLabel})";
            public string Tooltip => $"{Type.FullName}\nResolves from {(IsThingComp ? "a Thing" : "a Def")}.";
        }

        private sealed class PropertyOption
        {
            public static readonly PropertyOption WholeComp = new PropertyOption(null);

            public PropertyOption(MemberInfo member)
            {
                Member = member;
            }

            public MemberInfo Member { get; }
            public bool IsWholeComp => Member == null;
            public string Label => IsWholeComp ? "(whole comp)" : Member.Name;
            public string Tooltip => IsWholeComp
                ? "Use the comp itself"
                : $"{GetMemberType(Member).Name} {Member.DeclaringType?.Name}.{Member.Name}";

            private static Type GetMemberType(MemberInfo member)
            {
                return member is PropertyInfo propertyInfo
                    ? propertyInfo.PropertyType
                    : member is FieldInfo fieldInfo
                        ? fieldInfo.FieldType
                        : typeof(object);
            }
        }
    }
}
