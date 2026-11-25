using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace APC.UI.Tools
{
    /// <summary>어셈블리의 public/protected API를 Markdown 한 장으로 요약한다(docs/api.md).</summary>
    public static class ApiWriter
    {
        private const BindingFlags Declared =
            BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public static string Write(string dllPath)
        {
            var asm = AssemblyLoader.Load(dllPath);
            var docs = LoadDocs(Path.ChangeExtension(dllPath, ".xml"));
            var sb = new StringBuilder();
            sb.Append("# ").Append(asm.GetName().Name).Append(" API (v").Append(asm.GetName().Version).Append(")\n\n");
            sb.Append("> build.sh가 자동 생성한다. 직접 고치지 않는다.\n");

            var types = asm.GetExportedTypes().OrderBy(x => x.FullName, StringComparer.Ordinal).ToArray();
            // 목차: 앞부분만 읽혀도 전체 형식 목록이 보이게. 필요한 절은 `grep -n "^## 이름"`으로 찾는다.
            sb.Append("\n목차: ").Append(string.Join(" · ", types.Select(x => x.Name).ToArray())).Append('\n');
            foreach (var t in types)
                WriteType(sb, t, docs);
            return sb.ToString();
        }

        private static void WriteType(StringBuilder sb, Type t, Dictionary<string, string> docs)
        {
            sb.Append("\n## ").Append(t.Name).Append("\n\n");
            sb.Append('`').Append(t.Namespace).Append("` · ").Append(Kind(t));
            if (t.IsClass && t.BaseType != null && t.BaseType != typeof(object)) sb.Append(" : ").Append(TypeName(t.BaseType));
            sb.Append('\n');
            AppendSummary(sb, docs, "T:" + t.FullName, "\n");
            sb.Append('\n');

            if (t.IsEnum)
            {
                sb.Append("- ").Append(string.Join(", ", Enum.GetNames(t))).Append('\n');
                return;
            }

            var lines = new List<KeyValuePair<string, string>>();
            foreach (var m in t.GetMembers(Declared))
            {
                string sig = Signature(m);
                if (sig != null) lines.Add(new KeyValuePair<string, string>(sig, DocId(m)));
            }
            if (lines.Count == 0) return;
            foreach (var l in lines.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                sb.Append("- `").Append(l.Key).Append('`');
                AppendSummary(sb, docs, l.Value, " — ");
                sb.Append('\n');
            }
        }

        private static void AppendSummary(StringBuilder sb, Dictionary<string, string> docs, string id, string prefix)
        {
            string s;
            if (id != null && docs.TryGetValue(id, out s)) sb.Append(prefix).Append(s);
        }

        private static string Kind(Type t)
        {
            if (t.IsEnum) return "enum";
            if (t.IsInterface) return "interface";
            if (t.IsValueType) return "struct";
            if (typeof(Delegate).IsAssignableFrom(t)) return "delegate";
            if (t.IsAbstract && t.IsSealed) return "static class";
            return "class";
        }

        private static bool Visible(MethodBase m)
        {
            return m != null && (m.IsPublic || m.IsFamily || m.IsFamilyOrAssembly);
        }

        private static string Access(bool isPublic) { return isPublic ? "" : "protected "; }

        /// <summary>문서에 실을 멤버면 C# 형태 시그니처, 아니면 null.</summary>
        private static string Signature(MemberInfo m)
        {
            var ctor = m as ConstructorInfo;
            if (ctor != null)
                return Visible(ctor) && !ctor.IsStatic ? Access(ctor.IsPublic) + m.DeclaringType.Name + "(" + Params(ctor) + ")" : null;

            var method = m as MethodInfo;
            if (method != null)
            {
                if (!Visible(method) || method.IsSpecialName) return null;
                if (method.GetBaseDefinition().DeclaringType != method.DeclaringType) return null; // 오버라이드(OnPaint 등)는 사용자 API가 아니다
                return Access(method.IsPublic) + (method.IsStatic ? "static " : "") + TypeName(method.ReturnType) + " " + method.Name + "(" + Params(method) + ")";
            }

            var prop = m as PropertyInfo;
            if (prop != null)
            {
                var get = prop.GetGetMethod(true);
                var set = prop.GetSetMethod(true);
                bool g = Visible(get), s = Visible(set);
                if (!g && !s) return null;
                var any = g ? get : set;
                return Access(any.IsPublic) + (any.IsStatic ? "static " : "") + TypeName(prop.PropertyType) + " " + prop.Name
                    + " { " + (g ? "get; " : "") + (s ? "set; " : "") + "}";
            }

            var ev = m as EventInfo;
            if (ev != null)
            {
                var add = ev.GetAddMethod(true);
                return Visible(add) ? Access(add.IsPublic) + (add.IsStatic ? "static " : "") + "event " + TypeName(ev.EventHandlerType) + " " + ev.Name : null;
            }

            var field = m as FieldInfo;
            if (field != null)
            {
                if (field.IsSpecialName || !(field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly)) return null;
                string mod = field.IsLiteral ? "const " : (field.IsStatic ? "static " : "") + (field.IsInitOnly ? "readonly " : "");
                return Access(field.IsPublic) + mod + TypeName(field.FieldType) + " " + field.Name;
            }
            return null;
        }

        private static string Params(MethodBase m)
        {
            return string.Join(", ", m.GetParameters().Select(p =>
            {
                string s = (p.ParameterType.IsByRef ? (p.IsOut ? "out " : "ref ") : "")
                    + (p.IsDefined(typeof(ParamArrayAttribute), false) ? "params " : "")
                    + TypeName(p.ParameterType) + " " + p.Name;
                if (p.IsOptional) s += " = " + Literal(p.RawDefaultValue);
                return s;
            }).ToArray());
        }

        private static string Literal(object v)
        {
            if (v == null || v == DBNull.Value) return "null";
            if (v is string) return "\"" + v + "\"";
            if (v is bool) return (bool)v ? "true" : "false";
            return Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static readonly Dictionary<Type, string> Aliases = new Dictionary<Type, string>
        {
            { typeof(void), "void" }, { typeof(object), "object" }, { typeof(string), "string" }, { typeof(bool), "bool" },
            { typeof(int), "int" }, { typeof(long), "long" }, { typeof(float), "float" }, { typeof(double), "double" },
            { typeof(byte), "byte" }, { typeof(char), "char" }, { typeof(decimal), "decimal" },
        };

        private static string TypeName(Type t)
        {
            if (t.IsByRef) return TypeName(t.GetElementType());
            if (t.IsArray) return TypeName(t.GetElementType()) + "[]";
            string alias;
            if (Aliases.TryGetValue(t, out alias)) return alias;
            if (t.IsGenericType)
            {
                var def = t.GetGenericTypeDefinition();
                if (def == typeof(Nullable<>)) return TypeName(t.GetGenericArguments()[0]) + "?";
                string name = def.Name.Substring(0, def.Name.IndexOf('`'));
                return name + "<" + string.Join(", ", t.GetGenericArguments().Select(TypeName).ToArray()) + ">";
            }
            return t.Name;
        }

        // ---- XML 문서 주석 ----

        private static Dictionary<string, string> LoadDocs(string xmlPath)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!File.Exists(xmlPath)) return map;
            foreach (var member in XDocument.Load(xmlPath).Descendants("member"))
            {
                var summary = member.Element("summary");
                var name = (string)member.Attribute("name");
                if (summary == null || name == null) continue;
                // <see cref="T:Ns.Type.Member"/>는 Value에서 빠지므로 끝 이름으로 바꿔 넣는다.
                foreach (var see in summary.Descendants().Where(e => e.Name == "see" || e.Name == "paramref").ToList())
                {
                    string target = (string)see.Attribute("cref") ?? (string)see.Attribute("name") ?? (string)see.Attribute("langword") ?? "";
                    target = Regex.Replace(target, @"^\w:|\(.*$", "");
                    see.ReplaceWith(target.Substring(target.LastIndexOf('.') + 1));
                }
                map[name] = Regex.Replace(summary.Value, @"\s+", " ").Trim();
            }
            return map;
        }

        private static string DocId(MemberInfo m)
        {
            string owner = m.DeclaringType.FullName;
            var mb = m as MethodBase;
            if (mb != null)
            {
                string name = m is ConstructorInfo ? "#ctor" : m.Name;
                var ps = mb.GetParameters();
                return "M:" + owner + "." + name + (ps.Length == 0 ? "" : "(" + string.Join(",", ps.Select(p => DocType(p.ParameterType)).ToArray()) + ")");
            }
            if (m is PropertyInfo) return "P:" + owner + "." + m.Name;
            if (m is EventInfo) return "E:" + owner + "." + m.Name;
            if (m is FieldInfo) return "F:" + owner + "." + m.Name;
            return null;
        }

        private static string DocType(Type t)
        {
            if (t.IsByRef) return DocType(t.GetElementType()) + "@";
            if (t.IsArray) return DocType(t.GetElementType()) + "[]";
            if (t.IsGenericType)
            {
                string def = t.GetGenericTypeDefinition().FullName;
                return def.Substring(0, def.IndexOf('`')) + "{" + string.Join(",", t.GetGenericArguments().Select(DocType).ToArray()) + "}";
            }
            return t.FullName;
        }
    }
}
