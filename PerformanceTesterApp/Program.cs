using SimplifiedDataTableServersideProcessingNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceTesterApp
{
    public class MyClass//:IMappable
    {
        public Guid Guid { get; set; }
        public DateTime DateTime { get; set; }
        public string Text { get; set; }
        public int Number { get; set; }
        public Guid Guid2 { get; set; }
        public DateTime DateTime2 { get; set; }
        public string Text2 { get; set; }
        public int Number2 { get; set; }
        public MyClass()
        {
            Guid = Guid.NewGuid();
            Guid2 = Guid.NewGuid();
            DateTime = DateTime.Now;
            DateTime2 = DateTime.UtcNow;
            Text = "Text";
            Text2 = "Text2";
        }

        //public object MapValue(string propertyName)
        //{
        //    return propertyName == "Guid" ? Guid :
        //    propertyName == "DateTime" ? DateTime :
        //    propertyName == "Text" ? Text :
        //    propertyName == "Number" ? Number :
        //    propertyName == "Guid2" ? Guid2 :
        //    propertyName == "DateTime2" ? DateTime2 :
        //    propertyName == "Text2" ? Text2 :
        //    propertyName == "Number2" ? Number2 : (object)null;
        //}

    }
    public class Program
    {
        public static void Main()
        {
            DataTableRequest request = new DataTableRequest()
            {
                Draw = 1,
                Start = 1,
                Length = 100,
                Order = new DataTableOrder[] { 
                    new DataTableOrder() {
                        Column = 0,
                        Dir = "asc"
                    },
                    new DataTableOrder() {
                        Column = 1,
                        Dir = "desc"
                    }
                },
                Columns = new DataTableColumn[] {
                    new DataTableColumn() {
                        Data = "Guid",
                        Name = "Guid",
                        Searchable = true,
                        Orderable = true,
                    },
                    new DataTableColumn() {
                        Data = "DateTime",
                        Name = "DateTime",
                        Searchable = true,
                        Orderable = true,
                    },
                    new DataTableColumn() {
                        Data = "Text",
                        Name = "Text",
                        Searchable = true,
                        Orderable = true,
                    },
                    new DataTableColumn() {
                        Data = "Number",
                        Name = "Number",
                        Searchable = true,
                        Orderable = true,
                    },
                    new DataTableColumn() {
                        Data = "Guid2",
                        Name = "Guid2",
                        Searchable = true,
                        Orderable = true,
                    },
                    new DataTableColumn() {
                        Data = "DateTime2",
                        Name = "DateTime2",
                        Searchable = true,
                        Orderable = true,
                    },
                    new DataTableColumn() {
                        Data = "Text2",
                        Name = "Text2",
                        Searchable = true,
                        Orderable = true,
                    },
                    new DataTableColumn() {
                        Data = "Number2",
                        Name = "Number2",
                        Searchable = true,
                        Orderable = true,
                    }
                },
                Search= new DataTableSearch()
                {
                    Value = "tex",
                    Regex = false
                }
            };
            var data = Enumerable.Repeat(new MyClass(), 100000);
            DateTime a = DateTime.Now;
            var test = data.ProcessDataTable(request);
            DateTime b = DateTime.Now;
            Console.WriteLine(b.Subtract(a).TotalMilliseconds);
            Console.ReadKey();
        }
    }
}
