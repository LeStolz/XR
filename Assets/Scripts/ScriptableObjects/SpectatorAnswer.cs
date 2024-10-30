using System;
using UnityEngine;

[Serializable]
public struct SpectatorAnswer
{
	public string spectatorId;
	public string spectatorSeat;
	public string answerId;
	public Vector3 answerPosition;
	public int confidence;
	public ulong timestamp;

	public SpectatorAnswer(
		string spectatorId, string spectatorSeat, string answerId, Vector3 answerPosition, int confidence, ulong timestamp
	)
	{
		this.spectatorId = spectatorId;
		this.spectatorSeat = spectatorSeat;
		this.answerId = answerId;
		this.answerPosition = answerPosition;
		this.confidence = confidence;
		this.timestamp = timestamp;
	}
}