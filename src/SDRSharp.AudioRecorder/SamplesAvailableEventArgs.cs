using System;

namespace SDRSharp.AudioRecorder;

public sealed class SamplesAvailableEventArgs : EventArgs
{
	public int Length { get; set; }

	public unsafe float* Buffer { get; set; }
}
