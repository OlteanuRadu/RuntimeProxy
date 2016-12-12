using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginLib
{
    public class MyPlugin :IPlugin 
    {
        public string TestProperty { get; set; }

        public void TestMethod()
        {
            //Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Hello from {nameof(MyPlugin)}");
        }

        public void TestMethod2(string data1, int data2) {
           // throw new Exception("");
            Console.WriteLine(data1 + " " + data2);
        }
        public string TestMethod3(string data) {
            return data;
        }
    }

    public interface IPlugin
    {
        void TestMethod();
        string TestMethod3(string data);
        void TestMethod2(string data1, int data2);
        string TestProperty { get; set; }
    }
}
