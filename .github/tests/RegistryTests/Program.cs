using System.Collections;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Win32;

const string TestKey = @"Software\RegistryKeyCheckTest";
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

int RunCmd(string line)
{
    ProcessStartInfo info = new ProcessStartInfo("cmd.exe") { Arguments = "/c " + line, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
    using Process process = Process.Start(info)!;
    process.StandardOutput.ReadToEnd();
    process.StandardError.ReadToEnd();
    process.WaitForExit();
    return process.ExitCode;
}

List<(string Line, string Status, string Data)> Run(string[] list, string[] ignore, bool execute, Func<string, bool>? only = null)
{
    Directory.CreateDirectory(work);
    File.WriteAllLines(Path.Combine(work, "List.txt"), list);
    File.WriteAllLines(Path.Combine(work, "Ignore.txt"), ignore);
    Directory.SetCurrentDirectory(work);
    viewModels.GetMethod("ReadListFile")!.Invoke(null, null);
    viewModels.GetMethod("QueryFromRegEdit")!.Invoke(null, new object?[] { null });
    if (execute)
    {
        IEnumerable all = (IEnumerable)viewModels.GetField("ObsColl_RegCommands")!.GetValue(null)!;
        Type rowType = all.Cast<object>().First().GetType();
        object? include = null;
        if (only != null)
        {
            foreach (object row in all)
            {
                rowType.GetProperty("IsChecked")!.SetValue(row, only((string)rowType.GetProperty("ListLineString")!.GetValue(row)!));
            }
            ParameterExpression parameter = Expression.Parameter(rowType, "row");
            include = Expression.Lambda(typeof(Func<,>).MakeGenericType(rowType, typeof(bool)), Expression.Property(parameter, "IsChecked"), parameter).Compile();
        }
        ((Task)viewModels.GetMethod("ExecuteMissingCommands")!.Invoke(null, new object?[] { include, null })!).GetAwaiter().GetResult();
        viewModels.GetMethod("QueryFromRegEdit")!.Invoke(null, new object?[] { null });
    }
    List<(string, string, string)> rows = new List<(string, string, string)>();
    foreach (object row in (IEnumerable)viewModels.GetField("ObsColl_RegCommands")!.GetValue(null)!)
    {
        Type type = row.GetType();
        rows.Add(((string)type.GetProperty("ListLineString")!.GetValue(row)!, (string)type.GetProperty("ListMatchWithRegEdit")!.GetValue(row)!, (string)type.GetProperty("RegEditValueData")!.GetValue(row)!));
    }
    return rows;
}

Registry.CurrentUser.DeleteSubKeyTree(TestKey, false);
try
{
    string a = @"HKCU\Software\RegistryKeyCheckTest\A";
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
        $"reg add \"{a}\" /v \"Exp\" /t REG_EXPAND_SZ /d \"%SystemRoot%\\system32\" /f"
    };
    foreach (string line in regExe)
    {
        Check(RunCmd(line) == 0, "reg.exe ran " + line);
    }
    string[] listA = regExe.Concat(new[]
    {
        "reg add \"HKEY_CURRENT_USER\\software\\registrykeychecktest\\a\" /v \"KUCUKbuyuK\" /t REG_SZ /d \"TEST\" /f",
        "reg add \"hkey_current_user\\Software\\RegistryKeyCheckTest\\A\" /v \"small\" /t reg_dword /d 10 /f",
        $"reg delete \"{a}\" /v \"NotThere\" /f",
        "reg delete \"HKCU\\Software\\RegistryKeyCheckTest\\Nope\" /f"
    }).ToArray();
    foreach ((string line, string status, string data) in Run(listA, Array.Empty<string>(), false))
    {
        Check(status == "True", "Query RegEdit says " + status + " for " + line + " [" + data + "]");
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
        $"reg delete \"{b}\" /v \"DELVAL\" /f",
        $"reg delete \"{b}\\Sub\" /f",
        ignored
    };
    foreach ((string line, string status, string data) in Run(listB, new[] { ignored }, false))
    {
        Check(status == (line == ignored ? "Ignored" : "False"), "Before Execute Missing Commands " + status + " for " + line);
    }
    foreach ((string line, string status, string data) in Run(listB, new[] { ignored }, true))
    {
        Check(status == (line == ignored ? "Ignored" : "True"), "After Execute Missing Commands " + status + " for " + line + " [" + data + "]");
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
    }
    string c = @"HKCU\Software\RegistryKeyCheckTest\C";
    string[] listC =
    {
        $"reg add \"{c}\" /v \"Picked\" /d \"yes\" /f",
        $"reg add \"{c}\" /v \"NotPicked\" /d \"yes\" /f"
    };
    List<(string Line, string Status, string Data)> selectedRun = Run(listC, Array.Empty<string>(), true, line => line.Contains("\"Picked\""));
    Check(selectedRun[0].Status == "True", "Execute Selected changed the selected row: " + selectedRun[0].Status);
    Check(selectedRun[1].Status == "False", "Execute Selected left the unselected row alone: " + selectedRun[1].Status);
    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(TestKey + @"\B\command")!)
    {
        using RegistryKey regExeKey = Registry.CurrentUser.OpenSubKey(TestKey + @"\A\command")!;
        Check((string?)key.GetValue("") == (string?)regExeKey.GetValue(""), "Execute Missing Commands wrote the same as reg.exe: " + regExeKey.GetValue(""));
    }
}
finally
{
    Registry.CurrentUser.DeleteSubKeyTree(TestKey, false);
}
string log = Path.Combine(work, "log.txt");
if (failures > 0 && File.Exists(log))
{
    Console.WriteLine(File.ReadAllText(log));
}
Console.WriteLine(failures == 0 ? "All registry tests passed" : failures + " registry tests failed");
return failures == 0 ? 0 : 1;
