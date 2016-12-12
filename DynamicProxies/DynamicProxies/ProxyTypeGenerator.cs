using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace DynamicProxies
{
    public class ProxyTypeGenerator
    {
        AssemblyBuilder GetAssemblyBuilder()
        {
            string assemblyName = $"dynamic.AutoProxies.{Guid.NewGuid().ToString().Replace("-", "")}";
            AssemblyName name = new AssemblyName(assemblyName);
            var builder = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
            return builder;
        }
        ModuleBuilder BuildModule(AssemblyBuilder builder)
        {
            string moduleName = $"dynamic.AutoProxies.{Guid.NewGuid().ToString().Replace("-", "")}";
            var module = builder.DefineDynamicModule(moduleName);
            return module;
        }

        TypeBuilder BuildType<T>(ModuleBuilder builder)
        {

            Type baseType = typeof(T);
            Type parentType = typeof(ProxyBase<T>);

            string typeName = $"dynamic.AutoProxies.{baseType.Name}";

            Type[] interfaces = { baseType };
            var typebuilder = builder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class, parentType, interfaces);



            return typebuilder;
        }

        bool ImplementMethods<T>(TypeBuilder builder)
        {
            var baseType = typeof(T);
            Type parentType = typeof(ProxyBase<T>);

            var methods = baseType.GetMethods();

            foreach (var method in methods)
            {
                var methodBuilder = DefineMethod(builder, method);

                ProxyMethod(methodBuilder, method, parentType);

                builder.DefineMethodOverride(methodBuilder, method);
            }

            return true;
        }

        void ProxyMethod(MethodBuilder method, MethodInfo proxyMethod, Type parentType)
        {
            var gen = method.GetILGenerator();
            var preAction = parentType.GetProperty("PreAction");
            var postAction = parentType.GetProperty("PostAction");
            var exceptionActionProp = parentType.GetProperty("ExceptionAction");
            var preActionNoLabel = gen.DefineLabel();
            var postActionNoLabel = gen.DefineLabel();
            var returnLabel = gen.DefineLabel();
            var funcInvokeMI = typeof(Func<Exception,object, ExceptionReturnTypes>).GetMethod("Invoke");
            var actionInvokeMI = typeof(Func<object>).GetMethod("Invoke");
            var actionOfObjectInvokeMI = typeof(Action<object>).GetMethod("Invoke");
            var returnTypeIgnoreLabel = gen.DefineLabel();
            var continueWhileLoopLabel = gen.DefineLabel();
            var nullStateCreatorLabel = gen.DefineLabel();
            var stateCreatorProp = parentType.GetProperty("StateCreator");

            var whileLoopLocal= gen.DeclareLocal(typeof(bool));
            var exceptionLocal = gen.DeclareLocal(typeof(Exception));
            var whileLoopLabel = gen.DefineLabel();
            var rethrowLabel = gen.DefineLabel();
            var returnLocal = gen.DeclareLocal(typeof(ExceptionReturnTypes));
            var stateObject = gen.DeclareLocal(typeof(object));

            //execute StateCreator()
            gen.Emit(OpCodes.Ldarg_0); //load "this" on the stack;
            gen.Emit(OpCodes.Call, stateCreatorProp.GetGetMethod()); //obtain the Get StateCreator
            gen.Emit(OpCodes.Ldnull); //push null value on the evaluatian stack;
            gen.Emit(OpCodes.Cgt_Un); //compare with null
            gen.Emit(OpCodes.Brfalse_S, whileLoopLabel); //jump to label if not null
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Call, stateCreatorProp.GetGetMethod()); //obtain the Get StateCreator
            gen.Emit(OpCodes.Callvirt, actionInvokeMI);
            gen.Emit(OpCodes.Stloc, stateObject);

            //define a while loop
            gen.MarkLabel(whileLoopLabel);
            gen.Emit(OpCodes.Ldc_I4_1); 
            gen.Emit(OpCodes.Stloc, whileLoopLocal);

            var tryBlock = gen.BeginExceptionBlock();

            //execute PreAction
            gen.Emit(OpCodes.Ldarg_0); //load "this" on the stack;
            gen.Emit(OpCodes.Call, preAction.GetGetMethod()); //obtain the Get PreAction
            gen.Emit(OpCodes.Ldnull); //push null value on the evaluatian stack;
            gen.Emit(OpCodes.Cgt_Un); //compare with null
            gen.Emit(OpCodes.Brfalse_S, preActionNoLabel); //jump to label if null
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Call, preAction.GetGetMethod()); //obtain the Get PreAction
            gen.Emit(OpCodes.Ldloc, stateObject);
            gen.Emit(OpCodes.Callvirt, actionOfObjectInvokeMI); //execute PreAction 
            gen.MarkLabel(preActionNoLabel);

            //execute base method
            gen.Emit(OpCodes.Ldarg_0);// load "this" on the stack;
            gen.Emit(OpCodes.Ldfld, parentType
                                        .GetFields(BindingFlags.NonPublic |
                                                   BindingFlags.Instance)
                                        .FirstOrDefault(_ => _.Name == "_proxyObject")); //this._proxyObject; load on the stack the proxy obj;

            var parms = proxyMethod.GetParameters();
            int pIndex = 1;
            foreach (var parm in parms) {
                gen.Emit(OpCodes.Ldarg, pIndex++);
            }

            gen.EmitCall(OpCodes.Callvirt, proxyMethod, null);//this._proxyOject.ProxyMethod();

            //execute PostAction
            gen.Emit(OpCodes.Ldarg_0); //load "this" on the stack;
            gen.Emit(OpCodes.Call, postAction.GetGetMethod()); //obtain the Get PostAction
            gen.Emit(OpCodes.Ldnull); //push null value on the evaluatian stack;
            gen.Emit(OpCodes.Cgt_Un); //compare with null
            gen.Emit(OpCodes.Brfalse_S, postActionNoLabel); //jump to label if null
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Call, postAction.GetGetMethod()); //obtain the Get PostAction
            gen.Emit(OpCodes.Ldloc, stateObject);
            gen.Emit(OpCodes.Callvirt, actionOfObjectInvokeMI); //execute PostAction 
            gen.MarkLabel(postActionNoLabel);
            gen.Emit(OpCodes.Leave_S, returnLabel);

            gen.BeginCatchBlock(typeof(Exception));
            gen.Emit(OpCodes.Stloc, exceptionLocal);

            //execute ExceptionAction()
            gen.Emit(OpCodes.Ldarg_0); //load "this" on the stack;
            gen.Emit(OpCodes.Call, exceptionActionProp.GetGetMethod()); //obtain the Get PreAction
            gen.Emit(OpCodes.Ldnull); //push null value on the evaluatian stack;
            gen.Emit(OpCodes.Cgt_Un); //compare with null
            gen.Emit(OpCodes.Brfalse_S, rethrowLabel); //jump to label if null
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Call, exceptionActionProp.GetGetMethod()); //obtain the Get PreAction
            gen.Emit(OpCodes.Ldloc, exceptionLocal);
            gen.Emit(OpCodes.Ldloc, stateObject);
            gen.Emit(OpCodes.Callvirt, funcInvokeMI); //call  ExceptionAction()
            gen.Emit(OpCodes.Stloc, returnLocal);

            //if returnLocal === FAIL
            gen.Emit(OpCodes.Ldloc, returnLocal);
            gen.Emit(OpCodes.Ldc_I4,(Int32)ExceptionReturnTypes.FAIL);
            gen.Emit(OpCodes.Ceq);
            gen.Emit(OpCodes.Brfalse_S,returnTypeIgnoreLabel);
            gen.Emit(OpCodes.Rethrow);

            //if returnLocal === RETRY
            gen.MarkLabel(returnTypeIgnoreLabel);
            gen.Emit(OpCodes.Ldloc, returnLocal);
            gen.Emit(OpCodes.Ldc_I4, (Int32)ExceptionReturnTypes.RETRY);
            gen.Emit(OpCodes.Ceq);
            gen.Emit(OpCodes.Brfalse_S, continueWhileLoopLabel);
            gen.Emit(OpCodes.Leave_S,whileLoopLabel);
            gen.MarkLabel(continueWhileLoopLabel);
            gen.Emit(OpCodes.Leave_S, returnLabel);
            gen.MarkLabel(rethrowLabel);
            gen.Emit(OpCodes.Rethrow);
            gen.EndExceptionBlock();

            gen.MarkLabel(returnLabel);
            gen.Emit(OpCodes.Ret);// return;
        }

        MethodBuilder DefineMethod(TypeBuilder builder, MethodInfo info)
        {

            var returnType = info.ReturnType;
            var paramInfoList = info.GetParameters();
            var paramTypeList = paramInfoList.Select(_ => _.ParameterType).ToArray();

            var methodBuilder = builder.DefineMethod(info.Name, MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, info.CallingConvention, returnType, paramTypeList);
            return methodBuilder;
        }
        public Type Build<T>()
        {
            var builder = GetAssemblyBuilder();
            var module = BuildModule(builder);
            var typeBuilder = BuildType<T>(module);

            ImplementCtor<T>(typeBuilder);

            if (!ImplementMethods<T>(typeBuilder))
            {
                throw new InvalidOperationException("Can't proxy methods");
            }

            var type =  typeBuilder.CreateType();
            return type;
        }

        void ImplementCtor<T>(TypeBuilder builder)
        {
            var baseType = typeof(T);
            var parentType = typeof(ProxyBase<T>);
            var proxyObjectField = parentType
                                    .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                                    .FirstOrDefault(_ => _.Name == "_proxyObject");

            var ctorbuilder = builder
                            .DefineConstructor(MethodAttributes.Public, 
                                               CallingConventions.Standard |
                                               CallingConventions.HasThis, new Type[] { baseType });

            var gen = ctorbuilder.GetILGenerator();

            gen.Emit(OpCodes.Ldarg_0);// load this;
            gen.Emit(OpCodes.Ldarg_1);// load baseType object
            gen.Emit(OpCodes.Stfld, proxyObjectField);
            gen.Emit(OpCodes.Ret);
        }
    }
}
