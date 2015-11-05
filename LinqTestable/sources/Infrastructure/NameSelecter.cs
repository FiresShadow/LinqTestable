using System;
using System.Linq.Expressions;

namespace LinqTestable.Sources.Infrastructure
{
    public static class NameSelecter
    {
        public static string GetMemberName<TOwner>(Expression<Func<TOwner, object>> expression)
        {
            var memberExpression = expression.Body as MemberExpression;

            if (memberExpression == null)
            {
                var unaryExpression = expression.Body as UnaryExpression;
                if (unaryExpression != null)
                    memberExpression = unaryExpression.Operand as MemberExpression;
            }

            if (memberExpression == null)
            {
                throw new Exception("Failed get member name");
            }

            return memberExpression.Member.Name;
        }
    }
}