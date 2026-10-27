using System;
using SDRSharp.Common;
using SDRSharp.Radio;

namespace SDRSharp.AudioRecorder;

public class RecordingAudioProcessor : IRealProcessor, IStreamProcessor, IBaseProcessor
{
	private bool _enabled;

	private double _sampleRate;

	private readonly ISharpControl _control;

	// Пікове значення рівня демодульованого аудіо між зчитуваннями Decibels. Незалежне від
	// модуляції — працює і для SSB (USB/LSB)/CW, де штатний squelch SDR# недоступний.
	// Використовується VOX-гейтом у AudioRecorderPanel.
	private readonly PeakMeter _meter = new PeakMeter();

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

	// Рівень аудіо в dB (пік за вікно з моменту попереднього зчитування).
	// Зчитування скидає накопичувач, тому викликати рівно один раз на тік таймера.
	public double Decibels => _meter.ReadDecibelsAndReset();

	public event EventHandler<SamplesAvailableEventArgs> AudioReady;

	public RecordingAudioProcessor(ISharpControl control)
	{
		_control = control;
		_control.RegisterStreamHook((object)this, (ProcessorType)3);
	}

	public unsafe void Process(float* audio, int length)
	{
		_meter.Accumulate(audio, length);
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
