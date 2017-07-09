namespace DynamicDataTableService
{
    public class SelectedFilter
    {
        public string Header { get; set; }
        public string Identifier { get; set; }
        public DataTableOperatorEnum SelectedOperator { get; set; }
        public object FirstSelectedValue { get; set; }
        public object SecondSelectedValue { get; set; }
        public bool ColumnFilter { get; set; }
    }
}
