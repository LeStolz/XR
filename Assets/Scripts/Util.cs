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

	public static string ResultToCSV(ConditionResult conditionResult)
	{
		Condition condition = conditionResult.condition;
		string pos = condition.pos;
		string pov = condition.pov;
		string assisted = condition.assisted.ToString();
		string ringCount = condition.ringCount.ToString();
		string targetCount = condition.targetCount.ToString();

		var csvLines = new List<string>
		{
			"participantId,participantSeat,pos,pov,assisted,ringCount,targetCount," +
			"trialStartTimestamp,actualAnswerId,actualAnswerPosX,actualAnswerPosY,actualAnswerPosZ," +
			"participantAnswerTimestamp,participantAnswerId,participantAnswerPosX,participantAnswerPosY,participantAnswerPosZ,participantConfidence"
		};

		foreach (var trial in conditionResult.trialResults)
		{
			string trialStartTimestamp = trial.timestamp.ToString();
			string actualAnswerId = trial.actualAnswerId;
			string actualX = trial.actualAnswerPosition.x.ToString().Replace(",", ".");
			string actualY = trial.actualAnswerPosition.y.ToString().Replace(",", ".");
			string actualZ = trial.actualAnswerPosition.z.ToString().Replace(",", ".");

			foreach (var spectator in trial.spectatorAnswers)
			{
				string spectatorId = spectator.spectatorId;
				string spectatorSeat = spectator.spectatorSeat;
				string spectatorAnswerId = spectator.answerId;
				string answerX = spectator.answerPosition.x.ToString().Replace(",", ".");
				string answerY = spectator.answerPosition.y.ToString().Replace(",", ".");
				string answerZ = spectator.answerPosition.z.ToString().Replace(",", ".");
				string spectatorConfidence = spectator.confidence.ToString();
				string spectatorAnswerTimestamp = spectator.timestamp.ToString();

				csvLines.Add(
					$"{spectatorId},{spectatorSeat},{pos},{pov},{assisted},{ringCount},{targetCount}," +
					$"{trialStartTimestamp},{actualAnswerId},{actualX},{actualY},{actualZ}," +
					$"{spectatorAnswerTimestamp},{spectatorAnswerId},{answerX},{answerY},{answerZ},{spectatorConfidence}"
				);
			}
		}

		return string.Join("\n", csvLines);
	}
}