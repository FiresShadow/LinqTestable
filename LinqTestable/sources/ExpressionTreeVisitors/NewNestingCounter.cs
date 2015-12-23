using System;
using System.Linq.Expressions;
using LinqTestable.Sources.ExpressionTreeChangers;
using LinqTestable.Sources.Infrastructure;

namespace LinqTestable.Sources.ExpressionTreeVisitors
{
    /// <summary>
    /// Ищет глубину вложенности new внутри new. Может вернуть число чуть большее чем глубина :)
    /// </summary>
    public class NewNestingCounter : DeepExpressionVisitor
    {
        private bool _isInsideNew;
        private int _currentNesting;
        private int _maxNesting;

        public int FindCount(Expression sourceExpression)
        {
            Visit(sourceExpression);
            return _maxNesting;
        }

        protected override Expression VisitNew(NewExpression original)
        {
            return Visit(() => base.VisitNew(original));
        }

        protected override Expression VisitMemberInit(MemberInitExpression sourceExpression)
        {
            _currentNesting--;
            return Visit(() => base.VisitMemberInit(sourceExpression));
        }

        protected override Expression VisitListInit(ListInitExpression original)
        {
            _currentNesting--;
            return Visit(() => base.VisitListInit(original));
        }

        private Expression Visit(Func<Expression> visitExpression)
        {
            bool isNestStartingHere = _isInsideNew.Not();
            _isInsideNew = true;
            _currentNesting++;

            var visited = visitExpression();

            if (isNestStartingHere)
            {
                _maxNesting = Math.Max(_maxNesting, _currentNesting);
                _currentNesting = 0;
                _isInsideNew = false;
            }

            return visited;
        }
    }
}