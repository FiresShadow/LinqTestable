using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace LinqTestable.Sources.ExpressionTreeChangers
{
    public abstract class DeepExpressionVisitor
    {
        public virtual Expression Visit(Expression expression)
        {
            if (expression == null)
                return null;

            switch (expression.NodeType)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.ArrayLength:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                    return VisitUnary((UnaryExpression)expression);
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.Coalesce:
                case ExpressionType.ArrayIndex:
                case ExpressionType.RightShift:
                case ExpressionType.LeftShift:
                case ExpressionType.ExclusiveOr:
                    return VisitBinary((BinaryExpression)expression);
                case ExpressionType.TypeIs:
                    return VisitTypeIs((TypeBinaryExpression)expression);
                case ExpressionType.Conditional:
                    return VisitConditional((ConditionalExpression)expression);
                case ExpressionType.Constant:
                    return VisitConstant((ConstantExpression)expression);
                case ExpressionType.Parameter:
                    return VisitParameter((ParameterExpression)expression);
                case ExpressionType.MemberAccess:
                    return VisitMemberAccess((MemberExpression)expression);
                case ExpressionType.Call:
                    return VisitMethodCall((MethodCallExpression)expression);
                case ExpressionType.Lambda:
                    return VisitLambda((LambdaExpression)expression);
                case ExpressionType.New:
                    return VisitNew((NewExpression)expression);
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    return VisitNewArray((NewArrayExpression)expression);
                case ExpressionType.Invoke:
                    return VisitInvocation((InvocationExpression)expression);
                case ExpressionType.MemberInit:
                    return VisitMemberInit((MemberInitExpression)expression);
                case ExpressionType.ListInit:
                    return VisitListInit((ListInitExpression)expression);
                case ExpressionType.Block:
                    return VisitBlock((BlockExpression)expression);
                case ExpressionType.Assign:
                    return VisitAssign((BinaryExpression)expression);
                default:
                    throw new Exception(string.Format("Unhandled expression type: '{0}'", expression.NodeType));
            }
        }

        protected virtual MemberBinding VisitBinding(MemberBinding binding)
        {
            switch (binding.BindingType)
            {
                case MemberBindingType.Assignment:
                    return VisitMemberAssignment((MemberAssignment)binding);
                case MemberBindingType.MemberBinding:
                    return VisitMemberMemberBinding((MemberMemberBinding)binding);
                case MemberBindingType.ListBinding:
                    return VisitMemberListBinding((MemberListBinding)binding);
                default:
                    throw new Exception(string.Format("Unhandled binding type '{0}'", binding.BindingType));
            }
        }

        protected virtual ElementInit VisitElementInitializer(ElementInit initializer)
        {
            ReadOnlyCollection<Expression> argumentsVisited = VisitExpressionList(initializer.Arguments);
            if (argumentsVisited != initializer.Arguments)
            {
                return Expression.ElementInit(initializer.AddMethod, argumentsVisited);
            }
            return initializer;
        }

        protected virtual Expression VisitUnary(UnaryExpression unaryExpression)
        {
            Expression operandVisited = Visit(unaryExpression.Operand);
            if (operandVisited != unaryExpression.Operand)
            {
                return Expression.MakeUnary(unaryExpression.NodeType, operandVisited, unaryExpression.Type, unaryExpression.Method);
            }
            return unaryExpression;
        }

        protected virtual Expression VisitBinary(BinaryExpression binaryExpression)
        {
            Expression leftVisited = Visit(binaryExpression.Left);
            Expression rightVisited = Visit(binaryExpression.Right);
            Expression conversionVisited = Visit(binaryExpression.Conversion);
            if (leftVisited != binaryExpression.Left || rightVisited != binaryExpression.Right || conversionVisited != binaryExpression.Conversion)
            {
                if (binaryExpression.NodeType == ExpressionType.Coalesce && binaryExpression.Conversion != null)
                    return Expression.Coalesce(leftVisited, rightVisited, conversionVisited as LambdaExpression);
                else
                    return Expression.MakeBinary(binaryExpression.NodeType, leftVisited, rightVisited, binaryExpression.IsLiftedToNull, binaryExpression.Method);
            }
            return binaryExpression;
        }

        protected virtual Expression VisitTypeIs(TypeBinaryExpression binaryExpression)
        {
            Expression expressionVisited = Visit(binaryExpression.Expression);
            if (expressionVisited != binaryExpression.Expression)
            {
                return Expression.TypeIs(expressionVisited, binaryExpression.TypeOperand);
            }
            return binaryExpression;
        }

        protected virtual Expression VisitConstant(ConstantExpression constantExpression)
        {
            return constantExpression;
        }

        protected virtual Expression VisitConditional(ConditionalExpression conditionalExpression)
        {
            Expression conditionVisited = Visit(conditionalExpression.Test);
            Expression ifTrueVisited = Visit(conditionalExpression.IfTrue);
            Expression ifFalseVisited = Visit(conditionalExpression.IfFalse);
            if (conditionVisited != conditionalExpression.Test || ifTrueVisited != conditionalExpression.IfTrue || ifFalseVisited != conditionalExpression.IfFalse)
            {
                return Expression.Condition(conditionVisited, ifTrueVisited, ifFalseVisited);
            }
            return conditionalExpression;
        }

        protected virtual Expression VisitParameter(ParameterExpression parameterExpression)
        {
            return parameterExpression;
        }

        protected virtual Expression VisitBlock(BlockExpression originalBlock)
        {
            var expressionsVisited = VisitExpressionList(originalBlock.Expressions);

            if (expressionsVisited != originalBlock.Expressions)
            {
                return Expression.Block(originalBlock.Variables, expressionsVisited);
            }

            return originalBlock;
        }

        protected virtual Expression VisitAssign(BinaryExpression originalAssign)
        {
            var rightVisited = Visit(originalAssign.Right);

            if (rightVisited != originalAssign.Right)
            {
                return Expression.Assign(originalAssign.Left, rightVisited);
            }

            return originalAssign;
        }

        protected virtual Expression VisitMemberAccess(MemberExpression memberExpression)
        {
            Expression expressionVisited = Visit(memberExpression.Expression);
            if (expressionVisited != memberExpression.Expression)
            {
                return Expression.MakeMemberAccess(expressionVisited, memberExpression.Member);
            }
            return memberExpression;
        }

        protected virtual Expression VisitMethodCall(MethodCallExpression callExpression)
        {
            Expression objectVisited = Visit(callExpression.Object);
            IEnumerable<Expression> argumentsVisited = VisitExpressionList(callExpression.Arguments);
            if (objectVisited != callExpression.Object || argumentsVisited != callExpression.Arguments)
            {
                return Expression.Call(objectVisited, callExpression.Method, argumentsVisited);
            }
            return callExpression;
        }

        protected virtual ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
        {
            List<Expression> list = null;
            for (int i = 0, n = original.Count; i < n; i++)
            {
                Expression expressionVisited = Visit(original[i]);
                if (list != null)
                {
                    list.Add(expressionVisited);
                }
                else if (expressionVisited != original[i])
                {
                    list = new List<Expression>(n);
                    for (int j = 0; j < i; j++)
                    {
                        list.Add(original[j]);
                    }
                    list.Add(expressionVisited);
                }
            }
            if (list != null)
            {
                return list.AsReadOnly();
            }
            return original;
        }

        protected virtual MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
        {
            Expression expressionVisited = Visit(assignment.Expression);
            if (expressionVisited != assignment.Expression)
            {
                return Expression.Bind(assignment.Member, expressionVisited);
            }
            return assignment;
        }

        protected virtual MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
        {
            IEnumerable<MemberBinding> bindingsVisited = VisitBindingList(binding.Bindings);
            if (bindingsVisited != binding.Bindings)
            {
                return Expression.MemberBind(binding.Member, bindingsVisited);
            }
            return binding;
        }

        protected virtual MemberListBinding VisitMemberListBinding(MemberListBinding binding)
        {
            IEnumerable<ElementInit> initializersVisited = VisitElementInitializerList(binding.Initializers);
            if (initializersVisited != binding.Initializers)
            {
                return Expression.ListBind(binding.Member, initializersVisited);
            }
            return binding;
        }

        protected virtual IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
        {
            List<MemberBinding> list = null;
            for (int i = 0, n = original.Count; i < n; i++)
            {
                MemberBinding bindingVisited = VisitBinding(original[i]);
                if (list != null)
                {
                    list.Add(bindingVisited);
                }
                else if (bindingVisited != original[i])
                {
                    list = new List<MemberBinding>(n);
                    for (int j = 0; j < i; j++)
                    {
                        list.Add(original[j]);
                    }
                    list.Add(bindingVisited);
                }
            }
            if (list != null)
                return list;
            return original;
        }

        protected virtual IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
        {
            List<ElementInit> list = null;
            for (int i = 0, n = original.Count; i < n; i++)
            {
                ElementInit initVisited = VisitElementInitializer(original[i]);
                if (list != null)
                {
                    list.Add(initVisited);
                }
                else if (initVisited != original[i])
                {
                    list = new List<ElementInit>(n);
                    for (int j = 0; j < i; j++)
                    {
                        list.Add(original[j]);
                    }
                    list.Add(initVisited);
                }
            }
            return list != null ? (IEnumerable<ElementInit>)list : original;
        }

        protected virtual Expression VisitLambda(LambdaExpression lambda)
        {
            Expression bodyVisited = Visit(lambda.Body);
            return bodyVisited != lambda.Body ? Expression.Lambda(lambda.Type, bodyVisited, lambda.Parameters) : lambda;
        }

        protected virtual NewExpression VisitNew(NewExpression original)
        {
            IEnumerable<Expression> argumentsVisited = VisitExpressionList(original.Arguments);
            return argumentsVisited != original.Arguments
                       ? (original.Members != null
                              ? Expression.New(original.Constructor, argumentsVisited, original.Members)
                              : Expression.New(original.Constructor, argumentsVisited))
                       : original;
        }

        protected virtual Expression VisitMemberInit(MemberInitExpression original)
        {
            NewExpression newVisited = VisitNew(original.NewExpression);
            IEnumerable<MemberBinding> bindings = VisitBindingList(original.Bindings);
            return newVisited != original.NewExpression || bindings != original.Bindings ? Expression.MemberInit(newVisited, bindings) : original;
        }

        protected virtual Expression VisitListInit(ListInitExpression original)
        {
            NewExpression newVisited = VisitNew(original.NewExpression);
            IEnumerable<ElementInit> initializersVisited = VisitElementInitializerList(original.Initializers);
            return newVisited != original.NewExpression || initializersVisited != original.Initializers ? Expression.ListInit(newVisited, initializersVisited) : original;
        }

        protected virtual Expression VisitNewArray(NewArrayExpression original)
        {
            IEnumerable<Expression> expressionVisited = VisitExpressionList(original.Expressions);
            return expressionVisited != original.Expressions
                       ? (original.NodeType == ExpressionType.NewArrayInit
                              ? Expression.NewArrayInit(original.Type.GetElementType(), expressionVisited)
                              : Expression.NewArrayBounds(original.Type.GetElementType(), expressionVisited))
                       : original;
        }

        protected virtual Expression VisitInvocation(InvocationExpression original)
        {
            IEnumerable<Expression> argumentsVisited = VisitExpressionList(original.Arguments);
            Expression expr = Visit(original.Expression);
            return argumentsVisited != original.Arguments || expr != original.Expression ? Expression.Invoke(expr, argumentsVisited) : original;
        }
    }
}