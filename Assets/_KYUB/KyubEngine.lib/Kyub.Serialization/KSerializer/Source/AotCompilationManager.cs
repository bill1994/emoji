using System;
using System.Collections.Generic;
using System.Text;
using Kyub.Serialization.Internal;

namespace Kyub.Serialization {
    /// <summary>
    /// The AOT compilation manager 
    /// </summary>
    public class AotCompilationManager {
        /// <summary>
        /// Ahead of time compilations that are available. The type maps to the object type the generated converter
        /// will serialize/deserialize, and the string is the text content for a converter that will do the serialization.
        /// <para />
        /// The generated serializer is completely independent and you don't need to do anything. Simply add the file to
        /// your project and it'll get used instead of the reflection based one.
        /// </summary>
        public static Dictionary<Type, string> AvailableAotCompilations {
            get {
                for (int i = 0; i < _uncomputedAotCompilations.Count; ++i) {
                    var item = _uncomputedAotCompilations[i];
                    _computedAotCompilations[item.Type] = GenerateDirectConverterForTypeInCSharp(item.Type, item.Members, item.IsConstructorPublic);
                }
                _uncomputedAotCompilations.Clear();

                return _computedAotCompilations;
            }
        }
        private static Dictionary<Type, string> _computedAotCompilations = new Dictionary<Type, string>();

        private struct AotCompilation {
            public Type Type;
            public MetaProperty[] Members;
            public bool IsConstructorPublic;
        }
        private static List<AotCompilation> _uncomputedAotCompilations = new List<AotCompilation>();

        /// <summary>
        /// This is a helper method that makes it simple to run an AOT compilation on the given type.
        /// </summary>
        /// <param name="type">The type to perform the AOT compilation on.</param>
        /// <param name="aotCompiledClassInCSharp">The AOT class. Add this C# code to your project.</param>
        /// <returns>True if AOT compilation was successful.</returns>
        public static bool TryToPerformAotCompilation(Type type, SerializerConfig p_config, out string aotCompiledClassInCSharp) {
            if (MetaTypeCache.GetInGlobalCache(type, p_config).EmitAotData()) {
                aotCompiledClassInCSharp = AvailableAotCompilations[type];
                return true;
            }

            aotCompiledClassInCSharp = default(string);
            return false;
        }

        /// <summary>
        /// Adds a new AOT compilation unit.
        /// </summary>
        /// <param name="type">The type of object we are AOT compiling.</param>
        /// <param name="members">The members on the object which will be serialized/deserialized.</param>
        public static void AddAotCompilation(Type type, MetaProperty[] members, bool isConstructorPublic) {
            _uncomputedAotCompilations.Add(new AotCompilation {
                Type = type,
                Members = members,
                IsConstructorPublic = isConstructorPublic
            });
        }

        /// <summary>
        /// AOT compiles the object (in C#).
        /// </summary>
        private static string GenerateDirectConverterForTypeInCSharp(Type type, MetaProperty[] members, bool isConstructorPublic) {
            var sb = new StringBuilder();
            string typeName = type.CSharpName(/*includeNamespace:*/ true);
            string typeNameSafeDecl = type.CSharpName(true, true);

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine("namespace Kyub.Serialization {");
            sb.AppendLine("    partial class ConverterRegister {");
            sb.AppendLine("        public static Speedup." + typeNameSafeDecl + "_DirectConverter " + "Register_" + typeNameSafeDecl + ";");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("namespace Kyub.Serialization.Speedup {");
            sb.AppendLine("    public class " + typeNameSafeDecl + "_DirectConverter : DirectConverter<" + typeName + "> {");
            sb.AppendLine("        protected override Result DoSerialize(" + typeName + " model, Dictionary<string, Data> serialized) {");
            sb.AppendLine("            var result = Result.Success;");
            sb.AppendLine();
            foreach (var member in members) {
                sb.AppendLine("            result += SerializeMember(serialized, \"" + member.JsonName + "\", model." + member.MemberName + ");");
            }
            sb.AppendLine();
            sb.AppendLine("            return result;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        protected override Result DoDeserialize(Dictionary<string, Data> data, ref " + typeName + " model) {");
            sb.AppendLine("            var result = Result.Success;");
            sb.AppendLine();
            for (int i = 0; i < members.Length; ++i) {
                var member = members[i];
                sb.AppendLine("            var t" + i + " = model." + member.MemberName + ";");
                sb.AppendLine("            result += DeserializeMember(data, \"" + member.JsonName + "\", out t" + i + ");");
                sb.AppendLine("            model." + member.MemberName + " = t" + i + ";");
                sb.AppendLine();
            }
            sb.AppendLine("            return result;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public override object CreateInstance(Data data, Type storageType) {");
            if (isConstructorPublic) {
                sb.AppendLine("            return new " + typeName + "();");
            }
            else {
                sb.AppendLine("            return Activator.CreateInstance(typeof(" + typeName + "), /*nonPublic:*/true);");
            }
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}