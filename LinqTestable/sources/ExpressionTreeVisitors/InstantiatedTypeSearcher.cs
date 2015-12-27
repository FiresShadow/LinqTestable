using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqTestable.Sources.ExpressionTreeChangers;
using LinqTestable.Sources.Infrastructure;

namespace LinqTestable.Sources.ExpressionTreeVisitors
{
    /// <summary>
    /// Ищет классы, которые создаются через new. В них могут передаваться данные, и возможен случай, что в не Nullable поле потребуется поместить null.
    /// Поэтому все объекты таких классов будем подменять на упакованные (сериализованные) объекты CompressedObject и делать все поля Nullable
    /// </summary>
    public class InstantiatedTypeSearcher : DeepExpressionVisitor
    {
        protected override Expression VisitNew(NewExpression newExpression)
        {
            _types.Add(newExpression.Type);

            return base.VisitNew(newExpression);
        }

        public List<Type> Find(Expression expression)
        {
            _types = new List<Type>();
            Visit(expression);
            return _types.Distinct().Except(new[]{typeof(CompressedObject)}).ToList();
        }

        private List<Type> _types;
    }
}