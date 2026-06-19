using System;
using SDRSharp.Common;
using SDRSharp.Radio;

namespace SDRSharp.AudioRecorder;

public class RecordingAudioProcessor : IRealProcessor, IStreamProcessor, IBaseProcessor
{
	private bool _enabled;

	private double _sampleRate;

	private readonly ISharpControl _control;

	public bool Enabled
	{
		get
		{
			return _enabled;
		}
		set
		{
			_enabled = value;
		}
	}

	public double SampleRate
	{
		get
		{
			return _sampleRate;
		}
		set
		{
			_sampleRate = value;
		}
	}

	public event EventHandler<SamplesAvailableEventArgs> AudioReady;

	public RecordingAudioProcessor(ISharpControl control)
	{
		_control = control;
		_control.RegisterStreamHook((object)this, (ProcessorType)3);
	}

	public unsafe void Process(float* audio, int length)
	{
		OnSamplesReady(new SamplesAvailableEventArgs
		{
			Buffer = audio,
			Length = length
		});
	}

	protected virtual void OnSamplesReady(SamplesAvailableEventArgs args)
	{
		this.AudioReady?.Invoke(this, args);
	}
}
