using System.Collections;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

internal static class Program
{
    private const string TestKey = @"Software\RegistryKeyCheckUi";

    private static string output = string.Empty;

    private static int failures;

    [STAThread]
    private static int Main(string[] args)
    {
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
        string dll = Path.GetFullPath(args[0]);
        output = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(output);
        string work = Path.Combine(Path.GetTempPath(), "RegistryKeyCheckUi");
        Directory.CreateDirectory(work);
        string key = @"HKEY_CURRENT_USER\Software\RegistryKeyCheckUi";
        string[] list =
        {
            ":: RegistryKey Check sample list",
            $"reg add \"{key}\\Desktop\" /v \"WaitToKillServiceTimeout\" /t REG_SZ /d \"5000\" /f",
            $"reg add \"{key}\\Desktop\" /v \"MenuShowDelay\" /t REG_SZ /d \"0\" /f",
            $"reg add \"{key}\\Explorer\\Advanced\" /v \"Start_SearchFiles\" /t REG_DWORD /d \"0\" /f",
            $"reg add \"{key}\\Explorer\\Advanced\" /v \"HideFileExt\" /t REG_DWORD /d \"0\" /f",
            $"reg add \"{key}\\ShellFolder\" /v \"Attributes\" /t REG_DWORD /d \"0xb0940064\" /f",
            $"reg add \"{key}\\Zones\\1\" /v \"{{A8A88C49-5EB2-4990-A1A2-0876022C854F}}\" /t REG_BINARY /d 1a3761592352350c7a5f20172f1e1a190e2b017313371312141a1539 /f",
            $"reg add \"{key}\\regfile\\shell\\RunAs\\command\" /ve /d \"regedit.exe \"%1\"\" /f",
            $"reg add \"{key}\\Telemetry\" /v \"AllowTelemetry\" /t REG_DWORD /d \"0\" /f",
            $"reg add \"{key}\\Edge\" /v \"DefaultSearchProviderSearchURL\" /t REG_SZ /d \"https://google.com/search?q=%s\" /f",
            $"reg delete \"{key}\\OneDrive\" /f",
            $"reg delete \"{key}\\Desktop\" /v \"OldValue\" /f",
            $"reg add \"{key}\\Ignored\" /v \"Anything\" /d \"x\" /f",
            "echo Done"
        };
        File.WriteAllLines(Path.Combine(work, "List.txt"), list);
        File.WriteAllLines(Path.Combine(work, "Ignore.txt"), new[] { list[12] });
        Registry.CurrentUser.DeleteSubKeyTree(TestKey, false);
        using (RegistryKey desktop = Registry.CurrentUser.CreateSubKey(TestKey + @"\Desktop"))
        {
            desktop.SetValue("WaitToKillServiceTimeout", "5000");
            desktop.SetValue("MenuShowDelay", "400");
            desktop.SetValue("OldValue", "1");
        }
        using (RegistryKey advanced = Registry.CurrentUser.CreateSubKey(TestKey + @"\Explorer\Advanced"))
        {
            advanced.SetValue("Start_SearchFiles", 0, RegistryValueKind.DWord);
            advanced.SetValue("HideFileExt", 1, RegistryValueKind.DWord);
        }
        using (RegistryKey shellFolder = Registry.CurrentUser.CreateSubKey(TestKey + @"\ShellFolder"))
        {
            shellFolder.SetValue("Attributes", unchecked((int)0xb0040064), RegistryValueKind.DWord);
        }
        Registry.CurrentUser.CreateSubKey(TestKey + @"\OneDrive").Dispose();
        Directory.SetCurrentDirectory(work);
        try
        {
            Assembly app = Assembly.LoadFrom(dll);
            Type appType = app.GetType("RegistryKey_Check.App", true)!;
            Application application = (Application)Activator.CreateInstance(appType)!;
            appType.GetMethod("InitializeComponent")!.Invoke(application, null);
            Run(app);
        }
        catch (Exception ex)
        {
            Console.WriteLine("FAIL " + ex);
            failures++;
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(TestKey, false);
        }
        Console.WriteLine(failures == 0 ? "All windows opened and were saved as screenshots" : failures + " windows failed");
        return failures == 0 ? 0 : 1;
    }

