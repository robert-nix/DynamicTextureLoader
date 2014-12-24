using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace HookRegistry
{
    public class HookRegistry
    {
        public delegate object Callback(string typeName, string methodName, params object[] args);

        List<Callback> callbacks = new List<Callback>();

        public static object OnCall(params object[] args)
        {
            return HookRegistry.Get().Internal_OnCall(args);
        }

        public static void Register(Callback cb)
        {
            HookRegistry.Get().callbacks.Add(cb);
        }

        public static void Unregister(Callback cb)
        {
            HookRegistry.Get().callbacks.Remove(cb);
        }

        List<object> activeHooks = new List<object>();

        /// <summary>
        /// Loads all types marked with [RuntimeHook] and instantiates them with their 0-argument constructor.  The
        /// called constructor must call HookRegistry.Register() to receive intercepted method calls.
        /// </summary>
        void LoadRuntimeHooks()
        {
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                var hooks = type.GetCustomAttributes(typeof(RuntimeHookAttribute), false);
                if (hooks != null && hooks.Length > 0)
                {
                    activeHooks.Add(type.GetConstructor(Type.EmptyTypes).Invoke(new object[0]));
                }
            }
        }

        object Internal_OnCall(params object[] args)
        {
            if (args.Length >= 1)
            {
                var rmh = (RuntimeMethodHandle)args[0];
                var method = MethodBase.GetMethodFromHandle(rmh);
                var typeName = method.DeclaringType.FullName;
                var methodName = method.Name;
                Debug.Log(String.Format("{0}.{1}(...)", typeName, methodName));
                foreach (var cb in callbacks)
                {
                    var o = cb(typeName, methodName, args.Slice(1));
                    if (o != null)
                    {
                        return o;
                    }
                }
                return null;
            }
            else
            {
                Debug.Log("OnCall needs RMH+args");
                return null;
            }
        }

        static HookRegistry _instance;

        public static HookRegistry Get()
        {
            if (_instance == null)
            {
                _instance = new HookRegistry();
                _instance.LoadRuntimeHooks();
            }
            return _instance;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class RuntimeHookAttribute : Attribute
    {
    }
}
