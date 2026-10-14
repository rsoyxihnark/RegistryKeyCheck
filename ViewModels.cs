using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace RegistryKey_Check
{
    internal static class ViewModels
    {
        public static ObservableCollection<ModelRegCommand> ObsColl_RegCommands = new ObservableCollection<ModelRegCommand>();

        public static ObservableCollection<ModelNonRegCommand> ObsColl_Non_RegCommands = new ObservableCollection<ModelNonRegCommand>();

        public static int RegCommandIds;

        public static int Non_RegCommandIds;

        public static int ListIds;

        public static int DisplayingIDs;

        private static string lineTrimed = string.Empty;

        private static readonly RegexOptions optionsNone = RegexOptions.None;

        private static readonly Regex regexSpace1 = new Regex("[ ]{2,}", optionsNone);

        private static readonly Regex regexBaseKey = new Regex("(?<= \")(.*?)(?=\\\\)", optionsNone);

        private static readonly Regex regexSubKey = new Regex("(?<=\\\\)(.*?)(?=\")", optionsNone);

        private static readonly Regex regexValueName = new Regex("(?<= \\/v \")(.*?)(?=\")", optionsNone);

        private static readonly Regex regexValueName2 = new Regex(" \\/v \\S*", optionsNone);

        private static readonly Regex regexType = new Regex("(?<= \\/t \")(.*?)(?=\")", optionsNone);

        private static readonly Regex regexType2 = new Regex(" \\/t \\S*", optionsNone);

        private static readonly Regex regexValueData = new Regex("(?<= \\/d \")(.*?)(?=\")", optionsNone);

        private static readonly Regex regexValueData2 = new Regex(" \\/d \\S*", optionsNone);

        private static RegistryKey? registryKey;

        private static RegistryValueKind registryValueKind;

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
            foreach (string line in File.ReadLines("List.txt"))
            {
                ListIds++;
                ModelRegCommand modelRegCommand = new ModelRegCommand();
                ModelNonRegCommand modelNonRegCommand = new ModelNonRegCommand();
                lineTrimed = line.Trim().Replace(" /ve", " /v \"\"");
                lineTrimed = regexSpace1.Replace(lineTrimed, " ");
                if (lineTrimed.Length > 11 && lineTrimed.Substring(0, 11).ToLower() == "reg delete ")
                {
                    modelRegCommand.RegCommandId = ++RegCommandIds;
                    modelRegCommand.ListId = ListIds;
                    modelRegCommand.ListCommand = "reg delete";
                    modelRegCommand.ListBaseKey = regexBaseKey.Match(lineTrimed).Value;
                    modelRegCommand.ListSubKey = regexSubKey.Match(lineTrimed).Value;
                    if (regexValueName.IsMatch(lineTrimed))
                    {
                        modelRegCommand.ListValueName = regexValueName.Match(lineTrimed).Value;
                    }
                    else if (regexValueName2.IsMatch(lineTrimed))
                    {
                        modelRegCommand.ListValueName = regexValueName2.Match(lineTrimed).Value.Replace(" /v ", "");
                    }
                    else
                    {
                        modelRegCommand.ListValueName = "This_field_is_NULL";
                    }
                    modelRegCommand.ListMatchWithRegEdit = "Unknown";
                    ObsColl_RegCommands.Add(modelRegCommand);
                }
                else if (lineTrimed.Length > 8 && lineTrimed.Substring(0, 8).ToLower() == "reg add ")
                {
                    modelRegCommand.RegCommandId = ++RegCommandIds;
                    modelRegCommand.ListId = ListIds;
                    modelRegCommand.ListCommand = "reg add";
                    modelRegCommand.ListBaseKey = regexBaseKey.Match(lineTrimed).Value;
                    modelRegCommand.ListSubKey = regexSubKey.Match(lineTrimed).Value;
                    if (regexValueName.IsMatch(lineTrimed))
                    {
                        modelRegCommand.ListValueName = regexValueName.Match(lineTrimed).Value;
                        if (regexType.IsMatch(lineTrimed))
                        {
                            modelRegCommand.ListValueType = regexType.Match(lineTrimed).Value.ToUpper();
                        }
                        else if (regexType2.IsMatch(lineTrimed))
                        {
                            modelRegCommand.ListValueType = regexType2.Match(lineTrimed).Value.Replace(" /t ", "").ToUpper();
                        }
                        else
                        {
                            modelRegCommand.ListValueType = "REG_SZ";
                        }
                        if (regexValueData.IsMatch(lineTrimed))
                        {
                            modelRegCommand.ListValueData = regexValueData.Match(lineTrimed).Value;
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
                            if (modelRegCommand.ListValueType == "REG_MULTI_SZ" && modelRegCommand.ListValueData.Length >= 2 && modelRegCommand.ListValueData.Substring(modelRegCommand.ListValueData.Length - 2, 2) == "\\0")
                            {
                                modelRegCommand.ListValueData = modelRegCommand.ListValueData[..^2];
                            }
                        }
                        else if (regexValueData2.IsMatch(lineTrimed))
                        {
                            modelRegCommand.ListValueData = regexValueData2.Match(lineTrimed).Value.Replace(" /d ", "");
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
                            if (modelRegCommand.ListValueType == "REG_MULTI_SZ" && modelRegCommand.ListValueData.Length >= 2 && modelRegCommand.ListValueData.Substring(modelRegCommand.ListValueData.Length - 2, 2) == "\\0")
                            {
                                modelRegCommand.ListValueData = modelRegCommand.ListValueData[..^2];
                            }
                        }
                        else
                        {
                            modelRegCommand.ListValueData = "";
                        }
                    }
                    else if (regexValueName2.IsMatch(lineTrimed))
                    {
                        modelRegCommand.ListValueName = regexValueName2.Match(lineTrimed).Value.Replace(" /v ", "");
                        if (regexType.IsMatch(lineTrimed))
                        {
                            modelRegCommand.ListValueType = regexType.Match(lineTrimed).Value.ToUpper();
                        }
                        else if (regexType2.IsMatch(lineTrimed))
                        {
                            modelRegCommand.ListValueType = regexType2.Match(lineTrimed).Value.Replace(" /t ", "").ToUpper();
                        }
                        else
                        {
                            modelRegCommand.ListValueType = "REG_SZ";
                        }
                        if (regexValueData.IsMatch(lineTrimed))
                        {
                            modelRegCommand.ListValueData = regexValueData.Match(lineTrimed).Value;
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
                            if (modelRegCommand.ListValueType == "REG_MULTI_SZ" && modelRegCommand.ListValueData.Length >= 2 && modelRegCommand.ListValueData.Substring(modelRegCommand.ListValueData.Length - 2, 2) == "\\0")
                            {
                                modelRegCommand.ListValueData = modelRegCommand.ListValueData[..^2];
                            }
                        }
                        else if (regexValueData2.IsMatch(lineTrimed))
                        {
                            modelRegCommand.ListValueData = regexValueData2.Match(lineTrimed).Value.Replace(" /d ", "");
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
                            if (modelRegCommand.ListValueType == "REG_MULTI_SZ" && modelRegCommand.ListValueData.Length >= 2 && modelRegCommand.ListValueData.Substring(modelRegCommand.ListValueData.Length - 2, 2) == "\\0")
                            {
                                modelRegCommand.ListValueData = modelRegCommand.ListValueData[..^2];
                            }
                        }
                        else
                        {
                            modelRegCommand.ListValueData = "";
                        }
                    }
                    else
                    {
                        modelRegCommand.ListValueName = "This_field_is_NULL";
                    }
                    if (modelRegCommand.ListValueType == "REG_DWORD" && modelRegCommand.ListValueData.Contains("0x"))
                    {
                        modelRegCommand.ListValueData = Convert.ToInt32(modelRegCommand.ListValueData[2..], 16).ToString();
                    }
                    if (modelRegCommand.ListValueType == "REG_EXPAND_SZ")
                    {
                        modelRegCommand.ListValueData = modelRegCommand.ListValueData.Replace("%SystemRoot%", Environment.ExpandEnvironmentVariables("%SystemRoot%"));
                    }
                    modelRegCommand.ListMatchWithRegEdit = "Unknown";
                    ObsColl_RegCommands.Add(modelRegCommand);
                }
                else
                {
                    modelNonRegCommand.Non_RegCommandId = ++Non_RegCommandIds;
                    modelNonRegCommand.ListId = ListIds;
                    modelNonRegCommand.ListLineString = line;
                    ObsColl_Non_RegCommands.Add(modelNonRegCommand);
                }
            }
            Shown = ObsColl_RegCommands.Count;
            Total = ObsColl_RegCommands.Count;
        }

        public static void QueryFromRegEdit()
        {
            foreach (ModelRegCommand mo in ObsColl_RegCommands)
            {
                try
                {
                    if (AvailableSubKeyMethod(mo.ListBaseKey, mo.ListSubKey))
                    {
                        mo.AvailableSubKey = "It's Available";
                        if (registryKey != null && mo.ListValueName != "This_field_is_NULL")
                        {
                            if (Enumerable.Contains(registryKey.GetValueNames(), mo.ListValueName))
                            {
                                mo.ReturnObject = registryKey.GetValue(mo.ListValueName)!;
                                mo.AvailableValueName = "It's Available";
                                registryValueKind = registryKey.GetValueKind(mo.ListValueName);
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
                                    mo.RegEditValueData = Convert.ToString((int)mo.ReturnObject);
                                }
                                else if (registryValueKind == RegistryValueKind.QWord)
                                {
                                    mo.RegEditValueType = "REG_QWORD";
                                    mo.RegEditValueData = Convert.ToString((long)mo.ReturnObject);
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
                                if (mo.RegEditValueData == mo.ListValueData)
                                {
                                    mo.ListMatchWithRegEdit = "True";
                                }
                                else if (mo.RegEditValueData != mo.ListValueData)
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
                        else if (registryKey != null && mo.ListValueName == "This_field_is_NULL")
                        {
                            if (mo.ListCommand == "reg add")
                            {
                                mo.ListMatchWithRegEdit = "True";
                            }
                            else if (mo.ListCommand == "reg delete")
                            {
                                mo.ListMatchWithRegEdit = "False";
                            }
                        }
                        else if (registryKey == null && mo.ListValueName == "This_field_is_NULL")
                        {
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
                    else if (!AvailableSubKeyMethod(mo.ListBaseKey, mo.ListSubKey))
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
                    }
                }
                catch (Exception ex)
                {
                    File.AppendAllText("log.txt", "---vvv ! ! ! Warning ! ! ! vvv--- QueryMethod " + DateTime.Now.ToString() + "\n");
                    File.AppendAllText("log.txt", "     " + mo.RegCommandId + "-" + mo.ListId + "-" + mo.ListCommand + "-" + mo.ListBaseKey + "\\" + mo.ListSubKey + "-" + mo.ListValueName + "-" + mo.ListValueType + "-" + mo.ListValueData + "\n     " + mo.AvailableSubKey + "-" + mo.AvailableValueName + "-" + mo.RegEditValueType + "-" + mo.RegEditValueData + "\n");
                    File.AppendAllText("log.txt", ex.ToString() + "\n---^^^ ! ! ! Warning ! ! ! ^^^---\n");
                }
            }
        }

        private static bool AvailableSubKeyMethod(string listBasekey, string listSubKey)
        {
            registryKey = null;
            switch (listBasekey)
            {
            case "HKEY_CLASSES_ROOT":
            case "HKCR":
                registryKey = Registry.ClassesRoot.OpenSubKey(listSubKey);
                break;
            case "HKEY_CURRENT_USER":
            case "HKCU":
                registryKey = Registry.CurrentUser.OpenSubKey(listSubKey);
                break;
            case "HKEY_LOCAL_MACHINE":
            case "HKLM":
                registryKey = Registry.LocalMachine.OpenSubKey(listSubKey);
                break;
            case "HKEY_USERS":
            case "HKU":
                registryKey = Registry.Users.OpenSubKey(listSubKey);
                break;
            case "HKEY_CURRENT_CONFIG":
            case "HKCC":
                registryKey = Registry.CurrentConfig.OpenSubKey(listSubKey);
                break;
            case "HKEY_PERFORMANCE_DATA":
            case "HKPD":
                registryKey = Registry.PerformanceData.OpenSubKey(listSubKey);
                break;
            }
            if (registryKey != null)
            {
                return true;
            }
            return false;
        }

        private static bool AvailableSubKeyMethodTrue(string listBasekey, string listSubKey)
        {
            registryKey = null;
            switch (listBasekey)
            {
            case "HKEY_CLASSES_ROOT":
            case "HKCR":
                registryKey = Registry.ClassesRoot.OpenSubKey(listSubKey, writable: true);
                break;
            case "HKEY_CURRENT_USER":
            case "HKCU":
                registryKey = Registry.CurrentUser.OpenSubKey(listSubKey, writable: true);
                break;
            case "HKEY_LOCAL_MACHINE":
            case "HKLM":
                registryKey = Registry.LocalMachine.OpenSubKey(listSubKey, writable: true);
                break;
            case "HKEY_USERS":
            case "HKU":
                registryKey = Registry.Users.OpenSubKey(listSubKey, writable: true);
                break;
            case "HKEY_CURRENT_CONFIG":
            case "HKCC":
                registryKey = Registry.CurrentConfig.OpenSubKey(listSubKey, writable: true);
                break;
            case "HKEY_PERFORMANCE_DATA":
            case "HKPD":
                registryKey = Registry.PerformanceData.OpenSubKey(listSubKey, writable: true);
                break;
            }
            if (registryKey != null)
            {
                return true;
            }
            return false;
        }

        private static bool CreateSubKeyMethod(string listBasekey, string listSubKey)
        {
            registryKey = null;
            switch (listBasekey)
            {
            case "HKEY_CLASSES_ROOT":
            case "HKCR":
                registryKey = Registry.ClassesRoot.CreateSubKey(listSubKey, writable: true);
                break;
            case "HKEY_CURRENT_USER":
            case "HKCU":
                registryKey = Registry.CurrentUser.CreateSubKey(listSubKey, writable: true);
                break;
            case "HKEY_LOCAL_MACHINE":
            case "HKLM":
                registryKey = Registry.LocalMachine.CreateSubKey(listSubKey, writable: true);
                break;
            case "HKEY_USERS":
            case "HKU":
                registryKey = Registry.Users.CreateSubKey(listSubKey, writable: true);
                break;
            case "HKEY_CURRENT_CONFIG":
            case "HKCC":
                registryKey = Registry.CurrentConfig.CreateSubKey(listSubKey, writable: true);
                break;
            case "HKEY_PERFORMANCE_DATA":
            case "HKPD":
                registryKey = Registry.PerformanceData.CreateSubKey(listSubKey, writable: true);
                break;
            }
            if (registryKey != null)
            {
                return true;
            }
            return false;
        }

        public static async Task ExecuteMissingCommands()
        {
            foreach (ModelRegCommand mo in ObsColl_RegCommands)
            {
                try
                {
                    if (mo.ListMatchWithRegEdit == "False")
                    {
                        if (mo.ListCommand == "reg delete" && mo.ListValueName == "This_field_is_NULL")
                        {
                            if (mo.ListBaseKey == "HKEY_CLASSES_ROOT" || mo.ListBaseKey == "HKCR")
                            {
                                Registry.ClassesRoot.DeleteSubKeyTree(mo.ListSubKey);
                            }
                            else if (mo.ListBaseKey == "HKEY_CURRENT_USER" || mo.ListBaseKey == "HKCU")
                            {
                                Registry.CurrentUser.DeleteSubKeyTree(mo.ListSubKey);
                            }
                            else if (mo.ListBaseKey == "HKEY_LOCAL_MACHINE" || mo.ListBaseKey == "HKLM")
                            {
                                Registry.LocalMachine.DeleteSubKeyTree(mo.ListSubKey);
                            }
                            else if (mo.ListBaseKey == "HKEY_USERS" || mo.ListBaseKey == "HKU")
                            {
                                Registry.Users.DeleteSubKeyTree(mo.ListSubKey);
                            }
                            else if (mo.ListBaseKey == "HKEY_CURRENT_CONFIG" || mo.ListBaseKey == "HKCC")
                            {
                                Registry.CurrentConfig.DeleteSubKeyTree(mo.ListSubKey);
                            }
                            else if (mo.ListBaseKey == "HKEY_PERFORMANCE_DATA" || mo.ListBaseKey == "HKPD")
                            {
                                Registry.PerformanceData.DeleteSubKeyTree(mo.ListSubKey);
                            }
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
                                registryKey.SetValue(mo.ListValueName, int.Parse(mo.ListValueData), RegistryValueKind.DWord);
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
                                registryKey.SetValue(mo.ListValueName, long.Parse(mo.ListValueData), RegistryValueKind.QWord);
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
            }
        }
    }
}
