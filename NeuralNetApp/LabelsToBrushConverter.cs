using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PendleCodeMonkey.NeuralNetApp
{
	public class LabelsToBrushConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var actual = (values[0] != null && values[0] != DependencyProperty.UnsetValue) ? System.Convert.ToInt32(values[0]) : 0;
			var evaluated = (values[1] != null && values[1] != DependencyProperty.UnsetValue) ? System.Convert.ToInt32(values[1]) : -1;
			if (evaluated < 0)
			{
				return new SolidColorBrush(Color.FromArgb(255, 192, 192, 192));
			}
			return actual == evaluated ? new SolidColorBrush(Color.FromArgb(255, 64, 192, 64)) :
										new SolidColorBrush(Color.FromArgb(255, 224, 128, 128));
		}

		public object[] ConvertBack(object values, Type[] targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
