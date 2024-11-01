using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct RingCount_Gap
{
	public int ringCount;
	public float gap;

	public RingCount_Gap(
		int ringCount, float gap
	)
	{
		this.ringCount = ringCount;
		this.gap = gap;
	}
}