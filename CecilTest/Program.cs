using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.PE;
using System.IO;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace CecilTest
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: CecilTest [directory containing Assembly-CSharp.dll]");
            }
            const string dllPath = @"Assembly-CSharp.dll";
            var inStream = File.Open(Path.Combine(args[0], dllPath), FileMode.Open, FileAccess.Read);
            var test = AssemblyDefinition.ReadAssembly(inStream);
            var testMod = test.MainModule;
            testMod.AssemblyReferences.Add(new AssemblyNameReference("HookRegistry", new Version(1, 0, 0, 0)));
            var hooker = new Hooker(testMod);
            var testTypes = testMod.Types.Where(t => t.Name.EndsWith("GameState"));
            foreach (var testType in testTypes)
            {
                hooker.AddHook(testType.Methods.First(m => m.Name.EndsWith("OnPowerHistory")));
            }
            
            test.Write("Assembly-CSharp.out.dll");
        }
    }

    class Hooker
    {
        public ModuleDefinition Module { get; private set; }

        readonly TypeReference rmhType;
        readonly MethodReference onCallMethod;

        public Hooker(ModuleDefinition module)
        {
            Module = module;

            Module.Import(typeof(HookRegistry.HookRegistry));
            rmhType = Module.Import(typeof(System.RuntimeMethodHandle));
            onCallMethod = Module.Import(
                typeof(HookRegistry.HookRegistry).GetMethods().First(mi => mi.Name.Contains("OnCall")));
        }

        public void AddHook(MethodDefinition method)
        {
            if (method.HasGenericParameters)
            {
                // check me
                throw new InvalidOperationException("Generic parameters not supported");
            }
            method.Body.Variables.Add(new VariableDefinition(Module.TypeSystem.Object));
            var numArgs = method.Parameters.Count;
            var hook = new List<Instruction>();
            hook.Add(Instruction.Create(OpCodes.Ldc_I4, numArgs + (method.IsStatic ? 1 : 2)));
            hook.Add(Instruction.Create(OpCodes.Newarr, Module.TypeSystem.Object));
            hook.Add(Instruction.Create(OpCodes.Stloc_0));

            hook.Add(Instruction.Create(OpCodes.Ldloc_0));
            hook.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            hook.Add(Instruction.Create(OpCodes.Ldtoken, method));
            hook.Add(Instruction.Create(OpCodes.Box, rmhType));
            hook.Add(Instruction.Create(OpCodes.Stelem_Ref));

            var i = 1;
            if (!method.IsStatic)
            {
                hook.Add(Instruction.Create(OpCodes.Ldloc_0));
                hook.Add(Instruction.Create(OpCodes.Ldc_I4, i));
                hook.Add(Instruction.Create(OpCodes.Ldarg_0));
                hook.Add(Instruction.Create(OpCodes.Stelem_Ref));
                i++;
            }

            foreach (var param in method.Parameters)
            {
                hook.Add(Instruction.Create(OpCodes.Ldloc_0));
                hook.Add(Instruction.Create(OpCodes.Ldc_I4, i));
                hook.Add(Instruction.Create(OpCodes.Ldarg_S, param));
                if (param.ParameterType.IsValueType)
                {
                    hook.Add(Instruction.Create(OpCodes.Box, param.ParameterType));
                }
                hook.Add(Instruction.Create(OpCodes.Stelem_Ref));
                i++;
            }
            hook.Add(Instruction.Create(OpCodes.Ldloc_0));
            hook.Add(Instruction.Create(OpCodes.Call, onCallMethod));
            hook.Add(Instruction.Create(OpCodes.Stloc_0));
            if (!method.ReturnType.FullName.EndsWith("Void"))
            {
                hook.Add(Instruction.Create(OpCodes.Ldloc_0));
                hook.Add(Instruction.Create(OpCodes.Ldnull));
                hook.Add(Instruction.Create(OpCodes.Ceq));
                hook.Add(Instruction.Create(OpCodes.Brtrue_S, method.Body.Instructions.First()));
                hook.Add(Instruction.Create(OpCodes.Ldloc_0));
                hook.Add(Instruction.Create(OpCodes.Castclass, method.ReturnType));
                hook.Add(Instruction.Create(OpCodes.Unbox_Any, method.ReturnType));
                hook.Add(Instruction.Create(OpCodes.Ret));
            }

            hook.Reverse();
            foreach (var inst in hook)
            {
                method.Body.Instructions.Insert(0, inst);
            }
            method.Body.OptimizeMacros();
        }
    }
}
