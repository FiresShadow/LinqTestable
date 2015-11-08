using System.Linq.Expressions;
using LinqTestable.Sources.Infrastructure;

namespace LinqTestable.Sources.ExpressionTreeVisitors
{
    /// <summary>
    /// Подменяет сравнения так, чтобы null == null было false, если только в одной из частей явно не указано значение null (для вызова is null) 
    /// </summary>
    public class NullComparisonChanger : ExpressionVisitor
    {
        public override Expression Visit(Expression expression)
        {
            if (expression == null)
                return null;

            switch (expression.NodeType)
            {
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                    return VisitCompare((BinaryExpression)expression);
                default:
                    return base.Visit(expression);
            }
        }

        private Expression VisitCompare(BinaryExpression sourceExpression)
        {
            Expression leftVisited = Visit(sourceExpression.Left);
            Expression rightVisited = Visit(sourceExpression.Right);

            bool leftIsAlwaysNull = leftVisited.NodeType == ExpressionType.Constant && ((ConstantExpression)leftVisited).Value == null;
            bool rightIsAlwaysNull = rightVisited.NodeType == ExpressionType.Constant && ((ConstantExpression)rightVisited).Value == null;

            bool isNeedSimpleCompare = leftIsAlwaysNull && rightIsAlwaysNull.Not() || rightIsAlwaysNull && leftIsAlwaysNull.Not();

            if (isNeedSimpleCompare)
            {
                if (leftVisited != sourceExpression.Left || rightVisited != sourceExpression.Right)
                {
                    return Expression.MakeBinary(sourceExpression.NodeType, leftVisited, rightVisited, sourceExpression.IsLiftedToNull, sourceExpression.Method);
                }
                return sourceExpression;
            }

            var boolVariable = Expression.Variable(typeof (bool));
            
            var ifThenElse = Expression.IfThenElse(
                Expression.Or(
                    Expression.Equal(leftVisited, Expression.Constant(null)),
                    Expression.Equal(rightVisited, Expression.Constant(null))),

                Expression.Assign(boolVariable, Expression.Constant(false)),

                Expression.Assign(boolVariable, sourceExpression)
                );

            var block = Expression.Block(new[] { boolVariable }, ifThenElse, boolVariable);

            return block;
        }
    }
}