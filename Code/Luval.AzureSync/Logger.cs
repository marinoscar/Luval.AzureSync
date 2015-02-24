using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.AzureSync
{
    public interface ILogger
    {
        void WriteLine(string message);
        void WriteLine(string message, params object[] o);
        void Write(string message);
        void Write(string message, params object[] o);
    }

    public class NoLogger : ILogger
    {
        public void WriteLine(string message)
        {
        }
        public void WriteLine(string message, params object[] o)
        {
        }
        public void Write(string message)
        {
        }
        public void Write(string message, params object[] o)
        {
        }
    }

    public class DebugLogger : ILogger
    {
        public void WriteLine(string message)
        {
            Debug.WriteLine(message);
        }
        public void WriteLine(string message, params object[] o)
        {
            Debug.WriteLine(message, o);
        }
        public void Write(string message)
        {
            Debug.Write(message, null);
        }
        public void Write(string message, params object[] o)
        {
            Debug.Write(string.Format(message, o));
        }
    }

    public class ConsoleLogger : ILogger
    {
        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }
        public void WriteLine(string message, params object[] o)
        {
            Console.WriteLine(message, o);
        }
        public void Write(string message)
        {
            Console.Write(message);
        }
        public void Write(string message, params object[] o)
        {
            Console.Write(message, o);
        }
    }
}
