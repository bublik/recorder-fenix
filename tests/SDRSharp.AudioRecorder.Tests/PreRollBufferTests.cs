using System;
using System.Linq;
using SDRSharp.AudioRecorder;
using Xunit;

namespace SDRSharp.AudioRecorder.Tests;

public class PreRollBufferTests
{
	private static unsafe void Write(PreRollBuffer buffer, params float[] samples)
	{
		fixed (float* p = samples)
		{
			buffer.Write(p, samples.Length);
		}
	}

	private static float[] Range(int from, int count)
	{
		return Enumerable.Range(from, count).Select(i => (float)i).ToArray();
	}

	// Вміст у порядку від найстарішого до найновішого — так його допише SimpleRecorder.
	private static float[] Contents(PreRollBuffer buffer)
	{
		buffer.GetSegments(out int start, out int len1, out int len2);
		return buffer.Data.Skip(start).Take(len1).Concat(buffer.Data.Take(len2)).ToArray();
	}

	[Fact]
	public void RejectsNonPositiveCapacity()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new PreRollBuffer(0));
	}

	[Fact]
	public void EmptyBuffer_HasNoSegments()
	{
		PreRollBuffer buffer = new PreRollBuffer(8);
		Assert.Equal(0, buffer.Count);
		Assert.Empty(Contents(buffer));
	}

	[Fact]
	public void PartialFill_KeepsOrder()
	{
		PreRollBuffer buffer = new PreRollBuffer(8);
		Write(buffer, Range(0, 4));
		Write(buffer, Range(4, 2));
		Assert.Equal(Range(0, 6), Contents(buffer));
	}

	[Fact]
	public void Wraparound_KeepsLatestInOrder()
	{
		PreRollBuffer buffer = new PreRollBuffer(8);
		Write(buffer, Range(0, 6));
		Write(buffer, Range(6, 6)); // всього 12 → лишаються 4..11
		Assert.Equal(8, buffer.Count);
		Assert.Equal(Range(4, 8), Contents(buffer));
	}

	[Fact]
	public void BlockLargerThanCapacity_KeepsTail()
	{
		PreRollBuffer buffer = new PreRollBuffer(8);
		Write(buffer, Range(0, 2));
		Write(buffer, Range(100, 20));
		Assert.Equal(Range(112, 8), Contents(buffer));
	}

	[Fact]
	public void ManySmallWrites_MatchesLastCapacitySamples()
	{
		PreRollBuffer buffer = new PreRollBuffer(10);
		int next = 0;
		Random rnd = new Random(1);
		for (int i = 0; i < 200; i++)
		{
			int n = rnd.Next(1, 7) * 2; // інтерлівд-стерео: парна довжина
			Write(buffer, Range(next, n));
			next += n;
			int expected = Math.Min(next, 10);
			Assert.Equal(Range(next - expected, expected), Contents(buffer));
		}
	}

	[Fact]
	public void StereoFramesStayAligned()
	{
		// Парна місткість + парні блоки → найстаріший семпл завжди лівий канал (парний індекс).
		PreRollBuffer buffer = new PreRollBuffer(6);
		Write(buffer, Range(0, 4));
		Write(buffer, Range(4, 4));
		Write(buffer, Range(8, 10));
		Assert.All(Contents(buffer).Where((_, i) => i % 2 == 0), v => Assert.Equal(0f, v % 2));
	}

	[Fact]
	public void Clear_Empties()
	{
		PreRollBuffer buffer = new PreRollBuffer(8);
		Write(buffer, Range(0, 6));
		buffer.Clear();
		Assert.Equal(0, buffer.Count);
		Write(buffer, Range(50, 2));
		Assert.Equal(Range(50, 2), Contents(buffer));
	}

	[Fact]
	public void ZeroLengthWrite_IsNoop()
	{
		PreRollBuffer buffer = new PreRollBuffer(8);
		Write(buffer);
		Assert.Equal(0, buffer.Count);
	}
}
