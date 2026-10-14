using System.ComponentModel;

namespace RegistryKey_Check
{
    public class ModelRegCommand : INotifyPropertyChanged
    {
        private bool isChecked;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsChecked
        {
            get => isChecked;
            set
            {
                if (isChecked != value)
                {
                    isChecked = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
                }
            }
        }

        public int RegCommandId { get; set; }

        public int ListId { get; set; }

        public string ListCommand { get; set; } = string.Empty;

        public string ListBaseKey { get; set; } = string.Empty;

        public string ListSubKey { get; set; } = string.Empty;

        public string ListPath { get; set; } = string.Empty;

        public bool DeleteAllValues { get; set; }

        public string ListValueNameText => DeleteAllValues ? "(All values)" : DisplayTextConverter.Display(ListValueName, true);

        public bool DataDiffers { get; set; }

        public bool TypeDiffers { get; set; }

        public string AvailableSubKey { get; set; } = string.Empty;

        public string ListValueName { get; set; } = "This_field_is_NULL";

        public string AvailableValueName { get; set; } = "This_field_is_NULL";

        public string ListValueType { get; set; } = "This_field_is_NULL";

        public string RegEditValueType { get; set; } = "This_field_is_NULL";

        public string ListValueData { get; set; } = "This_field_is_NULL";

        public object ReturnObject { get; set; } = "This_field_is_NULL";

        public string RegEditValueData { get; set; } = "This_field_is_NULL";

        public string ExecuteResult { get; set; } = string.Empty;

        public string ListMatchWithRegEdit { get; set; } = string.Empty;

        public string ListLineString { get; set; } = string.Empty;

        public ulong? ListValueNumber { get; set; }
    }
}
