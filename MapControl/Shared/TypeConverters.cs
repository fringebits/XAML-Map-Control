using System;
using System.ComponentModel;
using System.Globalization;

namespace MapControl
{
    public static class CoreTypeConverters
    {
        public static Helix.CoreTypes.Point ToCorePoint(this System.Windows.Point value)
        {
            return new Helix.CoreTypes.Point(value.X, value.Y);
        }

        public static System.Windows.Point ToSystemPoint(this Helix.CoreTypes.Point value)
        {
            return new System.Windows.Point(value.X, value.Y);
        }

        public static Helix.CoreTypes.Point ToCorePoint(this System.Windows.Vector value)
        {
            return new Helix.CoreTypes.Point(value.X, value.Y);
        }

        public static System.Windows.Rect ToSystemRect(this Helix.CoreTypes.Rect value)
        {
            return new System.Windows.Rect(value.X, value.Y, value.Width, value.Height);
        }
    }

    public class TileSourceConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return TileSource.Parse((string)value);
        }
    }
}
