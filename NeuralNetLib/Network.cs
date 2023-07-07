using MathNet.Numerics.LinearAlgebra;
using PendleCodeMonkey.NeuralNetLib.Properties;
using System.Diagnostics;

namespace PendleCodeMonkey.NeuralNetLib
{
	public class Network
	{
		private const string _header = "PCM_NN";
		private const short _version = 1;
		private static readonly Random _rand = new();

		private readonly List<int> _layerSizes;
		private readonly List<Matrix<float>> _biases;
		private readonly List<Matrix<float>> _weights;

		private int _numEpochs = 0;
		private int _miniBatchSize = 0;
		private float _learningRate = 0.0f;

		public Action<double>? ReportProgress { get; set; }
		public Action<int, List<int>, TimeSpan, TimeSpan>? ReportResults { get; set; }
		public Action<NetworkStatus, string>? ReportStatus { get; set; }

		public int NumberOfEpochs { get => _numEpochs; }
		public int MiniBatchSize { get => _miniBatchSize; }
		public float LearningRate { get => _learningRate; }

		public bool Trained { get; private set; }

		public enum NetworkStatus
		{
			Ready = 0,
			Training = 1,
			Evaluating = 2,
			Completed = 3,
			Cancelled = 4
		}

		public NetworkStatus Status { get; private set; }

		public Network(List<int> layerSizes)
		{
			_layerSizes = layerSizes;

			_biases = new List<Matrix<float>>();
			for (int i = 1; i < _layerSizes.Count; i++)
			{
				_biases.Add(BuildRandomMatrix(_layerSizes[i], 1));
			}

			_weights = new List<Matrix<float>>();
			for (int i = 1; i < _layerSizes.Count; i++)
			{
				_weights.Add(BuildRandomMatrix(_layerSizes[i], _layerSizes[i - 1]));
			}

			ReportProgress = null;
			ReportResults = null;
			Status = NetworkStatus.Ready;
			Trained = false;
		}


		public void SerializeTrainingData(string fileName)
		{
			// Only serialize the training data if the network has been successfully trained.
			if (!Trained)
			{
				return;
			}

			using BinaryWriter bw = new(File.Open(fileName, FileMode.Create));
			// Start with a special header that indicates that this is a Neural Net data file.
			bw.Write(_header);
			// Write current version number
			bw.Write(_version);
			// Write the parameters that were used to train the neural net.
			bw.Write(_numEpochs);
			bw.Write(_miniBatchSize);
			bw.Write(_learningRate);
			// Write the number of layers in the network
			bw.Write(_layerSizes.Count);
			// Followed by the actual layer sizes
			foreach (var size in _layerSizes)
			{
				bw.Write(size);
			}
			// Write the number of entries in the biases collection
			bw.Write(_biases.Count);
			// Followed by the bias matrix elements themselves.
			foreach (var bias in _biases)
			{
				// Write the number of rows and columns in this bias matrix.
				bw.Write(bias.RowCount);
				bw.Write(bias.ColumnCount);
				var bData = bias.ToArray();
				for (int r = 0; r < bias.RowCount; r++)
				{
					for (int c = 0; c < bias.ColumnCount; c++)
					{
						bw.Write(bData[r, c]);
					}
				}
			}
			// Write the number of entries in the weights collection
			bw.Write(_weights.Count);
			// Followed by the weight matrix elements themselves.
			foreach (var weight in _weights)
			{
				// Write the number of rows and columns in this weight matrix.
				bw.Write(weight.RowCount);
				bw.Write(weight.ColumnCount);
				var wData = weight.ToArray();
				for (int r = 0; r < weight.RowCount; r++)
				{
					for (int c = 0; c < weight.ColumnCount; c++)
					{
						bw.Write(wData[r, c]);
					}
				}
			}
		}

