using System.Windows;
using System.Windows.Input;

namespace RegistryKey_Check
{
    public partial class DetailWindow : Window
    {
        public DetailWindow()
        {
            InitializeComponent();
        }

        private void DetailWin_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void DetailWin_Loaded(object sender, RoutedEventArgs e)
        {
            TextBoxRegCommandId.Text = ViewModels.ObsColl_RegCommands[ViewModels.DisplayingIDs].RegCommandId.ToString();
            TextBoxListId.Text = ViewModels.ObsColl_RegCommands[ViewModels.DisplayingIDs].ListId.ToString();
            TextBoxListCommand.Text = ViewModels.ObsColl_RegCommands[ViewModels.DisplayingIDs].ListCommand;
            TextBoxListBaseKey.Text = ViewModels.ObsColl_RegCommands[ViewModels.DisplayingIDs].ListBaseKey;
            TextBoxListSubKey.Text = ViewModels.ObsColl_RegCommands[ViewModels.DisplayingIDs].ListSubKey;
            TextBoxAvailableSubKey.Text = ViewModels.ObsColl_RegCommands[ViewModels.DisplayingIDs].AvailableSubKey;
            TextBoxListValueName.Text = ViewModels.ObsColl_RegCommands[ViewModels.DisplayingIDs].ListValueName;
            TextBoxAvailableValueName.Text = ViewModels.ObsColl_RegCommands[ViewModels.DisplayingIDs].AvailableValueName;
            TextBoxListValueType.Text = ViewModels.ObsColl_RegCommands[ViewModels.DisplayingIDs].ListValueType;
            TextBoxRegEditValueType.Text = ViewModels.ObsColl_RegCommands[ViewModels.DisplayingIDs].RegEditValueType;
            TextBoxListValueData.Text = ViewModels.ObsColl_RegCommands[ViewModels.DisplayingIDs].ListValueData;
            TextBoxRegEditValueData.Text = ViewModels.ObsColl_RegCommands[ViewModels.DisplayingIDs].RegEditValueData;
            TextBoxExecuteResult.Text = ViewModels.ObsColl_RegCommands[ViewModels.DisplayingIDs].ExecuteResult;
            TextBoxListMatchWithRegEdit.Text = ViewModels.ObsColl_RegCommands[ViewModels.DisplayingIDs].ListMatchWithRegEdit;
            TextBoxComplianceDetails.Text = ViewModels.ObsColl_RegCommands[ViewModels.DisplayingIDs].ComplianceDetails;
        }
    }
}
