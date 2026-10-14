using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace RegistryKey_Check
{
    internal static class ViewModels
    {
        public static ObservableCollection<ModelRegCommand> ObsColl_RegCommands = new ObservableCollection<ModelRegCommand>();

        public static ObservableCollection<ModelNonRegCommand> ObsColl_Non_RegCommands = new ObservableCollection<ModelNonRegCommand>();

        public static List<string> IgnoreList = new List<string>();

        public static int RegCommandIds;

        public static int Non_RegCommandIds;

        public static int ListIds;

        public static int DisplayingIDs;

        private static RegistryKey? registryKey;

        public static int Total;

        public static int Shown;

        public static string HelpString { get; set; } =
            "RegistryKey Check\r\n" +
            "Version " + Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) + "\r\n\r\n" +
            "     Bu yazılım RegEdit kayıtlarını toplu olarak kontrol edip düzenlemek içindir. \"List.txt\" içerisine yazacağınız reg komutlarını kullanır.Hata bilgilerini \"Log.txt\" dosyasında görebilirsiniz.\r\n" +
            "     Komutların ne işe yaradığı konusunda bilginiz yoksa lütfen çalıştırmayınız.Bilgisayarınızı çalışmaz hale getirebilir.\r\n\r\n" +
            "     This software is for batch checking and editing RegEdit records. It uses the reg commands you write in \"List.txt\". You can see the error information in the \"Log.txt\" file.\r\n" +
            "      If you do not know what the commands do, please do not run them.It can render your computer inoperable.";

        public static void ReadListFile()
        {
            ObsColl_RegCommands.Clear();
            ObsColl_Non_RegCommands.Clear();
            RegCommandIds = 0;
            Non_RegCommandIds = 0;
            ListIds = 0;
            ReadIgnoreFile();
            foreach (string line in File.ReadLines("List.txt"))
            {
                ListIds++;
                List<string> args = SplitCommandLine(line.Trim());
                string command = args.Count >= 3 && args[0].Equals("reg", StringComparison.OrdinalIgnoreCase) ? args[1].ToLowerInvariant() : string.Empty;
                if (command == "add" || command == "delete")
                {
                    ModelRegCommand modelRegCommand = ReadRegCommand(args, command);
                    modelRegCommand.RegCommandId = ++RegCommandIds;
                    modelRegCommand.ListId = ListIds;
                    modelRegCommand.ListLineString = line.Trim();
                    modelRegCommand.ListMatchWithRegEdit = IgnoreList.Contains(modelRegCommand.ListLineString) ? "Ignored" : "Unknown";
                    ObsColl_RegCommands.Add(modelRegCommand);
                }
                else
                {
                    ModelNonRegCommand modelNonRegCommand = new ModelNonRegCommand();
                    modelNonRegCommand.Non_RegCommandId = ++Non_RegCommandIds;
                    modelNonRegCommand.ListId = ListIds;
                    modelNonRegCommand.ListLineString = line;
                    ObsColl_Non_RegCommands.Add(modelNonRegCommand);
                }
            }
            Shown = ObsColl_RegCommands.Count;
            Total = ObsColl_RegCommands.Count;
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

        private static ModelRegCommand ReadRegCommand(List<string> args, string command)
        {
            ModelRegCommand modelRegCommand = new ModelRegCommand();
            modelRegCommand.ListCommand = "reg " + command;
            string keyPath = args[2];
            int slash = keyPath.IndexOf('\\');
            modelRegCommand.ListBaseKey = slash < 0 ? keyPath : keyPath[..slash];
            modelRegCommand.ListSubKey = slash < 0 ? string.Empty : keyPath[(slash + 1)..];
            modelRegCommand.ListPath = FullBaseKeyName(modelRegCommand.ListBaseKey) + (modelRegCommand.ListSubKey.Length > 0 ? "\\" + modelRegCommand.ListSubKey : string.Empty);
            string? valueName = null;
            string? valueType = null;
            string? valueData = null;
            for (int i = 3; i < args.Count; i++)
            {
                string arg = args[i].ToLowerInvariant();
                if (arg == "/ve")
                {
                    valueName = string.Empty;
                }
                else if (arg == "/va" && command == "delete")
                {
                    modelRegCommand.DeleteAllValues = true;
                }
                else if (arg == "/v" && i + 1 < args.Count)
                {
                    valueName = args[++i];
                }
                else if (arg == "/t" && i + 1 < args.Count)
                {
                    valueType = args[++i].ToUpperInvariant();
                }
                else if (arg == "/d" && i + 1 < args.Count)
                {
                    valueData = args[++i];
                }
                else if (arg == "/s" && i + 1 < args.Count)
                {
                    i++;
                }
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
            modelRegCommand.ListValueData = valueData ?? string.Empty;
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
            if (modelRegCommand.ListValueType == "REG_MULTI_SZ" && modelRegCommand.ListValueData.EndsWith("\\0", StringComparison.Ordinal))
            {
                modelRegCommand.ListValueData = modelRegCommand.ListValueData[..^2];
            }
            if (modelRegCommand.ListValueType == "REG_DWORD" && TryParseNumber(modelRegCommand.ListValueData, uint.MaxValue, int.MinValue, out ulong dword))
            {
                modelRegCommand.ListValueNumber = dword;
                modelRegCommand.ListValueData = FormatDword(dword);
            }
            if (modelRegCommand.ListValueType == "REG_QWORD" && TryParseNumber(modelRegCommand.ListValueData, ulong.MaxValue, long.MinValue, out ulong qword))
            {
                modelRegCommand.ListValueNumber = qword;
                modelRegCommand.ListValueData = FormatQword(qword);
            }
            if (modelRegCommand.ListValueType == "REG_EXPAND_SZ")
            {
                modelRegCommand.ListValueData = Environment.ExpandEnvironmentVariables(modelRegCommand.ListValueData);
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

        public static void QueryFromRegEdit(IProgress<int>? progress)
        {
            Dictionary<string, QueryKey?> keys = new Dictionary<string, QueryKey?>(StringComparer.OrdinalIgnoreCase);
            int done = 0;
            try
            {
                foreach (ModelRegCommand mo in ObsColl_RegCommands)
                {
                    done++;
                    if (progress != null && (done % 50 == 0 || done == ObsColl_RegCommands.Count))
                    {
                        progress.Report(done);
                    }
                    if (mo.ListMatchWithRegEdit == "Ignored")
                    {
                        continue;
                    }
                    QueryRow(mo, keys);
                }
            }
            finally
            {
                foreach (QueryKey? key in keys.Values)
                {
                    key?.Key.Dispose();
                }
            }
        }

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

        private static QueryKey? OpenQueryKey(Dictionary<string, QueryKey?> keys, string listBasekey, string listSubKey)
        {
            string path = listBasekey + "\\" + listSubKey;
            if (!keys.TryGetValue(path, out QueryKey? key))
            {
                RegistryKey? opened = RootKey(listBasekey)?.OpenSubKey(listSubKey);
                key = opened == null ? null : new QueryKey(opened);
                keys[path] = key;
            }
            return key;
        }

        private static void QueryRow(ModelRegCommand mo, Dictionary<string, QueryKey?> keys)
        {
            try
            {
                mo.AvailableSubKey = string.Empty;
                mo.AvailableValueName = "This_field_is_NULL";
                mo.RegEditValueType = "This_field_is_NULL";
                mo.RegEditValueData = "This_field_is_NULL";
                mo.ReturnObject = "This_field_is_NULL";
                mo.DataDiffers = false;
                mo.TypeDiffers = false;
                QueryKey? queryKey = OpenQueryKey(keys, mo.ListBaseKey, mo.ListSubKey);
                RegistryKey? registryKey = queryKey?.Key;
                if (queryKey != null && registryKey != null)
                {
                    mo.AvailableSubKey = "It's Available";
                    if (mo.ListValueName != "This_field_is_NULL")
                    {
                        if (queryKey.ValueNames.Contains(mo.ListValueName))
                        {
                            mo.ReturnObject = registryKey.GetValue(mo.ListValueName)!;
                            mo.AvailableValueName = "It's Available";
                            RegistryValueKind registryValueKind = registryKey.GetValueKind(mo.ListValueName);
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
                            if (string.Equals(mo.RegEditValueData, mo.ListValueData, StringComparison.OrdinalIgnoreCase))
                            {
                                mo.ListMatchWithRegEdit = "True";
                            }
                            else
                            {
                                mo.ListMatchWithRegEdit = "False";
                                mo.DataDiffers = mo.ListCommand == "reg add";
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
                    else
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
                    mo.ListMatchWithRegEdit = "True";
                }
                if (mo.ListCommand == "reg add" && mo.ListValueType != "This_field_is_NULL" && mo.RegEditValueType != "This_field_is_NULL" && mo.ListValueType != mo.RegEditValueType)
                {
                    mo.ListMatchWithRegEdit = "False";
                    mo.TypeDiffers = true;
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("log.txt", "---vvv ! ! ! Warning ! ! ! vvv--- QueryMethod " + DateTime.Now.ToString() + "\n");
                File.AppendAllText("log.txt", "     " + mo.RegCommandId + "-" + mo.ListId + "-" + mo.ListCommand + "-" + mo.ListBaseKey + "\\" + mo.ListSubKey + "-" + mo.ListValueName + "-" + mo.ListValueType + "-" + mo.ListValueData + "\n     " + mo.AvailableSubKey + "-" + mo.AvailableValueName + "-" + mo.RegEditValueType + "-" + mo.RegEditValueData + "\n");
                File.AppendAllText("log.txt", ex.ToString() + "\n---^^^ ! ! ! Warning ! ! ! ^^^---\n");
            }
        }

        private static string FullBaseKeyName(string listBasekey)
        {
            switch (listBasekey.ToUpperInvariant())
            {
            case "HKEY_CLASSES_ROOT":
            case "HKCR":
                return "HKEY_CLASSES_ROOT";
            case "HKEY_CURRENT_USER":
            case "HKCU":
                return "HKEY_CURRENT_USER";
            case "HKEY_LOCAL_MACHINE":
            case "HKLM":
                return "HKEY_LOCAL_MACHINE";
            case "HKEY_USERS":
            case "HKU":
                return "HKEY_USERS";
            case "HKEY_CURRENT_CONFIG":
            case "HKCC":
                return "HKEY_CURRENT_CONFIG";
            case "HKEY_PERFORMANCE_DATA":
            case "HKPD":
                return "HKEY_PERFORMANCE_DATA";
            default:
                return listBasekey;
            }
        }

        private static RegistryKey? RootKey(string listBasekey)
        {
            switch (listBasekey.ToUpperInvariant())
            {
            case "HKEY_CLASSES_ROOT":
            case "HKCR":
                return Registry.ClassesRoot;
            case "HKEY_CURRENT_USER":
            case "HKCU":
                return Registry.CurrentUser;
            case "HKEY_LOCAL_MACHINE":
            case "HKLM":
                return Registry.LocalMachine;
            case "HKEY_USERS":
            case "HKU":
                return Registry.Users;
            case "HKEY_CURRENT_CONFIG":
            case "HKCC":
                return Registry.CurrentConfig;
            case "HKEY_PERFORMANCE_DATA":
            case "HKPD":
                return Registry.PerformanceData;
            default:
                return null;
            }
        }

        private static bool AvailableSubKeyMethodTrue(string listBasekey, string listSubKey)
        {
            registryKey = RootKey(listBasekey)?.OpenSubKey(listSubKey, writable: true);
            return registryKey != null;
        }

        private static bool CreateSubKeyMethod(string listBasekey, string listSubKey)
        {
            registryKey = RootKey(listBasekey)?.CreateSubKey(listSubKey, writable: true);
            return registryKey != null;
        }

        public static async Task ExecuteMissingCommands(Func<ModelRegCommand, bool>? include, IProgress<int>? progress)
        {
            List<ModelRegCommand> rows = include == null ? ObsColl_RegCommands.ToList() : ObsColl_RegCommands.Where(include).ToList();
            int done = 0;
            foreach (ModelRegCommand mo in rows)
            {
                done++;
                if (progress != null && (done % 10 == 0 || done == rows.Count))
                {
                    progress.Report(done);
                }
                try
                {
                    if (mo.ListMatchWithRegEdit == "False")
                    {
                        if (RootKey(mo.ListBaseKey) == null)
                        {
                            throw new InvalidOperationException("\"" + mo.ListBaseKey + "\" is not a base key RegEdit has.");
                        }
                        if (mo.ListCommand == "reg delete" && mo.DeleteAllValues)
                        {
                            await Task.Run(() =>
                            {
                                AvailableSubKeyMethodTrue(mo.ListBaseKey, mo.ListSubKey);
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
                            RootKey(mo.ListBaseKey)?.DeleteSubKeyTree(mo.ListSubKey);
                        }
                        else if (mo.ListCommand == "reg delete" && mo.AvailableValueName == "It's Available")
                        {
                            await Task.Run(() =>
                            {
                                AvailableSubKeyMethodTrue(mo.ListBaseKey, mo.ListSubKey);
                            });
                            if (registryKey != null)
                            {
                                registryKey.DeleteValue(mo.ListValueName);
                            }
                        }
                        else if (mo.ListCommand == "reg add" && mo.ListValueName == "This_field_is_NULL")
                        {
                            CreateSubKeyMethod(mo.ListBaseKey, mo.ListSubKey);
                        }
                        else if (mo.ListCommand == "reg add" && mo.ListValueName != "This_field_is_NULL")
                        {
                            await Task.Run(() =>
                            {
                                CreateSubKeyMethod(mo.ListBaseKey, mo.ListSubKey);
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
                                registryKey.SetValue(mo.ListValueName, mo.ListValueData.Split("\\0"), RegistryValueKind.MultiString);
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
                                registryKey.SetValue(mo.ListValueName, Encoding.Unicode.GetBytes(mo.ListValueData), RegistryValueKind.None);
                            }
                        }
                    }
                    mo.ExecuteResult = "successful";
                }
                catch (Exception ex)
                {
                    File.AppendAllText("log.txt", "---vvv ! ! ! Warning ! ! ! vvv--- ExecuteMethod" + DateTime.Now.ToString() + "\n");
                    File.AppendAllText("log.txt", "     " + mo.RegCommandId + "-" + mo.ListId + "-" + mo.ListCommand + "-" + mo.ListBaseKey + "\\" + mo.ListSubKey + "-" + mo.ListValueName + "-" + mo.ListValueType + "-" + mo.ListValueData + "\n     " + mo.AvailableSubKey + "-" + mo.AvailableValueName + "-" + mo.RegEditValueType + "-" + mo.RegEditValueData + "\n");
                    File.AppendAllText("log.txt", ex.ToString() + "\n---^^^ ! ! ! Warning ! ! ! ^^^---\n");
                    mo.ExecuteResult = "Error";
                }
                finally
                {
                    registryKey?.Dispose();
                    registryKey = null;
                }
            }
        }

        private static ulong ListNumber(ModelRegCommand mo)
        {
            return mo.ListValueNumber ?? throw new FormatException("\"" + mo.ListValueData + "\" is not a valid " + mo.ListValueType + " number.");
        }

        public static void JumpToRegEdit(ModelRegCommand mo)
        {
            try
            {
                RegistryKey? root = RootKey(mo.ListBaseKey);
                if (root == null)
                {
                    return;
                }
                foreach (Process running in Process.GetProcessesByName("regedit"))
                {
                    using (running)
                    {
                        try
                        {
                            running.Kill();
                            running.WaitForExit(2000);
                        }
                        catch (Exception ex)
                        {
                            WriteLog("JumpToRegEdit could not close RegEdit", ex);
                        }
                    }
                }
                string lastKey = "Computer\\" + root.Name + (mo.ListSubKey.Length > 0 ? "\\" + mo.ListSubKey : string.Empty);
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
            if (File.Exists("Ignore.txt"))
            {
                foreach (string line in File.ReadLines("Ignore.txt"))
                {
                    if (line.Trim().Length > 0 && !IgnoreList.Contains(line.Trim()))
                    {
                        IgnoreList.Add(line.Trim());
                    }
                }
            }
        }

        public static void AddToIgnoreList(IEnumerable<ModelRegCommand> rows)
        {
            foreach (ModelRegCommand mo in rows)
            {
                if (!IgnoreList.Contains(mo.ListLineString))
                {
                    IgnoreList.Add(mo.ListLineString);
                }
                mo.ListMatchWithRegEdit = "Ignored";
            }
            SaveIgnoreFile();
        }

        public static void RemoveFromIgnoreList(IEnumerable<ModelRegCommand> rows)
        {
            foreach (ModelRegCommand mo in rows)
            {
                IgnoreList.Remove(mo.ListLineString);
                if (mo.ListMatchWithRegEdit == "Ignored")
                {
                    mo.ListMatchWithRegEdit = "Unknown";
                }
            }
            SaveIgnoreFile();
        }

        private static void SaveIgnoreFile()
        {
            try
            {
                File.WriteAllLines("Ignore.txt", IgnoreList);
            }
            catch (Exception ex)
            {
                WriteLog("SaveIgnoreFile", ex);
            }
        }

        public static void WriteLog(string method, Exception ex)
        {
            try
            {
                File.AppendAllText("log.txt", "---vvv ! ! ! Warning ! ! ! vvv--- " + method + " " + DateTime.Now.ToString() + "\n");
                File.AppendAllText("log.txt", ex.ToString() + "\n---^^^ ! ! ! Warning ! ! ! ^^^---\n");
            }
            catch (IOException)
            {
            }
        }
    }
}
