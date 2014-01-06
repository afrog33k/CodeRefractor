﻿using CodeRefractor.CompilerBackend.Optimizations.Common;
using CodeRefractor.RuntimeBase.MiddleEnd;
using CodeRefractor.RuntimeBase.MiddleEnd.Methods;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations;
using CodeRefractor.CompilerBackend.Linker;

namespace CodeRefractor.CompilerBackend.Optimizations.Purity
{
    public class AnalyzeFunctionPurity : ResultingGlobalOptimizationPass
    {
        public static bool ReadPurity(MethodInterpreter intermediateCode)
        {
            return intermediateCode.MidRepresentation.GetProperties().IsPure;
        }
        public override void OptimizeOperations(MethodInterpreter intermediateCode)
        {
            if (ReadPurity(intermediateCode))
                return;
            var functionIsPure = ComputeFunctionPurity(intermediateCode);
            if (!functionIsPure) return;
            intermediateCode.MidRepresentation.GetProperties().IsPure = true;
            Result = true;
        }

        public static bool ComputeFunctionPurity(MethodInterpreter intermediateCode)
        {
            if(intermediateCode==null)
                return false;
            var operations = intermediateCode.MidRepresentation.LocalOperations;
            foreach (var localOperation in operations)
            {
                switch (localOperation.Kind)
                {
                    case OperationKind.SetStaticField:
                    case OperationKind.GetStaticField:
                    case OperationKind.CallRuntime:
                    case OperationKind.SetField:
                        return false;
                        
                    case OperationKind.Call:
                        var operationData = (MethodData)localOperation.Value;
                        var readPurity = LinkerInterpretersTableUtils.ReadPurity(operationData.Info);
                        if (!readPurity)
                            return false;
                        break;

                    case OperationKind.BranchOperator:
                    case OperationKind.AlwaysBranch:
                    case OperationKind.UnaryOperator:
                    case OperationKind.BinaryOperator:
                    case OperationKind.Assignment:
                    case OperationKind.Label:
                    case OperationKind.Return:
                        break;
                    default:
                        return false;
                }
            }
            return true;
        }
    }
}