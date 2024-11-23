using System;
using System.Collections.Generic;

class Util
{
	public const float EPS = 0.01f;

	public static ulong GetTimeSinceEpoch()
	{
		DateTime epochStart = new(2024, 11, 1, 0, 0, 0, DateTimeKind.Utc);

		return (ulong)(DateTime.UtcNow - epochStart).TotalMilliseconds;
	}

	public static List<T> Shuffle<T>(IList<T> originalList)
	{
		var list = new List<T>(originalList);
		var count = list.Count;
		var last = count - 1;

		for (var i = 0; i < last; ++i)
		{
			var r = UnityEngine.Random.Range(i, count);
			(list[r], list[i]) = (list[i], list[r]);
		}

		return list;
	}
}