    private static void Run(Assembly app)
    {
        Window main = (Window)Activator.CreateInstance(app.GetType("RegistryKey_Check.MainWindow", true)!)!;
        main.WindowStartupLocation = WindowStartupLocation.Manual;
        main.Left = 0;
        main.Top = 0;
        main.Width = 1600;
        main.Height = 900;
        main.Show();
        Pump(800);
        Shot(main, "01-start");
        Click(main, "ButtonReadListFile");
        Pump(600);
        Shot(main, "02-read");
        Click(main, "ButtonQueryFromRegEdit");
        WaitUntil(() => ((Button)main.FindName("ButtonQueryFromRegEdit")).IsEnabled);
        Pump(900);
        Shot(main, "03-queried");
        DataGrid grid = (DataGrid)main.FindName("DataGridRegCommands");
        IList rows = (IList)app.GetType("RegistryKey_Check.ViewModels", true)!.GetField("ObsColl_RegCommands")!.GetValue(null)!;
        for (int i = 0; i < rows.Count; i += 3)
        {
            rows[i]!.GetType().GetProperty("IsChecked")!.SetValue(rows[i], true);
        }
        grid.SelectedIndex = 2;
        Pump(300);
        Shot(main, "04-selected");
        ContextMenu rowMenu = (ContextMenu)main.FindResource("RowMenu");
        rowMenu.PlacementTarget = grid;
        rowMenu.IsOpen = true;
        Pump(500);
        ShotVisual(rowMenu, "05-row-menu");
        rowMenu.IsOpen = false;
        ContextMenu columnsMenu = (ContextMenu)main.GetType().GetField("columnsMenu", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(main)!;
        Click(main, "ButtonColumns");
        Pump(500);
        ShotVisual(columnsMenu, "06-columns-menu");
        columnsMenu.IsOpen = false;
        ((TextBox)main.FindName("TextBoxSearch")).Text = "Explorer";
        Pump(700);
        Shot(main, "07-search");
        ((TextBox)main.FindName("TextBoxSearch")).Text = string.Empty;
        ((ToggleButton)main.FindName("ChipTrue")).IsChecked = false;
        ((ToggleButton)main.FindName("ChipTrue")).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        Pump(700);
        Shot(main, "08-false-only");
        Click(main, "ButtonClearFilters");
        Pump(400);
        Type columnFilter = app.GetType("RegistryKey_Check.ColumnFilter", true)!;
        DataGridColumn typeColumn = (DataGridColumn)main.FindName("dgColumnRegListValueType");
        columnFilter.GetMethod("SetText")!.Invoke(null, new object[] { typeColumn, "DWORD" });
        Pump(700);
        Shot(main, "09-column-filter");
        Click(main, "ButtonClearFilters");
        ((TabControl)main.FindName("Tabs")).SelectedIndex = 1;
        Pump(500);
        Shot(main, "10-non-reg");
        ((TabControl)main.FindName("Tabs")).SelectedIndex = 0;
        Pump(300);

        Type viewModels = app.GetType("RegistryKey_Check.ViewModels", true)!;
        viewModels.GetField("DisplayingIDs")!.SetValue(null, 4);
        Window detail = (Window)Activator.CreateInstance(app.GetType("RegistryKey_Check.DetailWindow", true)!)!;
        detail.Owner = main;
        detail.Show();
        Pump(600);
        Shot(detail, "11-detail");
        detail.Close();

        Window help = (Window)Activator.CreateInstance(app.GetType("RegistryKey_Check.HelpWindow", true)!)!;
        help.Owner = main;
        help.Show();
        Pump(600);
        Shot(help, "12-help");
        help.Close();

        Window confirm = (Window)Activator.CreateInstance(app.GetType("RegistryKey_Check.ConfirmWindow", true)!)!;
        confirm.Owner = main;
        ((TextBlock)confirm.FindName("TextScope")).Text = "4 selected rows, only the ones that are False are changed.";
        confirm.Show();
        Pump(600);
        Shot(confirm, "13-confirm");
        confirm.Close();

        main.Width = 1300;
        main.Height = 700;
        Pump(700);
        Shot(main, "14-smallest");
        main.Close();
    }

    private static void Click(Window window, string name)
    {
        ((ButtonBase)window.FindName(name)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
    }

    private static void Pump(int milliseconds)
    {
        DispatcherFrame frame = new DispatcherFrame();
        DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(milliseconds) };
        timer.Tick += (sender, e) =>
        {
            timer.Stop();
            frame.Continue = false;
        };
        timer.Start();
        Dispatcher.PushFrame(frame);
    }

    private static void WaitUntil(Func<bool> condition)
    {
        DateTime deadline = DateTime.Now.AddSeconds(60);
        while (!condition() && DateTime.Now < deadline)
        {
            Pump(100);
        }
        if (!condition())
        {
            throw new TimeoutException("The window did not finish its work in time");
        }
    }

    private static void Shot(Window window, string name)
    {
        FrameworkElement root = (FrameworkElement)VisualTreeHelper.GetChild(window, 0);
        Save(root, window.Background, name);
    }

    private static void ShotVisual(FrameworkElement element, string name)
    {
        Save(element, new SolidColorBrush(Color.FromRgb(0x0B, 0x12, 0x20)), name);
    }

    private static void Save(FrameworkElement element, Brush background, string name)
    {
        int width = Math.Max(1, (int)Math.Ceiling(element.ActualWidth));
        int height = Math.Max(1, (int)Math.Ceiling(element.ActualHeight));
        RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        DrawingVisual backdrop = new DrawingVisual();
        using (DrawingContext context = backdrop.RenderOpen())
        {
            context.DrawRectangle(background, null, new Rect(0, 0, width, height));
        }
        bitmap.Render(backdrop);
        bitmap.Render(element);
        PngBitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream stream = File.Create(Path.Combine(output, name + ".png"));
        encoder.Save(stream);
        Console.WriteLine("Saved " + name + ".png " + width + "x" + height);
    }
}
