using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace RegistryKey_Check
{
    public partial class MainWindow : Window
    {
        private readonly AppSettings settings = AppSettings.Load();

        private readonly Dictionary<DataGridColumn, Func<ModelRegCommand, string>> columnTexts;

        private readonly ContextMenu columnsMenu = new ContextMenu();

        private bool filtersResetting;

        private bool filtersRestored;

        public MainWindow()
        {
            App.UseDarkTitleBar(this);
            InitializeComponent();
            Title = "RegistryKey Check " + Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            DataGridRegCommands.ItemsSource = ViewModels.ObsColl_RegCommands;
            DataGridNonRegCommands.ItemsSource = ViewModels.ObsColl_Non_RegCommands;
            DataGridRegCommands.Items.SortDescriptions.Add(new SortDescription(nameof(ModelRegCommand.ListFullPath), ListSortDirection.Ascending));
            DataGridRegCommands.Items.SortDescriptions.Add(new SortDescription(nameof(ModelRegCommand.ListId), ListSortDirection.Ascending));
            dgColumnRegFullPath.SortDirection = ListSortDirection.Ascending;
            dgColumnRegUID.SortDirection = null;
            columnTexts = new Dictionary<DataGridColumn, Func<ModelRegCommand, string>>
            {
                [dgColumnRegUID] = s => s.ListId.ToString(),
                [dgColumnRegListCommand] = s => s.ListCommand,
                [dgColumnRegFullPath] = s => s.ListFullPath,
                [dgColumnRegAvailableSubKey] = s => s.AvailableSubKey,
                [dgColumnRegListValueName] = s => s.ListValueNameShown,
                [dgColumnRegAvailableValueName] = s => ViewModels.Shown(s.AvailableValueName),
                [dgColumnRegListValueType] = s => ViewModels.Shown(s.ListValueType),
                [dgColumnRegRegEditValueType] = s => ViewModels.Shown(s.RegEditValueType),
                [dgColumnRegListValueData] = s => ViewModels.Shown(s.ListValueData),
                [dgColumnRegRegEditValueData] = s => ViewModels.Shown(s.RegEditValueData),
                [dgColumnRegExecuteResult] = s => s.ExecuteResult,
                [dgColumnRegListMatchWithRegEdit] = s => s.ListMatchWithRegEdit,
                [dgColumnRegComplianceDetails] = s => s.ComplianceDetails
            };
            foreach (DataGridColumn column in DataGridRegCommands.Columns)
            {
                MenuItem item = new MenuItem { Header = AppSettings.HeaderOf(column), IsCheckable = true, Tag = column };
                item.Click += ColumnsMenuItem_Click;
                columnsMenu.Items.Add(item);
            }
            columnsMenu.Opened += ColumnsMenu_Opened;
            Style headerStyle = new Style(typeof(DataGridColumnHeader), (Style)FindResource(typeof(DataGridColumnHeader)));
            headerStyle.Setters.Add(new Setter(ContextMenuProperty, columnsMenu));
            DataGridRegCommands.ColumnHeaderStyle = headerStyle;
            settings.ApplyWindow(this);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string filePathList = "List.txt";
            string filePathLog = "Log.txt";
            bool listFound = File.Exists(filePathList);
            try
            {
                foreach (string empty in new[] { filePathLog, "Ignore.txt" })
                {
                    if (File.Exists(empty) && new FileInfo(empty).Length == 0)
                    {
                        File.Delete(empty);
                    }
                }
            }
            catch (Exception)
            {
            }
            try
            {
                if (!File.Exists(filePathList))
                {
                    using (StreamWriter swList = new StreamWriter(filePathList, append: true))
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                ViewModels.WriteLog("List.txt could not be created beside RegistryKey Check. WindowLoaded", ex);
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
            if (listFound)
            {
                ButtonReadListFile_Click(sender, e);
                await QueryRegEdit();
            }
            ButtonFilterFalses.IsChecked = settings.NonCompliantFilter ?? true;
            ButtonFilterTrues.IsChecked = settings.CompliantFilter ?? false;
            filtersRestored = true;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            settings.ReadWindow(this);
            if (filtersRestored)
            {
                settings.NonCompliantFilter = ButtonFilterFalses.IsChecked == true ? null : false;
                settings.CompliantFilter = ButtonFilterTrues.IsChecked == true ? true : null;
            }
            settings.RegColumns = AppSettings.ReadColumns(DataGridRegCommands);
            settings.NonRegColumns = AppSettings.ReadColumns(DataGridNonRegCommands);
            settings.Save();
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
            DataGridColumn column = (DataGridColumn)item.Tag;
            column.Visibility = item.IsChecked ? Visibility.Visible : Visibility.Collapsed;
            if (TextBoxFind.Text.Length > 0)
            {
                FilterObsColl_RegCommands();
            }
        }

        private void Find_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = TabItemRegCommands.IsSelected;
        }

        private void Find_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            TextBoxFind.Focus();
            TextBoxFind.SelectAll();
        }

        private void TextBoxFind_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                TextBoxFind.Text = string.Empty;
                e.Handled = true;
            }
        }

        private void ButtonColumns_Click(object sender, RoutedEventArgs e)
        {
            columnsMenu.PlacementTarget = ButtonColumns;
            columnsMenu.Placement = PlacementMode.Top;
            columnsMenu.IsOpen = true;
        }

        private void ButtonOpenIgnoreFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("Notepad.exe", "Ignore.txt");
            }
            catch (Exception ex)
            {
                ViewModels.WriteLog("OpenIgnoreFile", ex);
            }
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

        private void MenuJumpToRegEdit_Click(object sender, RoutedEventArgs e)
        {
            ModelRegCommand? mo = ((sender as MenuItem)?.Parent as ContextMenu)?.PlacementTarget is DataGridRow { Item: ModelRegCommand clicked } ? clicked : SelectedRows().FirstOrDefault();
            if (mo != null)
            {
                ViewModels.JumpToRegEdit(mo);
            }
        }

        private async void MenuAddToIgnoreList_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.AddToIgnoreList(SelectedRows());
            DataGridRegCommands.Items.Refresh();
            UpdateCounts();
            await QueryRegEdit();
        }

        private async void MenuRemoveFromIgnoreList_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.RemoveFromIgnoreList(SelectedRows());
            DataGridRegCommands.Items.Refresh();
            UpdateCounts();
            await QueryRegEdit();
        }

        private void RegCommandsGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject? element = e.OriginalSource as DependencyObject;
            while (element != null)
            {
                if (element is DataGridRow || element is DataGridColumnHeader || element is ScrollBar || element is ButtonBase || element is Thumb || element is TextBoxBase)
                {
                    return;
                }
                element = element is Visual || element is Visual3D ? VisualTreeHelper.GetParent(element) : LogicalTreeHelper.GetParent(element);
            }
            DataGridRegCommands.UnselectAll();
        }

        private void ButtonReadListFile_Click(object sender, RoutedEventArgs e)
        {
            ButtonsDisable();
            AllFilterReset();
            try
            {
                ViewModels.ReadListFile();
            }
            catch (Exception ex)
            {
                ViewModels.WriteLog("ReadListFile", ex);
            }
            UpdateCounts();
            ButtonsEnable();
        }

        private void ButtonsEnable()
        {
            ButtonExecuteMissingCommands.IsEnabled = true;
            ButtonQueryFromRegEdit.IsEnabled = true;
            ButtonReadListFile.IsEnabled = true;
            ButtonOpenListFile.IsEnabled = true;
            ButtonOpenLogFile.IsEnabled = true;
            ButtonFilterFalses.IsEnabled = true;
            ButtonFilterTrues.IsEnabled = true;
            ButtonAllFiltersReset.IsEnabled = true;
            ButtonColumns.IsEnabled = true;
            ButtonOpenIgnoreFile.IsEnabled = true;
            ((ContextMenu)DataGridRegCommands.Resources["RowMenu"]).IsEnabled = true;
        }

        private void ButtonsDisable()
        {
            ButtonExecuteMissingCommands.IsEnabled = false;
            ButtonQueryFromRegEdit.IsEnabled = false;
            ButtonReadListFile.IsEnabled = false;
            ButtonOpenListFile.IsEnabled = false;
            ButtonOpenLogFile.IsEnabled = false;
            ButtonFilterFalses.IsEnabled = false;
            ButtonFilterTrues.IsEnabled = false;
            ButtonAllFiltersReset.IsEnabled = false;
            ButtonColumns.IsEnabled = false;
            ButtonOpenIgnoreFile.IsEnabled = false;
            ((ContextMenu)DataGridRegCommands.Resources["RowMenu"]).IsEnabled = false;
        }

        private void ButtonOpenListFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists("List.txt"))
                {
                    Process.Start("Notepad.exe", "List.txt");
                }
            }
            catch (Exception ex)
            {
                ViewModels.WriteLog("OpenListFile", ex);
            }
        }

        private void ButtonOpenLogFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists("Log.txt"))
                {
                    Process.Start("Notepad.exe", "Log.txt");
                }
            }
            catch (Exception ex)
            {
                ViewModels.WriteLog("OpenLogFile", ex);
            }
        }

        private async void ButtonQueryFromRegEdit_Click(object sender, RoutedEventArgs e)
        {
            await QueryRegEdit();
        }

        private async Task QueryRegEdit()
        {
            ButtonsDisable();
            await Task.Run(() =>
            {
                ViewModels.QueryFromRegEdit();
            });
            FilterObsColl_RegCommands();
            ButtonsEnable();
        }

        private void ButtonAllFiltersReset_Click(object sender, RoutedEventArgs e)
        {
            AllFilterReset();
        }

        private void TextBoxFind_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!filtersResetting)
            {
                FilterObsColl_RegCommands();
            }
        }

        public void FilterObsColl_RegCommands()
        {
            string find = TextBoxFind.Text;
            bool compliant = ButtonFilterTrues.IsChecked == true;
            bool nonCompliant = ButtonFilterFalses.IsChecked == true;
            List<Func<ModelRegCommand, string>> shownTexts = columnTexts.Where(p => p.Key.Visibility == Visibility.Visible).Select(p => p.Value).ToList();
            DataGridRegCommands.Items.Filter = item => item is ModelRegCommand s
                && ((!compliant && !nonCompliant) || (compliant && s.ListMatchWithRegEdit == "True") || (nonCompliant && s.ListMatchWithRegEdit == "False"))
                && (find.Length == 0 || shownTexts.Any(text => text(s).Contains(find, StringComparison.OrdinalIgnoreCase)));
            UpdateCounts();
        }

        private void UpdateCounts()
        {
            int lines = ViewModels.ObsColl_RegCommands.Count;
            TextLines.Text = lines + (lines == 1 ? " line" : " lines");
            TextCompliant.Text = ViewModels.ObsColl_RegCommands.Count(s => s.ListMatchWithRegEdit == "True") + " compliant";
            TextNonCompliant.Text = ViewModels.ObsColl_RegCommands.Count(s => s.ListMatchWithRegEdit == "False") + " noncompliant";
        }

        private void AllFilterReset()
        {
            filtersResetting = true;
            ButtonFilterFalses.IsChecked = false;
            ButtonFilterTrues.IsChecked = false;
            TextBoxFind.Text = "";
            filtersResetting = false;
            FilterObsColl_RegCommands();
        }

        private void FilterButton_Changed(object sender, RoutedEventArgs e)
        {
            if (!filtersResetting)
            {
                FilterObsColl_RegCommands();
            }
        }

        private async void ButtonExecuteMissingCommands_Click(object sender, RoutedEventArgs e)
        {
            ButtonsDisable();
            await Task.Run(() =>
            {
                ViewModels.QueryFromRegEdit();
            });
            await Task.Run(() => ViewModels.ExecuteMissingCommands());
            await Task.Run(() =>
            {
                ViewModels.QueryFromRegEdit();
            });
            FilterObsColl_RegCommands();
            ButtonsEnable();
        }

        private void DataGridRegCommands_Row_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (((DataGridRow)sender).Item is ModelRegCommand x)
            {
                ViewModels.DisplayingIDs = x.RegCommandId - 1;
            }
            DetailWindow detailWindow = new DetailWindow();
            detailWindow.ShowInTaskbar = false;
            detailWindow.Owner = Application.Current.MainWindow;
            detailWindow.ShowDialog();
        }
    }
}
