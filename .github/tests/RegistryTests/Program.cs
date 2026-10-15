using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.Win32;

const string TestKey = @"Software\RegistryKeyCheckTest";
const string LmKey = @"SOFTWARE\RegistryKeyCheckTest";
string dll = Path.GetFullPath(args[0]);
Type viewModels = Assembly.LoadFrom(dll).GetType("RegistryKey_Check.ViewModels", true)!;
string work = Path.Combine(Path.GetTempPath(), "RegistryKeyCheckTest");
int failures = 0;

void Check(bool ok, string what)
{
    Console.WriteLine((ok ? "PASS " : "FAIL ") + what);
    if (!ok)
    {
        failures++;
    }
}

int RunProcess(string file, string arguments)
{
    ProcessStartInfo info = new ProcessStartInfo(file) { Arguments = arguments, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
    using Process process = Process.Start(info)!;
    process.StandardOutput.ReadToEnd();
    process.StandardError.ReadToEnd();
    process.WaitForExit();
    return process.ExitCode;
}

int RunCmd(string line)
{
    return RunProcess("cmd.exe", "/c " + line);
}

RegistryKey LocalMachine(RegistryView view)
{
    return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
}

void CleanUp()
{
    Registry.CurrentUser.DeleteSubKeyTree(TestKey, false);
    foreach (RegistryView view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
    {
        using RegistryKey lm = LocalMachine(view);
        lm.DeleteSubKeyTree(LmKey, false);
    }
}

IEnumerable Rows()
{
    return (IEnumerable)viewModels.GetField("ObsColl_RegCommands")!.GetValue(null)!;
}

int NonRegCount()
{
    return ((IEnumerable)viewModels.GetField("ObsColl_Non_RegCommands")!.GetValue(null)!).Cast<object>().Count();
}

string Detail(string line)
{
    foreach (object row in Rows())
    {
        Type type = row.GetType();
        if ((string)type.GetProperty("ListLineString")!.GetValue(row)! == line)
        {
            return (string)type.GetProperty("ComplianceDetails")!.GetValue(row)!;
        }
    }
    return "missing";
}

List<(string Line, string Status, string Data, string Result)> Run(string[] list, string[] ignore, bool execute)
{
    Directory.CreateDirectory(work);
    File.WriteAllLines(Path.Combine(work, "List.txt"), list);
    File.WriteAllLines(Path.Combine(work, "Ignore.txt"), ignore);
    Directory.SetCurrentDirectory(work);
    viewModels.GetMethod("ReadListFile")!.Invoke(null, null);
    viewModels.GetMethod("QueryFromRegEdit")!.Invoke(null, null);
    if (execute)
    {
        ((Task)viewModels.GetMethod("ExecuteMissingCommands")!.Invoke(null, null)!).GetAwaiter().GetResult();
        viewModels.GetMethod("QueryFromRegEdit")!.Invoke(null, null);
    }
    List<(string, string, string, string)> rows = new List<(string, string, string, string)>();
    foreach (object row in Rows())
    {
        Type type = row.GetType();
        rows.Add(((string)type.GetProperty("ListLineString")!.GetValue(row)!, (string)type.GetProperty("ListMatchWithRegEdit")!.GetValue(row)!, (string)type.GetProperty("RegEditValueData")!.GetValue(row)!, (string)type.GetProperty("ExecuteResult")!.GetValue(row)!));
    }
    return rows;
}

CleanUp();
try
{
    string a = @"HKCU\Software\RegistryKeyCheckTest\A";
    string lmPath = @"HKLM\SOFTWARE\RegistryKeyCheckTest";
    using (RegistryKey va = Registry.CurrentUser.CreateSubKey(TestKey + @"\A\VA"))
    {
        va.SetValue("One", "1");
        va.SetValue("Two", 2, RegistryValueKind.DWord);
        va.CreateSubKey("Child").Dispose();
    }
    string[] regExe =
    {
        $"reg add \"{a}\" /v \"KucukbuyuK\" /t REG_SZ /d \"test\" /f",
        $"reg add \"{a}\" /v \"Attr\" /t REG_DWORD /d \"0xb0940064\" /f",
        $"reg add \"{a}\" /v \"Small\" /t REG_DWORD /d \"0xa\" /f",
        $"reg add \"{a}\" /v \"Big\" /t REG_DWORD /d 4294967295 /f",
        $"reg add \"{a}\" /v \"Bin\" /t REG_BINARY /d 1a3761592352350c7a5f /f",
        $"reg add \"{a}\" /v \"Zero\" /t REG_BINARY /d \"00\" /f",
        $"reg add \"{a}\\command\" /ve /d \"regedit.exe \"%1\"\" /f",
        $"reg add \"{a}\\command2\" /ve /d \"\\\"C:\\Program Files\\x.exe\\\" \\\"%1\\\"\" /f",
        $"reg add \"{a}\" /v \"Q\" /t REG_QWORD /d 0x10 /f",
        $"reg add \"{a}\" /v \"M\" /t REG_MULTI_SZ /d \"a\\0b\" /f",
        $"reg add \"{a}\" /v \"Spaces\" /d \"a  b\" /f",
        $"reg add {a} /v Unquoted /t REG_SZ /d plain /f",
        $"reg add \"{a}\\OnlyKey\" /f",
        $"reg add \"{a}\" /v \"Exp\" /t REG_EXPAND_SZ /d \"%SystemRoot%\\system32\" /f",
        $"reg add \"{a}\" /v \"ExpProgramFiles\" /t REG_EXPAND_SZ /d \"%ProgramFiles%\\Test\" /f",
        $"reg add \"{a}\" /v \"Odd\" /t REG_BINARY /d abc /f",
        $"reg add \"{a}\" /v \"None\" /t REG_NONE /d \"deadbeef\" /f",
        $"reg add \"{a}\" /v \"MS\" /t REG_MULTI_SZ /s \",\" /d \"a,b\" /f",
        $"reg delete \"{a}\\VA\" /va /f",
        $"reg add \"{lmPath}\" /v \"View\" /d \"32\" /f /reg:32",
        $"reg add \"{lmPath}\" /v \"View\" /d \"64\" /f /reg:64"
    };
    foreach (string line in regExe)
    {
        Check(RunCmd(line) == 0, "reg.exe ran " + line);
    }
    Check(RunCmd($"reg add \"{a}\" /v \"NoData\" /t REG_DWORD /f") != 0, "reg.exe refuses a REG_DWORD line with no /d");
    string expRaw = $"add \"{a}\" /v \"ExpRaw\" /t REG_EXPAND_SZ /d \"%SystemRoot%\\system32\" /f";
    Check(RunProcess("reg.exe", expRaw) == 0, "reg.exe ran without cmd reg " + expRaw);
    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(TestKey + @"\A")!)
    {
        Check((string?)key.GetValue("ExpRaw", null, RegistryValueOptions.DoNotExpandEnvironmentNames) == @"%SystemRoot%\system32", "reg.exe kept %SystemRoot% in ExpRaw");
        Check(((byte[])key.GetValue("Odd")!).SequenceEqual(new byte[] { 0x0a, 0xbc }), "reg.exe wrote REG_BINARY abc as 0a bc: " + Convert.ToHexString((byte[])key.GetValue("Odd")!));
        Check(((string[])key.GetValue("MS")!).SequenceEqual(new[] { "a", "b" }), "reg.exe split REG_MULTI_SZ a,b with /s");
    }
    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(TestKey + @"\A\VA")!)
    {
        Check(key.GetValueNames().Length == 0 && key.OpenSubKey("Child") != null, "reg.exe /va deleted the values of VA and kept its subkey");
    }
    Type model = viewModels.Assembly.GetType("RegistryKey_Check.ModelRegCommand", true)!;
    MethodInfo regEditPath = viewModels.GetMethod("RegEditPath", BindingFlags.NonPublic | BindingFlags.Static)!;
    string JumpPath(string subKey, RegistryView view)
    {
        object mo = Activator.CreateInstance(model)!;
        model.GetProperty("ListBaseKey")!.SetValue(mo, "HKLM");
        model.GetProperty("ListSubKey")!.SetValue(mo, subKey);
        model.GetProperty("ListView")!.SetValue(mo, view);
        return (string?)regEditPath.Invoke(null, new[] { mo }) ?? "null";
    }
    string jump32 = JumpPath(LmKey, RegistryView.Registry32);
    Check(string.Equals(jump32, @"Computer\HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\RegistryKeyCheckTest", StringComparison.OrdinalIgnoreCase), "Jump to RegEdit opens the 32-bit key of a /reg:32 line: " + jump32);
    string jump64 = JumpPath(LmKey, RegistryView.Registry64);
    Check(string.Equals(jump64, @"Computer\HKEY_LOCAL_MACHINE\SOFTWARE\RegistryKeyCheckTest", StringComparison.OrdinalIgnoreCase), "Jump to RegEdit opens the 64-bit key of a /reg:64 line: " + jump64);
    string jumpMissing = JumpPath(LmKey + @"\Missing", RegistryView.Registry32);
    Check(string.Equals(jumpMissing, @"Computer\HKEY_LOCAL_MACHINE\SOFTWARE\RegistryKeyCheckTest\Missing", StringComparison.OrdinalIgnoreCase), "Jump to RegEdit keeps the plain path of a missing /reg:32 key: " + jumpMissing);
    string[] listA = regExe.Concat(new[]
    {
        "reg add \"HKEY_CURRENT_USER\\software\\registrykeychecktest\\a\" /v \"KUCUKbuyuK\" /t REG_SZ /d \"TEST\" /f",
        "reg add \"hkey_current_user\\Software\\RegistryKeyCheckTest\\A\" /v \"small\" /t reg_dword /d 10 /f",
        $"reg delete \"{a}\" /v \"NotThere\" /f",
        "reg delete \"HKCU\\Software\\RegistryKeyCheckTest\\Nope\" /f",
        $"reg.exe add \"{a}\" /v \"KucukbuyuK\" /t REG_SZ /d \"test\" /f",
        $"@reg add \"{a}\" /v \"Small\" /t REG_DWORD /d \"0xa\" /f"
    }).ToArray();
    foreach ((string line, string status, string data, string result) in Run(listA, Array.Empty<string>(), false))
    {
        Check(status == "True", "Query RegEdit says " + status + " for " + line + " [" + data + "]");
        Check(Detail(line).EndsWith("Compliant"), "Compliance Details says " + Detail(line) + " for " + line);
    }

    string b = @"HKCU\Software\RegistryKeyCheckTest\B";
    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(TestKey + @"\B"))
    {
        key.SetValue("Name", "old");
        key.SetValue("DelVal", "x");
        key.CreateSubKey("Sub").Dispose();
    }
    string ignored = $"reg add \"{b}\" /v \"Ignored\" /d \"x\" /f";
    string[] listB =
    {
        $"reg add \"{b}\" /v \"NAME\" /d \"new\" /f",
        $"reg add \"{b}\" /v \"Attr\" /t REG_DWORD /d \"0xb0940064\" /f",
        $"reg add \"{b}\" /v \"Q\" /t REG_QWORD /d 0x10 /f",
        $"reg add \"{b}\" /v \"Bin\" /t REG_BINARY /d 0a0b /f",
        $"reg add \"{b}\" /v \"M\" /t REG_MULTI_SZ /d \"a\\0b\" /f",
        $"reg add \"{b}\\command\" /ve /d \"regedit.exe \"%1\"\" /f",
        $"reg add \"{b}\" /v \"ExpRaw\" /t REG_EXPAND_SZ /d \"%SystemRoot%\\system32\" /f",
        $"reg add \"{b}\" /v \"MS\" /t REG_MULTI_SZ /s \",\" /d \"a,b\" /f",
        $"reg add \"{b}\" /v \"None\" /t REG_NONE /d \"deadbeef\" /f",
        $"reg delete \"{b}\" /v \"DELVAL\" /f",
        $"reg delete \"{b}\\Sub\" /f",
        ignored
    };
    Dictionary<string, string> detailsB = new Dictionary<string, string>
    {
        [listB[0]] = "Value data differs",
        [listB[1]] = "Value not found in RegEdit",
        [listB[9]] = "Value still exists in RegEdit",
        [listB[10]] = "Key still exists in RegEdit",
        [ignored] = "On the Ignore List"
    };
    foreach ((string line, string status, string data, string result) in Run(listB, new[] { ignored }, false))
    {
        Check(status == (line == ignored ? "Ignored" : "False"), "Before Execute Missing Commands " + status + " for " + line);
        if (detailsB.TryGetValue(line, out string? expected))
        {
            Check(Detail(line) == expected, "Compliance Details says " + Detail(line) + " for " + line);
        }
    }
    foreach ((string line, string status, string data, string result) in Run(listB, new[] { ignored }, true))
    {
        Check(status == (line == ignored ? "Ignored" : "True"), "After Execute Missing Commands " + status + " for " + line + " [" + data + "]");
        Check(result == (line == ignored ? string.Empty : "successful"), "Execute Result says " + result + " for " + line);
        Check(Detail(line) == (line == ignored ? "On the Ignore List" : "Compliant"), "Compliance Details says " + Detail(line) + " after Execute Missing Commands for " + line);
    }
    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(TestKey + @"\B")!)
    {
        Check((string?)key.GetValue("Name") == "new", "NAME changed the existing Name value");
        Check(key.GetValueNames().Count(n => n.Equals("name", StringComparison.OrdinalIgnoreCase)) == 1, "Name was not added twice");
        Check(unchecked((uint)(int)key.GetValue("Attr")!) == 0xb0940064, "REG_DWORD 0xb0940064 was written");
        Check((long)key.GetValue("Q")! == 16, "REG_QWORD 0x10 was written");
        Check(((byte[])key.GetValue("Bin")!).SequenceEqual(new byte[] { 0x0a, 0x0b }), "REG_BINARY 0a0b was written as two bytes");
        Check(((string[])key.GetValue("M")!).SequenceEqual(new[] { "a", "b" }), "REG_MULTI_SZ a and b were written");
        Check(key.GetValue("DelVal") == null, "DELVAL deleted DelVal");
        Check(key.OpenSubKey("Sub") == null, "Sub key was deleted");
        Check(key.GetValue("Ignored") == null, "Ignored row was not executed");
        Check((string?)key.GetValue("ExpRaw", null, RegistryValueOptions.DoNotExpandEnvironmentNames) == @"%SystemRoot%\system32", "REG_EXPAND_SZ was written with %SystemRoot% kept");
        Check(((string[])key.GetValue("MS")!).SequenceEqual(new[] { "a", "b" }), "REG_MULTI_SZ a,b with /s was written as a and b");
        using RegistryKey regExeKey = Registry.CurrentUser.OpenSubKey(TestKey + @"\A")!;
        Check(((byte[])key.GetValue("None")!).SequenceEqual((byte[])regExeKey.GetValue("None")!), "REG_NONE was written the same as reg.exe: " + Convert.ToHexString((byte[])regExeKey.GetValue("None")!));
    }
    string typeLine = $"reg add \"{b}\" /v \"Name\" /t REG_DWORD /d 1 /f";
    string noKeyLine = $"reg add \"{b}\\NoSuchKey\" /v \"X\" /d \"1\" /f";
    Run(new[] { typeLine, noKeyLine }, Array.Empty<string>(), false);
    Check(Detail(typeLine) == "Value type differs", "Compliance Details says " + Detail(typeLine) + " for " + typeLine);
    Check(Detail(noKeyLine) == "Key not found in RegEdit", "Compliance Details says " + Detail(noKeyLine) + " for " + noKeyLine);

    string e = @"HKCU\Software\RegistryKeyCheckTest\E";
    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(TestKey + @"\E"))
    {
        key.SetValue(string.Empty, "default");
        key.SetValue("V1", "1");
        key.SetValue("V2", 2, RegistryValueKind.DWord);
        key.CreateSubKey("Keep").Dispose();
    }
    string[] listE = { $"reg delete \"{e}\" /va /f" };
    foreach ((string line, string status, string data, string result) in Run(listE, Array.Empty<string>(), false))
    {
        Check(status == "False", "Before Execute Missing Commands " + status + " for " + line);
        Check(Detail(line) == "Key still has values in RegEdit", "Compliance Details says " + Detail(line) + " for " + line);
    }
    foreach ((string line, string status, string data, string result) in Run(listE, Array.Empty<string>(), true))
    {
        Check(status == "True", "After Execute Missing Commands " + status + " for " + line);
        Check(Detail(line) == "Compliant", "Compliance Details says " + Detail(line) + " after Execute Missing Commands for " + line);
    }
    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(TestKey + @"\E")!)
    {
        Check(key.GetValueNames().Length == 0, "/va deleted every value of E");
        Check(key.OpenSubKey("Keep") != null, "/va kept the subkey Keep of E");
    }

    string trueLine = $"reg add \"{a}\" /v \"KucukbuyuK\" /t REG_SZ /d \"test\" /f";
    string misspelled = "reg add \"HKEY_CURRENT_USR\\Software\\RegistryKeyCheckTest\\F\" /v \"x\" /d \"1\" /f";
    string badType = "reg add \"HKCU\\Software\\RegistryKeyCheckTest\\F\" /v \"Foo\" /t REG_FOO /d \"1\" /f";
    string misspelledDelete = "reg delete \"HKEY_CURRENT_USR\\Software\\RegistryKeyCheckTest\\F\" /f";
    foreach ((string line, string status, string data, string result) in Run(new[] { trueLine, misspelled, badType, misspelledDelete }, Array.Empty<string>(), true))
    {
        Check(result == (line == trueLine ? string.Empty : "Error"), "Execute Result says " + result + " for " + line);
    }
    Check(Detail(misspelledDelete).EndsWith("Base key is not one RegEdit has"), "Compliance Details says " + Detail(misspelledDelete) + " for " + misspelledDelete);
    Check(Registry.CurrentUser.OpenSubKey(TestKey + @"\F") == null, "REG_FOO did not create its key");

    foreach (RegistryView view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
    {
        using RegistryKey lm = LocalMachine(view);
        lm.CreateSubKey(LmKey + @"\Del").Dispose();
    }
    string[] listG = { $"reg delete \"{lmPath}\\Del\" /reg:32 /f" };
    foreach ((string line, string status, string data, string result) in Run(listG, Array.Empty<string>(), false))
    {
        Check(status == "False", "Before Execute Missing Commands " + status + " for " + line);
    }
    foreach ((string line, string status, string data, string result) in Run(listG, Array.Empty<string>(), true))
    {
        Check(status == "True", "After Execute Missing Commands " + status + " for " + line);
    }
    using (RegistryKey lm32 = LocalMachine(RegistryView.Registry32))
    using (RegistryKey lm64 = LocalMachine(RegistryView.Registry64))
    {
        Check(lm32.OpenSubKey(LmKey + @"\Del") == null, "/reg:32 deleted the 32-bit Del key");
        Check(lm64.OpenSubKey(LmKey + @"\Del") != null, "/reg:32 kept the 64-bit Del key");
    }

    string caseLine = $"reg add \"{e}\" /v \"Case\" /d \"x\" /f";
    foreach ((string line, string status, string data, string result) in Run(new[] { caseLine }, new[] { caseLine.ToUpperInvariant() }, false))
    {
        Check(status == "Ignored", "Ignore.txt in upper case still ignores " + line + ": " + status);
    }

    string ignoreFile = Path.Combine(work, "Ignore.txt");
    Run(new[] { caseLine }, Array.Empty<string>(), false);
    File.AppendAllLines(ignoreFile, new[] { "typed by hand" });
    viewModels.GetMethod("AddToIgnoreList")!.Invoke(null, new[] { Rows() });
    string[] afterAdd = File.ReadAllLines(ignoreFile);
    Check(afterAdd.Contains("typed by hand") && afterAdd.Contains(caseLine), "Add to Ignore List kept the line typed into Ignore.txt by hand");
    File.AppendAllLines(ignoreFile, new[] { "typed by hand again" });
    viewModels.GetMethod("RemoveFromIgnoreList")!.Invoke(null, new[] { Rows() });
    string[] afterRemove = File.ReadAllLines(ignoreFile);
    Check(afterRemove.Contains("typed by hand") && afterRemove.Contains("typed by hand again") && !afterRemove.Contains(caseLine), "Remove from Ignore List kept the lines typed into Ignore.txt by hand");
    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(TestKey + @"\B\command")!)
    {
        using RegistryKey regExeKey = Registry.CurrentUser.OpenSubKey(TestKey + @"\A\command")!;
        Check((string?)key.GetValue("") == (string?)regExeKey.GetValue(""), "Execute Missing Commands wrote the same as reg.exe: " + regExeKey.GetValue(""));
    }

    string h = @"HKCU\Software\RegistryKeyCheckTest\H";
    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(TestKey + @"\H"))
    {
        key.SetValue("V", "1");
        key.CreateSubKey("Sub").Dispose();
    }
    string[] listH = { $"reg delete \"{h}\\Sub\" /f", $"reg delete \"{h}\\Sub\" /f", $"reg delete \"{h}\" /v \"V\" /f", $"reg delete \"{h}\" /v \"V\" /f" };
    foreach ((string line, string status, string data, string result) in Run(listH, Array.Empty<string>(), true))
    {
        Check(status == "True" && result == "successful", "Execute Missing Commands says " + status + " and " + result + " for " + line);
    }
    Check(Detail(listH[0]).StartsWith("List syntax warning: line 2 also changes this key"), "Compliance Details says " + Detail(listH[0]) + " for " + listH[0]);
    Check(Detail(listH[2]).StartsWith("List syntax warning: line 4 also changes this value"), "Compliance Details says " + Detail(listH[2]) + " for " + listH[2]);

    string m = @"HKCU\Software\RegistryKeyCheckTest\M";
    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(TestKey + @"\M\Keep"))
    {
        key.SetValue("Bar", "1");
    }
    string[] listM =
    {
        @"reg delete HKCU\Software\RegistryKeyCheckTest\M\Keep Me /f",
        $"reg delete \"{m}\\Keep\" /vv \"Bar\" /f",
        $"reg delete \"{m}\\Keep\" /f /v",
        $"reg add \"{m}\\Keep\" /v \"Bar\" /d \"2\" /f >nul 2>&1",
        $"reg add \"{m}\\Keep\" /v \"Glued\" /d \"3\" /f>nul",
        $"reg delete \"{m}\\Keep\" /va /v \"Bar\" /f",
        $"reg add \"{m}\\Keep\\\" /v \"Junk\" /d \"1\" /f"
    };
    List<(string Line, string Status, string Data, string Result)> rowsM = Run(listM, Array.Empty<string>(), true);
    Check(rowsM.Count == 2 && rowsM.All(r => (r.Line == listM[3] || r.Line == listM[4]) && r.Status == "True" && r.Result == "successful"), "Only the two well formed lines of M were read as reg lines and run: " + string.Join(" | ", rowsM.Select(r => r.Line + " " + r.Status + " " + r.Result)));
    Check(NonRegCount() == 5, "Five lines of M went to Non RegistryKey Commands: " + NonRegCount());
    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(TestKey + @"\M")!)
    {
        using RegistryKey? keep = key.OpenSubKey("Keep");
        Check(keep != null && (string?)keep.GetValue("Bar") == "2" && (string?)keep.GetValue("Glued") == "3", "M\\Keep kept Bar and got Bar = 2 and Glued = 3");
        Check(key.GetSubKeyNames().SequenceEqual(new[] { "Keep" }), "No other key was made under M: " + string.Join(", ", key.GetSubKeyNames()));
    }

    string w = @"HKCU\Software\RegistryKeyCheckTest\W";
    string[] listW =
    {
        $"reg add \"{w}\" /v \"A\" /d \"1\" /f",
        $"reg add \"{w}\" /v \"a\" /d \"2\" /f",
        $"reg add \"{w}\" /v \"B\" /d \"1\" /f",
        $"reg delete \"{w}\\Two\" /f",
        $"reg add \"{w}\\Two\\Sub\" /v \"C\" /d \"1\" /f",
        $"reg delete \"{w}\\Three\" /va /f",
        $"reg add \"{w}\\Three\" /v \"D\" /d \"1\" /f",
        $"reg add \"{w}\\Three\\Child\" /v \"E\" /d \"1\" /f",
        "reg add \"HKEY_CURRENT_USER\\Software\\RegistryKeyCheckTest\\W\\Four\" /v \"F\" /d \"1\" /f",
        "reg add \"HKCU\\Software\\RegistryKeyCheckTest\\W\\Four\" /v \"F\" /d \"2\" /f"
    };
    string[] warningsW =
    {
        "List syntax warning: line 2 also changes this value",
        "List syntax warning: line 1 also changes this value",
        "",
        "List syntax warning: line 5 also changes this key",
        "List syntax warning: line 4 also changes this key",
        "List syntax warning: line 7 also changes this key",
        "List syntax warning: line 6 also changes this key",
        "",
        "List syntax warning: line 10 also changes this value",
        "List syntax warning: line 9 also changes this value"
    };
    Run(listW, Array.Empty<string>(), false);
    for (int i = 0; i < listW.Length; i++)
    {
        string detail = Detail(listW[i]);
        Check(warningsW[i].Length > 0 ? detail.StartsWith(warningsW[i]) : !detail.Contains("List syntax warning"), "Compliance Details says " + detail + " for " + listW[i]);
    }
    Run(listW, new[] { listW[1] }, false);
    Check(!Detail(listW[0]).Contains("List syntax warning"), "Ignoring line 2 clears the warning of line 1: " + Detail(listW[0]));
    Check(Detail(listW[1]) == "On the Ignore List", "Compliance Details says " + Detail(listW[1]) + " for an ignored line");

    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    Encoding ansi = Encoding.GetEncoding(0);
    string n = @"HKCU\Software\RegistryKeyCheckTest\N";
    Directory.SetCurrentDirectory(work);
    File.WriteAllLines(Path.Combine(work, "List.txt"), new[] { $"reg add \"{n}\" /v \"M\u00fczik\" /d \"\u00dc\" /f" }, ansi);
    File.WriteAllLines(Path.Combine(work, "Ignore.txt"), new[] { "M\u00fczik typed by hand" }, ansi);
    viewModels.GetMethod("ReadListFile")!.Invoke(null, null);
    viewModels.GetMethod("QueryFromRegEdit")!.Invoke(null, null);
    ((Task)viewModels.GetMethod("ExecuteMissingCommands")!.Invoke(null, null)!).GetAwaiter().GetResult();
    using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(TestKey + @"\N"))
    {
        Check((string?)key?.GetValue("M\u00fczik") == "\u00dc", "List.txt saved in code page " + ansi.CodePage + " wrote the value with its letters intact");
    }
    viewModels.GetMethod("AddToIgnoreList")!.Invoke(null, new object[] { Array.CreateInstance(model, 0) });
    string ignoreBack = File.ReadAllText(Path.Combine(work, "Ignore.txt"), new UTF8Encoding(false, true));
    Check(ignoreBack.Contains("M\u00fczik typed by hand"), "Ignore.txt saved in code page " + ansi.CodePage + " kept its letters after Add to Ignore List");
}
finally
{
    CleanUp();
}
string log = Path.Combine(work, "Log.txt");
if (failures > 0 && File.Exists(log))
{
    Console.WriteLine(File.ReadAllText(log));
}
Console.WriteLine(failures == 0 ? "All registry tests passed" : failures + " registry tests failed");
return failures == 0 ? 0 : 1;
