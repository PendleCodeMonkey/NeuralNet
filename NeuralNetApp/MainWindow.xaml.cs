using Microsoft.Win32;
using PendleCodeMonkey.NeuralNetLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PendleCodeMonkey.NeuralNetApp
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private readonly string _appName = "PCMNeuralNet";
		private readonly SettingsManager<UserSettings> _settingsManager;
		private UserSettings? _userSettings;

		private double _initialWindowHeight;
		private double _initialWindowWidth;
		private Network? _network;

		private readonly DataModel _dataModel;
		private CancellationTokenSource? _cancelSource;

		public static readonly RoutedCommand LoadDataFilesCmd = new();
		public static readonly RoutedCommand LoadTrainingDataCmd = new();
		public static readonly RoutedCommand SaveTrainingDataCmd = new();
		public static readonly RoutedCommand ExitCmd = new();
		public static readonly RoutedCommand TrainNetworkCmd = new();
		public static readonly RoutedCommand CancelTrainingCmd = new();
		public static readonly RoutedCommand IdentifyUserDrawnSymbolsCmd = new();

		private bool _trainingActive = false;
		private readonly bool _loadingData = false;
		private Stopwatch? _trainingStopwatch;
		private Network.NetworkStatus _netStatus;
		private int _cachedPercentComplete = 0;

		public MainWindow()
		{
			InitializeComponent();

			_dataModel = new DataModel();
			_userSettings = new UserSettings();
			_settingsManager = new SettingsManager<UserSettings>(_appName, "UserSettings.json");
			_netStatus = Network.NetworkStatus.Ready;
			DataContext = _dataModel;

			_initialWindowHeight = Height;
			_initialWindowWidth = Width;

			_userSettings = _settingsManager.LoadSettings() ?? new UserSettings();
			if (_userSettings != null)
			{
				if (_userSettings.NetV1Settings != null)
				{
					_dataModel.NumberOfEpochs = _userSettings.NetV1Settings.NumberOfEpochs;
					_dataModel.MiniBatchSize = _userSettings.NetV1Settings.MiniBatchSize;
					_dataModel.LearningRate = _userSettings.NetV1Settings.LearningRate;
				}
				UserSettings.LoadWindowStateSettings(_userSettings.MainWindowStateSettings, this);
			}
		}

		private static string GetStringResource(string id)
		{
			return (string)Application.Current.FindResource(id);
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			// Don't want to allow the window to be resized any smaller than the initial (default) size.
			SetValue(MinWidthProperty, _initialWindowWidth);
			SetValue(MinHeightProperty, _initialWindowHeight);
		}

		private void TrainNetwork()
		{
			if (_dataModel.TranslatedTrainingData != null)
			{
				_trainingActive = true;
				var cacheBackground = StatusTextBarItem.Background;
				var cacheForeground = StatusTextBlock.Foreground;
				StatusTextBarItem.Background = Brushes.DarkRed;
				StatusTextBlock.Foreground = Brushes.White;
				RemainingTimeTextBlock.Text = string.Empty;

				List<int> sizes = new() { 784, 30, 10 };
				_network = new Network(sizes)
				{
					ReportResults = ResultReporter,
					ReportProgress = m => Dispatcher.Invoke(() =>
					{
						progress.Value = m;
						EstimateRemainingTime(m);
					}),
					ReportStatus = (s, m) => Dispatcher.Invoke(() =>
					{
						OnNetworkStatusChanged(s);
						StatusTextBlock.Text = m;
					})
				};

				_trainingStopwatch = Stopwatch.StartNew();

				_cancelSource = new CancellationTokenSource();
				Task.Factory.StartNew(() =>
				_network.SGD(
					_dataModel.TranslatedTrainingData,
					_dataModel.NumberOfEpochs,
					_dataModel.MiniBatchSize,
					_dataModel.LearningRate,
					_dataModel.TranslatedTestData,
					_cancelSource.Token)
				)
				.ContinueWith(t =>
				{
					_trainingStopwatch.Stop();
					_trainingActive = false;
					Dispatcher.Invoke(() =>
					{
						progress.Value = 0.0;
						StatusTextBarItem.Background = cacheBackground;
						StatusTextBlock.Foreground = cacheForeground;
						RemainingTimeTextBlock.Text = string.Empty;
						// Force CommandBinding's "CanExecute" to be re-evaluated (ensuring that the
						// "Cancel" button is disabled upon completion)
						CommandManager.InvalidateRequerySuggested();
					});
				}
				, TaskScheduler.FromCurrentSynchronizationContext());

			}
		}

		private void EstimateRemainingTime(double percentComplete)
		{
			if (_trainingStopwatch != null)
			{
				if (percentComplete > 10.0)
				{
					int intPercent = (int)percentComplete;
					if (intPercent != _cachedPercentComplete)
					{
						var timeElapsed = _trainingStopwatch.ElapsedMilliseconds;
						var timeRemaining = (timeElapsed / percentComplete) * (100.0 - percentComplete);
						var timeRemainingSeconds = (int)(timeRemaining / 1000.0);
						if (timeRemainingSeconds < 3)
						{
							RemainingTimeTextBlock.Text = GetStringResource("aFewSecondsRemaining");
						}
						else
						{
							RemainingTimeTextBlock.Text = string.Format(GetStringResource("timeRemainingFormat"), timeRemainingSeconds);
						}
						_cachedPercentComplete = intPercent;
					}
				}
			}
		}

		private void OnNetworkStatusChanged(Network.NetworkStatus newStatus)
		{
			if (newStatus == Network.NetworkStatus.Training || newStatus == Network.NetworkStatus.Evaluating)
			{
				_trainingStopwatch?.Restart();
				_cachedPercentComplete = 0;
			}
			RemainingTimeTextBlock.Text = string.Empty;

			_netStatus = newStatus;
		}

		private void ResultReporter(int epoch, List<int> results, TimeSpan trainTime, TimeSpan evalTime)
		{
			// If there are no results then there's nothing to report.
			if (results.Count == 0)
			{
				return;
			}

			int correctCount = 0;
			for (int i = 0; i < results.Count; i++)
			{
				if (results[i] == _dataModel.TranslatedTestData![i].label)
				{
					correctCount++;
				}
			}

			Dispatcher.Invoke(() =>
			{
				symbolPanelCtrl.SetEvaluatedLabels(results, correctCount);
				symbolPanelCtrl.RefreshPage();
			});
		}

		private void SerializeTrainingData()
		{
			if (_network != null)
			{
				SaveFileDialog dialog = new()
				{
					Filter = GetStringResource("trainingResultsFileFilter"),
					DefaultExt = GetStringResource("trainingResultsFileDefaultExt")
				};

				if (dialog.ShowDialog() == true)
				{
					_network.SerializeTrainingData(dialog.FileName);
				}
			}
		}

		private void DeserializeTrainingData()
		{
			if (_dataModel.TranslatedTestData != null)
			{
				OpenFileDialog openFileDialog = new()
				{
					Filter = GetStringResource("trainingResultsFileFilter"),
					DefaultExt = GetStringResource("trainingResultsFileDefaultExt")
				};
				if (openFileDialog.ShowDialog() == true)
				{
					List<int> sizes = new() { 1, 1, 1 };
					_network = new Network(sizes)
					{
						ReportResults = ResultReporter,
						ReportProgress = m => Dispatcher.Invoke(() =>
						{
							progress.Value = m;
							EstimateRemainingTime(m);
						}),
						ReportStatus = (s, m) => Dispatcher.Invoke(() =>
						{
							OnNetworkStatusChanged(s);
							StatusTextBlock.Text = m;
						})
					};

					bool success = false;
					try
					{
						success = _network!.DeserializeTrainingData(openFileDialog.FileName);
					}
					catch
					{
						success = false;
					}

					if (!success)
					{
						MessageBox.Show(GetStringResource("loadTrainingResultsFailed"));
						return;
					}

					_dataModel.NumberOfEpochs = _network.NumberOfEpochs;
					_dataModel.MiniBatchSize = _network.MiniBatchSize;
					_dataModel.LearningRate = _network.LearningRate;

					int correctCount = 0;
					List<int> evalResults = new();
					_trainingStopwatch = Stopwatch.StartNew();
					_cancelSource = new CancellationTokenSource();
					StatusTextBlock.Text = GetStringResource("statusIdentifying");
					var cacheBackground = StatusTextBarItem.Background;
					var cacheForeground = StatusTextBlock.Foreground;
					StatusTextBarItem.Background = Brushes.DarkRed;
					StatusTextBlock.Foreground = Brushes.White;
					RemainingTimeTextBlock.Text = string.Empty;
					_trainingActive = true;

					Task.Factory.StartNew(() =>
						(correctCount, evalResults) = _network.Evaluate(_dataModel.TranslatedTestData, _cancelSource.Token)
					)
					.ContinueWith(t =>
					{
						_trainingStopwatch.Stop();
						_trainingActive = false;
						Dispatcher.Invoke(() =>
						{
							progress.Value = 0.0;
							StatusTextBarItem.Background = cacheBackground;
							StatusTextBlock.Foreground = cacheForeground;
							symbolPanelCtrl.SetEvaluatedLabels(evalResults, correctCount);
							symbolPanelCtrl.RefreshPage();
							StatusTextBlock.Text = GetStringResource("statusReady");
							RemainingTimeTextBlock.Text = string.Empty;
							// Force CommandBinding's "CanExecute" to be re-evaluated (ensuring that the
							// "Cancel" button is disabled upon completion)
							CommandManager.InvalidateRequerySuggested();
						});
					}
					, TaskScheduler.FromCurrentSynchronizationContext());
				}
			}
		}

		// ExecutedRoutedEventHandler for the Load Data Files command.
		private void LoadDataFilesCmdExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			LoadDataFilesWindow loadDataFiles = new()
			{
				Owner = this
			};
			if (_userSettings != null)
			{
				loadDataFiles.TrainingDataSelected = _userSettings.LoadTrainingDataEnabled;
				loadDataFiles.TrainingDataFilePath = _userSettings.TrainingDataPath ?? "";
				loadDataFiles.TestDataSelected = _userSettings.LoadTestDataEnabled;
				loadDataFiles.TestDataFilePath = _userSettings.TestDataPath ?? "";
			}
			var res = loadDataFiles.ShowDialog();
			if (res != null && res == true)
			{
				if (_userSettings != null)
				{
					_userSettings.LoadTrainingDataEnabled = loadDataFiles.TrainingDataSelected;
					_userSettings.TrainingDataPath = loadDataFiles.TrainingDataFilePath;
					_userSettings.LoadTestDataEnabled = loadDataFiles.TestDataSelected;
					_userSettings.TestDataPath = loadDataFiles.TestDataFilePath;
				}
				if (loadDataFiles.TrainingDataSelected)
				{
					try
					{
						_dataModel.LoadTrainingData(loadDataFiles.TrainingDataFilePath);
					}
					catch
					{
						MessageBox.Show(GetStringResource("failedToLoadTrainingData"), GetStringResource("loadFailedCaption"));
						return;
					}
				}
				if (loadDataFiles.TestDataSelected)
				{
					try
					{
						_dataModel.LoadTestData(loadDataFiles.TestDataFilePath);
					}
					catch
					{
						MessageBox.Show(GetStringResource("failedToLoadTestData"), GetStringResource("loadFailedCaption"));
						return;
					}
					symbolPanelCtrl.SetDataModel(_dataModel);
					symbolPanelCtrl.Populate();
				}
			}
		}

		// CanExecuteRoutedEventHandler for the Load Data Files command.
		private void LoadDataFilesCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = !_trainingActive && !_loadingData;
		}

		// ExecutedRoutedEventHandler for the Load Training Data command.
		private void LoadTrainingDataCmdExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			DeserializeTrainingData();
		}

		// CanExecuteRoutedEventHandler for the Load Training Data command.
		private void LoadTrainingDataCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = !_trainingActive;
		}

		// ExecutedRoutedEventHandler for the Save Training Data command.
		private void SaveTrainingDataCmdExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			SerializeTrainingData();
		}

		// CanExecuteRoutedEventHandler for the Save Training Data command.
		private void SaveTrainingDataCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = !_trainingActive && _network != null && _network.Trained;
		}

		// ExecutedRoutedEventHandler for the Exit command.
		private void ExitCmdExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (_trainingActive)
			{
				_cancelSource?.Cancel();
				_trainingActive = false;
			}
			Close();
		}

		// CanExecuteRoutedEventHandler for the Exit command.
		private void ExitCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		// ExecutedRoutedEventHandler for the Train Network command.
		private void TrainNetworkCmdExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			TrainNetwork();
		}

		// CanExecuteRoutedEventHandler for the Train Network command.
		private void TrainNetworkCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			// We must have training data loaded and not currently be performing a training
			// operation for the "Train Network" functionality to be available.
			e.CanExecute = !_trainingActive && _dataModel != null &&
				_dataModel.TranslatedTrainingData != null &&
				_dataModel.TranslatedTrainingData.Count > 0;
		}

		// ExecutedRoutedEventHandler for the Cancel Training command.
		private void CancelTrainingCmdExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			_cancelSource?.Cancel();
			_trainingActive = false;
		}

		// CanExecuteRoutedEventHandler for the Cancel Training command.
		private void CancelTrainingCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _trainingActive;
		}

		// ExecutedRoutedEventHandler for the Identify User-drawn Symbols command.
		private void IdentifyUserDrawnSymbolsCmdExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			UserDrawnSymbol uds = new(_userSettings);
			uds.SetNetwork(_network);
			uds.ShowDialog();
		}

		// CanExecuteRoutedEventHandler for the Identify User-drawn Symbols command.
		private void IdentifyUserDrawnSymbolsCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = !_trainingActive && _network != null && _network.Trained;
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (_userSettings != null)
			{
				if (_userSettings.NetV1Settings != null && _dataModel != null)
				{
					_userSettings.NetV1Settings.NumberOfEpochs = _dataModel.NumberOfEpochs;
					_userSettings.NetV1Settings.MiniBatchSize = _dataModel.MiniBatchSize;
					_userSettings.NetV1Settings.LearningRate = _dataModel.LearningRate;
				}
				_userSettings.MainWindowStateSettings = UserSettings.GetWindowStateSettings(this);

				_settingsManager.SaveSettings(_userSettings);
			}
		}

		private bool IsValid(DependencyObject obj)
		{
			// The dependency object is valid if it has no errors and all
			// of its children (that are dependency objects) are error-free.
			return !Validation.GetHasError(obj) && LogicalTreeHelper.GetChildren(obj).OfType<DependencyObject>().All(IsValid);
		}
	}
}
