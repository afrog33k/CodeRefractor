#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations.Identifiers;

#endregion

namespace CodeRefractor.RuntimeBase
{
    public static class TypeNamerUtils
    {
        public const string StdSharedPtr = "std::shared_ptr";

        public static string ClangMethodSignature(this MethodBase method)
        {
            var mappedType = method.DeclaringType;
            var typeName = mappedType.ToCppMangling();
            var methodName = method.Name;
            if (method is ConstructorInfo)
                methodName = "ctor";
            return String.Format("{0}_{1}", typeName, methodName);
        }

        public static Type[] GetMethodArgumentTypes(this MethodBase method)
        {
            var resultList = new List<Type>();
            if (!method.IsStatic)
            {
                resultList.Add(method.DeclaringType);
            }
            var arguments = method.GetParameters();
            resultList.AddRange(arguments.Select(arg => arg.ParameterType));

            return resultList.ToArray();
        }


        public static string GetCommaSeparatedParameters(Type[] parameters)
        {
            return string.Join(",", parameters.Select(paramType => paramType.ToCppMangling()));
        }

        public static string ToCppMangling(this Type type, bool handleGenerics = false)
        {
            if (IsVoid(type)) return "void";
            var typesCount = 0;
            if (handleGenerics && type.IsGenericType)
            {
                typesCount = type.GetGenericArguments().Length;
            }
            if (type.IsGenericFieldUsage())
            {
                return string.Format("T{0}", type.GenericParameterPosition + 1);
            }
            return type.Name.ToCppMangling(type.Namespace,
                typesCount);
        }

        public static bool IsGenericFieldUsage(this Type type)
        {
            return type.ContainsGenericParameters && !type.IsGenericTypeDefinition;
        }

        public static string ToCppMangling(this string s, string nameSpace = "", int genericTypeCount = 0)
        {
            if (String.IsNullOrEmpty(s))
                return String.Empty;
            nameSpace = nameSpace ?? "";
            nameSpace = nameSpace.Replace(".", "_");
            if (s.Contains("`"))
            {
                s = s.Remove(s.IndexOf('`'));
            }
            if (genericTypeCount > 0)
            {
                var typeNames = new List<string>();
                for (var i = 1; i <= genericTypeCount; i++)
                {
                    typeNames.Add("T" + i);
                }
                s = string.Format("{0}<{1}>", s,
                    String.Join(", ", typeNames));
            }
            var fullName = nameSpace + "_" + s;
            if (s.EndsWith("[]"))
            {
                s = s.Remove(s.Length - 2, 2);
                fullName = String.Format("std::shared_ptr <Array<{0}_{1}> > &", nameSpace, s);
            }
            return fullName;
        }

