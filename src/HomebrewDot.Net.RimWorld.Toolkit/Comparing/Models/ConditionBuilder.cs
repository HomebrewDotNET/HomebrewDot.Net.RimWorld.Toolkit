using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.RimWorld.Collecting;
using HomebrewDot.Net.RimWorld.Collecting.Models;
using HomebrewDot.Net.RimWorld.Referencing;
using RimWorld;
using static HomebrewDot.Net.RimWorld.Toolkit.Helpers;

namespace HomebrewDot.Net.RimWorld.Comparing.Models
{
    /// <summary>
    /// Model for fluently building a condition for comparing objects using <see cref="IConditionBuilder{TReturn}"/>
    /// </summary>
    /// <typeparam name="TReturn">The return type for fluent chaining.</typeparam>
    public abstract class ConditionBuilder<TReturn> :
        IConditionBuilder<TReturn>,
        IConditionCompareBuilder<TReturn>,
        IConditionChainBuilder<TReturn>,
        IConditionToOperatorBuilder<TReturn>,
        IConditionWithBuilder<TReturn>,
        IConditionToBuilder<TReturn>,
        IConditionToRightBuilder<TReturn>
        where TReturn : IConditionToRightBuilder<TReturn>, IConditionChainBuilder<TReturn>
    {
        // State
        /// <summary>
        /// Current list of conditions being built.
        /// </summary>
        protected List<ConditionDef> _conditions = new List<ConditionDef>();
        /// <summary>
        /// The current state of the builder, used to enforce correct method chaining and provide better error messages when the builder is used incorrectly.
        /// </summary>
        protected int _state = 0; // 0 = initial, 1 = setting left operand, 2 = set left operand, setting operator, 3 = set operator, setting right operand, 4 = set right operand, 5 = condition groups set
        private object _leftOperand;
        private object _rightOperand;
        private object _operator;
        private bool _isOr;
        private IReadOnlyList<ConditionDef> _groupConditions;

        // Properties
        /// <summary>
        /// The final result of the builder, which is the list of conditions that have been built. This is only accessible once a complete condition has been built (i.e. left operand, operator, and right operand have all been set). Attempting to access this property before a complete condition has been built will throw an exception.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IReadOnlyList<ConditionDef> Conditions
        {
            get
            {
                if (_state == 0)
                {
                    return _conditions;
                }
                else if (_state == 4 || _state == 5)
                {
                    FinalizeCondition();
                    return _conditions;
                }
                else
                {
                    throw new InvalidOperationException("Cannot get conditions: condition being built is not complete (left operand, operator, or right operand not set).");
                }
            }
        }
        /// <summary>
        /// The return value for the builder, which is used for fluent chaining. This is typically the builder instance itself, but can be overridden by derived classes to return a different type if needed.
        /// </summary>
        public abstract TReturn Return { get; }

        /// <inheritdoc cref="ConditionBuilder{TReturn}"/>
        public ConditionBuilder()
        {
        }
        /// <inheritdoc />

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        IConditionCompareBuilder<TReturn> IConditionBuilder<TReturn>.Compare
        {
            get
            {
                if (_state != 0)
                    throw new InvalidOperationException("Cannot set left operand for condition: left operand already set or operator already set.");
                _state = 1;
                return this;
            }
        }
        /// <inheritdoc />
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        IConditionWithBuilder<TReturn> IConditionToOperatorBuilder<TReturn>.With
        {
            get
            {
                if (_state != 2)
                    throw new InvalidOperationException("Cannot set operator for condition: left operand not set, or operator already set but right operand not set, or both operands already set.");
                _state = 3;
                return this;
            }
        }
        /// <inheritdoc />
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        IConditionToBuilder<TReturn> IConditionToRightBuilder<TReturn>.To
        {
            get
            {
                if (_state != 3)
                    throw new InvalidOperationException("Cannot set right operand for condition: left operand not set, or operator not set, or right operand already set.");
                _state = 4;
                return this;
            }
        }
        /// <inheritdoc />
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        IConditionBuilder<TReturn> IConditionChainBuilder<TReturn>.And
        {
            get
            {
                if(_state < 4)
                    throw new InvalidOperationException("Cannot chain condition with AND: condition being built is not complete (left operand, operator, or right operand not set).");
                _isOr = false;
                if(_state == 4 || _state == 5)
                    FinalizeCondition();
                _state = 0;
                return this;
            }
        }
        /// <inheritdoc />
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        IConditionBuilder<TReturn> IConditionChainBuilder<TReturn>.Or
        {
            get
            {
                if (_state < 4)
                    throw new InvalidOperationException("Cannot chain condition with AND: condition being built is not complete (left operand, operator, or right operand not set).");
                _isOr = true;
                if (_state == 4 || _state == 5)
                    FinalizeCondition();
                _state = 0;
                return this;
            }
        }

        /// <inheritdoc />
        IConditionChainBuilder<TReturn> IConditionBuilder<TReturn>.Group(Func<IConditionBuilder, IConditionBuilder> groupBuilder)
        {
            groupBuilder = Guard.NotNull(groupBuilder, nameof(groupBuilder));
            if(_state == 0)
            {
                var nestedBuilder = new ConditionBuilder();
                _ = groupBuilder(nestedBuilder);
                _groupConditions = nestedBuilder.Conditions;
                _state = 5;
            }
            else
            {
                throw new InvalidOperationException("Cannot start condition group: condition being built is not complete (left operand, operator, or right operand not set).");
            }
            return this;
        }
        /// <inheritdoc />
        TReturn IConditionWithBuilder<TReturn>.Operator(IOperator @operator)
        {
            @operator = Guard.NotNull(@operator, nameof(@operator));
            if (_state == 3)
            {
                _operator = @operator;
                _state = 3;
            }
            else
            {
                throw new InvalidOperationException("Cannot set operator for condition: left operand not set, or operator already set but right operand not set, or both operands already set.");
            }
            return Return;
        }
        /// <inheritdoc />
        TReturn IConditionWithBuilder<TReturn>.Operator(string @operator)
        {
            @operator = Guard.NotNullOrWhitespace(@operator, nameof(@operator));
            if (_state == 3)
            {
                _operator = @operator;
                _state = 3;
            }
            else
            {
                throw new InvalidOperationException("Cannot set operator for condition: left operand not set, or operator already set but right operand not set, or both operands already set.");
            }
            return Return;
        }
        /// <inheritdoc />
        IConditionToOperatorBuilder<TReturn> IConditionOperandBuilder<IConditionToOperatorBuilder<TReturn>>.Reference(IReference reference)
        {
            reference = Guard.NotNull(reference, nameof(reference));
            if (_state == 1)
            {
                _leftOperand = reference;
                _state = 2;
            }
            else
            {
                throw new InvalidOperationException("Cannot set operand for condition: operator already set but right operand not set, or both operands already set.");
            }
            return this;
        }
        /// <inheritdoc />
        TReturn IConditionOperandBuilder<TReturn>.Reference(IReference reference)
        {
            reference = Guard.NotNull(reference, nameof(reference));
            if (_state == 4)
            {
                _rightOperand = reference;
            }
            else
            {
                throw new InvalidOperationException("Cannot set operand for condition: operator already set but right operand not set, or both operands already set.");
            }
            return Return;
        }

        private void FinalizeCondition()
        {
            if (_state < 4)
                throw new InvalidOperationException("Cannot finalize condition: left operand, operator, or right operand not set.");
            _conditions.Add(new ConditionDef
            {
                Compare = _leftOperand,
                With = _operator,
                To = _rightOperand,
                IsOr = _isOr,
                Conditions = _groupConditions?.ToArray() ?? Array.Empty<ConditionDef>(),
                ConditionGroupIsOr = _state == 5 ? _isOr : false
            });
            _state = 0;
            _leftOperand = null;
            _operator = null;
            _rightOperand = null;
            _isOr = false;
            _groupConditions = null;
        }
    }
    /// <summary>
    /// Model for fluently building a condition for comparing objects using <see cref="IConditionBuilder{TReturn}"/>. This is the non-generic version of <see cref="ConditionBuilder{TReturn}"/>, which simply returns itself for fluent chaining.
    /// </summary>
    public class ConditionBuilder : ConditionBuilder<IConditionBuilder>, IConditionBuilder
    {
        public ConditionBuilder() : base()
        {
        }

        /// <inheritdoc />
        public override IConditionBuilder Return => this;
    }
}
