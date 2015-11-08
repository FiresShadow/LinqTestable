using System.Linq.Expressions;
using LinqTestable.Sources.ExpressionTreeChangers;

namespace LinqTestable.Sources.ExpressionTreeVisitors
{
    /// <summary>
    /// Ищет методы Select или SelectMany
    /// </summary>
    public class SelectOrSelectManySearcher : DeepExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression sourceExpression)
        {
            var methodName = sourceExpression.Method.Name;

            if (methodName == "Select" || methodName == "SelectMany")
                _finded = true;

            return base.VisitMethodCall(sourceExpression);
        }

        public bool IsFinded(Expression expression)
        {
            _finded = false;
            base.Visit(expression);
            return _finded;
        }

        private bool _finded;
    }
}