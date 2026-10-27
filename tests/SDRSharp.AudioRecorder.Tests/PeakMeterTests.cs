using System;
using System.Threading;
using System.Threading.Tasks;
using SDRSharp.AudioRecorder;
using Xunit;

namespace SDRSharp.AudioRecorder.Tests;

public class PeakMeterTests
{
	private static unsafe void Feed(PeakMeter meter, params float[] samples)
	{
		fixed (float* p = samples)
		{
			meter.Accumulate(p, samples.Length);
		}
	}

	[Fact]
	public void EmptyMeter_ReturnsFloor()
	{
		Assert.Equal(PeakMeter.FloorDb, new PeakMeter().ReadDecibelsAndReset());
	}

	[Fact]
	public void FullScale_IsZeroDb()
	{
		PeakMeter meter = new PeakMeter();
		Feed(meter, 0.1f, -1f, 0.5f);
		Assert.Equal(0.0, meter.ReadDecibelsAndReset(), 6);
	}

	[Fact]
	public void UsesAbsolutePeakAcrossBuffers()
	{
		PeakMeter meter = new PeakMeter();
		Feed(meter, 0.01f, -0.1f);
		Feed(meter, 0.05f);
		Assert.Equal(-20.0, meter.ReadDecibelsAndReset(), 4);
	}

	[Fact]
	public void ReadResetsAccumulator()
	{
		PeakMeter meter = new PeakMeter();
		Feed(meter, 0.5f);
		meter.ReadDecibelsAndReset();
		Assert.Equal(PeakMeter.FloorDb, meter.ReadDecibelsAndReset());
		Feed(meter, 0.001f);
		Assert.Equal(-60.0, meter.ReadDecibelsAndReset(), 4);
	}

	[Fact]
	public void SilenceAndNaN_ReturnFloor()
	{
		PeakMeter meter = new PeakMeter();
		Feed(meter, 0f, float.NaN, 1E-09f);
		Assert.Equal(PeakMeter.FloorDb, meter.ReadDecibelsAndReset());
	}

	[Fact]
	public async Task ConcurrentAccumulateAndReset_NeverReportsStalePeak()
	{
		// Аудіопотік пише гучні піки лише до «моменту» скидання, потім — тихий сигнал.
		// Після скидання стара гучність не повинна «воскресати» (раніше скидання губилося).
		PeakMeter meter = new PeakMeter();
		int phase = 0;
		using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		Task writer = Task.Run(() =>
		{
			float[] loud = { 1f, -1f, 1f, -1f };
			float[] quiet = { 0.001f, -0.001f };
			while (!cts.IsCancellationRequested)
			{
				Feed(meter, Volatile.Read(ref phase) == 0 ? loud : quiet);
			}
		});
		await Task.Delay(50);
		Volatile.Write(ref phase, 1);
		meter.ReadDecibelsAndReset(); // забирає «гучне» вікно
		await Task.Delay(50);
		// Можливий лише один «запізнілий» гучний буфер, що вже був у роботі під час перемикання фази.
		meter.ReadDecibelsAndReset();
		await Task.Delay(50);
		double db = meter.ReadDecibelsAndReset();
		cts.Cancel();
		await writer;
		Assert.InRange(db, -61.0, -59.0);
	}
}
