using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;

namespace RegistryKey_Check
{
    public class ColumnSetting
    {
        public string Header { get; set; } = string.Empty;

        public double Width { get; set; }

        public string WidthType { get; set; } = string.Empty;

        public int DisplayIndex { get; set; }

        public bool Visible { get; set; } = true;
    }

    public class AppSettings
    {
        private const string FilePath = "Settings.json";

        public double? Left { get; set; }

        public double? Top { get; set; }

        public double? Width { get; set; }

        public double? Height { get; set; }

        public bool Maximized { get; set; }

        public List<ColumnSetting> RegColumns { get; set; } = new List<ColumnSetting>();

        public List<ColumnSetting> NonRegColumns { get; set; } = new List<ColumnSetting>();

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? NonCompliantFilter { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? CompliantFilter { get; set; }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                ViewModels.WriteLog("Settings.json could not be read, so the default layout is used. LoadSettings", ex);
            }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                ViewModels.WriteLog("SaveSettings", ex);
            }
        }

        public void ApplyWindow(Window window)
        {
            if (Left.HasValue && Top.HasValue && Width > 0 && Height > 0)
            {
                Rect saved = new Rect(Left.Value, Top.Value, Width.Value, Height.Value);
                Rect screen = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
                if (screen.IntersectsWith(saved))
                {
                    window.WindowStartupLocation = WindowStartupLocation.Manual;
                    window.Left = saved.Left;
                    window.Top = saved.Top;
                    window.Width = saved.Width;
                    window.Height = saved.Height;
                }
            }
            if (Maximized)
            {
                window.WindowState = WindowState.Maximized;
            }
        }

        public void ReadWindow(Window window)
        {
            Rect bounds = window.WindowState == WindowState.Normal ? new Rect(window.Left, window.Top, window.ActualWidth, window.ActualHeight) : window.RestoreBounds;
            if (!bounds.IsEmpty)
            {
                Left = bounds.Left;
                Top = bounds.Top;
                Width = bounds.Width;
                Height = bounds.Height;
            }
            Maximized = window.WindowState == WindowState.Maximized;
        }

        public static void ApplyColumns(DataGrid grid, List<ColumnSetting> saved)
        {
            foreach (ColumnSetting setting in saved.OrderBy(s => s.DisplayIndex))
            {
                DataGridColumn? column = grid.Columns.FirstOrDefault(c => HeaderOf(c) == setting.Header);
                if (column == null)
                {
                    ViewModels.WriteNote("ApplyColumns", "Settings.json has a layout for the column \"" + setting.Header + "\", which RegistryKey Check no longer has, so that layout was not used and the other columns kept their saved or default layout.");
                    continue;
                }
                if (setting.Width > 0 && setting.WidthType == nameof(DataGridLengthUnitType.Pixel))
                {
                    column.Width = new DataGridLength(setting.Width);
                }
                else if (setting.Width > 0 && setting.WidthType == nameof(DataGridLengthUnitType.Star))
                {
                    column.Width = new DataGridLength(setting.Width, DataGridLengthUnitType.Star);
                }
                else if (setting.WidthType == nameof(DataGridLengthUnitType.Auto))
                {
                    column.Width = DataGridLength.Auto;
                }
                else if (setting.WidthType == nameof(DataGridLengthUnitType.SizeToCells))
                {
                    column.Width = DataGridLength.SizeToCells;
                }
                else if (setting.WidthType == nameof(DataGridLengthUnitType.SizeToHeader))
                {
                    column.Width = DataGridLength.SizeToHeader;
                }
                column.Visibility = setting.Visible ? Visibility.Visible : Visibility.Collapsed;
                if (setting.DisplayIndex >= 0 && setting.DisplayIndex < grid.Columns.Count)
                {
                    column.DisplayIndex = setting.DisplayIndex;
                }
            }
        }

        public static List<ColumnSetting> ReadColumns(DataGrid grid)
        {
            return grid.Columns.Select(c => new ColumnSetting
            {
                Header = HeaderOf(c),
                Width = c.Width.Value,
                WidthType = c.Width.UnitType.ToString(),
                DisplayIndex = c.DisplayIndex,
                Visible = c.Visibility == Visibility.Visible
            }).ToList();
        }

        public static string HeaderOf(DataGridColumn column)
        {
            return column.Header?.ToString()?.Trim() ?? string.Empty;
        }
    }
}
