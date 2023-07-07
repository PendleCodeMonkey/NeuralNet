using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PendleCodeMonkey.NeuralNetApp
{
	/// <summary>
	/// Interaction logic for SymbolControl.xaml
	/// </summary>
	public partial class SymbolControl : UserControl
	{
		public SymbolControl()
		{
			InitializeComponent();

			DataContext = this;
		}

		public void SetUserDrawnSymbolMode()
		{
			SymbolLabellingGrid.Background = Brushes.Silver;
			LabelPanel.Children.Remove(txtActualValue);
			txtEvaluatedValue.Width = 56;
		}

		public static readonly DependencyProperty actualLabelProperty =
			DependencyProperty.Register("actualLabel", typeof(int),
			typeof(SymbolControl), new FrameworkPropertyMetadata(0));

		public int ActualLabel
		{
			get { return (int)GetValue(actualLabelProperty); }
			set { SetValue(actualLabelProperty, value); }
		}

		public static readonly DependencyProperty evaluatedLabelProperty =
			DependencyProperty.Register("evaluatedLabel", typeof(int),
			typeof(SymbolControl), new FrameworkPropertyMetadata(0));

		public int EvaluatedLabel
		{
			get { return (int)GetValue(evaluatedLabelProperty); }
			set { SetValue(evaluatedLabelProperty, value); }
		}
	}
}
