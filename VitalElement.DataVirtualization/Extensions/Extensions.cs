namespace VitalElement.DataVirtualization.Extensions;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using DataManagement;
using Interfaces;
using Pageing;

public static class Extensions
{
    public static DataSource? GetDataSource<TViewModel>(this IReadOnlyObservableCollection<DataItem<TViewModel>> items)
        where TViewModel : class
    {
        if(items is VirtualizingObservableCollection<DataItem<TViewModel>> collection)
        {
            var provider = collection.Provider;

            if (provider is PaginationManager<DataItem<TViewModel>> paginationManager)
            {
                return paginationManager.ProviderAsync as DataSource;
            }
        }

        return null;
    }

    public static DataSource<TViewModel, T> AddSortDescription<TViewModel, T, TProperty>(this DataSource<TViewModel, T> source, Expression<Func<T, TProperty>> propertyExpression, ListSortDirection direction)
        where TViewModel : class
    {
        var propertyName = GetPropertyName(propertyExpression);

        source.SortDescriptionList.Add(new SortDescription(propertyName, direction));

        return source;
    }
    
    public static SortDescription CreateSortDescription<TViewModel, T, TProperty>(this DataSource<TViewModel, T> source, Expression<Func<T, TProperty>> propertyExpression, ListSortDirection direction)
        where TViewModel : class
    {
        var propertyName = GetPropertyName(propertyExpression);

        return new SortDescription(propertyName, direction);
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