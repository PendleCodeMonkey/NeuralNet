using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace PendleCodeMonkey.NeuralNetApp
{
	internal class FloatRangeRule : ValidationRule
	{
		public float Min { get; set; }
		public float Max { get; set; }

		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			var floatValue = 0.0f;

			try
			{
				if (((string)value).Length > 0)
				{
					floatValue = float.Parse((string)value);
				}
			}
			catch (Exception e)
			{
				return new ValidationResult(false, e.Message);
			}

			if ((floatValue < Min) || (floatValue > Max))
			{
				return new ValidationResult(false,
					string.Format((string)Application.Current.FindResource("invalidRangeValue"), Min, Max));
			}
			return new ValidationResult(true, null);
		}
	}
}
