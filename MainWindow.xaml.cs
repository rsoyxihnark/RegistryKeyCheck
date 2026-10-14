using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RegistryKey_Check
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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
        }

        private void ButtonReadListFile_Click(object sender, RoutedEventArgs e)
        {
            ButtonsDisable();
            AllFilterReset();
            ViewModels.ReadListFile();
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

        private async void ButtonQueryFromRegEdit_Click(object sender, RoutedEventArgs e)
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

        private void TextBoxFilterRegCommandId_KeyUp(object sender, KeyEventArgs e)
        {
            FilterObsColl_RegCommands();
        }

        public void FilterObsColl_RegCommands()
        {
            DataGridRegCommands.ItemsSource = ViewModels.ObsColl_RegCommands.Where((ModelRegCommand s) =>
                s.RegCommandId.ToString().Contains(TextBoxFilterRegCommandId.Text)
                && s.ListId.ToString().Contains(TextBoxFilterListId.Text)
                && s.ListCommand.Contains(TextBoxFilterListCommand.Text, StringComparison.OrdinalIgnoreCase)
                && s.ListBaseKey.Contains(TextBoxFilterListBaseKey.Text, StringComparison.OrdinalIgnoreCase)
                && s.ListSubKey.Contains(TextBoxFilterListSubKey.Text, StringComparison.OrdinalIgnoreCase)
                && s.AvailableSubKey.Contains(TextBoxFilterAvailableSubKey.Text, StringComparison.OrdinalIgnoreCase)
                && s.ListValueName.Contains(TextBoxFilterListValueName.Text, StringComparison.OrdinalIgnoreCase)
                && s.AvailableValueName.Contains(TextBoxFilterAvailableValueName.Text, StringComparison.OrdinalIgnoreCase)
                && s.ListValueType.Contains(TextBoxFilterListValueType.Text, StringComparison.OrdinalIgnoreCase)
                && s.RegEditValueType.Contains(TextBoxFilterRegEditValueType.Text, StringComparison.OrdinalIgnoreCase)
                && s.ListValueData.Contains(TextBoxFilterListValueData.Text, StringComparison.OrdinalIgnoreCase)
                && s.RegEditValueData.Contains(TextBoxFilterRegEditValueData.Text, StringComparison.OrdinalIgnoreCase)
                && s.ExecuteResult.Contains(TextBoxFilterExecuteResult.Text, StringComparison.OrdinalIgnoreCase)
                && s.ListMatchWithRegEdit.Contains(TextBoxFilterListMatchWithRegEdit.Text, StringComparison.OrdinalIgnoreCase));
            LabelShown.Content = "Shown " + DataGridRegCommands.Items.Count;
            LabelTotal.Content = "Total " + ViewModels.ObsColl_RegCommands.Count;
        }

        private void TextBoxFilterListId_KeyUp(object sender, KeyEventArgs e)
        {
            FilterObsColl_RegCommands();
        }

        private void TextBoxFilterListCommand_KeyUp(object sender, KeyEventArgs e)
        {
            FilterObsColl_RegCommands();
        }

        private void AllFilterReset()
        {
            TextBoxFilterRegCommandId.Text = "";
            TextBoxFilterListId.Text = "";
            TextBoxFilterListCommand.Text = "";
            TextBoxFilterListBaseKey.Text = "";
            TextBoxFilterListSubKey.Text = "";
            TextBoxFilterAvailableSubKey.Text = "";
            TextBoxFilterListValueName.Text = "";
            TextBoxFilterAvailableValueName.Text = "";
            TextBoxFilterListValueType.Text = "";
            TextBoxFilterRegEditValueType.Text = "";
            TextBoxFilterListValueData.Text = "";
            TextBoxFilterRegEditValueData.Text = "";
            TextBoxFilterExecuteResult.Text = "";
            TextBoxFilterListMatchWithRegEdit.Text = "";
            FilterObsColl_RegCommands();
        }

        private void TextBoxFilterListBaseKey_KeyUp(object sender, KeyEventArgs e)
        {
            FilterObsColl_RegCommands();
        }

        private void TextBoxFilterListSubKey_KeyUp(object sender, KeyEventArgs e)
        {
            FilterObsColl_RegCommands();
        }

        private void TextBoxFilterAvailableSubKey_KeyUp(object sender, KeyEventArgs e)
        {
            FilterObsColl_RegCommands();
        }

        private void TextBoxFilterListValueName_KeyUp(object sender, KeyEventArgs e)
        {
            FilterObsColl_RegCommands();
        }

        private void TextBoxFilterAvailableValueName_KeyUp(object sender, KeyEventArgs e)
        {
            FilterObsColl_RegCommands();
        }

        private void TextBoxFilterListValueType_KeyUp(object sender, KeyEventArgs e)
        {
            FilterObsColl_RegCommands();
        }

        private void TextBoxFilterRegEditValueType_KeyUp(object sender, KeyEventArgs e)
        {
            FilterObsColl_RegCommands();
        }

        private void TextBoxFilterListValueData_KeyUp(object sender, KeyEventArgs e)
        {
            FilterObsColl_RegCommands();
        }

        private void TextBoxFilterRegEditValueData_KeyUp(object sender, KeyEventArgs e)
        {
            FilterObsColl_RegCommands();
        }

        private void TextBoxFilterListMatchWithRegEdit_KeyUp(object sender, KeyEventArgs e)
        {
            FilterObsColl_RegCommands();
        }

        private void ButtonFilterFalses_Click(object sender, RoutedEventArgs e)
        {
            TextBoxFilterListMatchWithRegEdit.Text = "False";
            FilterObsColl_RegCommands();
        }

        private void ButtonFilterTrues_Click(object sender, RoutedEventArgs e)
        {
            TextBoxFilterListMatchWithRegEdit.Text = "True";
            FilterObsColl_RegCommands();
        }

        private async void ButtonExecuteMissingCommands_Click(object sender, RoutedEventArgs e)
        {
            ButtonsDisable();
            if (MessageBox.Show("   Dikkat! Bu işlem bilgisayarınızı çalışmaz hale getirebilir.\nDevam etmek istediğinizden emin misiniz?\n\n   Attention!, This action may render your computer inoperable.\nAre you sure you want to continue?", "Warning!", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
            {
                await Task.Run(() =>
                {
                    ViewModels.QueryFromRegEdit();
                });
                await Task.Delay(100);
                await Task.Run(() => ViewModels.ExecuteMissingCommands());
                await Task.Delay(100);
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
            if (DataGridRegCommands.Columns[0].GetCellContent(DataGridRegCommands.SelectedItem) is TextBlock x)
            {
                ViewModels.DisplayingIDs = int.Parse(x.Text) - 1;
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
