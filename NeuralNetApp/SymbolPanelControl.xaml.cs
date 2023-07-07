using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PendleCodeMonkey.NeuralNetApp
{
	/// <summary>
	/// Interaction logic for SymbolPanelControl.xaml
	/// </summary>
	public partial class SymbolPanelControl : UserControl
	{
		private DataModel? _dataModel;

		private int _totalPages = 0;

		// 200 symbols displayed per page.
		private const int _pageSize = 200;

		private int _page = 1;

		private List<int> _evaluatedLabels = new();

		public static readonly RoutedCommand PreviousPageCmd = new();
		public static readonly RoutedCommand NextPageCmd = new();

		public SymbolPanelControl()
		{
			InitializeComponent();

			NavigationPanel.Visibility = Visibility.Hidden;
		}

		public void SetDataModel(DataModel model)
		{
			_dataModel = model;
			_totalPages = 0;
			TestDataFileName.Text = string.Empty;

			if (_dataModel != null && _dataModel.TestData != null)
			{
				_totalPages = (_dataModel.TestData.Count / _pageSize) + (_dataModel.TestData.Count % _pageSize == 0 ? 0 : 1);
				TestDataFileName.Text = $"Test data: {_dataModel.TestDataFileName}";
				NavigationPanel.Visibility = Visibility.Visible;
			}
		}

		public void SetEvaluatedLabels(List<int> evaluatedLabels, int correctCount)
		{
			_evaluatedLabels = evaluatedLabels;
			CorrectlyEvaluatedTextBlock.Text = $"{correctCount} of {evaluatedLabels.Count} correctly identified.";
		}

		private static WriteableBitmap GenerateBitmap(List<byte> data, bool invertImage = false)
		{
			const int width = 28;
			const int height = 28;

			WriteableBitmap wbitmap = new(width, height, 96, 96, PixelFormats.Bgra32, null);
			byte[] pixels = new byte[height * width * 4];

			int index = 0;
			foreach (byte b in data)
			{
				byte rgbValue = invertImage ? (byte)(255 - b) : b;
				pixels[index++] = rgbValue;
				pixels[index++] = rgbValue;
				pixels[index++] = rgbValue;
				pixels[index++] = 255;
			}

			// Update writeable bitmap with the array of pixels.
			Int32Rect rect = new(0, 0, width, height);
			int stride = 4 * width;
			wbitmap.WritePixels(rect, pixels, stride, 0);

			return wbitmap;
		}


		public void Populate()
		{
			if (_dataModel != null && _dataModel.TestData != null)
			{
				_evaluatedLabels = Enumerable.Repeat(-1, _dataModel.TestData.Count).ToList();
			}
			PopulateSymbolPanel(_page - 1);
		}

		public void PopulateSymbolPanel(int page)
		{
			panelWrap.Children.Clear();
			if (_dataModel != null && _dataModel.TestData != null)
			{
				for (int i = page * _pageSize; i < Math.Min((page + 1) * _pageSize, _dataModel.TestData.Count); i++)
				{
					var data = _dataModel.TestData[i].image!.Select(x => (byte)x).ToList();
					SymbolControl symCtrl = new();
					symCtrl.imgSymbol.Source = GenerateBitmap(data, true);
					symCtrl.ActualLabel = _dataModel.TestData[i].label;
					symCtrl.EvaluatedLabel = _evaluatedLabels[i];
					panelWrap.Children.Add(symCtrl);
				}

				PageTextBlock.Text = $"{page + 1} of {_totalPages}";
			}
		}

		public void RefreshPage()
		{
			PopulateSymbolPanel(_page - 1);
		}

		// ExecutedRoutedEventHandler for the Previous Page command.
		private void PreviousPageCmdExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (_page > 1)
			{
				_page--;
				PopulateSymbolPanel(_page - 1);
			}
		}

		// CanExecuteRoutedEventHandler for the Previous Page command.
		private void PreviousPageCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _page > 1;
		}

		// ExecutedRoutedEventHandler for the Next Page command.
		private void NextPageCmdExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (_dataModel != null && _dataModel.TestData != null)
			{
				if (_page < _totalPages)
				{
					_page++;
					PopulateSymbolPanel(_page - 1);
				}
			}
		}

		// CanExecuteRoutedEventHandler for the Next Page command.
		private void NextPageCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _dataModel != null && _dataModel.TestData != null && _page < _totalPages;
		}
	}
}
