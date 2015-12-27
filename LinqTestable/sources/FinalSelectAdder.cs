using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqTestable.Sources.Infrastructure;

namespace LinqTestable.Sources
{
    /// <summary>
    /// Гарантирует возврат именно того типа, который возвращался изначально, до изменения дерева выражений
    /// </summary>
    class FinalSelectAdder
    {
        /// <summary>
        /// Получить Expression, создающее новый объект с дефолтным значением
        /// </summary>
        private Expression GetDefaultValue(Type elementType)
        {
            if (elementType.IsStruct().Not())
                return Expression.Convert(Expression.Constant(null), elementType);

            var constructor = elementType.GetConstructors().FirstOrDefault(x => x.GetParameters().Any().Not()) ??
                              elementType.GetConstructors().FirstOrDefault();

            if (constructor == null)
                return Expression.New(elementType);


            if (constructor.GetParameters().Any().Not())
                return Expression.New(elementType);

            var constructorParameters = constructor.GetParameters().Select(parameter => GetDefaultValue(parameter.ParameterType));

            return Expression.New(constructor, constructorParameters);
        }

        /// <summary>
        /// Возвращает Expression, конвертирующее CompressedObject в указанный тип
        /// </summary>
        private Expression DecompressObject(Expression sourceObject, Type elementType, int newNestDeep)
        {
            if (newNestDeep == 0)
                return GetDefaultValue(elementType);

            if (_typesToReplace.Contains(elementType))
                sourceObject = Expression.Convert(sourceObject, typeof(CompressedObject));

            var constructor = elementType.GetConstructors().FirstOrDefault(x => x.GetParameters().Any().Not()) ??
                              elementType.GetConstructors().First();

            var getValueByNumber = typeof(CompressedObject).GetMethod("GetValueByNumber"); //TODO name in string

            var constructorParametersExpressions = new List<Expression>();
            int numberParameter = 0;

            foreach (var parameter in constructor.GetParameters())
            {
                Expression getValueExpression = Expression.Call(sourceObject, getValueByNumber, new[] { (Expression)Expression.Constant(numberParameter) });

                if (_typesToReplace.Contains(parameter.ParameterType))
                {
                    constructorParametersExpressions.Add(DecompressObject(getValueExpression, parameter.ParameterType, newNestDeep - 1));
                }
                else
                {
                    constructorParametersExpressions.Add(Expression.Convert(
                        getValueExpression,
                        parameter.ParameterType));
                }

                numberParameter++;
            }

            var newExpression = Expression.New(constructor, constructorParametersExpressions); //TODO Refactor inline
            Expression newWithParametersExpression = newExpression;

            //TODO new MyClass(a,b){c,d}
            if (constructor.GetParameters().Any().Not())
            {
                var bindings = new List<MemberBinding>();
                var members = elementType.GetMembers().Where(x => x.MemberType == MemberTypes.Property || x.MemberType == MemberTypes.Field);

                var isContainsKeyMethod = typeof(CompressedObject).GetMethod("ContainsKey");
                var getMethod = typeof(CompressedObject).GetMethod("GetItem");
                foreach (var member in members)
                {
                    Expression getExpression = Expression.Call(sourceObject, getMethod, new[] { (Expression)Expression.Constant(member.Name) });

                    Type typeOfField;

                    switch (member.MemberType)
                    {
                        case MemberTypes.Property: { typeOfField = ((PropertyInfo)member).PropertyType; break; }
                        case MemberTypes.Field: { typeOfField = ((FieldInfo)member).FieldType; break; }
                        default: { throw new Exception("unknown member type in " + sourceObject); } //TODO custom type of exception?
                    }

                    if (_typesToReplace.Contains(typeOfField))
                    {
                        getExpression = DecompressObject(getExpression, typeOfField, newNestDeep - 1);
                    }

                    getExpression = Expression.Convert(getExpression, typeOfField);

                    var isContainsKeyExpression = Expression.Call(sourceObject, isContainsKeyMethod, new[] { (Expression)Expression.Constant(member.Name) });
                    getExpression = Expression.Condition(isContainsKeyExpression, getExpression, GetDefaultValue(typeOfField));

                    bindings.Add(Expression.Bind(member, getExpression));
                }

                newWithParametersExpression = Expression.MemberInit(newExpression, bindings);
            }

            return newWithParametersExpression;
        }

        private List<Type> _typesToReplace;

        public Expression AddFinalSelect(Expression sourceExpression, Type returnType, List<Type> typesToReplace, int maxNewNestDeep)
        {
            _typesToReplace = typesToReplace;

            if (returnType.IsGenericType.Not() || returnType.GetGenericTypeDefinition() != typeof(IQueryable<>))
            {
                if (returnType.IsNotNullableNotBooleanStruct())
                    return Expression.Convert(sourceExpression, returnType);

                if (_typesToReplace.Contains(returnType))
                    return DecompressObject(sourceExpression, returnType, maxNewNestDeep);

                return sourceExpression;
            }

            var elementType = returnType.GetGenericArguments().Single();
            var methodSelect = typeof(Queryable).GetMethods().Single(m => m.Name == "Select" && m.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments().Count() == 2);

            if (typesToReplace.Contains(elementType))
            {
                methodSelect = methodSelect.MakeGenericMethod(new[] { typeof(CompressedObject), elementType });

                var compressedObjectParameter = Expression.Parameter(typeof(CompressedObject));
                var newWithParametersExpression = DecompressObject(compressedObjectParameter, elementType, maxNewNestDeep);

                var createNewLambda = Expression.Lambda(newWithParametersExpression, new[] { compressedObjectParameter });
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