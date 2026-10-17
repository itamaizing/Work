using System;
using System.Linq;
using TMPro;

namespace Extensions
{
    public static class DropdownExtensions
    {
        public static TMP_Dropdown FillWithEnum<TEnum>(this TMP_Dropdown dropdown) where TEnum : Enum
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(Enum.GetNames(typeof(TEnum)).ToList());
            dropdown.value = 0;
            return dropdown;
        }
    }
}
