namespace DataGridAsyncDemoMVVM;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using Avalonia.Experimental.Data;
using Avalonia.Layout;
using VitalElement.DataVirtualization.DataManagement;

public static class Extensions
{
    internal static Task<T?> GetRowAsync<T>(this IQueryable<T> table, Expression<Func<T, bool>> predicate)
        where T : class
    {
        return Task.Run(() => table.FirstOrDefault(predicate))!;
    }

    internal static Task<int> GetRowCountAsync<T>(this IQueryable<T> table, Func<IQueryable<T>, IQueryable<T>>? query = null)
        where T : class
    {
        IQueryable<T> rows = table;

        if (query != null)
        {
            rows = query(rows);
        }

        return Task.Run(() => rows.Count());
    }

    internal static Task<IEnumerable<T>> GetRowsAsync<T>(this IQueryable<T> table, int offset, int count, Func<IQueryable<T>, IQueryable<T>>? query = null)
        where T : class
    {
        IQueryable<T> rows = table;

        if (query != null)
        {
            rows = query(rows);
        }

        return Task.Run(() => rows.Skip(offset).Take(count).AsEnumerable());
    }
    
    public static FlatTreeDataGridSource<TModel> AddAutoColumn<TModel, TValue>(
        this FlatTreeDataGridSource<TModel> source,
        object header,
        Expression<Func<TModel, TValue>> getter,
        bool readWrite = true,
        GridLength? gridLength = null)
        where TModel : class
    {
        source.Columns.Add(new TemplateColumn<TModel>(
            header,
            new CellTemplateFactory<TModel, TValue>(getter),
            readWrite ? new CellTemplateFactory<TModel, TValue>(getter, true) : null,
            gridLength ?? GridLength.Star));

        return source;
    }
}

public class CellTemplateFactory<TModel, TValue> : IDataTemplate
    where TModel : class
{
    private readonly Expression<Func<TModel, TValue>> getter;
    private bool editor;

    public CellTemplateFactory(Expression<Func<TModel, TValue>> getter, bool editor = false)
    {
        this.getter = getter;
        this.editor = editor;
    }

    public Control? Build(object? data)
    {
        if (data is null)
        {
            return null;
        }

        var binding = ToNullable(getter);

        if (data is IDataItem di)
        {
            var contentControl = new ContentControl()
            {
                Margin = new Thickness(10, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                [!ContentControl.ContentProperty] = binding.Instance(data as TModel).Select(x => x.Value).ToBinding(),
            };

            contentControl.DataTemplates.Add(new CellTemplateResolver(editor));

            return contentControl;
        }
        else
        {
            var contentControl = new ContentControl()
            {
                Margin = new Thickness(10, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                [!ContentControl.ContentProperty] = binding.Instance(data as TModel).Select(x => x.Value).ToBinding(),
            };

            contentControl.DataTemplates.Add(new CellTemplateResolver(editor));
            return contentControl;
        }
    }

    public bool Match(object? data)
    {
        return data is TModel;
    }

    private static TypedBinding<TModel, TValue?> ToNullable(Expression<Func<TModel, TValue>> getter)
    {
        var g = Expression.Lambda<Func<TModel, TValue?>>(
            Expression.Convert(getter.Body, typeof(TValue?)),
            getter.Parameters);

        return TypedBinding<TModel>.OneWay(g);
    }

    private class CellTemplateResolver : IDataTemplate
    {
        private bool editor;

        public CellTemplateResolver(bool isEditor)
        {
            editor = isEditor;
        }

        public Control? Build(object? data)
        {
            if (data is null)
            {
                return null;
            }

            var type = data.GetType();

            if (TryResolveView(type, out var viewType) || TryResolveViewFromInterface(type, out viewType))
            {
                var control = Activator.CreateInstance(viewType) as Control;

                if (control is { })
                {
                    control.VerticalAlignment = VerticalAlignment.Center;
                    control.DataContext = data;
                    return control;
                }
            }

            return new TextBlock { VerticalAlignment = VerticalAlignment.Center, Text = data.ToString() };
        }

        public bool Match(object? data)
        {
            return data is IDataItem;
        }

        private bool TryResolveView(Type type, [NotNullWhen(true)]out Type? viewType)
        {
            viewType = null;

            var name = type.Name;

            while (name.Contains('`') && type.BaseType != null)
            {
                type = type.BaseType;
                name = type.Name;
            }

            var replace = name.Replace("ViewModel", editor ? "EditView" : "View");
            var fullname = type.FullName!.Replace("ViewModels", "Views")
                .Replace(name, replace, StringComparison.Ordinal);

            viewType = Type.GetType(fullname);

            return viewType != null;
        }

        private bool TryResolveViewFromInterface(Type dataType, [NotNullWhen(true)]out Type? viewType)
        {
            bool result = false;
            viewType = null;

            var interfaces = dataType.GetInterfaces();

            foreach (var i in interfaces)
            {
                if (!string.IsNullOrWhiteSpace(i.FullName) && i.FullName.EndsWith("ViewModel"))
                {
                    var name = i.Name;

                    var replace = name.Replace("ViewModel", editor ? "EditView" : "View");

                    if (replace.StartsWith("I"))
                    {
                        replace = replace.Substring(1);
                    }

                    var fullname = i.FullName.Replace("ViewModels", "Views")
                        .Replace(name, replace, StringComparison.Ordinal);

                    var type = Type.GetType(fullname);

                    if (type != null)
                    {
                        viewType = type;
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }
    }
}