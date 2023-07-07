using MathNet.Numerics.LinearAlgebra;
using PendleCodeMonkey.NeuralNetLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace PendleCodeMonkey.NeuralNetApp
{
	public class DataModel : INotifyPropertyChanged
	{

		private int _numberOfEpochs;
		private int _miniBatchSize;
		private float _learningRate;

		internal List<(Matrix<float> image, Matrix<float> label)>? TranslatedTrainingData { get; private set; }

		internal List<MNIST_Data.Data>? TestData { get; private set; }

		internal List<(Matrix<float> image, int label)>? TranslatedTestData { get; private set; }

		internal string? TestDataFileName { get; private set; }

		public int NumberOfEpochs
		{
			get
			{
				return _numberOfEpochs;
			}

			set
			{
				if (value != _numberOfEpochs)
				{
					_numberOfEpochs = value;
					NotifyPropertyChanged();
				}
			}
		}

		public int MiniBatchSize
		{
			get
			{
				return _miniBatchSize;
			}

			set
			{
				if (value != _miniBatchSize)
				{
					_miniBatchSize = value;
					NotifyPropertyChanged();
				}
			}
		}

		public float LearningRate
		{
			get
			{
				return _learningRate;
			}

			set
			{
				if (value != _learningRate)
				{
					_learningRate = value;
					NotifyPropertyChanged();
				}
			}
		}

		public DataModel()
		{
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		// This method is called by the Set accessor of each property.  
		// The CallerMemberName attribute that is applied to the optional propertyName  
		// parameter causes the property name of the caller to be substituted as an argument.  
		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		internal void LoadTrainingData(string filePath)
		{
			TranslatedTrainingData?.Clear();

			var trainData = MNIST_Data.ReadData(filePath);
			TranslatedTrainingData = MNIST_Data.TranslateTrainingData(trainData);
		}

		internal void LoadTestData(string filePath)
		{
			TestData?.Clear();
			TranslatedTestData?.Clear();

			TestData = MNIST_Data.ReadData(filePath);
			TranslatedTestData = MNIST_Data.TranslateTestData(TestData);

			TestDataFileName = Path.GetFileNameWithoutExtension(filePath);
		}
	}
}
