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

            var expression = new NullableReplacer(typesToReplace).Visit(sourceExpression);
            
            //TODO refactor this
            var maxNewNestDeep = new NewNestingCounter().FindCount(sourceExpression);

            expression = new FinalSelectAdder().AddFinalSelect(expression, returnType, typesToReplace, maxNewNestDeep);
            expression = new NullComparisonChanger().Visit(expression);

            return expression;
        }

        
    }
}