		public bool DeserializeTrainingData(string filePath)
		{
			bool success = false;
			if (File.Exists(filePath))
			{
				using BinaryReader br = new(File.Open(filePath, FileMode.Open));
				var header = br.ReadString();
				if (header == _header)
				{
					var version = br.ReadInt16();
					var numEpochs = br.ReadInt32();
					var miniBatchSize = br.ReadInt32();
					var learningRate = br.ReadSingle();
					var numLayers = br.ReadInt32();
					_layerSizes.Clear();
					for (int layer = 0; layer < numLayers; layer++)
					{
						_layerSizes.Add(br.ReadInt32());
					}
					var biasCount = br.ReadInt32();
					_biases.Clear();
					for (int bias = 0; bias < biasCount; bias++)
					{
						var rowCount = br.ReadInt32();
						var colCount = br.ReadInt32();
						float[,] biasMatrixValues = new float[rowCount, colCount];
						for (int r = 0; r < rowCount; r++)
						{
							for (int c = 0; c < colCount; c++)
							{
								biasMatrixValues[r, c] = br.ReadSingle();
							}
						}
						_biases.Add(Matrix<float>.Build.DenseOfArray(biasMatrixValues));
					}
					var weightsCount = br.ReadInt32();
					_weights.Clear();
					for (int weight = 0; weight < weightsCount; weight++)
					{
						var rowCount = br.ReadInt32();
						var colCount = br.ReadInt32();
						float[,] weightMatrixValues = new float[rowCount, colCount];
						for (int r = 0; r < rowCount; r++)
						{
							for (int c = 0; c < colCount; c++)
							{
								weightMatrixValues[r, c] = br.ReadSingle();
							}
						}
						_weights.Add(Matrix<float>.Build.DenseOfArray(weightMatrixValues));
					}

					// If all of the above succeeds then update the parameters.
					_numEpochs = numEpochs;
					_miniBatchSize = miniBatchSize;
					_learningRate = learningRate;

					// Successfully loaded the training data so the network is in a "trained" state.
					Trained = true;

					success = true;
				}
			}
			return success;
		}

		// Train the neural network using mini-batch stochastic gradient descent.
		public void SGD(List<(Matrix<float>, Matrix<float>)> trainingData,
						int epochs,
						int miniBatchSize,
						float eta,
						List<(Matrix<float>, int)>? testData,
						CancellationToken cancelToken)
		{
			// Keep a record of the parameters that have been used when training the neural net.
			_numEpochs = epochs;
			_miniBatchSize = miniBatchSize;
			_learningRate = eta;
			Trained = false;

			for (int epoch = 0; epoch < epochs; epoch++)
			{
				Status = NetworkStatus.Training;
				ReportStatus?.Invoke(Status, string.Format(Resources.TrainingEpochNumber, (epoch + 1).AsOrdinal()));
				var trainEpochTime = Stopwatch.StartNew();
				var miniBatches = trainingData.SplitIntoRandomMiniBatches(miniBatchSize);
				int count = 0;
				foreach (var miniBatch in miniBatches)
				{
					UpdateMiniBatch(miniBatch, eta);
					count++;
					ReportProgress?.Invoke(100.0 * count / miniBatches.Count);
					if (cancelToken.IsCancellationRequested)
					{
						Status = NetworkStatus.Cancelled;
						ReportStatus?.Invoke(Status, Resources.StatusTrainingCancelled);
						return;
					}
				}
				trainEpochTime.Stop();

				Status = NetworkStatus.Training;
				ReportStatus?.Invoke(Status, Resources.StatusEvaluating);

				var evaluationTime = Stopwatch.StartNew();
				var (correctCount, evalResults) = testData != null ? Evaluate(testData, cancelToken) : (0, new List<int>());
				evaluationTime.Stop();

				ReportResults?.Invoke(epoch, evalResults, trainEpochTime.Elapsed, evaluationTime.Elapsed);
			}
			Status = NetworkStatus.Completed;
			Trained = true;
			ReportStatus?.Invoke(Status, Resources.StatusTrainingCompleted);
		}

		private void UpdateMiniBatch(List<(Matrix<float> img, Matrix<float> label)> miniBatch, float eta)
		{
			var nabla_b = CreateZeroMatrices(_biases).ToList();
			var nabla_w = CreateZeroMatrices(_weights).ToList();

			foreach (var (img, label) in miniBatch)
			{
				var (delta_nabla_b, delta_nabla_w) = BackProp(img, label);

				for (int i = 0; i < nabla_b.Count; i++)
				{
					nabla_b[i] += delta_nabla_b[i];
				}

				for (int i = 0; i < nabla_w.Count; i++)
				{
					nabla_w[i] += delta_nabla_w[i];
				}
			}

			var multiplier = eta / miniBatch.Count;

			for (int i = 0; i < _biases.Count; i++)
			{
				_biases[i] -= nabla_b[i] * multiplier;
			}

			for (int i = 0; i < _weights.Count; i++)
			{
				_weights[i] -= nabla_w[i] * multiplier;
			}
		}

