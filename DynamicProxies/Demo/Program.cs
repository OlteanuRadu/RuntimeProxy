using DynamicProxies;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.InterceptionExtension;
using PluginLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {

            //dynamic.AutoProxies Interception
            var container = new UnityContainer();
            container.RegisterType<IPlugin, MyPlugin>();
            container.RegisterType<IProxyFactory<IPlugin>, AutoProxyFactory<IPlugin>>();

           container
                      .Resolve<IProxyFactory<IPlugin>>()
                      .Build(container.Resolve<IPlugin>(),
                              null,
                              (state) => Console.WriteLine("This is a Pre-Action handler !"),
                              (state) => Console.WriteLine("This is a Post-Action handler !"),
                              (Exception e, object state) =>
                                    {
                                        Console.WriteLine(state);
                                        return ExceptionReturnTypes.RETRY;
                                    }).TestMethod2("Hello world", 42);


            //Unity Interception way;
            var container2 = new UnityContainer();
            container2.AddNewExtension<Interception>();
            container2.RegisterType<IPlugin, MyPlugin>(
                new Interceptor<InterfaceInterceptor>(),
                new InterceptionBehavior<MyPluginInterceptor>()
                );
            container2.Resolve<IPlugin>().TestMethod2("Hello world", 42);

            Console.ReadLine();
        }
    }

    public static class UnityExtenstions
    {
        public static IUnityContainer ContainerSetup(this IUnityContainer container) {
            container.RegisterType<IPlugin, MyPlugin>();
            container.RegisterType<IProxyFactory<IPlugin>, AutoProxyFactory<IPlugin>>();
            return container;
        }
    }
    public class MyPluginInterceptor : IInterceptionBehavior
    {
        public bool WillExecute => true;

        public IEnumerable<Type> GetRequiredInterfaces()
        {
            return Enumerable.Empty<Type>();
        }

        public IMethodReturn Invoke(IMethodInvocation input, GetNextInterceptionBehaviorDelegate getNext)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("This is a Pre-Action handler");

            var res = getNext()(input, getNext);

            Console.WriteLine("This is a Post-Action handler");
            return res;
        }
    }
}
