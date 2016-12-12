using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicProxies
{
    public class AutoProxyFactory<T> : IProxyFactory<T>
    {
        ProxyTypeGenerator _proxyGenerator;
        Type _cachedType = null;
        public AutoProxyFactory()
        {
            _proxyGenerator = new ProxyTypeGenerator();
        }
        public T Build(T proxyObject,
                       Func<object> stateCreator = null, 
                       Action<object> preAction= null,
                       Action<object> postAction = null,
                       Func<Exception,object,ExceptionReturnTypes> expcetionAction = null)
        {
            if (_cachedType == null)
            {
                _cachedType = _proxyGenerator.Build<T>();
            }

            var proxy =  (T)Activator.CreateInstance(_cachedType, proxyObject);

            ProxyBase<T> proxyBase = proxy as ProxyBase<T>;
            proxyBase.StateCreator = stateCreator;
            proxyBase.PreAction = preAction;
            proxyBase.PostAction = postAction;
            proxyBase.ExceptionAction = expcetionAction;
            return proxy;
        }
    }

    public interface IProxyFactory<T> {
        T Build(T proxyObject,
                  Func<object> stateCreator = null,
                  Action<object> preAction = null,
                  Action<object> postAction = null,
                  Func<Exception, object, ExceptionReturnTypes> expcetionAction = null);
    }
}
