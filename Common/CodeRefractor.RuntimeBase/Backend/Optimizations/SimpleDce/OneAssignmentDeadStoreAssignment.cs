#region Usings

using System.Collections.Generic;
using CodeRefractor.RuntimeBase.Analyze;
using CodeRefractor.RuntimeBase.Backend.Optimizations.Common;
using CodeRefractor.RuntimeBase.MiddleEnd;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations.Identifiers;

#endregion

namespace CodeRefractor.RuntimeBase.Backend.Optimizations.SimpleDce
{
    internal class OneAssignmentDeadStoreAssignment : ResultingInFunctionOptimizationPass
    {
        public override void OptimizeOperations(MethodInterpreter interpreter)
        {
            var useDef = interpreter.MidRepresentation.UseDef;
            var localOperations = useDef.GetLocalOperations();
            var constValues = GetAssignToConstOperations(localOperations, useDef);

            if (constValues.Count == 0)
                return;

            localOperations = useDef.GetLocalOperations();
            for (var index = 0; index < localOperations.Length; index++)
            {
                var op = localOperations[index];
                var variableUsages = useDef.GetUsages(index);
                if (variableUsages.Length == 0)
                    continue;
                foreach (var variable in variableUsages)
                {
                    ConstValue constMappedValue;
                    if (constValues.TryGetValue(variable, out constMappedValue))
                    {
                        op.SwitchUsageWithDefinition(variable, constMappedValue);
                        Result = true;
                    }
                }
            }
        }

        private Dictionary<LocalVariable, ConstValue> GetAssignToConstOperations(LocalOperation[] localOperations,
            UseDefDescription useDef)
        {
            var constValues = new Dictionary<LocalVariable, ConstValue>();
            var assignmentIds = useDef.GetOperationsOfKind(OperationKind.Assignment);
            foreach (var index in assignmentIds)
            {
                var op = localOperations[index];
                var assign = (Assignment) op.Value;
                var assignedTo = assign.AssignedTo;
                if (assignedTo.Kind == VariableKind.Argument)
                    continue;
                var constAssignedValue = assign.Right as ConstValue;
                if (constAssignedValue == null)
                    continue;
                constValues[assignedTo] = constAssignedValue;
            }
            for (var index = 0; index < localOperations.Length; index++)
            {
                var op = localOperations[index];
                var definition = useDef.GetDefinition(index);
                if (definition == null)
                    continue;
                if (op.Kind != OperationKind.Assignment)
                {
                    constValues.Remove(definition);
                    continue;
                }
                var assign = (Assignment) op.Value;
                var constAssignedValue = assign.Right as ConstValue;
                if (constAssignedValue == null)
                {
                    constValues.Remove(definition);
                    continue;
                }
                ConstValue constDefValue;
                if (!constValues.TryGetValue(definition, out constDefValue)) continue;
                if (constDefValue.Value != constAssignedValue.Value)
                    constValues.Remove(definition);
            }
            return constValues;
        }
    }
}