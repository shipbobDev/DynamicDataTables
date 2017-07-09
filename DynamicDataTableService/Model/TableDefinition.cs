using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;

namespace DynamicDataTableService
{
    public class TableDefinition
    {
        public TableDefinition()
        {
            PageSizeOptions = new int[] { 10, 25, 50 };
            FilterDefinitions = new List<FilterDefinition>();
        }
        public List<ColumnDefinition> ColumnDefinitions { get; set; }
        public List<FilterDefinition> FilterDefinitions { get; set; }
        // extracted to a function to make sure we are hiding in the json
        [ScriptIgnore]
        public string RootEntityName { get; set; }
        public string Identifier { get; set; }
        public int[] PageSizeOptions { get; set; }

        public TableDefinition GetClientModel()
        {
            return new TableDefinition
            {
                Identifier = Identifier,
                PageSizeOptions = PageSizeOptions,
                ColumnDefinitions = ColumnDefinitions.Where(d => d.Visible).ToList(),
                FilterDefinitions = FilterDefinitions.Where(d => d.ServerSide == false).ToList()
            };
        }
    }
}
