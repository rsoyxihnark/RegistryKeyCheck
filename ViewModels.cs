using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace RegistryKey_Check
{
    internal static class ViewModels
    {
        public static ObservableCollection<ModelRegCommand> ObsColl_RegCommands = new ObservableCollection<ModelRegCommand>();

        public static ObservableCollection<ModelNonRegCommand> ObsColl_Non_RegCommands = new ObservableCollection<ModelNonRegCommand>();

        public static List<string> IgnoreList = new List<string>();

        private static readonly HashSet<string> ignoreLookup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly string[] valueTypes = { "REG_SZ", "REG_EXPAND_SZ", "REG_MULTI_SZ", "REG_DWORD", "REG_QWORD", "REG_BINARY", "REG_NONE" };

        public static int RegCommandIds;

        public static int ListIds;

        public static int DisplayingIDs;

        private static RegistryKey? registryKey;

        private static RegistryValueKind registryValueKind;

        private sealed class QueryKey
        {
            public QueryKey(RegistryKey key)
            {
                Key = key;
                ValueNames = new HashSet<string>(key.GetValueNames(), StringComparer.OrdinalIgnoreCase);
            }

            public RegistryKey Key { get; }

            public HashSet<string> ValueNames { get; }
        }

        private sealed class QueryKeys : IDisposable
        {
            private readonly Dictionary<string, QueryKey?> keys = new Dictionary<string, QueryKey?>(StringComparer.OrdinalIgnoreCase);

            public QueryKey? Open(ModelRegCommand mo)
            {
                string path = mo.ListView + "\\" + mo.ListBaseKey + "\\" + mo.ListSubKey;
                if (!keys.TryGetValue(path, out QueryKey? key))
                {
                    RegistryKey? opened = RootKey(mo.ListBaseKey, mo.ListView)?.OpenSubKey(mo.ListSubKey);
                    try
                    {
                        key = opened == null ? null : new QueryKey(opened);
                    }
                    catch (Exception)
                    {
                        opened?.Dispose();
                        throw;
                    }
                    keys[path] = key;
                }
                return key;
            }

            public void Dispose()
            {
                foreach (QueryKey? key in keys.Values)
                {
                    key?.Key.Dispose();
                }
                keys.Clear();
            }
        }

        public static void ReadListFile()
        {
            ObsColl_RegCommands.Clear();
            ObsColl_Non_RegCommands.Clear();
            RegCommandIds = 0;
            ListIds = 0;
            ReadIgnoreFile();
            foreach (string line in ReadTextLines("List.txt"))
            {
                ListIds++;
                List<string> args = SplitCommandLine(CmdUnescape(BatchPercents(line.Trim())));
                string program = args.Count >= 3 ? args[0].TrimStart('@') : string.Empty;
                bool isReg = program.Equals("reg", StringComparison.OrdinalIgnoreCase) || program.Equals("reg.exe", StringComparison.OrdinalIgnoreCase) || program.EndsWith("\\System32\\reg.exe", StringComparison.OrdinalIgnoreCase) || program.EndsWith("\\Sysnative\\reg.exe", StringComparison.OrdinalIgnoreCase);
                string command = isReg ? args[1].ToLowerInvariant() : string.Empty;
                ModelRegCommand? modelRegCommand = command == "add" || command == "delete" ? ReadRegCommand(args, command) : null;
                if (modelRegCommand != null)
                {
                    modelRegCommand.RegCommandId = ++RegCommandIds;
                    modelRegCommand.ListId = ListIds;
                    modelRegCommand.ListLineString = line.Trim();
                    modelRegCommand.ListMatchWithRegEdit = ignoreLookup.Contains(modelRegCommand.ListLineString) ? "Ignored" : "Unknown";
                    modelRegCommand.ComplianceReason = modelRegCommand.ListMatchWithRegEdit == "Ignored" ? "On the Ignore List" : string.Empty;
                    ObsColl_RegCommands.Add(modelRegCommand);
                }
                else
                {
                    ModelNonRegCommand modelNonRegCommand = new ModelNonRegCommand();
                    modelNonRegCommand.ListId = ListIds;
                    modelNonRegCommand.ListLineString = line;
                    ObsColl_Non_RegCommands.Add(modelNonRegCommand);
                }
            }
            MarkListWarnings();
        }

        private static string[] ReadTextLines(string path)
        {
            try
            {
                return File.ReadAllLines(path, new UTF8Encoding(false, true));
            }
            catch (DecoderFallbackException)
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                return File.ReadAllLines(path, Encoding.GetEncoding(0));
            }
        }

        private static string BaseKeyName(string listBasekey)
        {
            switch (listBasekey.ToUpperInvariant())
            {
            case "HKCR":
                return "HKEY_CLASSES_ROOT";
            case "HKCU":
                return "HKEY_CURRENT_USER";
            case "HKLM":
                return "HKEY_LOCAL_MACHINE";
            case "HKU":
                return "HKEY_USERS";
            case "HKCC":
                return "HKEY_CURRENT_CONFIG";
            case "HKPD":
                return "HKEY_PERFORMANCE_DATA";
            default:
                return listBasekey.ToUpperInvariant();
            }
        }

        private static string KeyIdentity(ModelRegCommand mo)
        {
            RegistryView view = mo.ListView == RegistryView.Default ? RegistryView.Registry64 : mo.ListView;
            string subKey = mo.ListSubKey.Trim('\\');
            return view + "\\" + BaseKeyName(mo.ListBaseKey) + (subKey.Length > 0 ? "\\" + subKey : string.Empty);
        }

        private static void MarkListWarnings()
        {
            List<ModelRegCommand> rows = ObsColl_RegCommands.Where(mo => mo.ListMatchWithRegEdit != "Ignored").ToList();
            foreach (ModelRegCommand mo in ObsColl_RegCommands)
            {
                mo.ListWarning = string.Empty;
            }
            void Warn(ModelRegCommand mo, ModelRegCommand other, string what)
            {
                if (mo.ListWarning.Length == 0)
                {
                    mo.ListWarning = "List syntax warning: line " + other.ListId + " also changes this " + what;
                }
            }
            Dictionary<ModelRegCommand, string> keys = rows.ToDictionary(mo => mo, KeyIdentity);
            foreach (IGrouping<string, ModelRegCommand> value in rows.Where(mo => mo.ListValueName != "This_field_is_NULL").GroupBy(mo => keys[mo] + "\n" + mo.ListValueName, StringComparer.OrdinalIgnoreCase))
            {
                List<ModelRegCommand> same = value.ToList();
                for (int i = 0; i < same.Count; i++)
                {
                    for (int j = i + 1; j < same.Count; j++)
                    {
                        if (same[i].ListCommand == "reg delete" && same[j].ListCommand == "reg add")
                        {
                            continue;
                        }
                        Warn(same[i], same[j], "value");
                        Warn(same[j], same[i], "value");
                    }
                }
            }
            foreach (ModelRegCommand delete in rows.Where(mo => mo.ListCommand == "reg delete" && mo.ListValueName == "This_field_is_NULL"))
            {
                string key = keys[delete];
                string under = key + "\\";
                foreach (ModelRegCommand mo in rows)
                {
                    if (mo == delete || (mo.ListCommand == "reg add" && mo.ListId > delete.ListId))
                    {
                        continue;
                    }
                    string other = keys[mo];
                    bool clash = delete.DeleteAllValues
                        ? string.Equals(other, key, StringComparison.OrdinalIgnoreCase) && (mo.ListValueName != "This_field_is_NULL" || mo.DeleteAllValues)
                        : string.Equals(other, key, StringComparison.OrdinalIgnoreCase) || other.StartsWith(under, StringComparison.OrdinalIgnoreCase);
                    if (clash)
                    {
                        Warn(delete, mo, "key");
                        Warn(mo, delete, "key");
                    }
                }
            }
        }

        private static List<ModelRegCommand> LaterWrites(ModelRegCommand reset, List<ModelRegCommand> rows, Dictionary<ModelRegCommand, string> keys)
        {
            string key = keys[reset];
            return rows.Where(mo => mo.ListId > reset.ListId && mo.ListCommand == "reg add" && Rewrites(reset, key, mo, keys[mo])).ToList();
        }

        private static bool Rewrites(ModelRegCommand reset, string key, ModelRegCommand write, string writeKey)
        {
            bool sameKey = string.Equals(writeKey, key, StringComparison.OrdinalIgnoreCase);
            if (reset.ListValueName != "This_field_is_NULL")
            {
                return sameKey && string.Equals(write.ListValueName, reset.ListValueName, StringComparison.OrdinalIgnoreCase);
            }
            if (reset.DeleteAllValues)
            {
                return sameKey && write.ListValueName != "This_field_is_NULL";
            }
            return sameKey || writeKey.StartsWith(key + "\\", StringComparison.OrdinalIgnoreCase);
        }

        private static bool Undoes(ModelRegCommand done, string doneKey, ModelRegCommand later, string laterKey)
        {
            if (done.ListCommand == "reg delete")
            {
                return later.ListCommand == "reg add" && Rewrites(done, doneKey, later, laterKey);
            }
            if (later.ListCommand == "reg delete")
            {
                return Rewrites(later, laterKey, done, doneKey);
            }
            return done.ListValueName != "This_field_is_NULL" && string.Equals(laterKey, doneKey, StringComparison.OrdinalIgnoreCase) && string.Equals(later.ListValueName, done.ListValueName, StringComparison.OrdinalIgnoreCase);
        }

        private static bool HoldsOnlyLaterWrites(ModelRegCommand reset, List<ModelRegCommand> writes, Dictionary<ModelRegCommand, string> keys)
        {
            if (reset.ListValueName != "This_field_is_NULL")
            {
                return true;
            }
            using RegistryKey? key = RootKey(reset.ListBaseKey, reset.ListView)?.OpenSubKey(reset.ListSubKey);
            return key == null || HoldsOnlyLaterWrites(key, keys[reset], writes, keys, !reset.DeleteAllValues);
        }

        private static bool HoldsOnlyLaterWrites(RegistryKey key, string path, List<ModelRegCommand> writes, Dictionary<ModelRegCommand, string> keys, bool subKeys)
        {
            HashSet<string> names = new HashSet<string>(writes.Where(mo => mo.ListValueName != "This_field_is_NULL" && string.Equals(keys[mo], path, StringComparison.OrdinalIgnoreCase)).Select(mo => mo.ListValueName), StringComparer.OrdinalIgnoreCase);
            if (key.GetValueNames().Any(name => !names.Contains(name)))
            {
                return false;
            }
            if (!subKeys)
            {
                return true;
            }
            foreach (string name in key.GetSubKeyNames())
            {
                string child = path + "\\" + name;
                if (!writes.Any(mo => string.Equals(keys[mo], child, StringComparison.OrdinalIgnoreCase) || keys[mo].StartsWith(child + "\\", StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
                using RegistryKey? sub = key.OpenSubKey(name);
                if (sub != null && !HoldsOnlyLaterWrites(sub, child, writes, keys, true))
                {
                    return false;
                }
            }
            return true;
        }

        private static void MarkResets()
        {
            List<ModelRegCommand> rows = ObsColl_RegCommands.Where(mo => mo.ListMatchWithRegEdit != "Ignored").ToList();
            Dictionary<ModelRegCommand, string> keys = rows.ToDictionary(mo => mo, KeyIdentity);
            foreach (ModelRegCommand reset in rows.Where(mo => mo.ListCommand == "reg delete" && mo.ListMatchWithRegEdit == "False" && RootKey(mo.ListBaseKey) != null))
            {
                try
                {
                    List<ModelRegCommand> writes = LaterWrites(reset, rows, keys);
                    if (writes.Count == 0)
                    {
                        continue;
                    }
                    if (HoldsOnlyLaterWrites(reset, writes, keys))
                    {
                        reset.ListMatchWithRegEdit = "True";
                        reset.ComplianceReason = ComplianceReasonOf(reset);
                    }
                    else
                    {
                        reset.ComplianceReason = reset.DeleteAllValues ? "Key has values that no later line writes" : "Key has values or subkeys that no later line writes";
                    }
                }
                catch (Exception ex)
                {
                    WriteLog("MarkResets", ex);
                }
            }
        }

        private static readonly Regex BatchArgument = new Regex(@"\G(?:\*|[0-9]|~[fdpnxsatz]*(?:\$[^%:]*:)?[0-9])", RegexOptions.IgnoreCase);

        private static readonly Regex BatchSubstring = new Regex(@"^~(-?[0-9]+)?(,(-?[0-9]+)?)?$");

        private static readonly HashSet<string> batchOnlyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CD", "DATE", "TIME", "RANDOM", "ERRORLEVEL", "CMDEXTVERSION", "CMDCMDLINE", "HIGHESTNUMANODENUMBER", "__CD__", "__APPDIR__" };

        private const char BatchOnly = '\uFFFF';

        private static string BatchPercents(string line)
        {
            StringBuilder text = new StringBuilder(line.Length);
            int i = 0;
            while (i < line.Length)
            {
                if (line[i] != '%')
                {
                    text.Append(line[i]);
                    i++;
                    continue;
                }
                if (i + 1 < line.Length && line[i + 1] == '%')
                {
                    text.Append('%');
                    i += 2;
                    continue;
                }
                Match argument = BatchArgument.Match(line, i + 1);
                if (argument.Success)
                {
                    if (argument.Value.EndsWith('0'))
                    {
                        text.Append(BatchOnly);
                    }
                    i += argument.Length + 1;
                    continue;
                }
                int stop = line.IndexOfAny(new[] { '%', ':' }, i + 1);
                if (stop < 0)
                {
                    i++;
                    continue;
                }
                if (line[stop] == ':' && stop + 1 < line.Length && line[stop + 1] == '%')
                {
                    stop++;
                }
                string name = line[(i + 1)..stop];
                string? value = name.Length > 0 ? Environment.GetEnvironmentVariable(name) : null;
                if (value == null && batchOnlyNames.Contains(name))
                {
                    text.Append(BatchOnly);
                    int end = line[stop] == '%' ? stop : line.IndexOf('%', stop + 1);
                    i = (end < 0 ? stop : end) + 1;
                    continue;
                }
                if (line[stop] == '%')
                {
                    text.Append(value);
                    i = stop + 1;
                    continue;
                }
                if (value == null)
                {
                    i = stop + 1;
                    continue;
                }
                int close = line.IndexOf('%', stop + 1);
                string? modified = close < 0 ? null : ModifyBatchValue(value, line[(stop + 1)..close]);
                if (modified == null)
                {
                    i++;
                    continue;
                }
                text.Append(modified);
                i = close + 1;
            }
            return text.ToString();
        }

        private static string? ModifyBatchValue(string value, string modifier)
        {
            Match substring = BatchSubstring.Match(modifier);
            if (substring.Success)
            {
                int begin = 0;
                int count = 0;
                if ((substring.Groups[1].Success && !int.TryParse(substring.Groups[1].Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out begin)) || (substring.Groups[3].Success && !int.TryParse(substring.Groups[3].Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out count)))
                {
                    return null;
                }
                begin = begin < 0 ? Math.Max(value.Length + begin, 0) : Math.Min(begin, value.Length);
                if (!substring.Groups[2].Success)
                {
                    return value[begin..];
                }
                int end = Math.Min(count < 0 ? value.Length + count : begin + count, value.Length);
                return end > begin ? value[begin..end] : string.Empty;
            }
            int equals = modifier.IndexOf('=');
            if (equals < 0)
            {
                return null;
            }
            string search = modifier[..equals];
            string replacement = modifier[(equals + 1)..];
            bool star = search.StartsWith('*');
            string pattern = star ? search[1..] : search;
            if (pattern.Length == 0)
            {
                return null;
            }
            if (!star)
            {
                return value.Replace(pattern, replacement, StringComparison.OrdinalIgnoreCase);
            }
            int found = value.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            return found < 0 ? value : replacement + value[(found + pattern.Length)..];
        }

        private static string CmdUnescape(string line)
        {
            StringBuilder text = new StringBuilder(line.Length);
            bool quoted = false;
            bool afterOperator = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                bool operatorChar = false;
                if (c == '"')
                {
                    quoted = !quoted;
                }
                else if (c == '^' && !quoted && i + 1 < line.Length)
                {
                    c = line[++i];
                }
                else if (!quoted && c is '&' or '|' or '<' or '>')
                {
                    operatorChar = true;
                    bool handle = c is '<' or '>' && text.Length > 0 && char.IsDigit(text[^1]) && (text.Length == 1 || char.IsWhiteSpace(text[^2]));
                    if (text.Length > 0 && !char.IsWhiteSpace(text[^1]) && !afterOperator && !handle)
                    {
                        text.Append(' ');
                    }
                }
                text.Append(c);
                afterOperator = operatorChar;
            }
            return text.ToString();
        }

        public static List<string> SplitCommandLine(string line)
        {
            List<string> args = new List<string>();
            StringBuilder current = new StringBuilder();
            bool inQuotes = false;
            bool hasArg = false;
            int i = 0;
            while (i < line.Length)
            {
                char c = line[i];
                if (c == '\\')
                {
                    int count = 0;
                    while (i < line.Length && line[i] == '\\')
                    {
                        count++;
                        i++;
                    }
                    if (i < line.Length && line[i] == '"')
                    {
                        current.Append('\\', count / 2);
                        if (count % 2 == 1)
                        {
                            current.Append('"');
                            i++;
                        }
                    }
                    else
                    {
                        current.Append('\\', count);
                    }
                    hasArg = true;
                }
                else if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i += 2;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                        i++;
                    }
                    hasArg = true;
                }
                else if (!inQuotes && char.IsWhiteSpace(c))
                {
                    if (hasArg)
                    {
                        args.Add(current.ToString());
                        current.Clear();
                        hasArg = false;
                    }
                    i++;
                }
                else
                {
                    current.Append(c);
                    hasArg = true;
                    i++;
                }
            }
            if (hasArg)
            {
                args.Add(current.ToString());
            }
            return args;
        }

        private static ModelRegCommand? ReadRegCommand(List<string> args, string command)
        {
            ModelRegCommand modelRegCommand = new ModelRegCommand();
            modelRegCommand.ListCommand = "reg " + command;
            string keyPath = args[2];
            int slash = keyPath.IndexOf('\\');
            if (keyPath.Contains('"') || keyPath.EndsWith("\\\\", StringComparison.Ordinal) || slash == keyPath.Length - 1)
            {
                return null;
            }
            modelRegCommand.ListBaseKey = slash < 0 ? keyPath : keyPath[..slash];
            modelRegCommand.ListSubKey = slash < 0 ? string.Empty : keyPath[(slash + 1)..];
            string? valueName = null;
            string? valueType = null;
            string? valueData = null;
            string? valueSeparator = null;
            HashSet<string> seen = new HashSet<string>();
            for (int i = 3; i < args.Count; i++)
            {
                string arg = args[i].ToLowerInvariant();
                int glued = arg.StartsWith('/') ? arg.IndexOfAny(new[] { '>', '<', '&', '|' }) : -1;
                if (glued > 0)
                {
                    if (arg[..glued] is not ("/f" or "/ve" or "/va" or "/reg:32" or "/reg:64"))
                    {
                        return null;
                    }
                    args.Insert(i + 1, arg[glued..]);
                    arg = arg[..glued];
                }
                if (arg.StartsWith('/') && !seen.Add(arg switch { "/ve" => "/v", "/reg:64" => "/reg:32", _ => arg }))
                {
                    return null;
                }
                if (arg == "/ve")
                {
                    valueName = string.Empty;
                }
                else if (arg == "/va" && command == "delete")
                {
                    modelRegCommand.DeleteAllValues = true;
                }
                else if (arg == "/reg:32")
                {
                    modelRegCommand.ListView = RegistryView.Registry32;
                }
                else if (arg == "/reg:64")
                {
                    modelRegCommand.ListView = RegistryView.Registry64;
                }
                else if (arg == "/v" && i + 1 < args.Count)
                {
                    valueName = args[++i];
                }
                else if (arg == "/t" && command == "add" && i + 1 < args.Count)
                {
                    valueType = args[++i].ToUpperInvariant();
                }
                else if (arg == "/d" && command == "add" && i + 1 < args.Count)
                {
                    valueData = args[++i];
                }
                else if (arg == "/s" && command == "add" && i + 1 < args.Count && args[i + 1].Length == 1)
                {
                    valueSeparator = args[++i];
                }
                else if (arg == "/f")
                {
                }
                else if (arg.StartsWith('&') || arg.StartsWith("||"))
                {
                    return null;
                }
                else if (arg.StartsWith('|'))
                {
                    break;
                }
                else if (arg.StartsWith('>') || arg.StartsWith('<') || (arg.Length > 1 && char.IsDigit(arg[0]) && (arg[1] == '>' || arg[1] == '<')))
                {
                    if (arg.EndsWith('>') || arg.EndsWith('<'))
                    {
                        i++;
                    }
                }
                else
                {
                    return null;
                }
            }
            if (new[] { keyPath, valueName, valueType, valueData, valueSeparator }.Any(part => part != null && part.Contains(BatchOnly)))
            {
                return null;
            }
            if (modelRegCommand.DeleteAllValues && valueName != null)
            {
                return null;
            }
            if (valueName == null && (valueType != null || valueData != null))
            {
                valueName = string.Empty;
            }
            if (valueName == null)
            {
                return modelRegCommand;
            }
            modelRegCommand.ListValueName = valueName;
            if (command == "delete")
            {
                return modelRegCommand;
            }
            modelRegCommand.ListValueType = valueType ?? "REG_SZ";
            modelRegCommand.ListValueData = valueData ?? (modelRegCommand.ListValueType == "REG_QWORD" ? "0" : string.Empty);
            if (modelRegCommand.ListValueType == "REG_BINARY")
            {
                if (modelRegCommand.ListValueData.Length % 2 == 0)
                {
                    modelRegCommand.ListValueData = modelRegCommand.ListValueData.ToLower();
                }
                else
                {
                    modelRegCommand.ListValueData = "0" + modelRegCommand.ListValueData.ToLower();
                }
            }
            if (modelRegCommand.ListValueType == "REG_MULTI_SZ")
            {
                string separator = string.IsNullOrEmpty(valueSeparator) ? "\\0" : valueSeparator;
                bool onlySeparator = modelRegCommand.ListValueData == separator;
                if (modelRegCommand.ListValueData.EndsWith(separator, StringComparison.Ordinal))
                {
                    modelRegCommand.ListValueData = modelRegCommand.ListValueData[..^separator.Length];
                }
                modelRegCommand.ListValueStrings = onlySeparator ? new[] { string.Empty } : modelRegCommand.ListValueData.Length == 0 ? Array.Empty<string>() : modelRegCommand.ListValueData.Split(separator);
                modelRegCommand.ListValueData = string.Join("\\0", modelRegCommand.ListValueStrings);
            }
            if (modelRegCommand.ListValueType == "REG_DWORD" && TryParseNumber(modelRegCommand.ListValueData, uint.MaxValue, 0, out ulong dword))
            {
                modelRegCommand.ListValueNumber = dword;
                modelRegCommand.ListValueData = FormatDword(dword);
            }
            if (modelRegCommand.ListValueType == "REG_QWORD" && TryParseNumber(modelRegCommand.ListValueData, ulong.MaxValue, long.MinValue, out ulong qword))
            {
                modelRegCommand.ListValueNumber = qword;
                modelRegCommand.ListValueData = FormatQword(qword);
            }
            return modelRegCommand;
        }

        private static bool TryParseNumber(string text, ulong maxValue, long minValue, out ulong value)
        {
            text = text.Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return ulong.TryParse(text.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value) && value <= maxValue;
            }
            if (text.StartsWith('-'))
            {
                bool parsed = long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long negative) && negative >= minValue;
                value = parsed ? unchecked((ulong)negative) & maxValue : 0;
                return parsed;
            }
            return ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value <= maxValue;
        }

        private static string FormatDword(ulong value)
        {
            return "0x" + value.ToString("x8", CultureInfo.InvariantCulture) + " (" + value.ToString(CultureInfo.InvariantCulture) + ")";
        }

        private static string FormatQword(ulong value)
        {
            return "0x" + value.ToString("x16", CultureInfo.InvariantCulture) + " (" + value.ToString(CultureInfo.InvariantCulture) + ")";
        }

        public static void QueryFromRegEdit()
        {
            using QueryKeys keys = new QueryKeys();
            foreach (ModelRegCommand mo in ObsColl_RegCommands)
            {
                if (mo.ListMatchWithRegEdit == "Ignored")
                {
                    continue;
                }
                try
                {
                    mo.AvailableSubKey = string.Empty;
                    mo.AvailableValueName = "This_field_is_NULL";
                    mo.RegEditValueType = "This_field_is_NULL";
                    mo.RegEditValueData = "This_field_is_NULL";
                    mo.ReturnObject = "This_field_is_NULL";
                    mo.ListMatchWithRegEdit = "Unknown";
                    mo.ComplianceReason = string.Empty;
                    QueryKey? queryKey = keys.Open(mo);
                    RegistryKey? openedKey = queryKey?.Key;
                    if (queryKey != null)
                    {
                        mo.AvailableSubKey = "It's Available";
                        if (openedKey != null && mo.ListValueName != "This_field_is_NULL")
                        {
                            if (queryKey.ValueNames.Contains(mo.ListValueName))
                            {
                                mo.ReturnObject = openedKey.GetValue(mo.ListValueName, string.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames)!;
                                mo.AvailableValueName = "It's Available";
                                registryValueKind = openedKey.GetValueKind(mo.ListValueName);
                                if (registryValueKind == RegistryValueKind.String)
                                {
                                    mo.RegEditValueType = "REG_SZ";
                                    mo.RegEditValueData = (string)mo.ReturnObject;
                                }
                                else if (registryValueKind == RegistryValueKind.ExpandString)
                                {
                                    mo.RegEditValueType = "REG_EXPAND_SZ";
                                    mo.RegEditValueData = (string)mo.ReturnObject;
                                }
                                else if (registryValueKind == RegistryValueKind.MultiString)
                                {
                                    mo.RegEditValueType = "REG_MULTI_SZ";
                                    mo.RegEditValueData = string.Join("\\0", (string[])mo.ReturnObject);
                                }
                                else if (registryValueKind == RegistryValueKind.DWord)
                                {
                                    mo.RegEditValueType = "REG_DWORD";
                                    mo.RegEditValueData = FormatDword(unchecked((uint)(int)mo.ReturnObject));
                                }
                                else if (registryValueKind == RegistryValueKind.QWord)
                                {
                                    mo.RegEditValueType = "REG_QWORD";
                                    mo.RegEditValueData = FormatQword(unchecked((ulong)(long)mo.ReturnObject));
                                }
                                else if (registryValueKind == RegistryValueKind.Binary)
                                {
                                    mo.RegEditValueType = "REG_BINARY";
                                    mo.RegEditValueData = Convert.ToHexString((byte[])mo.ReturnObject).ToLower();
                                }
                                else if (registryValueKind == RegistryValueKind.None)
                                {
                                    mo.RegEditValueType = "REG_NONE";
                                    mo.RegEditValueData = Encoding.Unicode.GetString((byte[])mo.ReturnObject).TrimEnd('\0');
                                }
                                bool expand = mo.RegEditValueType == "REG_EXPAND_SZ" && mo.ListValueType == "REG_EXPAND_SZ";
                                if (string.Equals(expand ? Environment.ExpandEnvironmentVariables(mo.RegEditValueData) : mo.RegEditValueData, expand ? Environment.ExpandEnvironmentVariables(mo.ListValueData) : mo.ListValueData, StringComparison.OrdinalIgnoreCase))
                                {
                                    mo.ListMatchWithRegEdit = "True";
                                }
                                else
                                {
                                    mo.ListMatchWithRegEdit = "False";
                                }
                            }
                            else
                            {
                                mo.AvailableValueName = "Not Available";
                                if (mo.ListCommand == "reg add")
                                {
                                    mo.ListMatchWithRegEdit = "False";
                                }
                                else if (mo.ListCommand == "reg delete")
                                {
                                    mo.ListMatchWithRegEdit = "True";
                                }
                            }
                        }
                        else if (openedKey != null && mo.ListValueName == "This_field_is_NULL")
                        {
                            if (mo.ListCommand == "reg add")
                            {
                                mo.ListMatchWithRegEdit = "True";
                            }
                            else if (mo.ListCommand == "reg delete")
                            {
                                mo.ListMatchWithRegEdit = "False";
                            }
                            if (mo.DeleteAllValues)
                            {
                                mo.AvailableValueName = queryKey.ValueNames.Count > 0 ? "It's Available" : "Not Available";
                            }
                        }
                    }
                    else
                    {
                        mo.AvailableSubKey = "Not Available";
                        if (mo.ListCommand == "reg add")
                        {
                            mo.ListMatchWithRegEdit = "False";
                        }
                        else if (mo.ListCommand == "reg delete")
                        {
                            mo.ListMatchWithRegEdit = "True";
                        }
                    }
                    if (mo.ListCommand == "reg delete" && mo.AvailableSubKey == "It's Available" && mo.AvailableValueName == "It's Available")
                    {
                        mo.ListMatchWithRegEdit = "False";
                    }
                    else if (mo.ListCommand == "reg delete" && mo.AvailableSubKey == "It's Available" && mo.AvailableValueName == "Not Available")
                    {
                        mo.ListMatchWithRegEdit = "True";
                    }
                    else if (mo.ListCommand == "reg delete" && mo.AvailableSubKey == "It's Available" && mo.AvailableValueName == "This_field_is_NULL")
                    {
                        mo.ListMatchWithRegEdit = "False";
                    }
                    else if (mo.ListCommand == "reg delete" && mo.AvailableSubKey == "Not Available")
                    {
                        mo.ListMatchWithRegEdit = RootKey(mo.ListBaseKey) == null ? "False" : "True";
                    }
                    if (mo.ListCommand == "reg add" && mo.ListValueType != "This_field_is_NULL" && mo.RegEditValueType != "This_field_is_NULL" && mo.ListValueType != mo.RegEditValueType)
                    {
                        mo.ListMatchWithRegEdit = "False";
                    }
                    mo.ComplianceReason = ComplianceReasonOf(mo);
                }
                catch (Exception ex)
                {
                    mo.ComplianceReason = "Could not be read, see Log.txt";
                    AppendLog("---vvv ! ! ! Warning ! ! ! vvv--- QueryMethod " + DateTime.Now.ToString() + "\n");
                    AppendLog("     " + mo.RegCommandId + "-" + mo.ListId + "-" + mo.ListCommand + "-" + mo.ListBaseKey + "\\" + mo.ListSubKey + "-" + mo.ListValueName + "-" + mo.ListValueType + "-" + mo.ListValueData + "\n     " + mo.AvailableSubKey + "-" + mo.AvailableValueName + "-" + mo.RegEditValueType + "-" + mo.RegEditValueData + "\n");
                    AppendLog(ex.ToString() + "\n---^^^ ! ! ! Warning ! ! ! ^^^---\n");
                }
            }
            MarkResets();
        }

        private static string ComplianceReasonOf(ModelRegCommand mo)
        {
            if (mo.ListMatchWithRegEdit == "True")
            {
                return "Compliant";
            }
            if (mo.ListMatchWithRegEdit != "False")
            {
                return string.Empty;
            }
            if (RootKey(mo.ListBaseKey) == null)
            {
                return "Base key is not one the Registry has";
            }
            if (mo.ListCommand == "reg add")
            {
                if (mo.AvailableSubKey != "It's Available")
                {
                    return "Key not found in Registry";
                }
                if (mo.AvailableValueName != "It's Available")
                {
                    return "Value not found in Registry";
                }
                if (mo.RegEditValueType != mo.ListValueType)
                {
                    return "Value type differs";
                }
                return "Value data differs";
            }
            if (mo.ListValueName != "This_field_is_NULL")
            {
                return "Value still exists in Registry";
            }
            return mo.DeleteAllValues ? "Key still has values in Registry" : "Key still exists in Registry";
        }

        private static RegistryKey? RootKey(string listBasekey, RegistryView view = RegistryView.Default)
        {
            switch (listBasekey.ToUpperInvariant())
            {
            case "HKEY_CLASSES_ROOT":
            case "HKCR":
                return RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, view);
            case "HKEY_CURRENT_USER":
            case "HKCU":
                return RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view);
            case "HKEY_LOCAL_MACHINE":
            case "HKLM":
                return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            case "HKEY_USERS":
            case "HKU":
                return RegistryKey.OpenBaseKey(RegistryHive.Users, view);
            case "HKEY_CURRENT_CONFIG":
            case "HKCC":
                return RegistryKey.OpenBaseKey(RegistryHive.CurrentConfig, view);
            case "HKEY_PERFORMANCE_DATA":
            case "HKPD":
                return RegistryKey.OpenBaseKey(RegistryHive.PerformanceData, view);
            default:
                return null;
            }
        }

        private static bool AvailableSubKeyMethodTrue(string listBasekey, string listSubKey, RegistryView view)
        {
            registryKey = RootKey(listBasekey, view)?.OpenSubKey(listSubKey, writable: true);
            return registryKey != null;
        }

        private static bool CreateSubKeyMethod(string listBasekey, string listSubKey, RegistryView view)
        {
            registryKey = RootKey(listBasekey, view)?.CreateSubKey(listSubKey, writable: true);
            return registryKey != null;
        }

        public static async Task ExecuteMissingCommands()
        {
            List<ModelRegCommand> rows = ObsColl_RegCommands.Where(mo => mo.ListMatchWithRegEdit != "Ignored").ToList();
            Dictionary<ModelRegCommand, string> keys = rows.ToDictionary(mo => mo, KeyIdentity);
            HashSet<ModelRegCommand> rewrites = new HashSet<ModelRegCommand>();
            foreach (ModelRegCommand mo in ObsColl_RegCommands)
            {
                mo.ExecuteResult = string.Empty;
                mo.ExecuteRefusal = string.Empty;
                try
                {
                    if (mo.ListMatchWithRegEdit == "False" || rewrites.Contains(mo))
                    {
                        if (RootKey(mo.ListBaseKey) == null)
                        {
                            throw new InvalidOperationException("\"" + mo.ListBaseKey + "\" is not a base key the Registry has.");
                        }
                        if (mo.ListCommand == "reg delete" && mo.DeleteAllValues)
                        {
                            await Task.Run(() =>
                            {
                                AvailableSubKeyMethodTrue(mo.ListBaseKey, mo.ListSubKey, mo.ListView);
                            });
                            if (registryKey != null)
                            {
                                foreach (string name in registryKey.GetValueNames())
                                {
                                    registryKey.DeleteValue(name);
                                }
                            }
                        }
                        else if (mo.ListCommand == "reg delete" && mo.ListValueName == "This_field_is_NULL")
                        {
                            if (mo.ListSubKey.Length == 0)
                            {
                                throw new InvalidOperationException("A whole base key cannot be deleted.");
                            }
                            RootKey(mo.ListBaseKey, mo.ListView)?.DeleteSubKeyTree(mo.ListSubKey, throwOnMissingSubKey: false);
                        }
                        else if (mo.ListCommand == "reg delete")
                        {
                            await Task.Run(() =>
                            {
                                AvailableSubKeyMethodTrue(mo.ListBaseKey, mo.ListSubKey, mo.ListView);
                            });
                            if (registryKey != null)
                            {
                                registryKey.DeleteValue(mo.ListValueName, throwOnMissingValue: false);
                            }
                        }
                        else if (mo.ListCommand == "reg add" && mo.ListValueName == "This_field_is_NULL")
                        {
                            CreateSubKeyMethod(mo.ListBaseKey, mo.ListSubKey, mo.ListView);
                        }
                        else if (mo.ListCommand == "reg add" && mo.ListValueName != "This_field_is_NULL")
                        {
                            if (!valueTypes.Contains(mo.ListValueType))
                            {
                                throw new InvalidOperationException("\"" + mo.ListValueType + "\" is not a value type the Registry has.");
                            }
                            await Task.Run(() =>
                            {
                                CreateSubKeyMethod(mo.ListBaseKey, mo.ListSubKey, mo.ListView);
                            });
                            if (registryKey != null && mo.ListValueType == "REG_SZ")
                            {
                                registryKey.SetValue(mo.ListValueName, mo.ListValueData, RegistryValueKind.String);
                            }
                            else if (registryKey != null && mo.ListValueType == "REG_DWORD")
                            {
                                registryKey.SetValue(mo.ListValueName, unchecked((int)(uint)ListNumber(mo)), RegistryValueKind.DWord);
                            }
                            else if (registryKey != null && mo.ListValueType == "REG_MULTI_SZ")
                            {
                                if (mo.ListValueStrings.Length > 1 && mo.ListValueStrings.Contains(string.Empty))
                                {
                                    throw new FormatException("\"" + mo.ListValueData + "\" has an empty string, which reg.exe does not write in REG_MULTI_SZ data.");
                                }
                                registryKey.SetValue(mo.ListValueName, mo.ListValueStrings, RegistryValueKind.MultiString);
                            }
                            else if (registryKey != null && mo.ListValueType == "REG_BINARY")
                            {
                                registryKey.SetValue(mo.ListValueName, Convert.FromHexString(mo.ListValueData), RegistryValueKind.Binary);
                            }
                            else if (registryKey != null && mo.ListValueType == "REG_EXPAND_SZ")
                            {
                                registryKey.SetValue(mo.ListValueName, mo.ListValueData, RegistryValueKind.ExpandString);
                            }
                            else if (registryKey != null && mo.ListValueType == "REG_QWORD")
                            {
                                registryKey.SetValue(mo.ListValueName, unchecked((long)ListNumber(mo)), RegistryValueKind.QWord);
                            }
                            else if (registryKey != null && mo.ListValueType == "REG_NONE")
                            {
                                registryKey.SetValue(mo.ListValueName, Encoding.Unicode.GetBytes(mo.ListValueData + "\0"), RegistryValueKind.None);
                            }
                            else
                            {
                                throw new InvalidOperationException("The key could not be created.");
                            }
                        }
                        mo.ExecuteResult = "successful";
                        rewrites.UnionWith(rows.Where(later => later.ListId > mo.ListId && later.ListMatchWithRegEdit == "True" && Undoes(mo, keys[mo], later, keys[later])));
                    }
                }
                catch (Exception ex)
                {
                    mo.ExecuteRefusal = RefusedByWindows(mo, ex);
                    if (mo.ExecuteRefusal.Length > 0)
                    {
                        mo.ExecuteResult = "refused";
                    }
                    else
                    {
                        AppendLog("---vvv ! ! ! Warning ! ! ! vvv--- ExecuteMethod " + DateTime.Now.ToString() + "\n");
                        AppendLog("     " + mo.RegCommandId + "-" + mo.ListId + "-" + mo.ListCommand + "-" + mo.ListBaseKey + "\\" + mo.ListSubKey + "-" + mo.ListValueName + "-" + mo.ListValueType + "-" + mo.ListValueData + "\n     " + mo.AvailableSubKey + "-" + mo.AvailableValueName + "-" + mo.RegEditValueType + "-" + mo.RegEditValueData + "\n");
                        AppendLog(ex.ToString() + "\n---^^^ ! ! ! Warning ! ! ! ^^^---\n");
                        mo.ExecuteResult = "Error";
                    }
                }
                finally
                {
                    registryKey?.Dispose();
                    registryKey = null;
                }
            }
        }

        private static string RefusedByWindows(ModelRegCommand mo, Exception ex)
        {
            if (ex is UnauthorizedAccessException || ex is SecurityException)
            {
                return "Access denied.";
            }
            if (ex is not IOException)
            {
                return string.Empty;
            }
            string message = ex.Message.Trim();
            if (message.Length > 4 && message.StartsWith("The ", StringComparison.Ordinal))
            {
                message = char.ToUpperInvariant(message[4]) + message[5..];
            }
            return BaseKeyName(mo.ListBaseKey) == "HKEY_USERS" ? "SID Key not found in Registry. " + message : message;
        }

        private static ulong ListNumber(ModelRegCommand mo)
        {
            return mo.ListValueNumber ?? throw new FormatException("\"" + mo.ListValueData + "\" is not a valid " + mo.ListValueType + " number.");
        }

        [DllImport("ntdll.dll")]
        private static extern int NtQueryKey(SafeRegistryHandle key, int keyInformationClass, byte[] keyInformation, int length, out int resultLength);

        private static string? KernelName(RegistryKey key)
        {
            byte[] buffer = new byte[65536];
            if (NtQueryKey(key.Handle, 3, buffer, buffer.Length, out int _) != 0)
            {
                return null;
            }
            return Encoding.Unicode.GetString(buffer, 4, Math.Min(BitConverter.ToInt32(buffer, 0), buffer.Length - 4));
        }

        private static string? RegEditPath(ModelRegCommand mo)
        {
            RegistryKey? root = RootKey(mo.ListBaseKey);
            if (root == null)
            {
                return null;
            }
            string path = root.Name + (mo.ListSubKey.Length > 0 ? "\\" + mo.ListSubKey : string.Empty);
            if (mo.ListView == RegistryView.Registry32 && root.Name == "HKEY_LOCAL_MACHINE")
            {
                try
                {
                    using RegistryKey? key = RootKey(mo.ListBaseKey, RegistryView.Registry32)?.OpenSubKey(mo.ListSubKey);
                    string? real = key == null ? null : KernelName(key);
                    if (real != null && real.StartsWith("\\REGISTRY\\MACHINE\\", StringComparison.OrdinalIgnoreCase))
                    {
                        path = "HKEY_LOCAL_MACHINE\\" + real[18..];
                    }
                }
                catch (Exception)
                {
                }
            }
            return "Computer\\" + path;
        }

        public static void JumpToRegEdit(ModelRegCommand mo)
        {
            try
            {
                string? lastKey = RegEditPath(mo);
                if (lastKey == null)
                {
                    return;
                }
                using (RegistryKey regedit = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Applets\\Regedit", writable: true))
                {
                    regedit.SetValue("LastKey", lastKey, RegistryValueKind.String);
                }
                Process.Start("regedit.exe", "-m");
            }
            catch (Exception ex)
            {
                WriteLog("JumpToRegEdit", ex);
            }
        }

        public static void ReadIgnoreFile()
        {
            IgnoreList.Clear();
            ignoreLookup.Clear();
            if (File.Exists("Ignore.txt"))
            {
                foreach (string line in ReadTextLines("Ignore.txt"))
                {
                    if (line.Trim().Length > 0 && !IgnoreList.Contains(line.Trim()))
                    {
                        IgnoreList.Add(line.Trim());
                        ignoreLookup.Add(line.Trim());
                    }
                }
            }
        }

        private static bool ReloadIgnoreFile()
        {
            try
            {
                ReadIgnoreFile();
                return true;
            }
            catch (Exception ex)
            {
                WriteLog("Ignore.txt could not be read, so the Ignore List was not changed. ReadIgnoreFile", ex);
                return false;
            }
        }

        public static void AddToIgnoreList(IEnumerable<ModelRegCommand> rows)
        {
            if (!ReloadIgnoreFile())
            {
                return;
            }
            foreach (ModelRegCommand mo in rows)
            {
                if (!ignoreLookup.Contains(mo.ListLineString))
                {
                    IgnoreList.Add(mo.ListLineString);
                    ignoreLookup.Add(mo.ListLineString);
                }
                mo.ListMatchWithRegEdit = "Ignored";
                mo.ComplianceReason = "On the Ignore List";
            }
            ApplyIgnoreList();
            SaveIgnoreFile();
            MarkListWarnings();
        }

        public static void RemoveFromIgnoreList(IEnumerable<ModelRegCommand> rows)
        {
            if (!ReloadIgnoreFile())
            {
                return;
            }
            foreach (ModelRegCommand mo in rows)
            {
                IgnoreList.RemoveAll(line => string.Equals(line, mo.ListLineString, StringComparison.OrdinalIgnoreCase));
                ignoreLookup.Remove(mo.ListLineString);
                if (mo.ListMatchWithRegEdit == "Ignored")
                {
                    mo.ListMatchWithRegEdit = "Unknown";
                    mo.ComplianceReason = string.Empty;
                }
            }
            ApplyIgnoreList();
            SaveIgnoreFile();
            MarkListWarnings();
        }

        private static void ApplyIgnoreList()
        {
            foreach (ModelRegCommand mo in ObsColl_RegCommands)
            {
                bool ignored = ignoreLookup.Contains(mo.ListLineString);
                if (ignored && mo.ListMatchWithRegEdit != "Ignored")
                {
                    mo.ListMatchWithRegEdit = "Ignored";
                    mo.ComplianceReason = "On the Ignore List";
                }
                else if (!ignored && mo.ListMatchWithRegEdit == "Ignored")
                {
                    mo.ListMatchWithRegEdit = "Unknown";
                    mo.ComplianceReason = string.Empty;
                }
            }
        }

        private static void SaveIgnoreFile()
        {
            try
            {
                if (IgnoreList.Count > 0)
                {
                    File.WriteAllLines("Ignore.txt", IgnoreList);
                }
                else
                {
                    File.Delete("Ignore.txt");
                }
            }
            catch (Exception ex)
            {
                WriteLog("SaveIgnoreFile", ex);
            }
        }

        public static string Shown(string text)
        {
            return text == "This_field_is_NULL" ? string.Empty : text;
        }

        public static string ValueNameShown(string name)
        {
            return name.Length == 0 ? "(Default)" : Shown(name);
        }

        public static void WriteNote(string method, string text)
        {
            AppendLog("---vvv ! ! ! Warning ! ! ! vvv--- " + method + " " + DateTime.Now.ToString() + "\n" + text + "\n---^^^ ! ! ! Warning ! ! ! ^^^---\n");
        }

        public static void WriteLog(string method, Exception ex)
        {
            AppendLog("---vvv ! ! ! Warning ! ! ! vvv--- " + method + " " + DateTime.Now.ToString() + "\n");
            AppendLog(ex.ToString() + "\n---^^^ ! ! ! Warning ! ! ! ^^^---\n");
        }

        private static void AppendLog(string text)
        {
            try
            {
                File.AppendAllText("Log.txt", text);
            }
            catch (Exception)
            {
            }
        }
    }
}
