using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace DynamicDataTableService
{
    public class FilterDefinition
    {
        public FilterDefinition()
        {
            Visible = true;
        }
        public string PropertyName { get; set; }
        public string HeaderText { get; set; }
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
        //public OperatorEnum[] Operators { get; set; }
        public DataTableOperatorEnum Operator { get; set; }
        public object SelectModel { get; set; }
        public DataTableAggregateEnum Aggregate { get; set; }
        private Type _propertyType;
        public Type PropertyType
        {
            set
            {
                if (value != null)
                {
                    _propertyType = value;
                    Type = _propertyType.Name;
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
        }
        public string Type { get; set; }
        public bool Visible { get; set; }
        [ScriptIgnore]
        public bool ServerSide { get; set; }
        [ScriptIgnore]
        public string Injection { get; set; }
        [ScriptIgnore]
        public string DisableInjectionExpression { get; set; }
    }
}
