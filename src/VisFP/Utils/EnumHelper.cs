using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace VisFP.Utils
{
    public static class EnumHelper
    {
        public static string GetDisplayName(this Enum enumValue)
        {
            return enumValue.GetType().GetMember(enumValue.ToString())
                           .First()
                           .GetCustomAttribute<DisplayAttribute>()
                           .Name;
        }
    }
}
