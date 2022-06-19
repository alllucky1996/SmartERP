﻿using Smartstore.Utilities;

namespace Smartstore.Core.Rules
{
    public enum LogicalRuleOperator
    {
        And,
        Or
    }

    public interface IRuleExpressionGroup : IRuleExpression
    {
        int RefRuleId { get; set; }
        LogicalRuleOperator LogicalOperator { get; }
        bool IsSubGroup { get; }
        IRuleProvider Provider { get; }
        IEnumerable<IRuleExpression> Expressions { get; }
        void AddExpressions(IEnumerable<IRuleExpression> expressions);
    }

    public class RuleExpressionGroup : RuleExpression, IRuleExpressionGroup
    {
        private readonly List<IRuleExpression> _expressions = new();

        public int RefRuleId { get; set; }
        public LogicalRuleOperator LogicalOperator { get; set; }
        public bool IsSubGroup { get; set; }
        public IRuleProvider Provider { get; set; }

        public IEnumerable<IRuleExpression> Expressions => _expressions;

        public virtual void AddExpressions(IEnumerable<IRuleExpression> expressions)
        {
            Guard.NotNull(expressions, nameof(expressions));
            _expressions.AddRange(expressions);
        }

        public override int GetHashCode()
        {
            var combiner = HashCodeCombiner
                .Start()
                .Add(Expressions.Select(x => x.GetHashCode()));

            return combiner.CombinedHash;
        }
    }
}