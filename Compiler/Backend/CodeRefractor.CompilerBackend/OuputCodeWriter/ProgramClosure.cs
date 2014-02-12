#region Usings

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CodeRefactor.OpenRuntime;
using CodeRefractor.CompilerBackend.Optimizations.EscapeAndLowering;
using CodeRefractor.RuntimeBase;
using CodeRefractor.RuntimeBase.Analyze.TypeTableIndices;
using CodeRefractor.RuntimeBase.FrontEnd;
using CodeRefractor.RuntimeBase.MiddleEnd;

#endregion

namespace CodeRefractor.CompilerBackend.OuputCodeWriter
{
    public class ProgramClosure
    {
        public List<Type> UsedTypes;
        public MethodInterpreter EntryInterpreter { get; set; }
        public List<MethodInterpreter> MethodClosure = new List<MethodInterpreter>();

        public ProgramClosure(MethodInfo entryMethod)
        {
            EntryInterpreter = entryMethod.Register();

            BuildMethodClosure();
            MetaLinkerOptimizer.ApplyOptimizations(MethodClosure);
            BuildMethodClosure();
            UsedTypes = TypesClosureLinker.GetTypesClosure(MethodClosure);
            UsedTypes.Add(typeof (CrString));
            TypesClosureLinker.SortTypeClosure(UsedTypes);
        }

        private void BuildMethodClosure()
        {
            MethodClosure.Clear();
            MethodClosure.Add(EntryInterpreter);
            MetaLinker.Interpret(EntryInterpreter);

            var foundMethodCount = 1;
            var canContinue = true;
            while (canContinue)
            {
                var dependencies = EntryInterpreter.GetMethodClosure();
                canContinue = foundMethodCount != dependencies.Count;
                foundMethodCount = dependencies.Count;
                foreach (var interpreter in dependencies)
                {
                    MetaLinker.Interpret(interpreter);
                }
                MethodClosure = dependencies;
            }
        }

        public static void ComputeEscapeAnalysis(List<MethodInterpreter> methodClosures)
        {
            var escapeParameters = new AnalyzeParametersAreEscaping();
            var loweringVars = new InFunctionLoweringVars();
            for (var i = 0; i < 2; i++)
            {
                foreach (var methodInterpreter in methodClosures)
                {
                    escapeParameters.Optimize(methodInterpreter);
                }
                Parallel.ForEach(methodClosures, methodInterpreter =>
                    //foreach (var methodInterpreter in methodClosures)
                { loweringVars.Optimize(methodInterpreter); }
                    );
            }
        }

        public StringBuilder BuildFullSourceCode()
        {
            ComputeEscapeAnalysis(MethodClosure);

            return CppCodeGenerator.GenerateSourceStringBuilder(EntryInterpreter, UsedTypes, MethodClosure);
        }
    }
}