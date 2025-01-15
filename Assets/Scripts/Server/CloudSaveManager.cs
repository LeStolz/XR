using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

public class CloudSaveManager : MonoBehaviour
{
	public static CloudSaveManager Singleton { get; private set; }

	async void Awake()
	{
		if (Singleton != null)
		{
			Destroy(gameObject);
			return;
		}

		Singleton = this;
		DontDestroyOnLoad(gameObject);

		await UnityServices.InitializeAsync();
		await AuthenticationService.Instance.SignInAnonymouslyAsync();
	}

	public async Task<IEnumerable<string>> GetAllKeys()
	{
		var keys = await CloudSaveService.Instance.Data.Player.ListAllKeysAsync();

		return keys.Select(key => key.Key);
	}

	public async Task Save(string key, object value)
	{
		var data = new Dictionary<string, object> {
			{ key, value }
		};

		await CloudSaveService.Instance.Data.Player.SaveAsync(data);
	}

	public async Task<T> Load<T>(string key)
	{
		var data = await CloudSaveService.Instance.Data.Player.LoadAsync(
			new HashSet<string> { key }
		);

		if (data.TryGetValue(key, out var value))
		{
			return value.Value.GetAs<T>();
		}

		return default;
	}
}