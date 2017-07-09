using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Dynamic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDataTableService
{

    public class DataTableService<T> where T : DbContext
    {
        private const int maxSelect = 1000;
        public Dictionary<string, object> filterInjections { get; set; }
        List<TableDefinition> TableDefinitions { get; set; }
        public DataTableService(List<TableDefinition> TableDefinitions, Dictionary<string, object> filterInjections = null)
        {
            this.TableDefinitions = TableDefinitions;
            this.filterInjections = filterInjections;
            if (this.filterInjections == null)
                this.filterInjections = new Dictionary<string, object>();
        }
        /// <summary>
        /// Returns the Primary keys based on selected filters
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public ServerResponse<object> SelectAll(DataTableRequest request)
        {
            try
            {
                if (request.SelectedFilters == null)
                    request.SelectedFilters = new List<SelectedFilter>();

                using (var context = Activator.CreateInstance<T>())
                {
                    var nameSpace = context.GetType().Namespace;

                    var tableDefinition = TableDefinitions.FirstOrDefault(d => d.Identifier == request.Identifier);
                    InjectServerSideFilters(request, tableDefinition);


                    var result = GetWhereClause(nameSpace, request, tableDefinition);

                    var dbSet = context.GetType().GetProperty(tableDefinition.RootEntityName);
                    var query = (dbSet.GetValue(context, null) as IQueryable);

                    var includeList = GetIncludeList(tableDefinition.ColumnDefinitions);

                    foreach (var include in includeList)
                        query = query.Include(include);

                    if (string.IsNullOrEmpty(result.Item1) == false)
                        query = query.Where(result.Item1, result.Item2);

                    var colToSort = tableDefinition.ColumnDefinitions.Single(d => d.Identifier == request.SortColumn);

                    query = query
                        .OrderBy($"{colToSort.FullPropertyPath} {(request.Asc ? "asc" : "desc")}");


                    var primary = tableDefinition.ColumnDefinitions.Single(d => d.PrimaryKey);

                    var response = (query as IEnumerable<object>).Select($"{primary.FullPropertyPath}");

                    var payload = ((IQueryable)response).ToListAsync().Result;

                    if (payload.Count > maxSelect)
                    {
                        return new ServerResponse<object>
                        {
                            Error = $"Max {maxSelect} records can be selected!"
                        };
                    }

                    return new ServerResponse<object>
                    {
                        Success = true,
                        PayLoad = payload,
                    };
                }
            }
            catch (Exception ex)
            {
                return new ServerResponse<object>
                {
                    Error = "Error occurred!"
                };
            }
        }
        /// <summary>
        /// Returns the data based on selected filters
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public ServerResponse<object> GetData(DataTableRequest request)
        {
            try
            {

                if (request.SelectedFilters == null)
                    request.SelectedFilters = new List<SelectedFilter>();

                using (var context = Activator.CreateInstance<T>())
                {
                    var nameSpace = context.GetType().Namespace;

                    var tableDefinition = TableDefinitions.FirstOrDefault(d => d.Identifier == request.Identifier);
                    InjectServerSideFilters(request, tableDefinition);

                    Func<object, object> selectFunc = (d) =>
                    {
                        var p = new Dictionary<string, object> { };

                        var colsToSelect = tableDefinition.ColumnDefinitions.Where(x => x.Include).ToList();

                        foreach (var def in colsToSelect)
                        {
                            GetNestedData(d, def, p);
                        }

                        return p;
                    };

                    var result = GetWhereClause(nameSpace, request, tableDefinition);

                    var dbSet = context.GetType().GetProperty(tableDefinition.RootEntityName);
                    var query = (dbSet.GetValue(context, null) as IQueryable);

                    var includeList = GetIncludeList(tableDefinition.ColumnDefinitions);

                    foreach (var include in includeList)
                        query = query.Include(include);

                    if (string.IsNullOrEmpty(result.Item1) == false)
                        query = query.Where(result.Item1, result.Item2);

                    var colToSort = tableDefinition.ColumnDefinitions.Single(d => d.Identifier == request.SortColumn);

                    var mainQuery = query;
                    query = query
                        .OrderBy($"{colToSort.FullPropertyPath} {(request.Asc ? "asc" : "desc")}")
                        .Skip((request.PageNumber - 1) * request.PageSize)
                        .Take(request.PageSize);

                    var response = (query as IEnumerable<object>)
                        .Select(selectFunc)
                        .ToList();
                    return new ServerResponse<object>
                    {
                        PayLoad = new
                        {
                            Data = response,
                            TotalCount = mainQuery.Count()
                        },
                        Success = true
                    };
                }
            }
            catch (Exception ex)
            {
                return new ServerResponse<object>
                {
                    Error = "Error occurred!"
                };
            }
        }
        /// <summary>
        /// Injecting ServerSide Filters Eg. UserId, IsAdmin etc
        /// </summary>
        /// <param name="request"></param>
        /// <param name="tableDefinition"></param>
        private void InjectServerSideFilters(DataTableRequest request, TableDefinition tableDefinition)
        {
            var injectFilters = tableDefinition.FilterDefinitions.Where(d => string.IsNullOrEmpty(d.Injection) == false).ToList();
            foreach (var inject in injectFilters)
            {
                var disableInjection = string.IsNullOrEmpty(inject.DisableInjectionExpression) ? false : (bool)filterInjections[inject.DisableInjectionExpression];

                if (disableInjection == false)
                    request.SelectedFilters.Add(new SelectedFilter
                    {
                        Identifier = inject.Identifier,
                        FirstSelectedValue = filterInjections[inject.Injection],
                        SelectedOperator = DataTableOperatorEnum.Equals
                    });
            }
        }
        /// <summary>
        /// Recursive function to retrieve all nested data that is defined in Table Definition
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="def"></param>
        /// <param name="dynamicModel"></param>
        private void GetNestedData(object obj, ColumnDefinition def, Dictionary<string, object> dynamicModel)
        {
            object objectToBeSelected = obj;
            if (string.IsNullOrEmpty(def.PropertyPath) == false)
            {
                var split = def.PropertyPath.Split('.').ToList();
                foreach (var entity in split)
                {
                    if (objectToBeSelected == null)
                        return;
                    objectToBeSelected = objectToBeSelected.GetType().GetProperty(entity).GetValue(objectToBeSelected, null);
                }
            }
            if (objectToBeSelected == null)
                return;
            if (def.SubColumnDefinitions == null || def.SubColumnDefinitions.Count == 0)
            {
                var isList = objectToBeSelected.GetType().GetInterface("IEnumerable") != null;

                if (isList)
                {
                    if (objectToBeSelected == null)
                        return;

                    if (def.Aggregate == DataTableAggregateEnum.None)
                        throw new Exception("Aggregate function expected !");


                    var field = (objectToBeSelected as IEnumerable).Select(def.PropertyName);

                    var method = GetExtensionMethods(typeof(Enumerable).Assembly, typeof(IEnumerable<>).MakeGenericType(def.PropertyType), def.Aggregate.ToString());

                    dynamicModel[def.Identifier] = method.Invoke(field, new object[] { field });

                }
                else
                {
                    objectToBeSelected = objectToBeSelected.GetType().GetProperty(def.PropertyName).GetValue(objectToBeSelected, null);
                    if (string.IsNullOrEmpty(def.Identifier))
                        throw new ArgumentException("Identifier is missing!");
                    dynamicModel[def.Identifier] = objectToBeSelected;
                }
            }
            else
            {
                var columns = def.SubColumnDefinitions.ToList();

                List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();

                foreach (var row in (objectToBeSelected as IEnumerable))
                {
                    var subdict = new Dictionary<string, object> { };
                    foreach (var column in columns)
                    {
                        if (column.SubColumnDefinitions != null)
                            GetNestedData(row, column, subdict);
                        else
                        {
                            var currentRow = row;
                            if (string.IsNullOrEmpty(column.PropertyPath) == false)
                            {
                                var path = column.PropertyPath.Split('.');
                                foreach (var entity in path)
                                {
                                    currentRow = currentRow.GetType().GetProperty(entity).GetValue(currentRow);
                                }
                            }

                            subdict[column.Identifier] = currentRow.GetType().GetProperty(column.PropertyName).GetValue(currentRow);
                        }

                    }
                    list.Add(subdict);
                }

                if (string.IsNullOrEmpty(def.Identifier))
                    throw new ArgumentException("Identifier is missing!");
                dynamicModel[def.Identifier] = list;

            }
        }
        /// <summary>
        /// Returns Where clause and its parameters based on selected filters and injected filters
        /// </summary>
        /// <param name="nameSpace">Namespace of the DataContext</param>
        /// <param name="request">DataTable request which is retrieved from client</param>
        /// <param name="tableDefinition">Definition of the table in the lookup class</param>
        /// <returns></returns>
        private Tuple<string, object[]> GetWhereClause(string nameSpace, DataTableRequest request, TableDefinition tableDefinition)
        {
            if (request.SelectedFilters == null || tableDefinition.FilterDefinitions == null)
                return new Tuple<string, object[]>("", null);
            var parameters = new List<object>();
            int paramsIndex = 0;
            var main = new StringBuilder();
            var properties = tableDefinition.FilterDefinitions.Where(d => request.SelectedFilters.Any(x => x.Identifier == d.Identifier)).ToList();
            foreach (var item in request.SelectedFilters)
            {
                if (item.ColumnFilter)
                {
                    properties.Add(tableDefinition.ColumnDefinitions.First(d => d.Identifier == item.Identifier).FilterDefinition);
                }
            }

            var orderedFilters = properties.OrderBy(d => d.PropertyName).ToList();
            foreach (var filter in orderedFilters)
            {
                var definition = orderedFilters.FirstOrDefault(d => d.PropertyName == filter.PropertyName);
                var selectedFilter = request.SelectedFilters.FirstOrDefault(d => d.Identifier == filter.Identifier);

                var child = new StringBuilder();
                var root = typeof(T).GetProperty(tableDefinition.RootEntityName).PropertyType.GenericTypeArguments.First();
                int paranthesisCount = 0;
                if (string.IsNullOrEmpty(definition.PropertyPath) == false)
                {
                    var split = definition.PropertyPath.Split('.').ToList();
                    foreach (var entity in split)
                    {
                        var entityType = root.GetProperty(entity);
                        if (entityType == null)
                            throw new ArgumentException($"{entity} could not be found in {root.Name}");

                        var isList = entityType.PropertyType.GetInterface("IEnumerable") != null;

                        if (split.IndexOf(entity) == split.Count - 1 && definition.Aggregate != DataTableAggregateEnum.None)
                        {
                            child.Append(DataTableOperatorEnumResolver.Resolve(selectedFilter, definition, entity, ref paramsIndex));
                            main.Append(child.ToString());
                        }
                        else
                        {
                            if (isList)
                            {
                                child.Append(entity);
                                child.Append(".Any(");
                                paranthesisCount++;
                            }
                            else
                            {
                                child.Append(entity);
                                child.Append(".");
                            }
                        }

                        root = typeof(T).GetProperty(entity).PropertyType.GenericTypeArguments.First();
                    }
                }
                if (definition.Aggregate == DataTableAggregateEnum.None)
                {
                    child.Append(DataTableOperatorEnumResolver.Resolve(selectedFilter, definition, "", ref paramsIndex));
                    child.Append(')', paranthesisCount);
                    main.Append(child.ToString());
                }

                if (orderedFilters.IndexOf(filter) != orderedFilters.Count - 1)
                {
                    main.Append(" && ");
                }

                GetParameter(parameters, filter, selectedFilter);
            }

            return new Tuple<string, object[]>(main.ToString(), parameters.ToArray());
        }
        /// <summary>
        /// Gets the parameters after converting to their own types
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="filterDefinition"></param>
        /// <param name="selectedFilter"></param>
        /// <returns></returns>
        private object GetParameter(List<object> parameters, FilterDefinition filterDefinition, SelectedFilter selectedFilter)
        {
            switch (filterDefinition.Type)
            {
                case "Int32":
                    if (selectedFilter.FirstSelectedValue != null)
                        parameters.Add(Convert.ToInt32(selectedFilter.FirstSelectedValue));
                    if (selectedFilter.SecondSelectedValue != null)
                        parameters.Add(Convert.ToInt32(selectedFilter.SecondSelectedValue));
                    break;
                case "DateTime":
                    parameters.Add(Convert.ToDateTime(selectedFilter.FirstSelectedValue).ToUniversalTime());
                    if (selectedFilter.SecondSelectedValue != null)
                        parameters.Add(Convert.ToDateTime(selectedFilter.SecondSelectedValue).AddDays(1).ToUniversalTime());
                    break;
                case "Boolean":
                    parameters.Add(Convert.ToBoolean(selectedFilter.FirstSelectedValue));
                    break;
                case "String":
                default:
                    parameters.Add(selectedFilter.FirstSelectedValue);
                    break;
            }


            return null;
        }
        /// <summary>
        /// Returns the Include list to be included to query based on Sub Column definitions
        /// </summary>
        /// <param name="colDefinitions"></param>
        /// <param name="iteratingList"></param>
        /// <returns></returns>
        private string[] GetIncludeList(List<ColumnDefinition> colDefinitions, string[] iteratingList = null)
        {
            var total = new List<string>();
            foreach (var def in colDefinitions)
            {
                List<string> pro = new List<string>();

                if (string.IsNullOrEmpty(def.PropertyPath) == false && iteratingList == null)
                    pro.Add(def.PropertyPath);

                if (iteratingList != null && iteratingList.Length > 0)
                    foreach (var item in iteratingList)
                        if (string.IsNullOrEmpty(def.PropertyPath) == false)
                            pro.Add($"{item}.{def.PropertyPath}");

                total.AddRange(pro);

                if (def.SubColumnDefinitions != null)
                    total.AddRange(GetIncludeList(def.SubColumnDefinitions, pro.Distinct().ToArray()));
            }

            return total.Distinct().ToArray();
        }
        /// <summary>
        /// Returns the related IEnumarable methods (static ones) based on types Eg. Sum, Min, Max and Average etc.
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="extendedType"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        private MethodInfo GetExtensionMethods(Assembly assembly, Type extendedType, string methodName)
        {
            var query = from type in assembly.GetTypes()
                        where type.IsSealed && !type.IsGenericType && !type.IsNested
                        from method in type.GetMethods(BindingFlags.Static
                            | BindingFlags.Public | BindingFlags.NonPublic)
                        where method.IsDefined(typeof(ExtensionAttribute), false)
                        && method.GetParameters().FirstOrDefault()?.ParameterType == extendedType
                        && method.Name == methodName
                        select method;

            return query.FirstOrDefault();
        }
    }
}
