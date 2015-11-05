using System;
using System.Linq.Expressions;
using System.Reflection;
using LinqTestable.Sources.Infrastructure;

namespace LinqTestable.Sources.ExpressionTreeChangers
{
    class LeftJoinChanger : ExpressionVisitor
    {
        /// <summary>
        /// Изменяет дерево выражений, чтобы не выбрасывалось NullReferenceException при тестировании двух left join-ов и некорректном on во втором джойне
        /// </summary>
        /// <remarks>
        /// Подменяет (entity) => entity.Id на (entity) => entity == null ? (int?)null : entity.Id в первом селекторе Id метода GroupJoin
        /// При необходимости подменяет шаблон GroupJoin[Source1, Source2, int, Result] на GroupJoin[Source1, Source2, int?, Result]
        /// </remarks>
        protected override Expression VisitMethodCall(MethodCallExpression sourceExpression)
        {
            var methodName = sourceExpression.Method.Name;
            if (methodName != "GroupJoin")
                return sourceExpression;

            var expressionSourceFirstIdSelector = (LambdaExpression)((UnaryExpression)sourceExpression.Arguments[2]).Operand;
            var sourceFirstIdSelector = expressionSourceFirstIdSelector.Body;
            var sourceFirstIdSelectorParameter = expressionSourceFirstIdSelector.Parameters[0];

            var expressionSourceSecondIdSelector = (LambdaExpression)((UnaryExpression)sourceExpression.Arguments[3]).Operand;
            var sourceSecondIdSelector = expressionSourceSecondIdSelector.Body;
            var sourceSecondIdSelectorParameter = expressionSourceSecondIdSelector.Parameters[0];

            Type typeOfField = sourceFirstIdSelector.Type;
            bool isKeyNullable = typeOfField.IsGenericType && typeOfField.GetGenericTypeDefinition() == typeof(Nullable<>);
            Type nullableTypeOfField = isKeyNullable
                ? typeOfField
                : typeof(Nullable<>).MakeGenericType(new[] { typeOfField });


            //создание firstIdSelector = (entity) => entity == null ? (int?)null : entity.Id

            //во втором джойне в качестве селектора айдишки первой сущности прилетает anonimousType.entity.ID
            //поэтому в случае второго джойна должно выполняться ((expressionSourceFirstIdSelector.Body as MemberExpression).Expression as MemberExpression).Expression.Type должен быть анонимным типом
            var entitySelector = ((MemberExpression)(expressionSourceFirstIdSelector.Body)).Expression as MemberExpression;

            if (entitySelector == null)
                return sourceExpression;

            bool isAnonymousType = entitySelector.Expression.Type.IsAnonymous();

            if (!isAnonymousType)
                return sourceExpression;

            var expressionVariableId = Expression.Variable(nullableTypeOfField);
            Expression expressionIdSelector = Expression.IfThenElse(
                Expression.Equal(entitySelector, Expression.Constant(null)),
                Expression.Assign(expressionVariableId, Expression.Convert(Expression.Constant(null), nullableTypeOfField)),
                Expression.Assign(expressionVariableId, isKeyNullable ? sourceFirstIdSelector : Expression.Convert(sourceFirstIdSelector, nullableTypeOfField)));

            var expressionReturnVariableId = Expression.Block(new[] { expressionVariableId }, expressionIdSelector, expressionVariableId);

            var firstIdSelector = Expression.Lambda/*<Func<CAR, int?>>*/(expressionReturnVariableId, new[] { sourceFirstIdSelectorParameter });

            //если ключ не Nullable<>, то создадим secondIdSelector = (entity) => (int?)entity.Id
            Expression secondIdSelector;
            if (isKeyNullable)
            {
                secondIdSelector = sourceExpression.Arguments[3];
            }
            else
            {
                expressionVariableId = Expression.Variable(nullableTypeOfField);
                expressionIdSelector = Expression.Assign(expressionVariableId, Expression.Convert(sourceSecondIdSelector, nullableTypeOfField));
                expressionReturnVariableId = Expression.Block(new[] { expressionVariableId }, expressionIdSelector, expressionVariableId);
                secondIdSelector = Expression.Lambda(expressionReturnVariableId, new[] { sourceSecondIdSelectorParameter });
            }

            //если ключ не Nullable<>, то поменяем GroupJoin<Source1, Source2, int, Result> на GroupJoin<Source1, Source2, Nullable<int>, Result>
            MethodInfo finalGroupJoinMethodInfo;
            if (isKeyNullable)
            {
                finalGroupJoinMethodInfo = sourceExpression.Method;
            }
            else
            {
                Type[] originalGroupJoinGenericArguments = sourceExpression.Method.GetGenericArguments();
                finalGroupJoinMethodInfo = sourceExpression.Method.GetGenericMethodDefinition().MakeGenericMethod(new[] { originalGroupJoinGenericArguments[0], originalGroupJoinGenericArguments[1], nullableTypeOfField, originalGroupJoinGenericArguments[3] });
            }

            return Expression.Call(null, finalGroupJoinMethodInfo, sourceExpression.Arguments[0], sourceExpression.Arguments[1], firstIdSelector, secondIdSelector, sourceExpression.Arguments[4]);
        }
    }
}