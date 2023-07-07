using System;
using System.IO;
using System.Text.Json;

namespace PendleCodeMonkey.NeuralNetApp
{
	public class SettingsManager<T> where T : class
	{
		private readonly string _settingsFilePath;

		public SettingsManager(string appName, string fileName)
		{
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			_settingsFilePath = Path.Combine(appData, appName, fileName);
		}

		public T? LoadSettings() =>
			File.Exists(_settingsFilePath) ?
			JsonSerializer.Deserialize<T>(File.ReadAllText(_settingsFilePath)) :
			null;

		public void SaveSettings(T settings)
		{
			string json = JsonSerializer.Serialize(settings);
			var settingsPath = Path.GetDirectoryName(_settingsFilePath);
			if (settingsPath != null && !Directory.Exists(settingsPath))
			{
				Directory.CreateDirectory(settingsPath);
			}
			File.WriteAllText(_settingsFilePath, json);
		}
	}
}
