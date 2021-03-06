﻿#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using CodeRefractor.RuntimeBase;
using CodeRefractor.RuntimeBase.Backend.ComputeClosure;
using CodeRefractor.RuntimeBase.MiddleEnd;
using CodeRefractor.RuntimeBase.MiddleEnd.Methods;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations;

#endregion

namespace CodeRefractor.CompilerBackend.ProgramWideOptimizations.Virtual
{
    public class DevirtualizerIfOneImplemetor : ResultingProgramOptimizationBase
    {
        protected override void DoOptimize(ProgramClosure closure)
        {
            var methodInterpreters = closure.MethodClosure.Values
                .Where(m => m.Kind == MethodKind.Default)
                .ToList();
            foreach (var interpreter in methodInterpreters)
            {
                HandleInterpreterInstructions(interpreter, methodInterpreters, closure.UsedTypes);
            }
        }

        private void HandleInterpreterInstructions(MethodInterpreter interpreter,
            List<MethodInterpreter> methodInterpreters, List<Type> usedTypes)
        {
            var useDef = interpreter.MidRepresentation.UseDef;
            var calls = useDef.GetOperationsOfKind(OperationKind.CallVirtual).ToList();
            var allOps = useDef.GetLocalOperations();
            foreach (var callOp in calls)
            {
                var op = allOps[callOp];
                var methodData = (MethodData) op.Value;
                var callingInterpreterKey = methodData.Interpreter.ToKey();
                var declaringType = callingInterpreterKey.Interpreter.DeclaringType;
                var implementors = declaringType.ClrType.ImplementorsOfT(usedTypes);
                if (implementors.Count > 0)
                    continue;
                op.Kind = OperationKind.Call;
                Result = true;
            }
            if (Result)
            {
                interpreter.MidRepresentation.UpdateUseDef();
            }
        }
    }
}