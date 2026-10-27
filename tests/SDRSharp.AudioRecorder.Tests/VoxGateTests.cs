using SDRSharp.AudioRecorder;
using Xunit;

namespace SDRSharp.AudioRecorder.Tests;

public class VoxGateTests
{
	private static VoxGate Gate(double open, double close)
	{
		return new VoxGate { OpenDb = open, CloseDb = close };
	}

	[Fact]
	public void StartsClosed()
	{
		VoxGate gate = Gate(-30, -40);
		Assert.False(gate.IsOpen);
		Assert.Equal(VoxGate.FloorDb, gate.LastLevelDb);
	}

	[Theory]
	[InlineData(-29.9, true)]
	[InlineData(-30.0, true)]
	[InlineData(-30.1, false)]
	public void OpensAtOrAboveOpenThreshold(double level, bool expected)
	{
		Assert.Equal(expected, Gate(-30, -40).Update(level));
	}

	[Fact]
	public void StaysOpenBetweenThresholds_Hysteresis()
	{
		VoxGate gate = Gate(-30, -40);
		gate.Update(-20);
		Assert.True(gate.Update(-35));
		Assert.True(gate.Update(-40)); // close — строго «<»
		Assert.False(gate.Update(-40.1));
	}

	[Fact]
	public void StaysClosedBetweenThresholds_Hysteresis()
	{
		VoxGate gate = Gate(-30, -40);
		Assert.False(gate.Update(-35));
		Assert.False(gate.Update(-31));
	}

	[Fact]
	public void CloseAboveOpen_DoesNotFlapEveryTick()
	{
		// Некоректний конфіг: close (-30) вищий за open (-40). Рівень між ними раніше
		// відкривав і закривав гейт по черзі на кожному тіку.
		VoxGate gate = Gate(-40, -30);
		for (int i = 0; i < 10; i++)
		{
			Assert.True(gate.Update(-35));
		}
		Assert.Equal(-40, gate.EffectiveCloseDb);
		Assert.False(gate.Update(-41));
	}

	[Fact]
	public void EqualThresholds_NoFlapping()
	{
		VoxGate gate = Gate(-35, -35);
		Assert.True(gate.Update(-35));
		Assert.True(gate.Update(-35));
		Assert.False(gate.Update(-36));
	}

	[Theory]
	[InlineData(-32, -38, true)]
	[InlineData(-35, -35, true)]
	[InlineData(-40, -30, false)]
	public void IsValidThresholds(double open, double close, bool expected)
	{
		Assert.Equal(expected, VoxGate.IsValidThresholds(open, close));
	}

	[Fact]
	public void ResetClosesGate_KeepsThresholds()
	{
		VoxGate gate = Gate(-30, -40);
		gate.Update(-10);
		gate.Reset();
		Assert.False(gate.IsOpen);
		Assert.Equal(-30, gate.OpenDb);
		Assert.Equal(-40, gate.CloseDb);
	}

	[Fact]
	public void RecordsLastLevel()
	{
		VoxGate gate = Gate(-30, -40);
		gate.Update(-47.5);
		Assert.Equal(-47.5, gate.LastLevelDb);
	}
}
