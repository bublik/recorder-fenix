using System;
using SDRSharp.Common;
using SDRSharp.Radio;

namespace SDRSharp.AudioRecorder;

public class RecordingAudioProcessor : IRealProcessor, IStreamProcessor, IBaseProcessor
{
	private bool _enabled;

	private double _sampleRate;

	private readonly ISharpControl _control;

	// Пікове значення рівня демодульованого аудіо, накопичується між зчитуваннями
	// Decibels. Незалежне від модуляції — працює і для SSB (USB/LSB)/CW, де штатний
	// squelch SDR# недоступний. Використовується VOX-гейтом у AudioRecorderPanel.
	private float _peakHold;

	private double _lastDb = -100.0;

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
	public double Decibels
	{
		get
		{
			_lastDb = ((_peakHold > 1E-07f) ? (20.0 * Math.Log10(_peakHold)) : -100.0);
			_peakHold = 0f;
			return _lastDb;
		}
	}

	// Останнє обчислене значення без скидання — для індикації/налагодження.
	public double LastDecibels => _lastDb;

	public event EventHandler<SamplesAvailableEventArgs> AudioReady;

	public RecordingAudioProcessor(ISharpControl control)
	{
		_control = control;
		_control.RegisterStreamHook((object)this, (ProcessorType)3);
	}

	public unsafe void Process(float* audio, int length)
	{
		// Тримаємо пік за вікно: накопичувач скидається лише при зчитуванні Decibels.
		float peak = _peakHold;
		for (int i = 0; i < length; i++)
		{
			float a = audio[i];
			if (a < 0f)
			{
				a = 0f - a;
			}
			if (a > peak)
			{
				peak = a;
			}
		}
		_peakHold = peak;
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
