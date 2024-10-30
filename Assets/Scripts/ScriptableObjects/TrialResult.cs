using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct TrialResult
{
	public ulong timestamp;
	public string actualAnswerId;
	public Vector3 actualAnswerPosition;
	public List<SpectatorAnswer> spectatorAnswers;

	public TrialResult(ulong timestamp, string actualAnswerId, Vector3 actualAnswerPosition)
	{
		this.timestamp = timestamp;
		this.actualAnswerId = actualAnswerId;
		this.actualAnswerPosition = actualAnswerPosition;
		spectatorAnswers = new();
	}
}