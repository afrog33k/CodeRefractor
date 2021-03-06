#region Usings

using CodeRefractor.RuntimeBase.Optimizations;
using CodeRefractor.RuntimeBase.Runtime;

#endregion

namespace CodeRefractor.RuntimeBase.Backend.Optimizations.Common
{
    public abstract class ResultingInFunctionOptimizationPass : ResultingOptimizationPass
    {
        public static CrRuntimeLibrary Runtime { get; set; }

        public ResultingInFunctionOptimizationPass()
            : base(OptimizationKind.InFunction)
        {
        }
    }
}