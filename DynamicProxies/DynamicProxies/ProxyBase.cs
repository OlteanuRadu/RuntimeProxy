using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicProxies
{

    public enum ExceptionReturnTypes
    {
        FAIL,
        IGNORE,
        RETRY
    }
    public class ProxyBase<T>
    {
        protected T _proxyObject;
        public Action<object> PreAction { get; set; }
        public Action<object> PostAction { get; set; }
        public Func<object> StateCreator { get; set; }
        public Func<Exception,object,ExceptionReturnTypes> ExceptionAction { get; set; }
    }
}