        public static string ToDeclaredVariableType(this Type type, bool handleGenerics = true,
            EscapingMode isSmartPtr = EscapingMode.Smart)
        {
            if (type == null)
                return "void*";
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                var fullTypeName = elementType.ToCppName(handleGenerics);
                switch (isSmartPtr)
                {
                    case EscapingMode.Smart:
                        return String.Format(StdSharedPtr + "< Array < {0} > >", fullTypeName);
                    case EscapingMode.Pointer:
                        return String.Format("Array < {0} > *", fullTypeName);
                    case EscapingMode.Stack:
                        return String.Format("Array < {0} > ", fullTypeName);
                }
            }
            if (type.IsClass || isSmartPtr != EscapingMode.Smart)
            {
                if (type.IsPrimitive || type.IsValueType)
                    isSmartPtr = EscapingMode.Stack;
                if (type.Name.EndsWith("*") || type.Name.EndsWith("&"))
                {
                    var elementType = type.GetElementType();
                    var elementTypeCppName = elementType.ToCppName(handleGenerics);
                    return String.Format("{0}* ", elementTypeCppName);
                }
                var typeParameters = type.GetGenericArguments();
                if (typeParameters.Length != 0)
                {
                    var genericSpecializations = typeParameters.Select(
                        t => t.ToCppMangling()
                        ).ToList();
                    var genericSpecializationString = string.Join(", ", genericSpecializations);

                    switch (isSmartPtr)
                    {
                        case EscapingMode.Smart:
                            return String.Format(StdSharedPtr + "<{0} <{1}> >", type.ToCppMangling(),
                                genericSpecializationString);
                        case EscapingMode.Pointer:
                            return String.Format("{0} <{1}>*", type.ToCppMangling(), genericSpecializationString);
                        case EscapingMode.Stack:
                            return String.Format("{0} <{1}>", type.ToCppMangling(), genericSpecializationString);
                    }
                }
                switch (isSmartPtr)
                {
                    case EscapingMode.Smart:
                        return String.Format(StdSharedPtr + "<{0}>", type.ToCppMangling(handleGenerics));
                    case EscapingMode.Pointer:
                        return String.Format("{0} *", type.ToCppMangling());
                    case EscapingMode.Stack:
                        return String.Format("{0} ", type.ToCppMangling());
                }
            }
            if (!type.IsClass || isSmartPtr != EscapingMode.Smart)
            {
                return type.IsSubclassOf(typeof (Enum))
                    ? "int"
                    : type.Name.ToCppMangling(type.Namespace);
            }
            if (type.IsByRef)
            {
                var elementType = type.GetElementType();
                return String.Format("{0}*", elementType.ToCppMangling());
            }
            return String.Format(StdSharedPtr + "<{0}>", type.ToCppMangling());
        }

        public static string ToCppName(this Type type, bool handleGenerics = true,
            EscapingMode isSmartPtr = EscapingMode.Smart)
        {
            if (type == null)
                return "void*";
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                var fullTypeName = elementType.ToCppName(handleGenerics);
                switch (isSmartPtr)
                {
                    case EscapingMode.Smart:
                        return String.Format(StdSharedPtr + "< Array < {0} > >", fullTypeName);
                    case EscapingMode.Pointer:
                        return String.Format("Array < {0} > *", fullTypeName);
                    case EscapingMode.Stack:
                        return String.Format("Array < {0} > ", fullTypeName);
                }
            }
            if (type.IsClass || isSmartPtr != EscapingMode.Smart)
            {
                if (type.IsPrimitive || type.IsValueType)
                    isSmartPtr = EscapingMode.Stack;
                if (type.Name.EndsWith("*") || type.Name.EndsWith("&"))
                {
                    var elementType = type.GetElementType();
                    var elementTypeCppName = elementType.ToCppName(handleGenerics);
                    return String.Format("{0}* ", elementTypeCppName);
                }
                if (type.IsGenericFieldUsage())
                    isSmartPtr = EscapingMode.Stack;
                switch (isSmartPtr)
                {
                    case EscapingMode.Smart:
                        return String.Format(StdSharedPtr + "<{0}>", type.ToCppMangling(handleGenerics));
                    case EscapingMode.Pointer:
                        return String.Format("{0} *", type.ToCppMangling(handleGenerics));
                    case EscapingMode.Stack:
                        return String.Format("{0} ", type.ToCppMangling());
                }
            }
            if (!type.IsClass || isSmartPtr != EscapingMode.Smart)
            {
                return type.IsSubclassOf(typeof (Enum))
                    ? "int"
                    : type.Name.ToCppMangling(type.Namespace);
            }
            if (type.IsByRef)
            {
                var elementType = type.GetElementType();
                return String.Format("{0}*", elementType.ToCppMangling());
            }
            return String.Format(StdSharedPtr + "<{0}>", type.ToCppMangling());
        }

        public static string GetTypeTemplatePrefix(this int genericTypeCount)
        {
            var typeList = new List<string>();
            for (var i = 1; i <= genericTypeCount; i++)
            {
                typeList.Add(string.Format("class T{0}", i));
            }
            var result = string.Join(", ", typeList);
            return string.Format("template <{0}> ", result);
        }

        public static bool IsVoid(this Type type)
        {
            return type == typeof (void);
        }
    }
}