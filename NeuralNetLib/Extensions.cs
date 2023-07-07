namespace PendleCodeMonkey.NeuralNetLib
{
	public static class Extensions
	{

		public static List<List<T>> SplitIntoRandomMiniBatches<T>(this List<T> dataList, int miniBatchSize)
		{
			Random rnd = new();
			return dataList.OrderBy(x => rnd.Next()).Chunk(miniBatchSize).Select(x => x.ToList()).ToList();
		}

		public static string AsOrdinal(this int num)
		{
			return string.Concat(num, (num % 100) switch
			{
				11 or 12 or 13 => "th",
				int n => (n % 10) switch
				{
					1 => "st",
					2 => "nd",
					3 => "rd",
					_ => "th",
				}
			});
		}
	}
}
