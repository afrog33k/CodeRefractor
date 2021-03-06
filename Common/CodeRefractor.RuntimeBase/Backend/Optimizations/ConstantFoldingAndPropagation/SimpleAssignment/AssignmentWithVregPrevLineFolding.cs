#region Usings

using CodeRefractor.RuntimeBase.Analyze;
using CodeRefractor.RuntimeBase.Backend.Optimizations.Common;
using CodeRefractor.RuntimeBase.MiddleEnd;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations.Identifiers;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations.Operators;

#endregion

namespace CodeRefractor.RuntimeBase.Backend.Optimizations.ConstantFoldingAndPropagation.SimpleAssignment
{
    public class AssignmentWithVregPrevLineFolding : ResultingInFunctionOptimizationPass
    {
        public override void OptimizeOperations(MethodInterpreter interpreter)
        {
            var useDef = interpreter.MidRepresentation.UseDef;
            var operations = useDef.GetLocalOperations();
            var assigns = useDef.GetOperationsOfKind(OperationKind.Assignment);

            foreach (var index in assigns)
            {
                if(index==0)
                    return;
                var localOperation = operations[index];

                var localAssignment = localOperation.GetAssignment();
                var vregAssignment = localAssignment.Right as LocalVariable;

                if (vregAssignment == null || vregAssignment.Kind != VariableKind.Vreg) continue;

                var destOperation = operations[index - 1];
                var destOperationDefiniton = destOperation.GetDefinition();
                if (destOperationDefiniton == null || !destOperationDefiniton.Equals(vregAssignment)) continue;
                var localRight = (LocalVariable)localAssignment.Right;
                var usagesArr = operations.GetVariableUsages(localRight);
                if (usagesArr.Count != 1)
                {
                    return;
                }
                destOperation.SwitchUsageWithDefinition((LocalVariable) localAssignment.Right, localAssignment.AssignedTo);
                interpreter.MidRepresentation.LocalOperations.RemoveAt(index);
                Result = true;
                return;
            }
        }
    }
}