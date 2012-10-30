using System;
using System.Linq.Expressions;
using System.Reflection;

namespace TranslationHelper
{
    public static class PropertyHelper
    {
        public static string GetPropertyName<T>(this Expression<Func<T, object>> expression)
        {
            return GetProperty(expression).Name;
        }

        public static PropertyInfo GetProperty(LambdaExpression expression)
        {
            var memberExpression = GetMemberExpression(expression);
            return (PropertyInfo)memberExpression.Member;
        }

        private static MemberExpression GetMemberExpression(LambdaExpression expression)
        {
            MemberExpression memberExpression = null;
            if (expression.Body.NodeType == ExpressionType.Convert)
            {
                var body = (UnaryExpression)expression.Body;
                memberExpression = body.Operand as MemberExpression;
            }
            else if (expression.Body.NodeType == ExpressionType.MemberAccess)
            {
                memberExpression = expression.Body as MemberExpression;
            }

            if (memberExpression == null) 
                throw new ArgumentException("Not a member access", "member");

            return memberExpression;
        }
    }
}