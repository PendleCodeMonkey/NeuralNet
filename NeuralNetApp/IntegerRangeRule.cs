using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace PendleCodeMonkey.NeuralNetApp
{
	public class IntegerRangeRule : ValidationRule
	{
		public int Min { get; set; }
		public int Max { get; set; }

		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			var intValue = 0;

			try
			{
				if (((string)value).Length > 0)
				{
					intValue = int.Parse((string)value);
				}
			}
			catch (Exception e)
			{
				return new ValidationResult(false, e.Message);
			}

			if ((intValue < Min) || (intValue > Max))
			{
				return new ValidationResult(false,
					string.Format((string)Application.Current.FindResource("invalidRangeValue"), Min, Max));
			}
			return new ValidationResult(true, null);
		}
	}
}
