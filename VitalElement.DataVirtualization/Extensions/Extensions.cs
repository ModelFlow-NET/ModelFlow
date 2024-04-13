namespace VitalElement.DataVirtualization.Extensions;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DataManagement;

public static class Extensions
{
    public static DataSource<TViewModel, T> CreateSortDescription<TViewModel, T, TProperty>(this DataSource<TViewModel, T> source, Expression<Func<T, TProperty>> propertyExpression, ListSortDirection direction)
        where TViewModel : class
    {
        var propertyName = GetPropertyName(propertyExpression);

        source.SortDescriptionList.Add(new SortDescription(propertyName, direction));

        return source;
    }
    
    private static string GetPropertyName<T, TProperty>(Expression<Func<T, TProperty>> propertyExpression)
    {
        MemberExpression memberExpression = null;

        if (propertyExpression.Body is UnaryExpression unaryExpression)
        {
            memberExpression = unaryExpression.Operand as MemberExpression;
        }
        else if (propertyExpression.Body is MemberExpression body)
        {
            memberExpression = body;
        }

        if (memberExpression == null)
        {
            throw new ArgumentException("Invalid expression");
        }

        return memberExpression.Member.Name;
    }
}