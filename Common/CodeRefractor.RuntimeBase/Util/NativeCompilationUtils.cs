#region Usings

using System;
using System.IO;
using CodeRefractor.RuntimeBase.DataBase.SerializeXml;

#endregion

namespace CodeRefractor.RuntimeBase.Util
{
    public static class NativeCompilationUtils
    {
        public static Options CompilerOptions = new GccOptions();
        private class ClangOptions : Options
        {
            public ClangOptions()
            {
                PathOfCompilerTools = @"C:\Oss\Llvm\3_4_svn\bin\";
                CompilerExe = "clang++.exe";
                OptimizationFlags = "-O2 ";
            }
        }

        private class GccOptions : Options
        {
            public GccOptions()
            {
                PathOfCompilerTools = @"C:\Dev-Cpp\MinGW64\bin\";
                CompilerExe = "g++.exe";
                OptimizationFlags = "-Ofast -fomit-frame-pointer -ffast-math -std=c++11 -static-libgcc ";
                LinkerOptions = "";
            }
        }

        private class WindowsClOptions : Options
        {
            public WindowsClOptions()
            {
                PathOfCompilerTools = @"C:\Program Files (x86)\Microsoft Visual Studio 11.0\VC\bin\";
                CompilerExe = "cl.exe";
                OptimizationFlags = "";
                LinkerOptions = "";
            }
        }

        [XNode]
        public class Options
        {
            public string CompilerKind;
            public string PathOfCompilerTools;
            public string CompilerExe;
            public string OptimizationFlags;

            public string LinkerOptions;
        }


        public static void SetCompilerOptions(string compilerKind)
        {
            switch (compilerKind)
            {
                case "gcc":
                    CompilerOptions = new GccOptions();
                    break;
                case "clang":
                    CompilerOptions = new ClangOptions();
                    break;
                case "msvc":
                    CompilerOptions = new WindowsClOptions();
                    break;
            }
        }

        public static void CompileAppToNativeExe(string outputCpp, string applicationNativeExe)
        {
            var fileInfo = new FileInfo(outputCpp);
            if (!fileInfo.Exists)
            {
                throw new InvalidDataException(string.Format("Filename: {0} does not exist!", outputCpp));
            }
            outputCpp = fileInfo.FullName;
            var pathToGpp = CompilerOptions.PathOfCompilerTools + CompilerOptions.CompilerExe;

            var commandLineFormat = "{0} " + CompilerOptions.OptimizationFlags + " {2}";

            var arguments = String.Format(commandLineFormat, outputCpp, applicationNativeExe,
                CompilerOptions.LinkerOptions);
            var standardOutput = pathToGpp.ExecuteCommand(arguments, CompilerOptions.PathOfCompilerTools);
            if (!String.IsNullOrWhiteSpace(standardOutput))
            {
                throw new InvalidOperationException(String.Format("Errors when compiling: {0}", standardOutput));
            }
        }
    }
}