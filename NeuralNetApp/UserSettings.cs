using System.Windows;

namespace PendleCodeMonkey.NeuralNetApp
{
	public class UserSettings
	{
		public class NetworkV1Settings
		{
			public int NumberOfEpochs { get; set; }
			public int MiniBatchSize { get; set; }
			public float LearningRate { get; set; }
		}

		public class WindowStateSettings
		{
			public WindowState State { get; set; }
			public Visibility Visibility { get; set; }
			public double Top { get; set; }
			public double Left { get; set; }
			public double Height { get; set; }
			public double Width { get; set; }
		}

		public UserSettings()
		{
			LoadTrainingDataEnabled = false;
			TrainingDataPath = string.Empty;
			LoadTestDataEnabled = false;
			TestDataPath = string.Empty;

			NetV1Settings = new NetworkV1Settings()
			{
				NumberOfEpochs = 3,
				MiniBatchSize = 10,
				LearningRate = 3.0f
			};

			UserDrawnSymbolBrushSize = 32;
		}

		public bool LoadTrainingDataEnabled { get; set; }
		public string? TrainingDataPath { get; set; }

		public bool LoadTestDataEnabled { get; set; }
		public string? TestDataPath { get; set; }

		public NetworkV1Settings? NetV1Settings { get; set; }

		public WindowStateSettings? MainWindowStateSettings { get; set; }
		public WindowStateSettings? UserDrawnSymbolsWindowStateSettings { get; set; }

		public int UserDrawnSymbolBrushSize { get; set; }

		public static WindowStateSettings GetWindowStateSettings(Window wnd)
		{
			WindowStateSettings windowStateSettings = new ();
			if (wnd != null)
			{
				windowStateSettings.State = wnd.WindowState;
				windowStateSettings.Visibility = wnd.Visibility;
				if (wnd.WindowState == WindowState.Maximized)
				{
					windowStateSettings.Top = wnd.RestoreBounds.Top;
					windowStateSettings.Left = wnd.RestoreBounds.Left;
					windowStateSettings.Height = wnd.RestoreBounds.Height;
					windowStateSettings.Width = wnd.RestoreBounds.Width;
				}
				else
				{
					windowStateSettings.Top = wnd.Top;
					windowStateSettings.Left = wnd.Left;
					windowStateSettings.Height = wnd.Height;
					windowStateSettings.Width = wnd.Width;
				}
			}

			return windowStateSettings;
		}

		public static void LoadWindowStateSettings(WindowStateSettings? windowStateSettings, Window wnd)
		{
			if (wnd != null && windowStateSettings != null)
			{
				wnd.WindowState = windowStateSettings.State;
				wnd.Visibility = windowStateSettings.Visibility;
				wnd.Top = windowStateSettings.Top;
				wnd.Left = windowStateSettings.Left;
				wnd.Height = windowStateSettings.Height;
				wnd.Width = windowStateSettings.Width;
			}
		}
	}
}
