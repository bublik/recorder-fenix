using System;
using System.Threading;

namespace SDRSharp.AudioRecorder;

// Пікове значення рівня аудіо між зчитуваннями. Accumulate() викликається з аудіопотоку SDR#,
// ReadDecibelsAndReset() — з UI-таймера. Пік зберігається як біти float в int, щоб злиття
// й скидання були атомарними: скидання не губиться, якщо збігається з обробкою буфера.
public sealed class PeakMeter
{
	public const double FloorDb = -100.0;

	private int _peakBits;

	public unsafe void Accumulate(float* audio, int length)
	{
		float peak = 0f;
		for (int i = 0; i < length; i++)
		{
			float a = Math.Abs(audio[i]);
			if (a > peak)
			{
				peak = a;
			}
		}
		Merge(peak);
	}

	public double ReadDecibelsAndReset()
	{
		float peak = BitConverter.Int32BitsToSingle(Interlocked.Exchange(ref _peakBits, 0));
		if (!(peak > 1E-07f))
		{
			return FloorDb;
		}
		return 20.0 * Math.Log10(peak);
	}

	private void Merge(float peak)
	{
		int newBits = BitConverter.SingleToInt32Bits(peak);
		while (true)
		{
			int bits = Volatile.Read(ref _peakBits);
			if (!(peak > BitConverter.Int32BitsToSingle(bits)))
			{
				return;
			}
			if (Interlocked.CompareExchange(ref _peakBits, newBits, bits) == bits)
			{
				return;
			}
		}
	}
}
