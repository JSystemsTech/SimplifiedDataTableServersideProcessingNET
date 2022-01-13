using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SimplifiedDataTableServersideProcessingNET
{
    
    public class PropertyInfo<T>
        where T : class
    {
        private static ConcurrentDictionary<string, Func<T, object>> Getters { get; set; }
        private static IEnumerable<PropertyInfo> _Properties { get; set; }
        public static IEnumerable<PropertyInfo> Properties = _Properties ?? typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        public static object GetValue(T model, string name)
        {
            if (Getters == null)
            {
                Getters = new ConcurrentDictionary<string, Func<T, object>>();
            }
            if (!Getters.ContainsKey(name) && Properties.Any(p => p.Name == name))
            {
                Func<T, object> newGetter = (Func<T, object>)Delegate.CreateDelegate(typeof(Func<T, object>), null, typeof(T).GetProperty(name).GetGetMethod());
                Getters.TryAdd(name, newGetter);
            }
            if(Getters.TryGetValue(name, out Func<T, object> getter))
            {
                return getter(model);
            }
            return null;
        }
    }
    public static class DataTableExtensions
    {
        private static object GetValue<T>(this T model, string name) where T : class
            => PropertyInfo<T>.GetValue(model, name);
        private static string GetValueAsString<T>(this T model, string name) where T : class
        {
            object value = model.GetValue(name);
            return value != null && value is DateTime date ? date.ToString("yyyy/MM/dd HH:mm:ss:fff") : value != null ? value.ToString() : "";
        }
        private static bool IsMatch(this string value, DataTableSearch settings)
        {
            return settings.Regex ? Regex.Match(value, settings.Value, RegexOptions.IgnoreCase).Success : value.ContainsCaseInsensitive(settings.Value);
        }
        private static bool ContainsCaseInsensitive(this string source, string toCheck)
        => source?.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool IsMatch<T>(this T model, DataTableColumnSearch settings) where T : class
        {
            return model.GetValueAsString(settings.Column.Name).IsMatch(settings.Search);
        }
        private static IEnumerable<T> GeneralSearchDataTable<T>(this IEnumerable<T> data, DataTableRequest request) where T : class
        {
            var searchColumns = request.GetTableSearchColumns().Where(c => c.Search != null);
            return searchColumns.Count() > 0 ? data.Where(item => searchColumns.Any(c => item.IsMatch(c))) : data;
        }
        private static IEnumerable<T> ColumnSpecificSearchDataTable<T>(this IEnumerable<T> data, DataTableRequest request) where T : class
        {
            var searchColumns = request.GetSearchColumns().Where(c => c.Search != null);
            return searchColumns.Count() > 0 ? data.Where(item => searchColumns.All(c => item.IsMatch(c))) : data;
        }
        private static IEnumerable<T> OrderDataTable<T>(this IEnumerable<T> data, DataTableRequest request) where T : class
        {
            IEnumerable<DataTableColumnOrder> orderCols = request.GetOrderColumns();
            if (orderCols.Count() == 0)
            {
                return data;
            }
            IOrderedEnumerable<T> orderedData = null;

            for (int i = 0; i < orderCols.Count(); i++)
            {
                DataTableColumnOrder col = orderCols.ElementAt(i);
                orderedData =
                    i == 0 && col.Dir.ToLower() == "desc" ? data.OrderByDescending(m=> m.GetValue(col.Name)) :
                    i == 0 ? data.OrderBy(m => m.GetValue(col.Name)) :
                    col.Dir.ToLower() == "desc" ? orderedData.ThenByDescending(m => m.GetValue(col.Name)) :
                    orderedData.ThenBy(m => m.GetValue(col.Name));
            }
            return orderedData;
        }
        private static IEnumerable<T> GetDataTablePage<T>(this IEnumerable<T> data, DataTableRequest request) where T : class
        {
            return new ArraySegment<T>(data.ToArray(), request.Start, data.Count() - (request.Start + request.Length) < 0 ? data.Count() - request.Start : request.Length);
        }
        public static DataTableResponse ProcessDataTable<T>(
            this IEnumerable<T> data,
            DataTableRequest request,
            Func<T, string> getRowId = null,
            Func<T, string> getRowClass = null,
            Func<T, object> getRowData = null,
            Func<T, object> getRowAttr = null) where T : class
        {
            try
            {
                var filteredData = data.GeneralSearchDataTable(request).ColumnSpecificSearchDataTable<T>(request);
                var orderedData = filteredData.OrderDataTable(request);
                var pageData = orderedData.GetDataTablePage(request);

                var serializedData = pageData.SerializeData(getRowId, getRowClass, getRowData, getRowAttr);
                return new DataTableResponse(request, serializedData, data.Count(), filteredData.Count());
            }
            catch (Exception e)
            {
                return new DataTableResponse(request, e);
            }
        }
        public static IEnumerable<IDictionary<string, object>> SerializeData<T>(
            this IEnumerable<T> data,
            Func<T, string> getRowId = null,
            Func<T, string> getRowClass = null,
            Func<T, object> getRowData = null,
            Func<T, object> getRowAttr = null)
            where T : class
        {
            List<IDictionary<string, object>> dataForTable = new List<IDictionary<string, object>>();
            foreach (T model in data)
            {
                ConcurrentDictionary<string, object> item = new ConcurrentDictionary<string, object>();
                if (getRowId != null)
                {
                    item.TryAdd("DT_RowId", getRowId(model));
                }
                if (getRowClass != null)
                {
                    item.TryAdd("DT_RowClass", getRowClass(model));
                }
                if (getRowData != null)
                {
                    item.TryAdd("DT_RowData", getRowData(model));
                }
                if (getRowAttr != null)
                {
                    item.TryAdd("DT_RowAttr", getRowAttr(model));
                }
                foreach (var propInfo in PropertyInfo<T>.Properties)
                {
                    item.TryAdd(propInfo.Name, propInfo.GetValue(model));
                }
                dataForTable.Add(item);
            }
            return dataForTable;
        }

    }
    public class DataTableResponse<T>
        where T : class
    {
        public int draw { get; set; }
        public int recordsTotal { get; set; }
        public int recordsFiltered { get; set; }
        public IEnumerable<T> data { get; set; }
        public string error { get; set; }

        public DataTableResponse(DataTableRequest request, IEnumerable<T> data, int total, int filtered)
        {
            draw = request.Draw;
            this.data = data;
            recordsTotal = total;
            recordsFiltered = filtered;
        }
        public DataTableResponse(DataTableRequest request, Exception e)
        {
            draw = request.Draw;
            data = new T[0];
            recordsTotal = 0;
            recordsFiltered = 0;
            error = e.Message;
        }

    }
    public class DataTableResponse
    {
        public int draw { get; set; }
        public int recordsTotal { get; set; }
        public int recordsFiltered { get; set; }
        public IEnumerable<object> data { get; set; }
        public string error { get; set; }


        public DataTableResponse(DataTableRequest request, IEnumerable<object> data, int total, int filtered)
        {
            draw = request.Draw;
            this.data = data;
            recordsTotal = total;
            recordsFiltered = filtered;
        }
        public DataTableResponse(DataTableRequest request, Exception e)
        {
            draw = request.Draw;
            data = new object[0];
            recordsTotal = 0;
            recordsFiltered = 0;
            error = e.Message;
        }

    }
    public class DataTableRequest
    {

        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public IEnumerable<DataTableOrder> Order { get; set; }
        public IEnumerable<DataTableColumn> Columns { get; set; }
        public DataTableSearch Search { get; set; }

        public IEnumerable<DataTableColumnSearch> GetSearchColumns()
        => Columns.Where(c => c.Searchable).Select(c => new DataTableColumnSearch(c));
        public IEnumerable<DataTableColumnSearch> GetTableSearchColumns()
        => Columns.Where(c => c.Searchable).Select(c => new DataTableColumnSearch(c, Search));
        public IEnumerable<DataTableColumnOrder> GetOrderColumns()
        => Order.Select(o => new DataTableColumnOrder(Columns.ElementAt(o.Column), o.Dir)).Where(c => c.Orderable);
    }
    public class DataTableRequest<T> : DataTableRequest
    where T : class
    {
        public T Parameters { get; set; }
        public DataTableRequest() { Parameters = default(T); }
    }

    public class DataTableOrder
    {
        public int Column { get; set; }
        public string Dir { get; set; }
    }
    public class DataTableSearch
    {
        public string Value { get; set; }
        public bool Regex { get; set; }
    }
    public class DataTableColumn
    {
        public Guid Guid { get; private set; }
        public string Data { get; set; }
        public string Name { get; set; }
        public bool Searchable { get; set; }
        public bool Orderable { get; set; }
        public DataTableSearch Search { get; set; }

        public DataTableColumn()
        {
            Guid = Guid.NewGuid();
        }

    }
    public class DataTableColumnOrder
    {
        public string Data { get; set; }
        public string Name { get; set; }
        public string Dir { get; set; }
        public bool Orderable { get; set; }
        public DataTableColumnOrder(DataTableColumn col, string dir)
        {
            Data = col.Data;
            Name = col.Name;
            Dir = dir;
            Orderable = col.Orderable;
        }
    }
    public class DataTableColumnSearch
    {
        public DataTableSearch Search { get; set; }
        public DataTableColumn Column { get; set; }
        public DataTableColumnSearch(DataTableColumn col)
        {
            Column = col;
            if (col.Search != null && col.Search.Value != null)
            {
                Search = col.Search;
            }
        }
        public DataTableColumnSearch(DataTableColumn col, DataTableSearch search)
        {
            Column = col;
            if (search.Value != null)
            {
                Search = search;
            }
        }
    }
}
