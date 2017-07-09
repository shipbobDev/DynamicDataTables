using System;

namespace DynamicDataTableService
{
    public enum DataTableOperatorEnum
    {
        Equals,
        Contains,
        LessThan,
        GreaterThan,
        GreaterThanOrEqualTo,
        LessThanOrEqualTo,
        NotEquals,
        Between,
        NotContains,
        StartsWith,
        EndsWith
    }
    public class DataTableOperatorEnumResolver
    {
        public static string Resolve(SelectedFilter filter, FilterDefinition definition, string entity, ref int i)
        {
            // if there is an aggragate skip property name
            var prop = string.IsNullOrEmpty(entity) ? definition.PropertyName : entity;

            if (definition.Aggregate != DataTableAggregateEnum.None)
                prop += $".{DataTableAggregateEnumResolver.Resolve(definition)}";


            switch (filter.SelectedOperator)
            {
                case DataTableOperatorEnum.Equals:
                    return $"{prop} == @{i++}";
                case DataTableOperatorEnum.Contains:
                    return $"{prop}.Contains(@{i++})";
                case DataTableOperatorEnum.NotContains:
                    return $"{prop}.Contains(@{i++}) == false";
                case DataTableOperatorEnum.StartsWith:
                    return $"{prop}.StartsWith(@{i++})";
                case DataTableOperatorEnum.EndsWith:
                    return $"{prop}.EndsWith(@{i++})";
                case DataTableOperatorEnum.LessThan:
                    return $"{prop} < @{i++}";
                case DataTableOperatorEnum.GreaterThan:
                    return $"{prop} > @{i++}";
                case DataTableOperatorEnum.GreaterThanOrEqualTo:
                    return $"{prop} >= @{i++}";
                case DataTableOperatorEnum.LessThanOrEqualTo:
                    return $"{prop} <= @{i++}";
                case DataTableOperatorEnum.NotEquals:
                    return $"{prop} != @{i++}";
                case DataTableOperatorEnum.Between:
                    var statement = "";
                    if (filter.FirstSelectedValue != null)
                        statement = $"{prop} >= @{i++}";
                    if (filter.FirstSelectedValue != null && filter.SecondSelectedValue != null)
                        statement += " && ";
                    if (filter.SecondSelectedValue != null)
                        statement += $"{prop} <= @{i++}";
                    return statement;
            }
            throw new ArgumentException("Operator could not be found!");
        }
    }
}
