using Microsoft.Win32;

namespace RegistryKey_Check
{
    public class ModelRegCommand
    {
        public int RegCommandId { get; set; }

        public int ListId { get; set; }

        public string ListCommand { get; set; } = string.Empty;

        public string ListBaseKey { get; set; } = string.Empty;

        public string ListSubKey { get; set; } = string.Empty;

        public RegistryView ListView { get; set; } = RegistryView.Default;

        public bool DeleteAllValues { get; set; }

        public string AvailableSubKey { get; set; } = string.Empty;

        public string ListValueName { get; set; } = "This_field_is_NULL";

        public string AvailableValueName { get; set; } = "This_field_is_NULL";

        public string ListValueType { get; set; } = "This_field_is_NULL";

        public string RegEditValueType { get; set; } = "This_field_is_NULL";

        public string ListValueData { get; set; } = "This_field_is_NULL";

        public string ListValueDataRaw { get; set; } = "This_field_is_NULL";

        public object ReturnObject { get; set; } = "This_field_is_NULL";

        public string RegEditValueData { get; set; } = "This_field_is_NULL";

        public string ExecuteResult { get; set; } = string.Empty;

        public string ListMatchWithRegEdit { get; set; } = string.Empty;

        public string ListLineString { get; set; } = string.Empty;

        public ulong? ListValueNumber { get; set; }
    }
}
