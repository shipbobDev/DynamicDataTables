using DataModel;
using DynamicDataTableService;
using MvcExample.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MvcExample.Controllers
{
    public class DynamicDataTableController : Controller
    {
        public ActionResult GetTableDefinition(string identifier)
        {
            var def = TableDefinitionLookUp.Definitions.FirstOrDefault(d => d.Identifier == identifier)?.GetClientModel();
            return Json(def, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult GetTableData(DataTableRequest request)
        {
            var data = new DataTableService<AdventureWorksContext>(TableDefinitionLookUp.Definitions, GetInjection()).GetData(request);
            return Json(data);
        }
        [HttpPost]
        public ActionResult GetAllSelect(DataTableRequest request)
        {
            var data = new DataTableService<AdventureWorksContext>(TableDefinitionLookUp.Definitions, GetInjection()).SelectAll(request);
            return Json(data);
        }
        private Dictionary<string, object> GetInjection()
        {
            return new Dictionary<string, object> {
                //{ "UserId", UserIdToBeUsed },
                //{ "IsAdmin", IsAdmin }
            };
        }
    }
}