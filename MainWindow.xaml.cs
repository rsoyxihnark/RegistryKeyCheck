using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        private string matchFilter = string.Empty;

        public MainWindow()
        {
            App.UseDarkTitleBar(this);
            InitializeComponent();
            columnTexts = new Dictionary<DataGridColumn, Func<ModelRegCommand, string>>
            {
                [dgColumnRegUID] = s => s.ListId.ToString(),
                [dgColumnRegListCommand] = s => s.ListCommand,
                [dgColumnRegFullPath] = s => s.ListFullPath,
                [dgColumnRegAvailableSubKey] = s => s.AvailableSubKey,
                [dgColumnRegListValueName] = s => ViewModels.Shown(s.ListValueName),
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
            }
            catch (Exception ex)
            {
                ViewModels.WriteLog("List.txt or Log.txt could not be created beside RegistryKey Check. WindowLoaded", ex);
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
                ButtonFilterFalses_Click(sender, e);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            settings.ReadWindow(this);
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
            PanelFind.Visibility = Visibility.Visible;
            PanelFind.UpdateLayout();
            TextBoxFind.Focus();
            TextBoxFind.SelectAll();
        }

        private void TextBoxFind_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                TextBoxFind.Text = string.Empty;
                PanelFind.Visibility = Visibility.Collapsed;
                DataGridRegCommands.Focus();
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
                if (!File.Exists("Ignore.txt"))
                {
                    using (StreamWriter swIgnore = new StreamWriter("Ignore.txt", append: true))
                    {
                    }
                }
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

        private void MenuAddToIgnoreList_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.AddToIgnoreList(SelectedRows());
            DataGridRegCommands.Items.Refresh();
        }

        private void MenuRemoveFromIgnoreList_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.RemoveFromIgnoreList(SelectedRows());
            DataGridRegCommands.Items.Refresh();
        }

        private void RegCommandsGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject? element = e.OriginalSource as DependencyObject;
            while (element != null)
            {
                if (element is DataGridRow || element is DataGridColumnHeader || element is ScrollBar || element is ButtonBase || element is Thumb)
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
            DataGridRegCommands.ItemsSource = ViewModels.ObsColl_RegCommands;
            DataGridNonRegCommands.ItemsSource = ViewModels.ObsColl_Non_RegCommands;
            LabelShown.Content = "Shown " + DataGridRegCommands.Items.Count;
            LabelTotal.Content = "Total " + ViewModels.ObsColl_RegCommands.Count;
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
            ButtonHelp.IsEnabled = true;
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
            ButtonHelp.IsEnabled = false;
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
            DataGridRegCommands.Items.Refresh();
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
            List<Func<ModelRegCommand, string>> shownTexts = columnTexts.Where(p => p.Key.Visibility == Visibility.Visible).Select(p => p.Value).ToList();
            DataGridRegCommands.ItemsSource = ViewModels.ObsColl_RegCommands.Where((ModelRegCommand s) =>
                s.ListMatchWithRegEdit.Contains(matchFilter, StringComparison.OrdinalIgnoreCase)
                && (find.Length == 0 || shownTexts.Any(text => text(s).Contains(find, StringComparison.OrdinalIgnoreCase))));
            LabelShown.Content = "Shown " + DataGridRegCommands.Items.Count;
            LabelTotal.Content = "Total " + ViewModels.ObsColl_RegCommands.Count;
        }

        private void AllFilterReset()
        {
            filtersResetting = true;
            matchFilter = string.Empty;
            TextBoxFind.Text = "";
            PanelFind.Visibility = Visibility.Collapsed;
            filtersResetting = false;
            FilterObsColl_RegCommands();
        }

        private void ButtonFilterFalses_Click(object sender, RoutedEventArgs e)
        {
            matchFilter = "False";
            FilterObsColl_RegCommands();
        }

        private void ButtonFilterTrues_Click(object sender, RoutedEventArgs e)
        {
            matchFilter = "True";
            FilterObsColl_RegCommands();
        }

        private async void ButtonExecuteMissingCommands_Click(object sender, RoutedEventArgs e)
        {
            ButtonsDisable();
            if (MessageBox.Show("   Dikkat! Bu işlem bilgisayarınızı çalışmaz hale getirebilir.\nDevam etmek istediğinizden emin misiniz?\n\n   Attention! This action may render your computer inoperable.\nAre you sure you want to continue?", "Warning!", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
            {
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
                DataGridRegCommands.Items.Refresh();
            }
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

        private void ButtonHelp_Click(object sender, RoutedEventArgs e)
        {
            HelpWindow helpWindow = new HelpWindow();
            helpWindow.ShowInTaskbar = false;
            helpWindow.Owner = Application.Current.MainWindow;
            helpWindow.ShowDialog();
        }
    }
}
