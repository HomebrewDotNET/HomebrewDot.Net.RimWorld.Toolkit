using System;
using HomebrewDot.Net.Rimworld.Referencing;
using Verse;

namespace HomebrewDot.Net.Rimworld.UI
{
    /// <summary>
    /// UI helper that exposes a Window for selecting values for a certain <see cref="IReferenceType"/>.
    /// </summary>
    public interface IReferenceTypeInputHelper
    {
        /// <summary>
        /// Gets a Window for selecting a value for the given <see cref="IReferenceType"/>.
        /// </summary>
        /// <param name="name">The name of <paramref name="referenceType"/>.</param>
        /// <param name="referenceType">The reference type for which to select a value.</param>
        /// <param name="onSelected">Called with the selected value (e.g. defName) when the user confirms a selection.</param>
        /// <returns>A Window for selecting a value.</returns>
        Window GetInputWindow(string name, IReferenceType referenceType, Action<string> onSelected);
    }
}