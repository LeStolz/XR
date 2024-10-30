using System;

class Util
{
	public static ulong GetTimeSinceEpoch()
	{
		DateTime epochStart = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		return (ulong)(DateTime.UtcNow - epochStart).TotalMilliseconds;
	}
}