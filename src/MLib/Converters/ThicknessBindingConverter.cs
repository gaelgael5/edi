namespace MLib.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    /// <summary>
    /// Enumeration to determine the side of the thickness to ignore in thickness converter.
    /// </summary>
    public enum IgnoreThicknessSideType
    {
        /// <summary>
        /// Use all sides.
        /// </summary>
        None,
        /// <summary>
        /// Ignore the left side.
        /// </summary>
        Left,
        /// <summary>
        /// Ignore the top side.
        /// </summary>
        Top,
        /// <summary>
        /// Ignore the right side.
        /// </summary>
        Right,
        /// <summary>
        /// Ignore the bottom side.
        /// </summary>
        Bottom
    }

    /// <summary>
    /// Converts a Thickness to a new Thickness. It's possible to ignore a side
    /// with the IgnoreThicknessSide property.
    /// </summary>
    public class ThicknessBindingConverter : IValueConverter
    {
        
        /// <summary>
        /// Gets/sets the thickness sides that should be ignored in the conversion.
        /// </summary>
        public IgnoreThicknessSideType IgnoreThicknessSide { get; set; }

        /// <summary>
        /// Converts a Thickness to a new Thickness. It's possible to ignore a side
        /// with the IgnoreThicknessSide property.
        /// </summary>
        /// <param name = "value"></param>
        /// <param name = "targetType"></param>
        /// <param name = "parameter"></param>
        /// <param name = "culture"></param>
        /// <returns>The converted object.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Thickness)
            {
                // yes, we can override it with the parameter value
                if (parameter is IgnoreThicknessSideType)
                {
                    this.IgnoreThicknessSide = (IgnoreThicknessSideType)parameter;
                }
                
                var orgThickness = (Thickness)value;

                switch (this.IgnoreThicknessSide)
                {
                    case IgnoreThicknessSideType.Left:
                        return new Thickness(0, orgThickness.Top, orgThickness.Right, orgThickness.Bottom);
                    case IgnoreThicknessSideType.Top:
                        return new Thickness(orgThickness.Left, 0, orgThickness.Right, orgThickness.Bottom);
                    case IgnoreThicknessSideType.Right:
                        return new Thickness(orgThickness.Left, orgThickness.Top, 0, orgThickness.Bottom);
                    case IgnoreThicknessSideType.Bottom:
                        return new Thickness(orgThickness.Left, orgThickness.Top, orgThickness.Right, 0);
                    default:
                        return orgThickness;
                }
            }
            return default(Thickness);
        }

        /// <summary>
        /// Method is not implemented.
        /// </summary>
        /// <param name = "value"></param>
        /// <param name = "targetType"></param>
        /// <param name = "parameter"></param>
        /// <param name = "culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // for now no back converting
            return DependencyProperty.UnsetValue;
        }
    }
}
