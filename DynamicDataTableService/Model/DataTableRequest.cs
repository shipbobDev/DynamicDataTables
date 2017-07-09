using System.Collections.Generic;

namespace DynamicDataTableService
{
    public class DataTableRequest  // frontend side
    {
        public List<SelectedFilter> SelectedFilters { get; set; }
        public string Identifier { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public string SortColumn { get; set; }
        public bool Asc { get; set; }
        //public string SearchQuery { get; set; }
    }
}
