using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scarlet
{
    internal static class ExtensionMethods
    {
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            if (val.CompareTo(max) > 0) return max;
            return val;
        }

        /* http://stackoverflow.com/a/23177585 */
        public static bool IsValid<TEnum>(this TEnum enumValue) where TEnum : struct
        {
            var firstChar = enumValue.ToString()[0];
            return (firstChar < '0' || firstChar > '9') && firstChar != '-';
        }

        /* http://stackoverflow.com/a/18375526 */
        public static bool InheritsFrom(this Type? type, Type? baseType)
        {
            if (type is null) return false;
            if (baseType is null) return type.IsInterface;
            if (baseType.IsInterface) return type.GetInterfaces().Contains(baseType);

            var currentType = type;
            while (currentType is not null)
            {
                if (currentType.BaseType == baseType) return true;
                currentType = currentType.BaseType;
            }

            return false;
        }

        /* https://stackoverflow.com/a/25577853 */
        public static string ReadNullTerminatedString(this System.IO.BinaryReader reader)
        {
            StringBuilder str = new();
            char ch;
            while ((ch = reader.ReadChar()) != 0) str.Append(ch);
            return str.ToString();
        }
    }
}
