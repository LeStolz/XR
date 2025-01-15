using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class FileSaveManager : MonoBehaviour
{
	public static FileSaveManager Singleton { get; private set; }

	void Awake()
	{
		if (Singleton != null)
		{
			Destroy(gameObject);
			return;
		}

		Singleton = this;
		DontDestroyOnLoad(gameObject);
	}

	public async Task Save(string key, object value)
	{
		var path = Path.Combine(Application.persistentDataPath, key);
		var data = JsonConvert.SerializeObject(value);
		await File.WriteAllTextAsync(path, data);
	}

	public async Task<T> Load<T>(string key)
	{
		var path = Path.Combine(Application.persistentDataPath, key);

		if (!File.Exists(path))
		{
			var data = Resources.Load<TextAsset>(key);

			if (data == null)
			{
				return default;
			}

			return JsonConvert.DeserializeObject<T>(data.text);
		}
		else
		{
			var data = await File.ReadAllTextAsync(path);

			return JsonConvert.DeserializeObject<T>(data);
		}
	}
}