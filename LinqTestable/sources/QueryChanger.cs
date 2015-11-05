using System;
using System.Linq.Expressions;
using System.Reflection;
using LinqTestable.Sources.ExpressionTreeChangers;
using LinqTestable.Sources.Infrastructure;

namespace LinqTestable.Sources
{
    interface IQueryChanger
    {
        Expression Change(Expression expression);
    }

    class QueryChanger : ExpressionVisitor, IQueryChanger
    {
        public Expression Change(Expression sourceExpression)
        {
            var expression = new LeftJoinChanger().Visit(sourceExpression);
            expression = new NullComparisonChanger().Visit(expression);

            return expression;
        }
    }

    
}