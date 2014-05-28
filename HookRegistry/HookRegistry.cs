using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace HookRegistry
{
    public class HookRegistry
    {
        public static object OnCall(params object[] args)
        {
            if (args.Length >= 1)
            {
                var rmh = (RuntimeMethodHandle)args[0];
                var method = MethodBase.GetMethodFromHandle(rmh);
                Debug.Log(String.Format("{0}.{1}(...)", method.DeclaringType.FullName, method.Name));
                if (method.DeclaringType.FullName == "DatabaseLoaderTexture_TGA" && method.Name == "Load")
                {
                    return Test(args[1]);
                }
                return null;
            }
            else
            {
                Debug.Log("this is not right");
                return new Texture2D(256, 256);
            }
        }

        static IEnumerator Test(object self)
        {
            var dblType = self.GetType();
            var objProp = dblType.GetProperty("obj", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            objProp.SetValue(self, new global::GameDatabase.TextureInfo(new Texture2D(1, 1), false, true, false), null);
            var successProp = dblType.GetProperty("successful", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            successProp.SetValue(self, true, null);
            yield return false;
        }
    }
}
