using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace DynamicDataTableService
{
    public class ColumnDefinition
    {
        public ColumnDefinition()
        {
            Include = true;
            Visible = true;
            Sortable = true;
            Filterable = true;
        }
        public string PropertyName { get; set; }
        private string _identifier;
        public string Identifier
        {
            get
            {
                return _identifier;
            }
            set
            {
                if (string.IsNullOrEmpty(HeaderText))
                {
                    HeaderText = value;
                    _identifier = value;
                }
            }
        }
        [ScriptIgnore]
        public string PropertyPath { get; set; }
        public string HeaderText { get; set; }
        public bool PrimaryKey { get; set; }
        public bool Sortable { get; set; }
        public bool Visible { get; set; }
        public bool Include { get; set; }
        public bool Filterable { get; set; }
        public bool Editable { get; set; }
        private Type _propertyType;
        [ScriptIgnore]
        public Type PropertyType
        {
            get
            {
                return _propertyType;
            }
            set
            {
                _propertyType = value;
                Type = value.Name;
                if (value.IsEnum)
                {
                    SelectModel = typeof(EnumHelper<>).MakeGenericType(value).GetMethod("GetList").Invoke(null, null) as List<EnumModel>;
                    Type = "Enum";
                    Operator = DataTableOperatorEnum.Equals;
                }
                else if (value == typeof(string))
                    Operator = DataTableOperatorEnum.Contains;
                else if (value == typeof(DateTime) || value == typeof(int))
                    Operator = DataTableOperatorEnum.Between;
                else if (value == typeof(bool))
                    Operator = DataTableOperatorEnum.Equals;
            }
        }
        public string Type { get; set; }
        public object SelectModel { get; set; }
        public DataTableOperatorEnum Operator { get; set; }
        public DataTableAggregateEnum Aggregate { get; set; }

        [ScriptIgnore]
        public string FullPropertyPath
        {
            get
            {
                return (string.IsNullOrEmpty(PropertyPath) ? "" : $"{PropertyPath}.") + DataTableAggregateEnumResolver.Resolve(FilterDefinition);
            }
        }

        public FilterDefinition FilterDefinition
        {
            get
            {
                return new FilterDefinition
                {
                    Identifier = Identifier,
                    PropertyName = PropertyName,
                    PropertyPath = PropertyPath,
                    PropertyType = _propertyType,
                    Aggregate = Aggregate
                };
            }
        }

        public List<ColumnDefinition> SubColumnDefinitions { get; set; }
    }
}
