using System;
using UnityEngine;

[Serializable]
public struct ArCondition
{
	public string pos;
	public string pov;
	public bool assisted;
	public int answerRing;
	public int answerIndex;
	public int ringCount;
	public int targetCount;
	public float targetScale;

	public Vector3 lowestRingOrigin;
	public float radius;
	public float gap;

	public ArCondition(
		string pos, string pov, bool assisted,
		int answerRing, int answerIndex,
		int ringCount, int targetCount, float targetScale,
		Vector3 lowestRingOrigin, float radius, float gap
	)
	{
		this.pos = pos;
		this.pov = pov;
		this.assisted = assisted;
		this.answerRing = answerRing;
		this.answerIndex = answerIndex;
		this.ringCount = ringCount;
		this.targetCount = targetCount;
		this.targetScale = targetScale;
		this.lowestRingOrigin = lowestRingOrigin;
		this.radius = radius;
		this.gap = gap;
	}
}