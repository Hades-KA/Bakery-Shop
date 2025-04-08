using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace PhamTaManhLan_8888.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum enumValue)
        {
            var member = enumValue.GetType().GetMember(enumValue.ToString()).FirstOrDefault();

            if (member != null)
            {
                var attribute = member.GetCustomAttribute<DisplayAttribute>();
                if (attribute != null)
                {
                    return attribute.Name;
                }
            }
            return enumValue.ToString();
        }
    }
}
