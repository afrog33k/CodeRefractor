#region Uses

using System;
using System.Collections.Generic;
using System.Linq;
using CodeRefractor.CodeWriter.Linker;
using CodeRefractor.RuntimeBase.FrontEnd;
using CodeRefractor.RuntimeBase.MiddleEnd;
using CodeRefractor.RuntimeBase.MiddleEnd.Methods;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations;
using CodeRefractor.RuntimeBase.Runtime;

#endregion

namespace CodeRefractor.CompilerBackend.OuputCodeWriter.ComputeClosure
{
    public static class MetaLinkerClosureComputing
    {
        public static List<MethodInterpreter> GetMethodClosure(this MethodInterpreter entryPoints)
        {
            return GetClosureDictionary(entryPoints).Values.ToList();
        }

        private static Dictionary<MethodInterpreterKey, MethodInterpreter> GetClosureDictionary(this MethodInterpreter entryPoints)
        {
            var result = new Dictionary<MethodInterpreterKey, MethodInterpreter> { { entryPoints.ToKey(), entryPoints } };
            UpdateMethodEntryClosure(entryPoints, result);
            return result;
        }


        public static List<Type> ImplementorsOfT(this Type t, IEnumerable<Type> usedTypes)
        {
            var result = usedTypes.Where(usedType => usedType.IsSubclassOf(t)).ToList();
            return result;
        } 
        public static List<MethodInterpreter> GetMultiMethodsClosure(this List<MethodInterpreter> entryPoints)
        {
            var results = new List<Dictionary<MethodInterpreterKey, MethodInterpreter>>();
            foreach (var interpreter in entryPoints)
            {
                var result = GetClosureDictionary(interpreter);
                results.Add(result);
            }
            var finalResult = new Dictionary<MethodInterpreterKey, MethodInterpreter>();
            foreach (var result in results)
            {
                foreach (var entry in result)
                {
                    finalResult[entry.Key] = entry.Value;
                }
            }
            return finalResult.Values.ToList();
        }

        public static void UpdateMethodEntryClosure(MethodInterpreter entryPoint,
            Dictionary<MethodInterpreterKey, MethodInterpreter> result)
        {
            var useDef = entryPoint.MidRepresentation.UseDef;
            var ops = useDef.GetLocalOperations();
            var callList = useDef.GetOperationsOfKind(OperationKind.Call).ToList();
            callList.AddRange(useDef.GetOperationsOfKind(OperationKind.CallVirtual));
            callList.AddRange(useDef.GetOperationsOfKind(OperationKind.CallInterface));
            var localOperations = callList.Select(i=>ops[i]).ToArray();
            var toAdd = HandleCallInstructions(result, localOperations);
            var funcList = useDef.GetOperationsOfKind(OperationKind.LoadFunction).ToList();
            localOperations = funcList.Select(i => ops[i]).ToArray();
            HandleLoadFunctionInstructions(result, localOperations, toAdd);

            HandleTypeInitializers(result, toAdd);

            HandleGenerics(result, toAdd);
        }

        private static void HandleGenerics(Dictionary<MethodInterpreterKey, MethodInterpreter> result, List<MethodInterpreter> toAdd)
        {
            foreach (var interpreter in toAdd)
            {
                if (interpreter.IsGenericDeclaringType())
                {
                    //var genericSpecialization = interpreter.Clone();
                    //genericSpecialization.Specialize();
                    UpdateMethodEntryClosure(interpreter, result);
                }
                else
                    UpdateMethodEntryClosure(interpreter, result);
            }
        }

        private static void HandleTypeInitializers(Dictionary<MethodInterpreterKey, MethodInterpreter> result,
            List<MethodInterpreter> toAdd)
        {
            foreach (var it in toAdd)
            {
                var declaringType = it.Method.DeclaringType;
                if (declaringType == null)
                    continue;
                declaringType = declaringType.GetReversedType();
                if (declaringType.TypeInitializer == null) continue;
                var info = declaringType.TypeInitializer;
                
                var interpreter = info.GetInterpreter();
                if(interpreter==null)
                    continue;
                result[interpreter.ToKey()] = interpreter;
                UpdateMethodEntryClosure(interpreter, result);
            }
        }

        private static void HandleLoadFunctionInstructions(Dictionary<MethodInterpreterKey, MethodInterpreter> result,
            LocalOperation[] localOperations, List<MethodInterpreter> toAdd)
        {
            if (localOperations.Length == 0)
                return;
            foreach (var localOperation in localOperations)
            {
                var functionPointer = (FunctionPointerStore) localOperation.Value;
                var info = functionPointer.FunctionPointer;
                var interpreter = info.GetInterpreter();
                if (interpreter == null)
                    continue;
                result[interpreter.ToKey()] = interpreter;
                toAdd.Add(interpreter);
            }
        }

        private static List<MethodInterpreter> HandleCallInstructions(Dictionary<MethodInterpreterKey, MethodInterpreter> result,
            LocalOperation[] localOperations)
        {
            var toAdd = new List<MethodInterpreter>();
            if (localOperations.Length == 0)
                return toAdd;
            foreach (var localOperation in localOperations)
            {
                var methodData = (MethodData) localOperation.Value;
                var info = methodData.Info;
                if (info.DeclaringType == typeof (object)
                    || info.DeclaringType == typeof (IntPtr))
                    continue;
                var descInfo = methodData.Interpreter.ToKey();
                if (result.ContainsKey(descInfo))
                    continue;
                var interpreter = info.Register();
                if (interpreter == null)
                    continue;
                var isGenericDeclaringType = interpreter.IsGenericDeclaringType();
                if (isGenericDeclaringType)
                {
                    interpreter.Specialize();
                }
                var interpreterExists = result.ContainsKey(descInfo);
                if (!interpreterExists)
                {
                    result[descInfo] = interpreter;
                    UpdateMethodEntryClosure(interpreter, result);
                }

                toAdd.Add(interpreter);
            }
            return toAdd;
        }
    }
}