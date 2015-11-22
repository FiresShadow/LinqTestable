using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqTestable.Sources.ExpressionTreeChangers;
using LinqTestable.Sources.Infrastructure;

namespace LinqTestable.Sources.ExpressionTreeVisitors
{
    /// <summary>
    /// Меняет все структуры кроме bool на Nullable
    /// </summary>
    public class NullableReplacer : DeepExpressionVisitor
    {
        private List<Type> _typesToReplace;

        public NullableReplacer(List<Type> typesToReplace)
        {
            _typesToReplace = typesToReplace;
        }

        protected override Expression VisitMethodCall(MethodCallExpression sourceExpression)
        {
            //TODO кастомный заменяемый тип

            var method = sourceExpression.Method;
            var methodName = method.Name;

            Expression objectVisited = Visit(sourceExpression.Object);
            IEnumerable<Expression> argumentsVisited = VisitExpressionList(sourceExpression.Arguments);

            if (method.IsGenericMethod && method.GetGenericArguments().Any(x => x.IsNotNullableNotBooleanStruct() || _typesToReplace.Contains(x)))
            {
                var changedMethodGenericArguments = method.GetGenericArguments().Select(type => type.IsNotNullableNotBooleanStruct() ? type.GetNullable() : _typesToReplace.Contains(type) ? typeof(CompressedObject) : type).ToArray();
                changedMethodGenericArguments = changedMethodGenericArguments.Select(type => _typesToReplace.Contains(type) ? typeof(CompressedObject) : type).ToArray();
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

            /*if (methodName == "Select" || methodName == "SelectMany" || methodName == "Union")
            {
                //для Селекта особая обработка, потому что в случае если ORM не может привести null к int в итоговом селекте, она бросает исключение; значит тест тоже должен падать
                var finalGetDataSearcher = new SelectOrSelectManySearcher();
                var selectFinded = argumentsVisited.Any(finalGetDataSearcher.IsFinded);

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
            }*/


            if (method != sourceExpression.Method || objectVisited != sourceExpression.Object || argumentsVisited != sourceExpression.Arguments)
            {
                return Expression.Call(objectVisited, method, argumentsVisited);
            }

            return sourceExpression;
        }

        private NewExpression CreateNewCompressedObject(Type type)
        {
            var constructorCompressedObject = typeof(CompressedObject).GetConstructor(new[] { typeof(Type) });
//            var memberInfo = typeof(Type).GetMembers().Where(x => x.DeclaringType == typeof(Type)).First();
            var newExpression = Expression.New(constructorCompressedObject, Expression.Constant(type));
            return newExpression;
        }

        protected override Expression VisitNew(NewExpression sourceExpression)
        {
            //Создаёт анонимный тип с данными передаваемыми через конструкор

            if (_typesToReplace.Contains(sourceExpression.Type))
            {
                var newExpression = CreateNewCompressedObject(sourceExpression.Type);
                IEnumerable<Expression> argumentsVisited = VisitExpressionList(sourceExpression.Arguments);
                ReadOnlyCollection<Expression> argumentsVisited2;
                var addMethod = typeof (CompressedObject).GetMethods().Single(x => x.Name == "Add");
                
                var elementInits = new List<ElementInit>();
                int numberArgument = 0;
                foreach (var argument in argumentsVisited)
                {
                    var arguments = new List<Expression>();
                    string sourceArgumentName=null;

                    var sourceArgument = sourceExpression.Arguments[numberArgument];
                    if (sourceArgument is ParameterExpression)
                        sourceArgumentName = ((ParameterExpression)sourceArgument).Name;
                    if (sourceArgument is MemberExpression)
                        sourceArgumentName = ((MemberExpression) sourceArgument).Member.Name;
                    if (sourceArgumentName == null)
                        throw new Exception("sourceArgumentName");

                    arguments.Add(Expression.Constant(sourceArgumentName));
                    arguments.Add(Expression.Convert(argument, typeof(object)));
                    elementInits.Add(Expression.ElementInit(addMethod, arguments));
                    numberArgument++;
                }

                var listInit = Expression.ListInit(newExpression, elementInits);
                return listInit;
            }

            return base.VisitNew(sourceExpression);
        }

        protected override Expression VisitMemberInit(MemberInitExpression sourceExpression)
        {
            var newExpression = CreateNewCompressedObject(sourceExpression.Type);

            var addMethod = typeof(CompressedObject).GetMethods().Single(x => x.Name == "Add");
            var elementInits = new List<ElementInit>();
            foreach (var argument in sourceExpression.Bindings)
            {
                var arguments = new List<Expression>();
                arguments.Add(Expression.Constant(argument.Member.Name));
                var argumentExpressionVisited = Expression.Convert(Visit(((MemberAssignment)argument).Expression), typeof(object));
                arguments.Add(argumentExpressionVisited);
                elementInits.Add(Expression.ElementInit(addMethod, arguments));
            }

            var listInit = Expression.ListInit(newExpression, elementInits);
            return listInit;
        }

        protected override Expression VisitListInit(ListInitExpression sourceExpression)
        {
            return base.VisitListInit(sourceExpression);
        }

        protected override Expression VisitMemberAccess(MemberExpression sourceMemberExpression)
        {
            Expression expressionVisited = Visit(sourceMemberExpression.Expression);

            Type memberType;
            Expression memberAccess;
            if (expressionVisited.Type == typeof (CompressedObject))
            {
                var methodInfo = typeof(CompressedObject).GetMethod("get_Item");
                memberAccess = Expression.Call(expressionVisited, methodInfo, new[] { (Expression)Expression.Constant(sourceMemberExpression.Member.Name) });
            }
            else
            {
                memberAccess = Expression.MakeMemberAccess(expressionVisited, sourceMemberExpression.Member);
            }

            memberType = _typesToReplace.Contains(sourceMemberExpression.Type) ? typeof(CompressedObject) : sourceMemberExpression.Type.IsNotNullableNotBooleanStruct() ? sourceMemberExpression.Type.GetNullable() : sourceMemberExpression.Type;

            var variable = Expression.Variable(memberType);


            var ifThenElse = Expression.IfThenElse(Expression.Equal(expressionVisited, Expression.Constant(null)),
                            Expression.Assign(variable, Expression.Convert(Expression.Constant(null), memberType)),
                            Expression.Assign(variable, Expression.Convert(memberAccess, memberType)/*TODO not make?)*/));

            var block = Expression.Block(new[] { variable }, ifThenElse, variable);
            return block;
        }

        Dictionary<ParameterExpression, ParameterExpression> _parametersToReplace = new Dictionary<ParameterExpression, ParameterExpression>();

        protected override Expression VisitLambda(LambdaExpression lambda)
        {
            var correctedParameters = new List<ParameterExpression>();
            int numberParameter = 0;
            foreach (var parameter in lambda.Parameters)
            {
                if (_typesToReplace.Contains(parameter.Type))
                {
                    var newParameter = Expression.Parameter(typeof (CompressedObject), parameter.Name);
                    _parametersToReplace.Add(parameter, newParameter);
                    correctedParameters.Add(newParameter);
                }
                else
                {
                    correctedParameters.Add(parameter);
                }
            }

            Expression bodyVisited = Visit(lambda.Body);

            foreach (var parameter in lambda.Parameters)
            {
                if (_parametersToReplace.ContainsKey(parameter))
                    _parametersToReplace.Remove(parameter);
            }
            

            var genericArguments = lambda.Type.GetGenericArguments();
            var returnType = genericArguments.Last();

            var resultLambdaType = lambda.Type;

            var correctedGenericArguments = new List<Type>();
            foreach (var genericArgument in genericArguments)
            {
                correctedGenericArguments.Add(_typesToReplace.Contains(genericArgument)
                                                  ? typeof (CompressedObject)
                                                  : genericArgument);
            }

            if (returnType.IsNotNullableNotBooleanStruct())
            {
                correctedGenericArguments = correctedGenericArguments.Take(correctedGenericArguments.Count() - 1).Union(new[] { returnType.GetNullable() }).ToList();
            }

            resultLambdaType = lambda.Type.GetGenericTypeDefinition().MakeGenericType(correctedGenericArguments.ToArray());

            if (resultLambdaType != lambda.Type || bodyVisited != lambda.Body)
            {
                return Expression.Lambda(resultLambdaType, bodyVisited, correctedParameters);
            }
            
            return lambda;
        }

        protected override Expression VisitParameter(ParameterExpression parameterExpression)
        {
            if (_parametersToReplace.ContainsKey(parameterExpression))
                return _parametersToReplace[parameterExpression];

            if (_typesToReplace.Contains(parameterExpression.Type))
                return Expression.Parameter(typeof (CompressedObject));

            return parameterExpression;
        }
    }
}