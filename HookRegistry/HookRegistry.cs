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
                if (method.DeclaringType.FullName.StartsWith("DatabaseLoaderTexture") && method.Name == "Load")
                {
                    return Test(args[1]);
                }
                else if (method.Name == "RemoveTexture")
                {
                    return new Nullable<bool>(true);
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
            objProp.SetValue(self, test_texture_info, null);
            var successProp = dblType.GetProperty("successful", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            successProp.SetValue(self, true, null);
            yield return true;
        }

        static Texture2D test_texture;
        static GameDatabase.TextureInfo test_texture_info;
        static HookRegistry()
        {
            test_texture = new Texture2D(2, 2);
            test_texture.SetPixel(0, 0, Color.green);
            test_texture.SetPixel(1, 1, Color.green);
            test_texture.SetPixel(0, 1, Color.magenta);
            test_texture.SetPixel(1, 0, Color.magenta);
            test_texture_info = new GameDatabase.TextureInfo(test_texture, false, true, false);
        }
    }
}
