using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Referencing.Models;
using RimWorld;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using Expression = System.Linq.Expressions.Expression;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers.Logging;

namespace HomebrewDot.Net.Rimworld.Referencing.Components
{
	/// <summary>
	/// Reference type for resolving stats from a given input, which can be either a <see cref="Def"/> or a <see cref="Thing"/>. The value is expected to be the name of the stat to resolve. The reference will first attempt to resolve the stat from the def if the input is a def, and if that fails, it will attempt to resolve it from the thing if the input is a thing. If both attempts fail, it will return null.
	/// </summary>
	public class StatReferenceType : IReferenceTypeCompileable
	{
		// Constants
		/// <summary>
		/// The default name for this reference type, which can be used when defining references that should be resolved using this type.
		/// </summary>
		public const string DefaultTypeName = "Stat";
		// Statics
		/// <summary>
		/// The singleton instance of the <see cref="StatReferenceType"/>. This can be used wherever an instance of this reference type is needed, without the need to create multiple instances since it is stateless and thread-safe.
		/// </summary>
		public static StatReferenceType Instance { get; } = new StatReferenceType();

		private StatReferenceType()
		{

		}

		/// <inheritdoc/>
		public object Resolve(object input, object value, IReadOnlyDictionary<string, object> context)
		{
			StatDef statDef;
			if (value is StatDef s)
			{
				statDef = s;
			}
			else
			{
				var stat = value?.ToString();
				if (string.IsNullOrWhiteSpace(stat)) return null;
				statDef = StatDef.Named(stat);
				if (statDef is null)
				{
					if(IsVerboseEnabled) LogVerbose($"StatReferenceType: Could not find stat with name '{stat}'");
					return false;
				}
			}

			// Try def first
			Verse.Def def = null;
			if (input is IIndexed<Def> indexed)
			{
				def = indexed.Value;
			}
			else if (input is Def d)
			{
				def = d;
			}

			if (def != null)
			{
				if (def is BuildableDef buildableDef)
				{
					return statDef.Worker?.GetValueAbstract(buildableDef);
				}
				return null;
			}

			// Try thing last
			Thing thing = null;
			if (input is IIndexed<Thing> indexedThing)
			{
				thing = indexedThing.Value;
			}
			else if (input is Thing t)
			{
				thing = t;
			}

			if (thing != null)
			{
				return statDef.Worker?.GetValue(thing);
			}

