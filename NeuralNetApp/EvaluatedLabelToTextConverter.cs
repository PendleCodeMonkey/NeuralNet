using System;
using System.Globalization;
using System.Windows.Data;

namespace PendleCodeMonkey.NeuralNetApp
{
	[ValueConversion(typeof(int), typeof(string))]
	public class EvaluatedLabelToTextConverter : IValueConverter
	{
		public EvaluatedLabelToTextConverter()
		{
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var intValue = value as int? ?? -1;

			return intValue < 0 ? "-" : intValue.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
