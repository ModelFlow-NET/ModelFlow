namespace VitalElement.DataVirtualization.Extensions;

using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using DataManagement;

public static class Extensions
{
    public static string ToQueryString(this SortDescriptionList list)
    {
        var sort = "";

        var sortFound = false;
        foreach (var sortDescription in list)
        {
            if (sortFound)
                sort += ", ";

            sortFound = true;

            sort += sortDescription.PropertyName;
            sort += sortDescription.Direction == ListSortDirection.Ascending ? " ASC" : " DESC";
        }

        //if ((!sortFound) && (!string.IsNullOrWhiteSpace( primaryKey )))
        //  sort += primaryKey + " ASC";

        return sort;
    }

    public static string ToQueryString(this FilterDescriptionList list)
    {
        var filter = "";

        var filterFound = false;
        foreach (var filterDescription in list)
        {
            var subFilter = GetLinqQueryString(filterDescription);
            if (!string.IsNullOrWhiteSpace(subFilter))
            {
                if (filterFound)
                    filter += " and ";
                filterFound = true;
                filter += " " + subFilter + " ";
            }
        }

        return filter;
    }

    private static readonly Regex _regexSplit = new Regex(
        @"(and)|(or)|(==)|(<>)|(!=)|(<=)|(>=)|(&&)|(\|\|)|(=)|(>)|(<)|(\*[\-_a-zA-Z0-9]+)|([\-_a-zA-Z0-9]+\*)|([\-_a-zA-Z0-9]+)",
        RegexOptions.IgnoreCase);

    private static readonly Regex _regexOp =
        new Regex(@"(and)|(or)|(==)|(<>)|(!=)|(<=)|(>=)|(&&)|(\|\|)|(=)|(>)|(<)", RegexOptions.IgnoreCase);

    private static readonly Regex _regexComparOp =
        new Regex(@"(==)|(<>)|(!=)|(<=)|(>=)|(=)|(>)|(<)", RegexOptions.None);

    private static string GetLinqQueryString(FilterDescription filterDescription)
    {
        var ret = "";

        if (!string.IsNullOrWhiteSpace(filterDescription.Filter))
            try
            {
                // xceed syntax : empty (contains), AND (uppercase), OR (uppercase), <>, * (end with), =, >, >=, <, <=, * (start with)
                //    see http://doc.xceedsoft.com/products/XceedWpfDataGrid/Filter_Row.html 
                // linq.dynamic syntax : =, ==, <>, !=, <, >, <=, >=, &&, and, ||, or, x.m(�) (where x is the attrib and m the function (ex: Contains, StartsWith, EndsWith ...)
                //    see D:\DevC#\VirtualisingCollectionTest1\DynamicQuery\Dynamic Expressions.html 
                // ex : RemoteOrDbDataSourceEmulation.Instance.Items.Where( "Name.Contains(\"e_1\") or Name.Contains(\"e_2\")" );

                var exp = filterDescription.Filter;

                // arrange expression

                var previousTermIsOperator = false;
                foreach (Match match in _regexSplit.Matches(exp))
                    if (match.Success)
                        if (_regexOp.IsMatch(match.Value))
                        {
                            if (_regexComparOp.IsMatch(match.Value))
                            {
                                // simple operator >, <, ==, != ...
                                ret += " " + filterDescription.PropertyName + " " + match.Value;
                                previousTermIsOperator = true;
                            }
                            else
                            {
                                // and, or ...
                                ret += " " + match.Value;
                                previousTermIsOperator = false;
                            }
                        }
                        else
                        {
                            // Value
                            if (previousTermIsOperator)
                            {
                                ret += " " + match.Value;
                                previousTermIsOperator = false;
                            }
                            else
                            {
                                if (match.Value.StartsWith("*"))
                                    ret += " " + filterDescription.PropertyName + ".EndsWith( \"" +
                                           match.Value.Substring(1) + "\" )";
                                else if (match.Value.EndsWith("*"))
                                    ret += " " + filterDescription.PropertyName + ".StartsWith( \"" +
                                           match.Value.Substring(0, match.Value.Length - 1) + "\" )";
                                else
                                    ret += " " + filterDescription.PropertyName + ".Contains( \"" + match.Value +
                                           "\" )";
                                previousTermIsOperator = false;
                            }
                        }
            }
            catch (Exception)
            {
            }

        return ret;
    }
}