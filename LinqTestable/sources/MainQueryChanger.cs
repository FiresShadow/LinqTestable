using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqTestable.Sources.ExpressionTreeVisitors;
using LinqTestable.Sources.Infrastructure;

namespace LinqTestable.Sources
{
    interface IQueryChanger
    {
        Expression Change(Expression expression);
    }

    class MainQueryChanger : ExpressionVisitor, IQueryChanger
    {
        public Expression Change(Expression sourceExpression)
        {
            Type returnType = ((MethodCallExpression)sourceExpression).Method.ReturnType;
            List<Type> typesToReplace = new InstantiatedTypeSearcher().Find(sourceExpression);
//            List<Type> typesToReplace = instantiatedTypes.Except(new[] {returnType}).ToList();

            var expression = new NullableReplacer(typesToReplace).Visit(sourceExpression);
            expression = AddFinalSelect(expression, returnType, typesToReplace);
            expression = new NullComparisonChanger().Visit(expression);

            return expression;
        }

        private Expression AddFinalSelect(Expression sourceExpression, Type returnType, List<Type> typesToReplace)
        {
            Contract.Assert(returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(IQueryable<>));

            var elementType = returnType.GetGenericArguments().Single();
            var methodSelect = typeof(Queryable).GetMethods().Single(m => m.Name == "Select" && m.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments().Count() == 2);

            if (typesToReplace.Contains(elementType))
            {
                methodSelect = methodSelect.MakeGenericMethod(new[] {typeof (CompressedObject), elementType});

                var constructor = elementType.GetConstructors().FirstOrDefault(x => x.GetParameters().Any().Not()) ??
                                  elementType.GetConstructors().First();

                var compressedObjectParameter = Expression.Parameter(typeof(CompressedObject));

                var getValueByNumber = typeof(CompressedObject).GetMethod("GetValueByNumber");

                var parameters = new List<Expression>();
                //TODO написать в документации чтобы не делали штук наподобие new MyClass(a,b){c,d}
                int numberParameter = 0;
                foreach (var parameter in constructor.GetParameters())
                {
                    parameters.Add(Expression.Convert(
                        Expression.Call(compressedObjectParameter, getValueByNumber, new[]{(Expression)Expression.Constant(numberParameter)}),
                        parameter.ParameterType));

                    numberParameter++;
                }

//                var parameters = constructor.GetParameters().Select(parameterInfo => 
//                    Expression.Convert
//                    (
//                        Expression.Constant(parameterInfo.ParameterType.IsValueType ? Activator.CreateInstance(parameterInfo.ParameterType) : null),
//                        parameterInfo.ParameterType
//                    ));

                var newExpression = Expression.New(constructor, parameters);
                Expression newWithParametersExpression = newExpression;

                //TODO compressedObject into compressedObject
                if (constructor.GetParameters().Any().Not())
                {
                    var bindings = new List<MemberBinding>();
                    var members = elementType.GetMembers().Where(x => x.MemberType == MemberTypes.Property || x.MemberType == MemberTypes.Field);

                    var getMethod = typeof(CompressedObject).GetMethod("get_Item");
                    foreach (var member in members)
                    {
                        var getExpression = Expression.Call(compressedObjectParameter, getMethod, new[] { (Expression)Expression.Constant(member.Name) });
                        bindings.Add(Expression.Bind(member, getExpression));
                    }

                    newWithParametersExpression = Expression.MemberInit(newExpression, bindings);
                }


                var createNewLambda = Expression.Lambda(newWithParametersExpression, new[] {compressedObjectParameter});
                return Expression.Call(null, methodSelect, sourceExpression, createNewLambda);
            }

            if (elementType.IsNotNullableNotBooleanStruct())
            {
                methodSelect = methodSelect.MakeGenericMethod(elementType.GetNullable(), elementType);
                var parameter = Expression.Parameter(elementType.GetNullable());
                var castExpression = Expression.Lambda(Expression.Convert(parameter, elementType), parameter);

                return Expression.Call(null, methodSelect, sourceExpression, castExpression);
            }

            return sourceExpression;
        }
    }
}