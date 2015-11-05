using System.Linq.Expressions;

namespace LinqTestable.Sources.ExpressionTreeChangers
{
    public class NullComparisonChanger : ExpressionVisitor
    {
        public override Expression Visit(Expression expression)
        {
            return expression;
        }
    }
}