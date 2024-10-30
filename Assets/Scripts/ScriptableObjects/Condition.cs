using System;

[Serializable]
public struct Condition
{
	public string pos;
	public string pov;
	public bool assisted;
	public int ringCount;
	public int targetCount;

	public Condition(string pos, string pov, bool assisted, int ringCount, int targetCount)
	{
		this.pos = pos;
		this.pov = pov;
		this.assisted = assisted;
		this.ringCount = ringCount;
		this.targetCount = targetCount;
	}
}