			return null;
		}
		/// <inheritdoc/>
		public string GetCacheKey(object input, object value, IReadOnlyDictionary<string, object> context, out Type returnType)
		{
			returnType = typeof(float);
			if (input is null) return null;
			if (value is null) return null;
			if (!(input is IIndexed<Def> || input is Def || input is IIndexed<Thing> || input is Thing)) return null;
			if (!(value is string || value is StatDef)) return null;

			return $"{input.GetType()}:{value.GetType()}";
		}
		/// <inheritdoc/>
		public System.Linq.Expressions.Expression Compile(ParameterExpression inputParameter, object input, ParameterExpression contextParameter, object value, IReadOnlyDictionary<string, object> context)
		{
			StatDef statDef;
			if (value is StatDef s)
			{
				statDef = s;
			}
			else
			{
				var stat = value?.ToString();
				if (string.IsNullOrWhiteSpace(stat)) return null;
				statDef = StatDef.Named(stat);
				if (statDef is null)
				{
					if(IsVerboseEnabled) LogVerbose($"StatReferenceType: Could not find stat with name '{stat}'");
					return ToolkitConstants.Expressions<float>.Default;
				}
			}
			if (statDef.Worker is null)
			{
				if(IsVerboseEnabled) LogVerbose($"StatReferenceType: Stat def '{statDef}' does not have a worker");
				return ToolkitConstants.Expressions<float>.Default;
			}
			Expression getDef = null;

			if (input is IIndexed<Def> indexedDef)
			{
				var getIndexedValue = Toolkit.Helpers.Expression.GetProperty<IIndexed<Def>>(x => x.Value);
				getDef = Expression.Property(Expression.Convert(inputParameter, typeof(IIndexed<Def>)), getIndexedValue);
			}
			else if (input is Def defInput)
			{
				getDef = Expression.Convert(inputParameter, typeof(Def));
			}
			var getStatDefWorker = Expression.Constant(statDef.Worker);
			if(getDef != null)
			{
				var getValueAbstract = Toolkit.Helpers.Expression.GetMethod<StatWorker>(x => x.GetValueAbstract(default, default(ThingDef)));
				var buildableVariable = Expression.Variable(typeof(BuildableDef), "buildableDef");
				var resultVariable = Expression.Variable(typeof(float), "result");
				var assignBuildableVariable = Expression.Assign(buildableVariable, Expression.Convert(getDef, typeof(BuildableDef)));
				var ifBuildableDef = Expression.IfThen(Expression.NotEqual(buildableVariable, Expression.Default(typeof(BuildableDef))),
					Expression.Assign(resultVariable, Expression.Call(getStatDefWorker, getValueAbstract, buildableVariable, ToolkitConstants.Expressions<ThingDef>.Default)));
				var block = Expression.Block(new ParameterExpression[] { buildableVariable, resultVariable }, assignBuildableVariable, ifBuildableDef, resultVariable);
				return block;
			}
			Expression getThing = null;

			if (input is IIndexed<Thing> indexedThing)
			{
				var getIndexedValue = Toolkit.Helpers.Expression.GetProperty<IIndexed<Thing>>(x => x.Value);
				getThing = Expression.Property(Expression.Convert(inputParameter, typeof(IIndexed<Thing>)), getIndexedValue);
			}
			else if (input is Thing defInput)
			{
				getThing = Expression.Convert(inputParameter, typeof(Thing));
			}

			if(getThing != null)
			{
				var getValue = Toolkit.Helpers.Expression.GetMethod<StatWorker>(x => x.GetValue(default(Thing), default(bool), default));
				return Expression.Call(getStatDefWorker, getValue, getThing, Expression.Constant(true), Expression.Constant(TimeSpan.Zero));
			}
			return ToolkitConstants.Expressions<float>.Default;
		}
	}

	/// <summary>
	/// Contains extension methods related to the <see cref="StatReferenceType"/>.
	/// </summary>
	public static class StatReferenceTypeExtensions
	{
		/// <summary>
		/// Fluent syntax for creating a reference definition that uses the <see cref="StatReferenceType"/> to resolve a stat value from an object. The provided stat name will be used as the value for the reference definition, and the type will be set to the default type name of the <see cref="StatReferenceType"/>. This allows for easy creation of references that can access stats of objects in a fluent manner.
		/// </summary>
		/// <typeparam name="TReturn">The fluent return type.</typeparam>
		/// <param name="builder">The condition builder.</param>
		/// <param name="statName">The name of the stat to resolve from the object.</param>
		/// <returns>The fluent return type.</returns>
		public static TReturn Stat<TReturn>(this IConditionOperandBuilder<TReturn> builder, string statName)
			=> Guard.NotNull(builder, nameof(builder)).Reference(new ReferenceDef() { Type = StatReferenceType.DefaultTypeName, Value = Guard.NotNullOrEmpty(statName, nameof(statName)) });

		/// <summary>
		/// Fluent syntax for creating a reference definition that uses the <see cref="StatReferenceType"/> to resolve a stat value from an object. The provided <see cref="StatDef"/> will be used as the value for the reference definition, and the type will be set to the default type name of the <see cref="StatReferenceType"/>. This allows for easy creation of references that can access stats of objects in a fluent manner, while also providing type safety by using the actual <see cref="StatDef"/> instead of just its name as a string.
		/// </summary>
		/// <typeparam name="TReturn">The fluent return type.</typeparam>
		/// <param name="builder">The condition builder.</param>
		/// <param name="statDef">The stat definition to resolve from the object.</param>
		/// <returns>The fluent return type.</returns>
		public static TReturn Stat<TReturn>(this IConditionOperandBuilder<TReturn> builder, StatDef statDef)
			=> Guard.NotNull(builder, nameof(builder)).Reference(new ReferenceDef() { Type = StatReferenceType.DefaultTypeName, Value = Guard.NotNull(statDef, nameof(statDef)) });
	}
}
