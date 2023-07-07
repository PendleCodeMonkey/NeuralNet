using Microsoft.Win32;
using System.Windows;

namespace PendleCodeMonkey.NeuralNetApp
{
	/// <summary>
	/// Interaction logic for LoadDataFilesWindow.xaml
	/// </summary>
	public partial class LoadDataFilesWindow : Window
	{
		public static readonly DependencyProperty trainingDataSelectedProperty =
			DependencyProperty.Register("trainingDataSelected", typeof(bool),
			typeof(LoadDataFilesWindow), new FrameworkPropertyMetadata(false));

		public bool TrainingDataSelected
		{
			get { return (bool)GetValue(trainingDataSelectedProperty); }
			set { SetValue(trainingDataSelectedProperty, value); }
		}

		public static readonly DependencyProperty trainingDataFilePathProperty =
			DependencyProperty.Register("trainingDataFilePath", typeof(string),
			typeof(LoadDataFilesWindow), new FrameworkPropertyMetadata("", new PropertyChangedCallback(OnTrainingDataFilePathChanged)));

		public string TrainingDataFilePath
		{
			get { return (string)GetValue(trainingDataFilePathProperty); }
			set { SetValue(trainingDataFilePathProperty, value); }
		}

		private static void OnTrainingDataFilePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			LoadDataFilesWindow? wnd = d as LoadDataFilesWindow;
			wnd?.OnTrainingDataFilePathChanged(e);
		}

		private void OnTrainingDataFilePathChanged(DependencyPropertyChangedEventArgs e)
		{
			TrainingDataTextBox.Text = e.NewValue.ToString();
		}

		public static readonly DependencyProperty testDataSelectedProperty =
			DependencyProperty.Register("testDataSelected", typeof(bool),
			typeof(LoadDataFilesWindow), new FrameworkPropertyMetadata(false));

		public bool TestDataSelected
		{
			get { return (bool)GetValue(testDataSelectedProperty); }
			set { SetValue(testDataSelectedProperty, value); }
		}

		public static readonly DependencyProperty testDataFilePathProperty =
			DependencyProperty.Register("testDataFilePath", typeof(string),
			typeof(LoadDataFilesWindow), new FrameworkPropertyMetadata("", new PropertyChangedCallback(OnTestDataFilePathChanged)));

		public string TestDataFilePath
		{
			get { return (string)GetValue(testDataFilePathProperty); }
			set { SetValue(testDataFilePathProperty, value); }
		}

		private static void OnTestDataFilePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			LoadDataFilesWindow? wnd = d as LoadDataFilesWindow;
			wnd?.OnTestDataFilePathChanged(e);
		}

		private void OnTestDataFilePathChanged(DependencyPropertyChangedEventArgs e)
		{
			TestDataTextBox.Text = e.NewValue.ToString();
		}

		public LoadDataFilesWindow()
		{
			InitializeComponent();
			SizeToContent = SizeToContent.Height;
			ResizeMode = ResizeMode.NoResize;
			DataContext = this;
		}

		private void TrainingDataBrowseButtonClicked(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new()
			{
				Filter = (string)Application.Current.FindResource("dataFilesFilter")
			};
			if (openFileDialog.ShowDialog() == true)
			{
				TrainingDataFilePath = openFileDialog.FileName;
			}
		}

		private void TestDataBrowseButtonClicked(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new()
			{
				Filter = (string)Application.Current.FindResource("dataFilesFilter")
			};
			if (openFileDialog.ShowDialog() == true)
			{
				TestDataFilePath = openFileDialog.FileName;
			}
		}
		private void OKButtonClicked(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}
	}
}
