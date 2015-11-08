using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqTestable.Sources.ExpressionTreeChangers;
using LinqTestable.Sources.Infrastructure;

namespace LinqTestable.Sources.ExpressionTreeVisitors
{
    /// <summary>
    /// Меняет все структуры кроме bool на Nullable, кроме последнего Select или SelectMany
    /// </summary>
    public class NullableReplacer : DeepExpressionVisitor
    {
        //поменяем все MemberAccess-ы на Nullable<>
        //возможно, следует менять на все, а например только тех типов, которые сооветвуют типу в бд, но пока что не будем завязываться на это.

        protected override Expression VisitMethodCall(MethodCallExpression sourceExpression)
        {
            var method = sourceExpression.Method;
            var methodName = method.Name;

            Expression objectVisited = Visit(sourceExpression.Object);
            IEnumerable<Expression> argumentsVisited = VisitExpressionList(sourceExpression.Arguments);

            if (method.IsGenericMethod && method.GetGenericArguments().Any(x => x.IsNotNullableNotBooleanStruct()))
            {
                var changedMethodGenericArguments = method.GetGenericArguments().Select(type => type.IsNotNullableNotBooleanStruct() ? type.GetNullable() : type).ToArray();
                method = method.GetGenericMethodDefinition().MakeGenericMethod(changedMethodGenericArguments);
            }
            else if (method.DeclaringType == typeof(Enumerable) && method.ReturnType.IsNotNullableNotBooleanStruct() && method.IsGenericMethod)
            {
                //в Enumerable помимо TResult Min<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) есть много методов наподобие double Min<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector). Аналогично для Max и Average
                var anotherMethod = typeof(Enumerable).GetMethods().FirstOrDefault(m => m.Name == methodName && m.ReturnType == method.ReturnType.GetNullable() &&
                    m.GetParameters().Count() == method.GetParameters().Count() && m.GetGenericArguments().Count() == method.GetGenericArguments().Count());

                if (anotherMethod != null)
                {
                    method = anotherMethod.MakeGenericMethod(method.GetGenericArguments());
                }
            }

            if (methodName == "Select" || methodName == "SelectMany")
            {
                //для Селекта особая обработка, потому что в случае если ORM не может привести null к int в итоговом селекте, она бросает исключение; значит тест тоже должен падать
                var selectOrSelectManySearcher = new SelectOrSelectManySearcher();
                var selectFinded = argumentsVisited.Any(selectOrSelectManySearcher.IsFinded);

                if ( ! selectFinded)
                {
                    if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(IQueryable<>) && method.ReturnType.GetGenericArguments().Single().IsNotNullableNotBooleanStruct())
                    {

                        var methodSelect = typeof(Queryable).GetMethods().Single(m => m.Name == "Select" && m.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments().Count() == 2);
                        var type = method.ReturnType.GetGenericArguments().Single();
                        methodSelect = methodSelect.MakeGenericMethod(type, type.GetNullable());
                        var parameter = Expression.Parameter(type);
                        var castExpression = Expression.Lambda(Expression.Convert(parameter, type.GetNullable()), parameter);

                        var call = Expression.Call(objectVisited, method, argumentsVisited);

                        return Expression.Call(null, methodSelect, call, castExpression);
                    }

                    return sourceExpression;
                }
                //TODO
            }


            if (method != sourceExpression.Method || objectVisited != sourceExpression.Object || argumentsVisited != sourceExpression.Arguments)
            {
                return Expression.Call(objectVisited, method, argumentsVisited);
            }

            return sourceExpression;
        }

        protected override Expression VisitMemberAccess(MemberExpression sourceMemberExpression)
        {
            Expression expressionVisited = Visit(sourceMemberExpression.Expression);

            var memberType = sourceMemberExpression.Type.IsNotNullableNotBooleanStruct() ? sourceMemberExpression.Type.GetNullable() : sourceMemberExpression.Type;
            var variable = Expression.Variable(memberType);

            var ifThenElse = Expression.IfThenElse(Expression.Equal(sourceMemberExpression.Expression, Expression.Constant(null)),
                            Expression.Assign(variable, Expression.Convert(Expression.Constant(null), memberType)),
                            Expression.Assign(variable, Expression.Convert(Expression.MakeMemberAccess(expressionVisited, sourceMemberExpression.Member), memberType)/*TODO not make?)*/));

            var block = Expression.Block(new[] { variable }, ifThenElse, variable);
            return block;
        }

        protected override Expression VisitLambda(LambdaExpression lambda)
        {
            Expression bodyVisited = Visit(lambda.Body);

            var genericArguments = lambda.Type.GetGenericArguments();
            var returnType = genericArguments.Last();

            var resultLambdaType = lambda.Type;
            if (returnType.IsNotNullableNotBooleanStruct())
            {
                var correctedGenericArguments = genericArguments.Take(genericArguments.Count() - 1).Union(new[] { returnType.GetNullable() }).ToArray();
                resultLambdaType = lambda.Type.GetGenericTypeDefinition().MakeGenericType(correctedGenericArguments);
            }

            if (resultLambdaType != lambda.Type || bodyVisited != lambda.Body)
            {
                return Expression.Lambda(resultLambdaType, bodyVisited, lambda.Parameters);
            }
            
            return lambda;
        }
    }
}