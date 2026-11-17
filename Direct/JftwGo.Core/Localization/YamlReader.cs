using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JftwGo.Core.Localization
{
    /// <summary>
    /// 极简 YAML 读取器：只解析本软件语言包所用的两层结构：
    ///     key: value
    ///     group:
    ///       key: value
    /// 不引入第三方依赖，容错处理注释、空行、引号、转义（\n \t \" \\）。
    /// </summary>
    internal static class YamlReader
    {
        public static Dictionary<string, string> Parse(string text)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (text == null) return result;

            string currentGroup = "";
            using (var sr = new StringReader(text))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string trimmed = line.TrimStart();
                    if (trimmed.StartsWith("#") || trimmed.StartsWith("---")) continue;

                    int indent = line.Length - trimmed.Length;
                    int colon = trimmed.IndexOf(':');
                    if (colon < 0) continue;

                    string key = trimmed.Substring(0, colon).Trim();
                    string value = trimmed.Substring(colon + 1).Trim();
                    if (key.Length == 0) continue;

                    if (value.Length == 0)
                    {
                        // 分组行（顶层）→ 之后的键加前缀
                        currentGroup = indent == 0 ? key : currentGroup;
                        continue;
                    }

                    value = StripComment(value);
                    value = Unescape(Unquote(value));

                    string fullKey = indent > 0 && !string.IsNullOrEmpty(currentGroup)
                        ? currentGroup + "." + key
                        : key;
                    result[fullKey] = value;
                }
            }
            return result;
        }

        private static string StripComment(string v)
        {
            // 去掉行尾注释（仅当 # 前是空白且不在引号内）
            bool inSingle = false, inDouble = false;
            for (int i = 0; i < v.Length; i++)
            {
                char c = v[i];
                if (c == '\'' && !inDouble) inSingle = !inSingle;
                else if (c == '"' && !inSingle) inDouble = !inDouble;
                else if (c == '#' && !inSingle && !inDouble && (i == 0 || char.IsWhiteSpace(v[i - 1])))
                    return v.Substring(0, i).TrimEnd();
            }
            return v;
        }

        private static string Unquote(string v)
        {
            if (v.Length >= 2 &&
                ((v[0] == '"' && v[v.Length - 1] == '"') ||
                 (v[0] == '\'' && v[v.Length - 1] == '\'')))
                return v.Substring(1, v.Length - 2);
            return v;
        }

        private static string Unescape(string v)
        {
            var sb = new StringBuilder(v.Length);
            for (int i = 0; i < v.Length; i++)
            {
                if (v[i] == '\\' && i + 1 < v.Length)
                {
                    i++;
                    switch (v[i])
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case '"': sb.Append('"'); break;
                        case '\'': sb.Append('\''); break;
                        case '\\': sb.Append('\\'); break;
                        default: sb.Append('\\'); sb.Append(v[i]); break;
                    }
                }
                else sb.Append(v[i]);
            }
            return sb.ToString();
        }
    }
}
