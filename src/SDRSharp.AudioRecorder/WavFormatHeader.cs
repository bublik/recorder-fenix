namespace SDRSharp.AudioRecorder;

public struct WavFormatHeader
{
	public ushort FormatTag;

	public ushort Channels;

	public uint SamplesPerSec;

	public uint AvgBytesPerSec;

	public ushort BlockAlign;

	public ushort BitsPerSample;

	public WavFormatHeader(WavSampleFormat format, uint sampleRate)
	{
		BitsPerSample = 0;
		FormatTag = 0;
		Channels = 0;
		switch (format)
		{
		case WavSampleFormat.PCM8Stereo:
			FormatTag = 1;
			BitsPerSample = 8;
			Channels = 2;
			break;
		case WavSampleFormat.PCM16Stereo:
			FormatTag = 1;
			BitsPerSample = 16;
			Channels = 2;
			break;
		case WavSampleFormat.Float32:
			FormatTag = 3;
			BitsPerSample = 32;
			Channels = 2;
			break;
		case WavSampleFormat.PCM8Mono:
			FormatTag = 1;
			BitsPerSample = 8;
			Channels = 1;
			break;
		case WavSampleFormat.PCM16Mono:
			FormatTag = 1;
			BitsPerSample = 16;
			Channels = 1;
			break;
		}
		BlockAlign = (ushort)(Channels * ((uint)BitsPerSample / 8u));
		SamplesPerSec = sampleRate;
		AvgBytesPerSec = sampleRate * BlockAlign;
	}
}
