﻿using Smartstore.Core.Rules.Filters;

namespace Smartstore.Core.Rules.Operators
{
    internal sealed class EqualOperator : RuleOperator
    {
        internal EqualOperator()
            : base("=") { }

        protected override Expression GenerateExpression(Expression left, Expression right, IQueryProvider provider)
        {
            if (GetBodyType(left) == typeof(string))
            {
                left = left.CallToLower(provider);
                right = right.CallToLower(provider);
            }

            return Expression.Equal(left, right);
        }
    }

    internal sealed class NotEqualOperator : RuleOperator
    {
        internal NotEqualOperator()
            : base("!=") { }

        protected override Expression GenerateExpression(Expression left, Expression right, IQueryProvider provider)
        {
            if (GetBodyType(left) == typeof(string))
            {
                left = left.CallToLower(provider);
                right = right.CallToLower(provider);
            }

            return Expression.NotEqual(left, right);
        }
    }

    internal sealed class IsNullOperator : RuleOperator
    {
        internal IsNullOperator()
            : base("IsNull") { }

        protected override Expression GenerateExpression(Expression left, Expression right, IQueryProvider provider)
        {
            return Expression.Equal(
                left,
                ExpressionHelper.CreateConstantExpression(null, GetBodyType(left)));
        }
    }

    internal sealed class IsNotNullOperator : RuleOperator
    {
        internal IsNotNullOperator()
            : base("IsNotNull") { }

        protected override Expression GenerateExpression(Expression left, Expression right, IQueryProvider provider)
        {
            return Expression.NotEqual(
                left,
                ExpressionHelper.CreateConstantExpression(null, GetBodyType(left)));
        }
    }
}
