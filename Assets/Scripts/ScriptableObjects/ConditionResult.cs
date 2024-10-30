using System;
using System.Collections.Generic;

[Serializable]
public struct ConditionResult
{
	public Condition condition;
	public List<TrialResult> trialResults;
}