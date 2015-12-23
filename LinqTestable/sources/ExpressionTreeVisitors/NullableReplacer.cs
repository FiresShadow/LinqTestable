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
            var method = sourceExpression.Method;
            var methodName = method.Name;

            Expression objectVisited = Visit(sourceExpression.Object);
            var argumentsVisited = VisitExpressionList(sourceExpression.Arguments).ToList();

            if (method.IsGenericMethod)
            {
                var changedMethodGenericArguments = method.GetGenericArguments().Select(CorrectType).ToArray();

                method = method.GetGenericMethodDefinition().MakeGenericMethod(changedMethodGenericArguments);

                /*if (method.GetParameters().Any() && method.GetParameters().First().ParameterType.IsIQueryable() && argumentsVisited.Any() && argumentsVisited.First().Type.IsIEnumerable())
                {
                    var castMethod = typeof(Queryable).GetMethods().Single(m => m.Name == "AsQueryable" && m.IsGenericMethod).MakeGenericMethod(argumentsVisited.First().Type.GetIEnumerableParameter());
                    argumentsVisited[0] = Expression.Call(null, castMethod, new []{argumentsVisited[0]});
                }*/
            }
            else if ((method.DeclaringType == typeof(Enumerable) || method.DeclaringType == typeof(Queryable)) && method.ReturnType.IsNotNullableNotBooleanStruct() && method.IsGenericMethod)
            {
                //в Enumerable помимо TResult Min<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) есть много методов наподобие double Min<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector). Аналогично для Max и Average
                var anotherMethod = method.DeclaringType.GetMethods().FirstOrDefault(m => m.Name == methodName && m.ReturnType == method.ReturnType.GetNullable() &&
                    m.GetParameters().Count() == method.GetParameters().Count() && m.GetGenericArguments().Count() == method.GetGenericArguments().Count());

                if (anotherMethod != null)
                {
                    method = anotherMethod.MakeGenericMethod(method.GetGenericArguments());
                }
            }
            else if (method.DeclaringType != null && method.DeclaringType.IsIEnumerable() && method.DeclaringType.GetIEnumerableParameter().IsNotNullableNotBooleanStruct())
            {
                //TODO вынести эти большие куски в отдельные процедуры. Можно сгруппировать их в одном классе
                //для расширений над всякими List<int>, которые переданы внутрь Expression как параметр. Например, Contains
                var iEnumerableParameter = method.DeclaringType.GetIEnumerableParameter();
                var castMethod = typeof (Enumerable).GetMethod("Cast").MakeGenericMethod(iEnumerableParameter.GetNullable());
                objectVisited = Expression.Call(null, castMethod, new[]{objectVisited});

                if (LinqTestableSettings.NullableSynonims.ContainsKey(method))
                    method = LinqTestableSettings.NullableSynonims[method];
                else 
                {
                    method = typeof(Enumerable).GetMethods().SingleOrDefault(x =>
                        {
                            if (x.Name != method.Name)
                                return false;

                            if (x.IsGenericMethod.Not())
                                return false;

                            int sourceCountParameters = method.GetParameters().Count();
                            return x.GetParameters().Count() == sourceCountParameters + 1;
                        });

                    if (method == null)
                        throw new Exception(string.Format("Failed correct method {0} of type {1}. Please specify corrected method in {2}", sourceExpression.Method.Name, sourceExpression.Method.DeclaringType != null ? sourceExpression.Method.DeclaringType.ToString() : "null", typeof(LinqTestableSettings).Name));

                    method = method.MakeGenericMethod(iEnumerableParameter.GetNullable());

                    objectVisited = Expression.Convert(objectVisited, typeof(IEnumerable<>).MakeGenericType(iEnumerableParameter.GetNullable()));
                    argumentsVisited = new[] {objectVisited}.Union(argumentsVisited).ToList();
                    objectVisited = null;
                }
            }

            if (method.ReturnType.IsNotNullableNotBooleanStruct() && method.DeclaringType != null)
            {
                var correctedReturnType = method.ReturnType.GetNullable();

                var correctedMethod = method.DeclaringType.GetMethods().FirstOrDefault(m =>
                    (m.Name == method.Name)
                    && (m.IsGenericMethod == method.IsGenericMethod) 
                    && (method.IsGenericMethod.Not() || m.GetGenericArguments().Count() == method.GetGenericArguments().Count())
                    && (m.ReturnType == correctedReturnType));

                if (correctedMethod != null)
                {
                    if (correctedMethod.IsGenericMethod)
                        correctedMethod = correctedMethod.MakeGenericMethod(method.GetGenericArguments());

                    method = correctedMethod;
                }
            }

            if (methodName == "Sum" && (method.DeclaringType == typeof(Enumerable) || method.DeclaringType == typeof(Queryable))) //TODO этому if не место в данном классе, у него другая ответственность
            {
                var sourceCollection = argumentsVisited.First();

                var anyMethod = method.DeclaringType.GetMethods().Single(m => m.Name == "Any" && m.GetParameters().Count() == 1)
                    .MakeGenericMethod(sourceCollection.Type.GetGenericArguments().Single());

                var callAny = Expression.Call(null, anyMethod, new[] { sourceCollection });
                return Expression.Condition(callAny, Expression.Call(objectVisited, method, argumentsVisited), Expression.Constant(null, method.ReturnType));
            }

            if (method != sourceExpression.Method || objectVisited != sourceExpression.Object || argumentsVisited.AsReadOnly() != sourceExpression.Arguments)
            {
                return Expression.Call(objectVisited, method, argumentsVisited);
            }

            return sourceExpression;
        }

        private NewExpression CreateNewCompressedObject()
        {
            return Expression.New(typeof(CompressedObject));
        }

        protected override Expression VisitNew(NewExpression sourceExpression)
        {
            if (_typesToReplace.Contains(sourceExpression.Type))
            {
                var newExpression = CreateNewCompressedObject();

                if (sourceExpression.Arguments.Count == 0)
                    return newExpression;

                IEnumerable<Expression> argumentsVisited = VisitExpressionList(sourceExpression.Arguments);
                var addMethod = typeof (CompressedObject).GetMethods().Single(x => x.Name == "Add");
                
                var elementInits = new List<ElementInit>();
                int numberArgument = 0;
                foreach (var argument in argumentsVisited)
                {
                    var arguments = new List<Expression>();
                    
                    string sourceArgumentName = sourceExpression.Members[numberArgument].Name;

                    arguments.Add(Expression.Constant(sourceArgumentName));
                    arguments.Add(Expression.Convert(argument, typeof(object)));
                    elementInits.Add(Expression.ElementInit(addMethod, arguments)); //TODO подумать о выделении в отдельный метод добавления элементов в упакованный объект
                    numberArgument++;
                }

                if (elementInits.Any().Not())
                    return newExpression;

                var listInit = Expression.ListInit(newExpression, elementInits);
                return listInit;
            }

            return base.VisitNew(sourceExpression);
        }

        protected override Expression VisitMemberInit(MemberInitExpression sourceExpression)
        {
            var newExpression = CreateNewCompressedObject();

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

            if (elementInits.Any().Not())
                return newExpression;

            var listInit = Expression.ListInit(newExpression, elementInits);
            return listInit;
        }

        /*protected override Expression VisitListInit(ListInitExpression sourceExpression)
        {
            return base.VisitListInit(sourceExpression);
        }*/
        
        protected override Expression VisitMemberAccess(MemberExpression sourceMemberExpression)
        {
            //Замечены случаи, когда константный список интов зачем то закулисами заворачивается в служебный класс. Поэтому not null поля нужно подменять на null также и на уровне доступа к данным, т.е. в этом методе

            Expression expressionVisited = Visit(sourceMemberExpression.Expression);

            Type memberType;
            Expression memberAccess;
            if (expressionVisited.Type == typeof (CompressedObject))
            {
                var methodInfo = typeof(CompressedObject).GetMethod("GetItem");
                memberAccess = Expression.Call(expressionVisited, methodInfo, new[] { (Expression)Expression.Constant(sourceMemberExpression.Member.Name) });
            }
            else
            {
                var memberInfo = expressionVisited.Type.GetMember(sourceMemberExpression.Member.Name).Single();
                memberAccess = Expression.MakeMemberAccess(expressionVisited, memberInfo);
                memberAccess = CorrectExpression(memberAccess);
            }

            memberType = CorrectType(sourceMemberExpression.Type);

            if (memberType.IsIEnumerable() && memberType.GetIEnumerableParameter().IsNotNullableNotBooleanStruct())
            {
                var iEnumerableParameter = memberType.GetIEnumerableParameter();
                memberType = typeof(IEnumerable<>).MakeGenericType(iEnumerableParameter.GetNullable());

                var castMethod = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(iEnumerableParameter.GetNullable());
                memberAccess = Expression.Call(null, castMethod, new[] { Expression.Convert(memberAccess, typeof(IEnumerable<>).MakeGenericType(iEnumerableParameter)) });
            }
            else if (memberType.IsIEnumerable() && _typesToReplace.Contains(memberType.GetIEnumerableParameter()))
            {
//                memberAccess = ChangeConstantExpression(sourceMemberExpression);
                memberType = memberType.GetGenericTypeDefinition().MakeGenericType(typeof(CompressedObject));
            }

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
            foreach (var parameter in lambda.Parameters)
            {
                var correctedType = CorrectType(parameter.Type);
                if (correctedType != parameter.Type)
                {
                    var newParameter = Expression.Parameter(correctedType, parameter.Name);
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

            var resultLambdaType = lambda.Type;

            var correctedGenericArguments = genericArguments.Select(CorrectType).ToArray();
            resultLambdaType = lambda.Type.GetGenericTypeDefinition().MakeGenericType(correctedGenericArguments);

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

            if (parameterExpression.Type.IsNotNullableNotBooleanStruct())
                return Expression.Convert(parameterExpression, parameterExpression.Type.GetNullable());

            if (parameterExpression.Type.IsIEnumerable() && _typesToReplace.Contains(parameterExpression.Type.GetIEnumerableParameter())) //IGrouping<,> IEnumerable<>
            {
                var genericArguments = parameterExpression.Type.GetGenericArguments();
                var correctedGenericArguments = genericArguments.Select(arg => _typesToReplace.Contains(arg) ? typeof (CompressedObject) : arg);
                return Expression.Parameter(parameterExpression.Type.GetGenericTypeDefinition().MakeGenericType(correctedGenericArguments.ToArray()));
            }

            return parameterExpression;
        }

        protected override Expression VisitConstant(ConstantExpression constantExpression)
        {
            return ChangeConstantExpression(constantExpression);
        }

        private Expression ChangeConstantExpression(Expression sourceObject /*тут добавить параметр связанный с зацикливанием и переименовать метод*/)
        {
            //TODO Не обработано зацикливание, напр когда у типа есть ссылка на коллекцию элементов того же типа
            //TODO Увеличить покрытие этого метода тестами

            Type elementType = sourceObject.Type;

            if (_typesToReplace.Contains(elementType))
            {
                var newExpression = CreateNewCompressedObject();

                var addMethod = typeof(CompressedObject).GetMethods().Single(x => x.Name == "Add");
                var elementInits = new List<ElementInit>();

                foreach (var memberInfo in elementType.GetMembers().Where(x => x.MemberType == MemberTypes.Field || x.MemberType == MemberTypes.Property))
                {
                    var memberAccess = Expression.MakeMemberAccess(sourceObject, memberInfo);

                    var arguments = new List<Expression>();
                    arguments.Add(Expression.Constant(memberInfo.Name));

                    var visitedMember = ChangeConstantExpression(memberAccess);

                    arguments.Add(Expression.Convert(visitedMember, typeof(object)));
                    elementInits.Add(Expression.ElementInit(addMethod, arguments));
                }

                if (elementInits.Any().Not())
                    return newExpression;

                return Expression.ListInit(newExpression, elementInits);
            }
            
            if (elementType.IsIEnumerable() && _typesToReplace.Contains(elementType.GetIEnumerableParameter()))
            {
                var parameterOfElement = Expression.Parameter(elementType.GetIEnumerableParameter());
                var body = ChangeConstantExpression(parameterOfElement);
                var selector = Expression.Lambda(body, parameterOfElement);

                var methodSelect = typeof(Enumerable).GetMethods().Single(m => m.Name == "Select" && m.GetParameters()[1].ParameterType.GetGenericArguments().Count() == 2)
                    .MakeGenericMethod(new []{elementType.GetIEnumerableParameter(), typeof(CompressedObject)});

                return Expression.Call(null, methodSelect, sourceObject, selector);
            }

            if (elementType.IsNotNullableNotBooleanStruct())
                return Expression.Convert(sourceObject, elementType.GetNullable());

            if (elementType.IsIEnumerable() && elementType.GetIEnumerableParameter().IsNotNullableNotBooleanStruct())
            {
                var castMethod = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(sourceObject.Type.GetIEnumerableParameter().GetNullable());
                return Expression.Call(null, castMethod, new[] { sourceObject });
            }
            
            return sourceObject;
        }

        private Type CorrectType(Type type)
        {
            if (type.IsNotNullableNotBooleanStruct())
                return type.GetNullable();
            
            if (_typesToReplace.Contains(type))
                return typeof (CompressedObject);

            if (type.IsIEnumerable() && type.IsGenericType && type.GetGenericTypeDefinition() != typeof(Nullable<>))
            {
                var correctedGenericArguments = type.GetGenericArguments().Select(CorrectType).ToArray();
                return type.GetGenericTypeDefinition().MakeGenericType(correctedGenericArguments);
            }

            return type;
        }

        private Expression CorrectExpression(Expression sourceExpression)
        {
            //TODO Сделать универсальный метод кастования не-налловых типов и контейнеров к налловым, и повсюду его использовать.

            if (sourceExpression.Type.IsIEnumerable() && sourceExpression.Type.GetIEnumerableParameter().IsNotNullableNotBooleanStruct())
            {
                var destinationElementType = sourceExpression.Type.GetIEnumerableParameter().GetNullable();
                var castMethod = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(destinationElementType);
                var castExpression = Expression.Call(null, castMethod, new[] { sourceExpression });
                
                if (sourceExpression.Type.IsGenericType )
                {
                    if (sourceExpression.Type.GetGenericTypeDefinition() == typeof (List<>))
                    {
                        var toListMethod = typeof (Enumerable).GetMethod("ToList").MakeGenericMethod(destinationElementType);
                        return Expression.Call(null, toListMethod, castExpression);
                    }

                    if (sourceExpression.Type.IsArray)
                    {
                        var toArrayMethod = typeof(Enumerable).GetMethod("ToArray").MakeGenericMethod(destinationElementType);
                        return Expression.Call(null, toArrayMethod, castExpression);
                    }

                    if (sourceExpression.Type.GetGenericTypeDefinition() == typeof(IQueryable<>))
                    {
                        var toQueryableMethod = typeof(Queryable).GetMethod("AsQueryable").MakeGenericMethod(destinationElementType);
                        return Expression.Call(null, toQueryableMethod, castExpression);
                    }
                }
            }

            return sourceExpression;
        }
    }
}