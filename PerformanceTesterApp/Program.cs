using SimplifiedDataTableServersideProcessingNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceTesterApp
{
    public class MyClass
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
            DateTime = DateTime.UtcNow;
            Text = "Text";
            Text = "Text2";
        }
    }
    public class Program
    {
        public static void Main()
        {
            
            var data = Enumerable.Repeat(new MyClass(), 100000);
            DateTime a = DateTime.Now;
            var test = data.SerializeData();
            DateTime b = DateTime.Now;
            Console.WriteLine(b.Subtract(a).TotalMilliseconds);
            Console.ReadKey();
        }
    }
}
