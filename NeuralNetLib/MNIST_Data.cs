using MathNet.Numerics.LinearAlgebra;

namespace PendleCodeMonkey.NeuralNetLib
{
	public class MNIST_Data
	{
		public class Data
		{
			public IList<int>? image;
			public int label;

			public Data(IList<int>? img, int lbl)
			{
				image = img;
				label = lbl;
			}
		}

		public static List<Data> ReadData(string path)
		{
			List<Data> dataList = new();

			using (BinaryReader reader = new(File.Open(path, FileMode.Open)))
			{
				long len = reader.BaseStream.Length;
				while (reader.BaseStream.Position != reader.BaseStream.Length)
				{
					byte[] binData = new byte[784];
					var temp = reader.Read(binData, 0, 784);
					var label = reader.ReadByte();
					var test = binData.Select(x => (int)x).ToList();
					Data data = new(test, label);
					dataList.Add(data);
				}
			}

			return dataList;
		}

		public static List<(Matrix<float> image, Matrix<float> label)> TranslateTrainingData(List<Data> dataList)
		{
			static Matrix<float> VectorizeResult(int j)
			{
				var matrix = Matrix<float>.Build.Dense(10, 1);
				matrix[j, 0] = 1.0f;
				return matrix;
			}

			List<(Matrix<float> image, Matrix<float> label)> translatedData = new();
			foreach (Data data in dataList)
			{
				var imageData = data.image!.Select(x => x / 256.0f).ToArray();
				var img = Matrix<float>.Build.Dense(imageData.Length, 1, (float[])imageData.Clone());
				var lbl = VectorizeResult(data.label);
				translatedData.Add((img, lbl));
			}

			return translatedData;
		}

		public static List<(Matrix<float> image, int label)> TranslateTestData(List<Data> dataList)
		{
			List<(Matrix<float> image, int label)> translatedData = new();
			foreach (Data data in dataList)
			{
				var imageData = data.image!.Select(x => x / 256.0f).ToArray();
				var img = Matrix<float>.Build.Dense(imageData.Length, 1, (float[])imageData.Clone());
				translatedData.Add((img, data.label));
			}

			return translatedData;
		}
	}
}
