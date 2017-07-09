namespace DynamicDataTableService
{
    public enum DataTableAggregateEnum
    {
        None,
        Sum,
        Count,
        Min,
        Max,
        Average
    }
    public class DataTableAggregateEnumResolver
    {
        public static string Resolve(FilterDefinition definition)
        {
            switch (definition.Aggregate)
            {
                case DataTableAggregateEnum.Sum:
                    return ($"Sum({definition.PropertyName})");
                case DataTableAggregateEnum.Count:
                    return ("Count()");
                case DataTableAggregateEnum.Min:
                    return ($"Min({definition.PropertyName})");
                case DataTableAggregateEnum.Max:
                    return ($"Max({definition.PropertyName})");
                case DataTableAggregateEnum.Average:
                    return ($"Average({definition.PropertyName})");
                default:
                case DataTableAggregateEnum.None:
                    return definition.PropertyName;
            }
        }
    }
}
