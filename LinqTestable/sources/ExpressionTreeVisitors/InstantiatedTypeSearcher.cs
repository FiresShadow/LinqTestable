using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqTestable.Sources.ExpressionTreeChangers;
using LinqTestable.Sources.Infrastructure;

namespace LinqTestable.Sources.ExpressionTreeVisitors
{
    public class InstantiatedTypeSearcher : DeepExpressionVisitor
    {
        protected override Expression VisitNew(NewExpression newExpression)
        {
            _types.Add(newExpression.Type);

            return base.VisitNew(newExpression);
        }

//        protected override 

        public List<Type> Find(Expression expression)
        {
            _types = new List<Type>();
            Visit(expression);
            return _types.Distinct().ToList();
        }

        private List<Type> _types;
    }
}