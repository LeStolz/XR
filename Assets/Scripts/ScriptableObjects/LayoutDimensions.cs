using System;
using System.Collections.Generic;
using Unity.Netcode;

[Serializable]
public struct LayoutDimensions
{
	public float initialHeight;
	public float radius;
	public List<ConditionalDimensions> conditionalDimensions;

	public LayoutDimensions(
		float initialHeight, float radius, List<ConditionalDimensions> conditionalDimensions
	)
	{
		this.initialHeight = initialHeight;
		this.radius = radius;
		this.conditionalDimensions = conditionalDimensions;
	}
}

[Serializable]
public struct ConditionalDimensions : INetworkSerializable
{
	public int ringCount;
	public int targetCount;
	public float gap;
	public float scale;

	public ConditionalDimensions(int ringCount, int targetCount, float gap, float scale)
	{
		this.ringCount = ringCount;
		this.targetCount = targetCount;
		this.gap = gap;
		this.scale = scale;
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		serializer.SerializeValue(ref ringCount);
		serializer.SerializeValue(ref targetCount);
		serializer.SerializeValue(ref gap);
		serializer.SerializeValue(ref scale);
	}
}