using System.Windows;

namespace RegistryKey_Check
{
    public partial class ConfirmWindow : Window
    {
        public ConfirmWindow()
        {
            InitializeComponent();
            WindowTheme.UseDarkTitleBar(this);
        }

        public static bool Ask(Window owner, string scope)
        {
            ConfirmWindow window = new ConfirmWindow();
            window.Owner = owner;
            window.TextScope.Text = scope;
            return window.ShowDialog() == true;
        }

        private void ButtonExecute_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
