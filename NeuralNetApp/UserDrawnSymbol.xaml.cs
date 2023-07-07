using PendleCodeMonkey.NeuralNetLib;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PendleCodeMonkey.NeuralNetApp
{
	/// <summary>
	/// Interaction logic for UserDrawnSymbol.xaml
	/// </summary>
	public partial class UserDrawnSymbol : Window
	{
		private Network? _network;
		private UserSettings? _userSettings;
		private double _initialWindowHeight;
		private double _initialWindowWidth;
		private int _brushSize = 32;

		public UserDrawnSymbol(UserSettings? userSettings)
		{
			_userSettings = userSettings;
			InitializeComponent();

			_initialWindowHeight = Height;
			_initialWindowWidth = Width;

			if (_userSettings != null)
			{
				UserSettings.LoadWindowStateSettings(_userSettings.UserDrawnSymbolsWindowStateSettings, this);
				_brushSize = _userSettings.UserDrawnSymbolBrushSize;
			}
		}

		public void SetNetwork(Network? network)
		{
			_network = network;
		}

		private void CreateBitmapButtonClicked(object sender, RoutedEventArgs e)
		{
			var bitmap = ConvertToBitmapSource(inkCanvas);

			int newWidth = 28;
			int newHeight = 28;
			var bitmap2 = new TransformedBitmap(bitmap,
				new ScaleTransform(
					(double)newWidth / (double)bitmap.PixelWidth,
					(double)newHeight / (double)bitmap.PixelHeight));

			int width = bitmap2.PixelWidth;
			int height = bitmap2.PixelHeight;
			int bytesPerPixel = bitmap2.Format.BitsPerPixel / 8;
			int stride = width * bytesPerPixel;

			var pixelBuffer = new byte[height * stride];
			bitmap2.CopyPixels(pixelBuffer, stride, 0);

			List<int> bmpIntData = new();
			for (int i = 0; i < width * height; i++)
			{
				bmpIntData.Add(255 - pixelBuffer[i * bytesPerPixel]);
			}

			int identifiedAs = -1;

			MNIST_Data.Data data = new(bmpIntData, 0);
			var translatedData = MNIST_Data.TranslateTestData(new List<MNIST_Data.Data> { data });
			if (_network != null)
			{
				var cancelSource = new CancellationTokenSource();
				var result = _network.Evaluate(translatedData, cancelSource.Token);
				identifiedAs = result.evalResults[0];
			}

			var temp = bmpIntData.Select(x => (byte)x).ToList();
			var image = CreateBitmap(temp, true);
			SymbolControl symCtrl = new();
			symCtrl.imgSymbol.Source = image;
			symCtrl.ActualLabel = -1;
			symCtrl.EvaluatedLabel = identifiedAs;
			symCtrl.SetUserDrawnSymbolMode();
			symbolPanel.Children.Add(symCtrl);
			symCtrl.BringIntoView();

			// Clear the drawing canvas.
			inkCanvas.Strokes.Clear();
		}

		private static WriteableBitmap CreateBitmap(List<byte> data, bool invertImage = false)
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

			// Update writeable bitmap with the colorArray to the image.
			Int32Rect rect = new(0, 0, width, height);
			int stride = 4 * width;
			wbitmap.WritePixels(rect, pixels, stride, 0);

			return wbitmap;
		}

		public static BitmapSource ConvertToBitmapSource(UIElement element)
		{
			var target = new RenderTargetBitmap((int)(element.RenderSize.Width), (int)(element.RenderSize.Height), 96, 96, PixelFormats.Pbgra32);
			var brush = new VisualBrush(element);

			var visual = new DrawingVisual();
			var drawingContext = visual.RenderOpen();


			drawingContext.DrawRectangle(brush, null, new Rect(new System.Windows.Point(0, 0),
				new System.Windows.Point(element.RenderSize.Width, element.RenderSize.Height)));

			drawingContext.PushOpacityMask(brush);

			drawingContext.Close();

			target.Render(visual);

			return target;
		}

		private void ClearButtonClicked(object sender, RoutedEventArgs e)
		{
			inkCanvas.Strokes.Clear();
		}

		private void ClearIdentifiedPanelButtonClicked(object sender, RoutedEventArgs e)
		{
			symbolPanel.Children.Clear();
		}

		private void CloseButtonClicked(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			// Don't want to allow the window to be resized any smaller than the initial (default) size.
			SetValue(MinWidthProperty, _initialWindowWidth);
			SetValue(MinHeightProperty, _initialWindowHeight);

			for (int brushSize = 10; brushSize <= 40; brushSize++)
			{
				BrushSizeComboBox.Items.Add(brushSize.ToString());
			}
			BrushSizeComboBox.SelectedItem = _brushSize.ToString();
			inkCanvas.DefaultDrawingAttributes.Width = _brushSize;
			inkCanvas.DefaultDrawingAttributes.Height = _brushSize;
		}

		private void BrushSizeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			var success = int.TryParse(BrushSizeComboBox.SelectedItem.ToString(), out int size);
			if (success)
			{
				_brushSize = size;
				inkCanvas.DefaultDrawingAttributes.Width = size;
				inkCanvas.DefaultDrawingAttributes.Height = size;
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (_userSettings != null)
			{
				_userSettings.UserDrawnSymbolsWindowStateSettings = UserSettings.GetWindowStateSettings(this);
				_userSettings.UserDrawnSymbolBrushSize = _brushSize;
			}
		}
	}
}
