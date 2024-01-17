using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace KitchenECSExplorer.Utils
{
    internal static class ReflectionUtils
    {
        public static List<Type> GetReferencedTypes<T>()
        {
            var assembly = AssemblyDefinition.ReadAssembly(typeof(T).Assembly.Location);
            var module = assembly.MainModule;

            // Load all referenced assemblies into the current AppDomain
            var referencedAssemblies = module.AssemblyReferences;
            foreach (var referencedAssembly in referencedAssemblies)
            {
                var assemblyName = referencedAssembly.Name;
                var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == assemblyName);
                if (loadedAssembly == null)
                {
                    Assembly.Load(assemblyName);
                }
            }

            var types = new List<Type>();

            // Get all types used in the class
            var type = module.GetType(typeof(T).FullName);
            types.AddRange(GetTypesUsedInType(type));

            // Get all types used in the types of all fields in the class
            foreach (var field in type.Fields)
            {
                var fieldType = field.FieldType.Resolve();
                types.AddRange(GetTypesUsedInType(fieldType));
            }

            return types.Distinct().ToList();
        }

        private static IEnumerable<Type> GetTypesUsedInType(TypeDefinition type)
        {
            var types = new List<Type>();

            // Get types used in methods
            foreach (var method in type.Methods)
            {
                types.AddRange(GetTypesUsedInMethod(method));
            }

            // Get types used in properties
            foreach (var property in type.Properties)
            {
                types.AddRange(GetTypesUsedInMethod(property.GetMethod));
                types.AddRange(GetTypesUsedInMethod(property.SetMethod));
            }

            // Get types used in events
            foreach (var @event in type.Events)
            {
                types.AddRange(GetTypesUsedInMethod(@event.AddMethod));
                types.AddRange(GetTypesUsedInMethod(@event.RemoveMethod));
            }

            // Get types used in nested types
            foreach (var nestedType in type.NestedTypes)
            {
                types.AddRange(GetTypesUsedInType(nestedType));
            }

            return types;
        }

        private static IEnumerable<Type> GetTypesUsedInMethod(MethodDefinition method)
        {
            var types = new List<Type>();

            // Get types used in the method's return type
            if (method.ReturnType != null)
            {
                var returnType = method.ReturnType.Resolve();
                types.AddRange(GetTypesUsedInType(returnType));
            }

            // Get types used in the method's parameters
            foreach (var parameter in method.Parameters)
            {
                var parameterType = parameter.ParameterType.Resolve();
                types.AddRange(GetTypesUsedInType(parameterType));
            }

            // Get types used in the method's local variables
            foreach (var variable in method.Body.Variables)
            {
                var variableType = variable.VariableType.Resolve();
                types.AddRange(GetTypesUsedInType(variableType));
            }

            // Get types used in the method's body instructions
            foreach (var instruction in method.Body.Instructions)
            {
                if (instruction.Operand is TypeReference typeReference)
                {
                    var type = Type.GetType(typeReference.FullName);
                    if (type != null)
                    {
                        types.Add(type);
                    }
                }
            }

            return types;
        }

        public static string GetReadableTypeName(Type type)
        {
            return GetReadableTypeName(type, true);
        }

        public static string GetReadableTypeName(Type type, bool useFullname = true)
        {
            string typeName = type.Name;

            if (type.IsGenericType)
            {
                typeName = Regex.Replace(typeName, @"`\d+", ""); // Remove generic arity
                typeName += "<" + string.Join(", ", type.GetGenericArguments().Select(GetReadableTypeName)) + ">";
            }
            typeName = typeName.Replace('+', '.');

            if (useFullname && type.DeclaringType != null)
            {
                typeName = $"{GetReadableTypeName(type.DeclaringType)}.{typeName}";
            }
            return typeName;
        }
    }
}