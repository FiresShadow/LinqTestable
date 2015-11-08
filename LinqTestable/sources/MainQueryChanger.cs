using System.Linq.Expressions;
using LinqTestable.Sources.ExpressionTreeVisitors;

namespace LinqTestable.Sources
{
    interface IQueryChanger
    {
        Expression Change(Expression expression);
    }

    class MainQueryChanger : ExpressionVisitor, IQueryChanger
    {
        public Expression Change(Expression sourceExpression)
        {
            var expression = new NullableReplacer().Visit(sourceExpression);
            expression = new NullComparisonChanger().Visit(expression);

            return expression;
        }
    }
}