		// Return a tuple (nabla_b, nabla_w) representing the gradient for the cost function C_x.
		// nabla_b and nabla_w are layer-by-layer lists of matrices, similar to _biases and _weights.
		private (List<Matrix<float>> nabla_b, List<Matrix<float>> nabla_w) BackProp(Matrix<float> img, Matrix<float> label)
		{
			var nabla_b = CreateZeroMatrices(_biases).ToList();
			var nabla_w = CreateZeroMatrices(_weights).ToList();

			// feedforward
			var activation = img;
			var activations = new List<Matrix<float>>() { activation };
			var zs = new List<Matrix<float>>();

			for (int i = 0; i < _layerSizes.Count - 1; i++)
			{
				var z = _weights[i] * activation + _biases[i];
				zs.Add(z);

				activation = Sigmoid(z);
				activations.Add(activation);
			}

			// backward pass
			var delta = CostDerivative(activations[^1], label).PointwiseMultiply(SigmoidPrime(zs[^1]));
			nabla_b[^1] = delta;
			nabla_w[^1] = delta * activations[^2].Transpose();

			for (int i = 2; i < _layerSizes.Count; i++)
			{
				var z = zs[^i];
				var sp = SigmoidPrime(z);
				delta = (_weights[_weights.Count - i + 1].Transpose() * delta).PointwiseMultiply(sp);
				nabla_b[^i] = delta;
				nabla_w[^i] = delta * activations[activations.Count - i - 1].Transpose();
			}

			return (nabla_b, nabla_w);
		}

		public (int correctCount, List<int> evalResults) Evaluate(List<(Matrix<float> image, int label)> testData, CancellationToken cancelToken)
		{
			Matrix<float> FeedForward(Matrix<float> input, int handledCount)
			{
				ReportProgress?.Invoke(100.0 * handledCount / testData.Count);

				var a = input;
				for (int i = 0; i < _layerSizes.Count - 1; i++)
				{
					var z = _weights[i] * a + _biases[i];
					a = Sigmoid(z);
				}
				return a;
			}

			int handledCount = 0;
			var evalResult2 = testData.Select(x => FeedForward(x.image, handledCount++))
					.TakeWhile(x => !cancelToken.IsCancellationRequested).ToList();

			if (handledCount != testData.Count)
			{
				// Operation was cancelled before evaluation completed.
				Status = NetworkStatus.Cancelled;
				ReportStatus?.Invoke(Status, Resources.StatusEvaluationCancelled);
				return (0, new List<int>());
			}

			var evalResult = evalResult2
					.Select(x => x.EnumerateColumns()
					.First()
					.Select((value, index) => new { Value = value, Index = index })
					.Aggregate((a, b) => (a.Value > b.Value) ? a : b)
					.Index).ToList();

			int count = 0;
			for (int i = 0; i < testData.Count; i++)
			{
				if (evalResult[i] == testData[i].label)
				{
					count++;
				}
			}
			return (count, evalResult);
		}

		private static Matrix<float> Sigmoid(Matrix<float> z)
		{
			var sigZ = Matrix<float>.Build.Dense(z.RowCount, z.ColumnCount);
			for (int r = 0; r < sigZ.RowCount; r++)
			{
				for (int c = 0; c < sigZ.ColumnCount; c++)
				{
					sigZ[r, c] = (float)Math.Exp(-z[r, c]);
				}
			}
			sigZ = 1 / (1 + sigZ);

			return sigZ;
		}

		private static Matrix<float> SigmoidPrime(Matrix<float> z)
		{
			var sigZ = Sigmoid(z);
			return sigZ.PointwiseMultiply(1 - sigZ);
		}

		private static Matrix<float> CostDerivative(Matrix<float> outputActivations, Matrix<float> y) => outputActivations - y;


		// Creates a sequence of zero matrices that have the same shape (i.e. number of rows and columns) as
		// the matrices in the supplied sequence (i.e. effectively clone the source matrices, but with zero values).
		private static IEnumerable<Matrix<float>> CreateZeroMatrices(IEnumerable<Matrix<float>> srcMatrix)
		{
			foreach (var src in srcMatrix)
			{
				yield return Matrix<float>.Build.Dense(src.RowCount, src.ColumnCount);
			}
		}

		// Build a matrix of the specified shape (i.e. rows and columns) populated with random values.
		private static Matrix<float> BuildRandomMatrix(int numRows, int numCols)
		{
			// Local function that generates a Normal (Gaussian) distribution of random numbers.
			// Implemented using a Box-Muller Transform.
			static double NormalDistributionRandom(double mean, double deviation)
			{
				double u1 = 1.0 - _rand.NextDouble();
				double u2 = 1.0 - _rand.NextDouble();
				double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
				return mean + deviation * randStdNormal;
			}

			var matrix = Matrix<float>.Build.Dense(numRows, numCols);
			for (int r = 0; r < numRows; r++)
			{
				for (int c = 0; c < numCols; c++)
				{
					matrix[r, c] = (float)NormalDistributionRandom(0, 1);
				}
			}

			return matrix;
		}
	}
}
