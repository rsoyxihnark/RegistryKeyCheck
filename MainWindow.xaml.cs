using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace RegistryKey_Check
{
    public partial class MainWindow : Window
    {
        private enum StatusKind
        {
            Ready,
            Working,
            Success,
            Error
        }

        private static readonly string[] SearchFields =
        {
            "RegCommandId", "ListId", "ListCommand", "ListBaseKey", "ListSubKey", "AvailableSubKey", "ListValueName", "AvailableValueName",
            "ListValueType", "RegEditValueType", "ListValueData", "RegEditValueData", "ExecuteResult", "ListMatchWithRegEdit", "ListLineString"
        };

        private readonly AppSettings settings = AppSettings.Load();

        private readonly ContextMenu columnsMenu = new ContextMenu();

        private readonly DispatcherTimer filterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };

        private readonly DispatcherTimer progressResetTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };

        private readonly Stopwatch stopwatch = new Stopwatch();

        private ListCollectionView? regView;

        private string searchText = string.Empty;

        private List<(string Field, string Text)> columnFilters = new List<(string, string)>();

        private bool showTrue = true;

        private bool showFalse = true;

        private bool showUnknown = true;

        private bool showIgnored = true;

        public MainWindow()
        {
            InitializeComponent();
            WindowTheme.UseDarkTitleBar(this);
            foreach (DataGridColumn column in DataGridRegCommands.Columns)
            {
                MenuItem item = new MenuItem { Header = AppSettings.HeaderOf(column), IsCheckable = true, StaysOpenOnClick = true, Tag = column };
                item.Click += ColumnsMenuItem_Click;
                columnsMenu.Items.Add(item);
            }
            columnsMenu.Opened += ColumnsMenu_Opened;
            Style headerStyle = new Style(typeof(DataGridColumnHeader), (Style)FindResource("FilterHeader"));
            headerStyle.Setters.Add(new Setter(ContextMenuProperty, columnsMenu));
            DataGridRegCommands.ColumnHeaderStyle = headerStyle;
            Style selectHeaderStyle = new Style(typeof(DataGridColumnHeader), (Style)FindResource("SelectHeader"));
            selectHeaderStyle.Setters.Add(new Setter(ContextMenuProperty, columnsMenu));
            dgColumnSelect.HeaderStyle = selectHeaderStyle;
            filterTimer.Tick += (sender, e) =>
            {
                filterTimer.Stop();
                ApplyFilters();
            };
            progressResetTimer.Tick += (sender, e) =>
            {
                progressResetTimer.Stop();
                AnimateProgress(0);
            };
            ColumnFilter.Changed += (sender, e) => RestartFilterTimer();
            settings.ApplyWindow(this);
            SetStatus(StatusKind.Ready, "Ready", "Read List.txt to begin");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string filePathList = "List.txt";
            string filePathLog = "Log.txt";
            if (!File.Exists(filePathList))
            {
                using (StreamWriter swList = new StreamWriter(filePathList, append: true))
                {
                }
            }
            if (!File.Exists(filePathLog))
            {
                using (StreamWriter swLog = new StreamWriter(filePathLog, append: true))
                {
                }
            }
            try
            {
                AppSettings.ApplyColumns(DataGridRegCommands, settings.RegColumns);
                AppSettings.ApplyColumns(DataGridNonRegCommands, settings.NonRegColumns);
            }
            catch (Exception ex)
            {
                ViewModels.WriteLog("Saved column layout could not be applied, so the default layout is used. ApplyColumns", ex);
            }
            UpdateCounts();
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            settings.ReadWindow(this);
            settings.RegColumns = AppSettings.ReadColumns(DataGridRegCommands);
            settings.NonRegColumns = AppSettings.ReadColumns(DataGridNonRegCommands);
            settings.Save();
        }

        private IEnumerable<ButtonBase> ActionButtons()
        {
            return new ButtonBase[]
            {
                ButtonReadListFile, ButtonOpenListFile, ButtonQueryFromRegEdit, ButtonExecuteSelected, ButtonExecuteMissingCommands,
                ButtonColumns, ButtonOpenIgnoreFile, ButtonOpenLogFile, ButtonHelp, ButtonClearFilters
            };
        }

        private void ButtonsEnable()
        {
            foreach (ButtonBase button in ActionButtons())
            {
                button.IsEnabled = true;
            }
        }

        private void ButtonsDisable()
        {
            foreach (ButtonBase button in ActionButtons())
            {
                button.IsEnabled = false;
            }
        }

        private void SetStatus(StatusKind kind, string text, string detail)
        {
            Color color = kind switch
            {
                StatusKind.Working => Color.FromRgb(0xFA, 0xCC, 0x15),
                StatusKind.Success => Color.FromRgb(0x34, 0xD3, 0x99),
                StatusKind.Error => Color.FromRgb(0xF8, 0x71, 0x71),
                _ => Color.FromRgb(0x38, 0xBD, 0xF8)
            };
            StatusDot.Fill = new SolidColorBrush(color);
            StatusText.Text = text;
            StatusText.ToolTip = text;
            StatusDetail.Text = detail;
            if (StatusDot.Effect is DropShadowEffect glow)
            {
                glow.Color = color;
                if (kind == StatusKind.Working)
                {
                    DoubleAnimation pulse = new DoubleAnimation(4, 22, TimeSpan.FromMilliseconds(650))
                    {
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever,
                        EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                    };
                    glow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, pulse);
                }
                else
                {
                    glow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, null);
                    glow.BlurRadius = kind == StatusKind.Ready ? 0 : 10;
                }
            }
        }

        private void BeginWork(string text)
        {
            progressResetTimer.Stop();
            stopwatch.Restart();
            AnimateProgress(0);
            SetStatus(StatusKind.Working, text, "Working...");
        }

        private void ReportProgress(string text, int done, int total)
        {
            if (total > 0)
            {
                AnimateProgress(100.0 * done / total);
            }
            StatusDetail.Text = done.ToString("N0", CultureInfo.CurrentCulture) + " of " + total.ToString("N0", CultureInfo.CurrentCulture) + " rows";
            StatusText.Text = text;
        }

        private void EndWork(StatusKind kind, string text, string detail)
        {
            stopwatch.Stop();
            AnimateProgress(100);
            progressResetTimer.Start();
            SetStatus(kind, text, detail + "  ·  " + stopwatch.Elapsed.TotalSeconds.ToString("0.0", CultureInfo.CurrentCulture) + " s");
        }

        private void AnimateProgress(double value)
        {
            Progress.BeginAnimation(RangeBase.ValueProperty, new DoubleAnimation(value, TimeSpan.FromMilliseconds(250)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut } });
        }

        private void ButtonReadListFile_Click(object sender, RoutedEventArgs e)
        {
            ButtonsDisable();
            BeginWork("Reading List.txt");
            try
            {
                DataGridRegCommands.ItemsSource = null;
                DataGridNonRegCommands.ItemsSource = null;
                ViewModels.ReadListFile();
                ResetFilters(false);
                regView = new ListCollectionView(ViewModels.ObsColl_RegCommands) { Filter = RowFilter };
                DataGridRegCommands.ItemsSource = regView;
                DataGridNonRegCommands.ItemsSource = ViewModels.ObsColl_Non_RegCommands;
                UpdateCounts();
                EndWork(StatusKind.Success, "Read " + ViewModels.ObsColl_RegCommands.Count.ToString("N0", CultureInfo.CurrentCulture) + " reg commands", ViewModels.ObsColl_Non_RegCommands.Count.ToString("N0", CultureInfo.CurrentCulture) + " other lines");
            }
            catch (Exception ex)
            {
                ViewModels.WriteLog("ReadListFile", ex);
                EndWork(StatusKind.Error, "List.txt could not be read", "See Log.txt");
            }
            ButtonsEnable();
        }

        private void ButtonOpenListFile_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists("List.txt"))
            {
                Process.Start("Notepad.exe", "List.txt");
            }
        }

        private void ButtonOpenLogFile_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists("Log.txt"))
            {
                Process.Start("Notepad.exe", "Log.txt");
            }
        }

        private void ButtonOpenIgnoreFile_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists("Ignore.txt"))
            {
                using (StreamWriter swIgnore = new StreamWriter("Ignore.txt", append: true))
                {
                }
            }
            Process.Start("Notepad.exe", "Ignore.txt");
        }

        private async Task QueryAsync(string text)
        {
            int total = ViewModels.ObsColl_RegCommands.Count;
            Progress<int> progress = new Progress<int>(done => ReportProgress(text, done, total));
            await Task.Run(() => ViewModels.QueryFromRegEdit(progress));
        }

        private async void ButtonQueryFromRegEdit_Click(object sender, RoutedEventArgs e)
        {
            ButtonsDisable();
            BeginWork("Querying RegEdit");
            await QueryAsync("Querying RegEdit");
            RefreshRows();
            EndWork(StatusKind.Success, "Query finished", CountsText());
            ButtonsEnable();
        }

        private async void ButtonExecuteSelected_Click(object sender, RoutedEventArgs e)
        {
            int selected = ViewModels.ObsColl_RegCommands.Count(r => r.IsChecked);
            if (selected == 0)
            {
                SetStatus(StatusKind.Error, "No rows are selected", "Tick the Select box of the rows to execute");
                return;
            }
            await ExecuteAsync(r => r.IsChecked, selected.ToString("N0", CultureInfo.CurrentCulture) + " selected rows, only the ones that are False are changed.");
        }

        private async void ButtonExecuteMissingCommands_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteAsync(null, "Every row that is False is changed.");
        }

        private async Task ExecuteAsync(Func<ModelRegCommand, bool>? include, string scope)
        {
            ButtonsDisable();
            if (ConfirmWindow.Ask(this, scope))
            {
                BeginWork("Querying RegEdit");
                await QueryAsync("Querying RegEdit");
                await Task.Delay(100);
                int total = include == null ? ViewModels.ObsColl_RegCommands.Count : ViewModels.ObsColl_RegCommands.Count(include);
                Progress<int> progress = new Progress<int>(done => ReportProgress("Executing commands", done, total));
                await Task.Run(() => ViewModels.ExecuteMissingCommands(include, progress));
                await Task.Delay(100);
                await QueryAsync("Querying RegEdit again");
                RefreshRows();
                int errors = ViewModels.ObsColl_RegCommands.Count(r => r.ExecuteResult == "Error");
                if (errors > 0)
                {
                    EndWork(StatusKind.Error, errors.ToString("N0", CultureInfo.CurrentCulture) + " commands failed", "See Log.txt");
                }
                else
                {
                    EndWork(StatusKind.Success, "Commands executed", CountsText());
                }
            }
            ButtonsEnable();
        }

        private void ButtonHelp_Click(object sender, RoutedEventArgs e)
        {
            HelpWindow helpWindow = new HelpWindow();
            helpWindow.ShowInTaskbar = false;
            helpWindow.Owner = this;
            helpWindow.ShowDialog();
        }

        private void ButtonColumns_Click(object sender, RoutedEventArgs e)
        {
            columnsMenu.PlacementTarget = ButtonColumns;
            columnsMenu.Placement = PlacementMode.Bottom;
            columnsMenu.IsOpen = true;
        }

        private void ColumnsMenu_Opened(object sender, RoutedEventArgs e)
        {
            foreach (MenuItem item in columnsMenu.Items.OfType<MenuItem>())
            {
                item.IsChecked = ((DataGridColumn)item.Tag).Visibility == Visibility.Visible;
            }
        }

        private void ColumnsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            ((DataGridColumn)item.Tag).Visibility = item.IsChecked ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TextBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            RestartFilterTimer();
        }

        private void Chip_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ButtonClearFilters_Click(object sender, RoutedEventArgs e)
        {
            ResetFilters(true);
        }

        private void ResetFilters(bool apply)
        {
            TextBoxSearch.Text = string.Empty;
            foreach (DataGridColumn column in DataGridRegCommands.Columns)
            {
                ColumnFilter.SetText(column, string.Empty);
            }
            ChipTrue.IsChecked = true;
            ChipFalse.IsChecked = true;
            ChipUnknown.IsChecked = true;
            ChipIgnored.IsChecked = true;
            filterTimer.Stop();
            searchText = string.Empty;
            columnFilters = new List<(string, string)>();
            showTrue = true;
            showFalse = true;
            showUnknown = true;
            showIgnored = true;
            if (apply)
            {
                RefreshRows();
            }
        }

        private void RestartFilterTimer()
        {
            filterTimer.Stop();
            filterTimer.Start();
        }

        private void ApplyFilters()
        {
            searchText = TextBoxSearch.Text.Trim();
            columnFilters = DataGridRegCommands.Columns
                .Where(c => !string.IsNullOrEmpty(c.SortMemberPath) && ColumnFilter.GetText(c).Trim().Length > 0)
                .Select(c => (c.SortMemberPath, ColumnFilter.GetText(c).Trim()))
                .ToList();
            showTrue = ChipTrue.IsChecked == true;
            showFalse = ChipFalse.IsChecked == true;
            showUnknown = ChipUnknown.IsChecked == true;
            showIgnored = ChipIgnored.IsChecked == true;
            RefreshRows();
        }

        private bool RowFilter(object item)
        {
            ModelRegCommand mo = (ModelRegCommand)item;
            bool statusShown = mo.ListMatchWithRegEdit switch
            {
                "True" => showTrue,
                "False" => showFalse,
                "Ignored" => showIgnored,
                _ => showUnknown
            };
            if (!statusShown)
            {
                return false;
            }
            if (searchText.Length > 0 && !SearchFields.Any(field => FieldText(mo, field).Contains(searchText, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
            foreach ((string field, string text) in columnFilters)
            {
                if (!FieldText(mo, field).Contains(text, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            return true;
        }

        private static string FieldText(ModelRegCommand mo, string field)
        {
            return field switch
            {
                "RegCommandId" => mo.RegCommandId.ToString(CultureInfo.InvariantCulture),
                "ListId" => mo.ListId.ToString(CultureInfo.InvariantCulture),
                "ListCommand" => mo.ListCommand,
                "ListBaseKey" => mo.ListBaseKey,
                "ListSubKey" => mo.ListSubKey,
                "AvailableSubKey" => mo.AvailableSubKey,
                "ListValueName" => mo.ListValueName,
                "AvailableValueName" => mo.AvailableValueName,
                "ListValueType" => mo.ListValueType,
                "RegEditValueType" => mo.RegEditValueType,
                "ListValueData" => mo.ListValueData,
                "RegEditValueData" => mo.RegEditValueData,
                "ExecuteResult" => mo.ExecuteResult,
                "ListMatchWithRegEdit" => mo.ListMatchWithRegEdit,
                "ListLineString" => mo.ListLineString,
                _ => string.Empty
            };
        }

        private void RefreshRows()
        {
            if (regView != null)
            {
                ScrollViewer? viewer = FindChild<ScrollViewer>(DataGridRegCommands);
                double offset = viewer?.VerticalOffset ?? 0;
                regView.Refresh();
                if (viewer != null)
                {
                    DataGridRegCommands.UpdateLayout();
                    viewer.ScrollToVerticalOffset(offset);
                }
            }
            UpdateCounts();
        }

        private void UpdateCounts()
        {
            int total = ViewModels.ObsColl_RegCommands.Count;
            int shown = regView?.Count ?? 0;
            int trues = CountOf("True");
            int falses = CountOf("False");
            int ignored = CountOf("Ignored");
            ChipTrue.Content = "True (" + Number(trues) + ")";
            ChipFalse.Content = "False (" + Number(falses) + ")";
            ChipIgnored.Content = "Ignored (" + Number(ignored) + ")";
            ChipUnknown.Content = "Unknown (" + Number(total - trues - falses - ignored) + ")";
            TextShown.Text = "Shown " + Number(shown) + " of " + Number(total);
            TextTotal.Text = "Total " + Number(total);
        }

        private static string Number(int value)
        {
            return value.ToString("N0", CultureInfo.CurrentCulture);
        }

        private static int CountOf(string status)
        {
            return ViewModels.ObsColl_RegCommands.Count(r => r.ListMatchWithRegEdit == status);
        }

        private static string CountsText()
        {
            return Number(CountOf("True")) + " True, " + Number(CountOf("False")) + " False";
        }

        private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T found)
                {
                    return found;
                }
                T? nested = FindChild<T>(child);
                if (nested != null)
                {
                    return nested;
                }
            }
            return null;
        }

        private static T? FindAncestor<T>(DependencyObject element) where T : DependencyObject
        {
            DependencyObject? current = element;
            while (current != null)
            {
                if (current is T found)
                {
                    return found;
                }
                current = current is Visual || current is Visual3D ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current);
            }
            return null;
        }

        private void TextBoxGoTo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                GoToLine(TextBoxGoTo.Text);
                e.Handled = true;
            }
        }

        private void GoToLine(string text)
        {
            if (!int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out int line))
            {
                SetStatus(StatusKind.Error, "Type a line number", "Go to line takes a line number of List.txt");
                return;
            }
            ModelRegCommand? target = ViewModels.ObsColl_RegCommands.FirstOrDefault(r => r.ListId == line);
            if (target != null)
            {
                if (regView != null && !regView.Contains(target))
                {
                    ResetFilters(true);
                }
                Tabs.SelectedIndex = 0;
                DataGridRegCommands.SelectedItem = target;
                DataGridRegCommands.ScrollIntoView(target);
                DataGridRegCommands.Focus();
                SetStatus(StatusKind.Ready, "Line " + Number(line), "Row " + Number(target.RegCommandId) + " in RegistryKey Commands");
                return;
            }
            ModelNonRegCommand? other = ViewModels.ObsColl_Non_RegCommands.FirstOrDefault(r => r.ListId == line);
            if (other != null)
            {
                Tabs.SelectedIndex = 1;
                DataGridNonRegCommands.SelectedItem = other;
                DataGridNonRegCommands.ScrollIntoView(other);
                SetStatus(StatusKind.Ready, "Line " + Number(line), "Row " + Number(other.Non_RegCommandId) + " in Non RegistryKey Commands");
                return;
            }
            SetStatus(StatusKind.Error, "Line " + Number(line) + " was not found", "Read List.txt first, or check the line number");
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            bool control = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            if (e.Key == Key.F5 && ButtonQueryFromRegEdit.IsEnabled)
            {
                ButtonQueryFromRegEdit.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                e.Handled = true;
            }
            else if (e.Key == Key.F1 && ButtonHelp.IsEnabled)
            {
                ButtonHelp_Click(this, e);
                e.Handled = true;
            }
            else if (control && e.Key == Key.F)
            {
                TextBoxSearch.Focus();
                TextBoxSearch.SelectAll();
                e.Handled = true;
            }
            else if (control && e.Key == Key.G)
            {
                TextBoxGoTo.Focus();
                TextBoxGoTo.SelectAll();
                e.Handled = true;
            }
            else if (control && e.Key == Key.O && ButtonReadListFile.IsEnabled)
            {
                ButtonReadListFile.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && TextBoxSearch.IsKeyboardFocused && TextBoxSearch.Text.Length > 0)
            {
                TextBoxSearch.Text = string.Empty;
                e.Handled = true;
            }
            else if (DataGridRegCommands.IsKeyboardFocusWithin && e.OriginalSource is not TextBoxBase)
            {
                if (e.Key == Key.Enter && DataGridRegCommands.SelectedItem is ModelRegCommand mo)
                {
                    ShowDetails(mo);
                    e.Handled = true;
                }
                else if (e.Key == Key.Space && SelectedRows().Count > 0)
                {
                    List<ModelRegCommand> rows = SelectedRows();
                    bool check = rows.Any(r => !r.IsChecked);
                    foreach (ModelRegCommand row in rows)
                    {
                        row.IsChecked = check;
                    }
                    e.Handled = true;
                }
            }
        }

        private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject? element = e.OriginalSource as DependencyObject;
            while (element != null)
            {
                if (element is DataGridRow || element is DataGridColumnHeader || element is ScrollBar || element is ButtonBase || element is Thumb || element is TextBoxBase || element is MenuItem)
                {
                    return;
                }
                element = element is Visual || element is Visual3D ? VisualTreeHelper.GetParent(element) : LogicalTreeHelper.GetParent(element);
            }
            DataGridRegCommands.UnselectAll();
            DataGridNonRegCommands.UnselectAll();
        }

        private List<ModelRegCommand> SelectedRows()
        {
            return DataGridRegCommands.SelectedItems.OfType<ModelRegCommand>().ToList();
        }

        private void DataGridRegCommands_Row_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = (DataGridRow)sender;
            if (!row.IsSelected)
            {
                DataGridRegCommands.SelectedItems.Clear();
                row.IsSelected = true;
            }
        }

        private void DataGridRegCommands_Row_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject source && FindAncestor<CheckBox>(source) != null)
            {
                return;
            }
            if (((DataGridRow)sender).Item is ModelRegCommand mo)
            {
                ShowDetails(mo);
            }
        }

        private void ShowDetails(ModelRegCommand mo)
        {
            ViewModels.DisplayingIDs = mo.RegCommandId - 1;
            DetailWindow detailWindow = new DetailWindow();
            detailWindow.ShowInTaskbar = false;
            detailWindow.Owner = this;
            detailWindow.ShowDialog();
        }

        private void MenuShowDetails_Click(object sender, RoutedEventArgs e)
        {
            ModelRegCommand? mo = SelectedRows().FirstOrDefault();
            if (mo != null)
            {
                ShowDetails(mo);
            }
        }

        private void MenuJumpToRegEdit_Click(object sender, RoutedEventArgs e)
        {
            ModelRegCommand? mo = SelectedRows().FirstOrDefault();
            if (mo != null)
            {
                ViewModels.JumpToRegEdit(mo);
            }
        }

        private static void CopyText(string text)
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                ViewModels.WriteLog("CopyText", ex);
            }
        }

        private void MenuCopyCommand_Click(object sender, RoutedEventArgs e)
        {
            CopyText(string.Join(Environment.NewLine, SelectedRows().Select(r => r.ListLineString)));
        }

        private void MenuCopyKeyPath_Click(object sender, RoutedEventArgs e)
        {
            CopyText(string.Join(Environment.NewLine, SelectedRows().Select(r => r.ListSubKey.Length > 0 ? r.ListBaseKey + "\\" + r.ListSubKey : r.ListBaseKey)));
        }

        private void MenuCopyValueName_Click(object sender, RoutedEventArgs e)
        {
            CopyText(string.Join(Environment.NewLine, SelectedRows().Select(r => r.ListValueName == "This_field_is_NULL" ? string.Empty : r.ListValueName)));
        }

        private void MenuCheck_Click(object sender, RoutedEventArgs e)
        {
            foreach (ModelRegCommand mo in SelectedRows())
            {
                mo.IsChecked = true;
            }
        }

        private void MenuUncheck_Click(object sender, RoutedEventArgs e)
        {
            foreach (ModelRegCommand mo in SelectedRows())
            {
                mo.IsChecked = false;
            }
        }

        private void MenuAddToIgnoreList_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.AddToIgnoreList(SelectedRows());
            RefreshRows();
        }

        private void MenuRemoveFromIgnoreList_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.RemoveFromIgnoreList(SelectedRows());
            RefreshRows();
        }

        private void SelectAllCheckBox_Click(object sender, RoutedEventArgs e)
        {
            bool check = ((CheckBox)sender).IsChecked == true;
            if (regView == null)
            {
                return;
            }
            foreach (ModelRegCommand mo in regView.OfType<ModelRegCommand>())
            {
                mo.IsChecked = check;
            }
        }
    }
}
