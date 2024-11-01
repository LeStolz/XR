using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ArSettings
{
	public float targetScale;
	public float initialHeight;
	public float radius;
	public List<RingCount_Gap> ringCount_gaps;

	public ArSettings(
		float targetScale, float initialHeight, float radius, List<RingCount_Gap> ringCount_gaps
	)
	{
		this.targetScale = targetScale;
		this.initialHeight = initialHeight;
		this.radius = radius;
		this.ringCount_gaps = ringCount_gaps;
	}
}