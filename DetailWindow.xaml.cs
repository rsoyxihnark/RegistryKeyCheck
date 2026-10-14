using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace RegistryKey_Check
{
    public partial class DetailWindow : Window
    {
        public DetailWindow()
        {
            InitializeComponent();
            WindowTheme.UseDarkTitleBar(this);
        }

        private ModelRegCommand Row => ViewModels.ObsColl_RegCommands[ViewModels.DisplayingIDs];

        private void DetailWin_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void DetailWin_Loaded(object sender, RoutedEventArgs e)
        {
            ModelRegCommand mo = Row;
            TextTitle.Text = "Row " + mo.RegCommandId + "  ·  line " + mo.ListId + " of List.txt";
            TextBoxCommand.Text = mo.ListLineString;
            TextStatus.Text = mo.ListMatchWithRegEdit;
            (string background, string dot, string text) = mo.ListMatchWithRegEdit switch
            {
                "True" => ("#2634D399", "#FF34D399", "#FF6EE7B7"),
                "False" => ("#38F87171", "#FFF87171", "#FFFCA5A5"),
                "Unknown" => ("#26FBBF24", "#FFFBBF24", "#FFFCD34D"),
                _ => ("#2694A3B8", "#FF94A3B8", "#FFCBD5E1")
            };
            StatusPill.Background = (Brush)new BrushConverter().ConvertFromString(background)!;
            StatusDot.Fill = (Brush)new BrushConverter().ConvertFromString(dot)!;
            TextStatus.Foreground = (Brush)new BrushConverter().ConvertFromString(text)!;
            TextBoxRegCommandId.Text = mo.RegCommandId.ToString();
            TextBoxListId.Text = mo.ListId.ToString();
            TextBoxListCommand.Text = mo.ListCommand;
            TextBoxListBaseKey.Text = mo.ListBaseKey;
            TextBoxListSubKey.Text = mo.ListSubKey;
            TextBoxAvailableSubKey.Text = mo.AvailableSubKey;
            TextBoxListValueName.Text = mo.ListValueName;
            TextBoxAvailableValueName.Text = mo.AvailableValueName;
            TextBoxListValueType.Text = mo.ListValueType;
            TextBoxRegEditValueType.Text = mo.RegEditValueType;
            TextBoxListValueData.Text = mo.ListValueData;
            TextBoxRegEditValueData.Text = mo.RegEditValueData;
            TextBoxExecuteResult.Text = mo.ExecuteResult;
            TextBoxListMatchWithRegEdit.Text = mo.ListMatchWithRegEdit;
        }

        private void ButtonJumpToRegEdit_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.JumpToRegEdit(Row);
        }

        private void ButtonCopyCommand_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(Row.ListLineString);
            }
            catch (Exception ex)
            {
                ViewModels.WriteLog("CopyText", ex);
            }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
