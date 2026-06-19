using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using SDRSharp.Radio;

namespace SDRSharp.AudioRecorder;

public class SimpleRecorder : IDisposable
{
	private readonly SharpEvent _bufferEvent = new SharpEvent(false);

	private readonly RecordingAudioProcessor _audioProcessor;

	private List<int> _scaleModes = new List<int> { 115000, 250000, 400000, 175000, 175000, 175000, 60000, 60000 };

	private WavSampleFormat _wavSampleFormat;

	private SimpleWavWriter _wavWriter;

	private Thread _diskWriter;

	private FloatFifoStream _audioBuffer;

	private UnsafeBuffer _buffer;

	private unsafe float* _bufferPtr;

	private volatile bool _diskWriterRunning;

	private volatile bool _diskWriterPause;

	private string _fileName;

	private double _sampleRate;

	private string _infoName;

	private int _samplerateOut;

	private bool _unityGain;

	private bool _useNewAudioLevel;

	private int _audioBufferLength;

	private int _lostBuffers;

	private int _bufferUsage;

	private DetectorType _detector;

	private double _decibels;

	private double _max_tmp;

	private int _dbCount;

	public int SamplerateOut
	{
		get
		{
			return _samplerateOut;
		}
		set
		{
			_samplerateOut = value;
		}
	}

	public bool IsRecording => _diskWriterRunning;

	public DetectorType Detector
	{
		set
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			_detector = value;
		}
	}

	public bool DiskWriterPause
	{
		get
		{
			return _diskWriterPause;
		}
		set
		{
			_diskWriterPause = value;
		}
	}

	public double Decibels => _decibels;

	public bool IsStreamFull
	{
		get
		{
			if (_wavWriter != null)
			{
				return _wavWriter.IsStreamFull;
			}
			return false;
		}
	}

	public long BytesWritten
	{
		get
		{
			if (_wavWriter != null)
			{
				return _wavWriter.Length;
			}
			return 0L;
		}
	}

	public WavSampleFormat Format
	{
		get
		{
			return _wavSampleFormat;
		}
		set
		{
			if (_diskWriterRunning)
			{
				throw new ArgumentException("AudioRecorder: Format cannot be set while recording");
			}
			_wavSampleFormat = value;
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
			if (_diskWriterRunning)
			{
				throw new ArgumentException("AudioRecorder: SampleRate cannot be set while recording");
			}
			_sampleRate = value;
		}
	}

	public string FileName
	{
		get
		{
			return _fileName;
		}
		set
		{
			if (_diskWriterRunning)
			{
				throw new ArgumentException("AudioRecorder: FileName cannot be set while recording");
			}
			_fileName = value;
		}
	}

	public string infoName
	{
		get
		{
			return _infoName;
		}
		set
		{
			if (_diskWriterRunning)
			{
				throw new ArgumentException("AudioRecorder: Media info tags cannot be set while recording");
			}
			_infoName = value;
		}
	}

	public bool UnityGain
	{
		get
		{
			return _unityGain;
		}
		set
		{
			_unityGain = value;
		}
	}

	public int BufferUsage => _bufferUsage;

	public int LostBuffers => _lostBuffers;

	public bool UseNewAudioLevel
	{
		get
		{
			return _useNewAudioLevel;
		}
		set
		{
			_useNewAudioLevel = value;
		}
	}

	public SimpleRecorder(RecordingAudioProcessor audioProcessor)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Expected O, but got Unknown
		_audioProcessor = audioProcessor;
		_audioProcessor.AudioReady += AudioSamplesIn;
		_audioProcessor.Enabled = false;
		_audioBuffer = new FloatFifoStream((BlockMode)0);
	}

	~SimpleRecorder()
	{
		Dispose();
	}

	public void Dispose()
	{
		FreeBuffers();
	}

	public unsafe void AudioSamplesIn(object audio, SamplesAvailableEventArgs args)
	{
		_audioBufferLength = args.Length;
		if (!_diskWriterPause)
		{
			if ((double)_audioBuffer.Length < _sampleRate)
			{
				_audioBuffer.Write(args.Buffer, args.Length);
				_bufferEvent.Set();
			}
			else
			{
				_lostBuffers++;
			}
		}
		else
		{
			_decibels = -100.0;
		}
		_bufferUsage = (int)((double)_audioBuffer.Length / _sampleRate * 100.0);
		_bufferUsage = Math.Max(_bufferUsage, 0);
	}

	public unsafe void ScaleAudio(float* audio, int length)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected I4, but got Unknown
		float num = _scaleModes[(int)_detector];
		if (_useNewAudioLevel)
		{
			num /= 10f;
		}
		float num2 = 0f;
		for (int i = 0; i < length; i++)
		{
			audio[i] *= num;
			float num3 = Math.Abs(audio[i]);
			if (num3 > num2)
			{
				num2 = num3;
			}
		}
		_max_tmp += num2;
		_dbCount++;
		if (_dbCount > 7)
		{
			_decibels = 20.0 * Math.Log10(_max_tmp / (double)_dbCount);
			_max_tmp = 0.0;
			_dbCount = 0;
		}
	}

	public string SetScalingValues(string strArray)
	{
		//IL_0373: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		string[] array = strArray.Split(new char[1] { ',' });
		List<int> list = (_useNewAudioLevel ? new List<int> { 10, 10, 10, 10, 10, 10, 10, 10 } : new List<int> { 115000, 250000, 400000, 175000, 175000, 175000, 60000, 60000 });
		if (array.Length != 8)
		{
			_scaleModes = (_useNewAudioLevel ? new List<int> { 10, 10, 10, 10, 10, 10, 10, 10 } : new List<int> { 115000, 250000, 400000, 175000, 175000, 175000, 60000, 60000 });
			MessageBox.Show("AudioRecorder: Error with setting 'AudioRecorder.ScaleAudioMode'\nWrong number of elements=" + array.Length + " (8 expected)\nReset to defaults");
			return string.Join(",", _scaleModes);
		}
		int num = (_useNewAudioLevel ? 2 : 50000);
		int num2 = (_useNewAudioLevel ? 15 : 500000);
		for (int i = 0; i < array.Length; i++)
		{
			try
			{
				int num3 = 0;
				try
				{
					num3 = Convert.ToInt32(array[i]);
				}
				catch (Exception)
				{
					num3 = 0;
				}
				if (num3 != 0)
				{
					if (num3 < num || num3 > num2)
					{
						_scaleModes[i] = list[i];
						MessageBox.Show("AudioRecorder: Error with setting 'AudioRecorder.ScaleAudioMode'\nElement: #" + i + " is out of range [" + num + " - " + num2 + "]\nReset to default=" + _scaleModes[i]);
					}
					else
					{
						_scaleModes[i] = num3;
					}
				}
				else
				{
					_scaleModes[i] = list[i];
					MessageBox.Show("AudioRecorder: Error with setting 'AudioRecorder.ScaleAudioMode'\nElement: #" + i + " is invalid [use " + num + " - " + num2 + "]\nReset to default=" + _scaleModes[i]);
				}
			}
			catch (Exception)
			{
				_scaleModes[i] = list[i];
				MessageBox.Show("AudioRecorder: Error with setting 'AudioRecorder.ScaleAudioMode'\nElement: #" + i + " is exception [use " + num + " - " + num2 + "]\nReset to default=" + _scaleModes[i]);
			}
		}
		return string.Join(",", _scaleModes);
	}

	private unsafe void DiskWriterThread()
	{
		while (_diskWriterRunning && !_wavWriter.IsStreamFull)
		{
			if (_audioBuffer == null || _audioBufferLength == 0)
			{
				Thread.Sleep(10);
				continue;
			}
			if (_audioBuffer.Length < _audioBufferLength)
			{
				_bufferEvent.WaitOne();
				continue;
			}
			if (_buffer == null || _buffer.Length != _audioBufferLength * 2)
			{
				if (_buffer != null)
				{
					_buffer.Dispose();
					_buffer = null;
					_bufferPtr = null;
				}
				_buffer = UnsafeBuffer.Create(_audioBufferLength * 2, 4);
				_bufferPtr = (float*)UnsafeBuffer.op_Implicit(_buffer);
			}
			_audioBuffer.Read(_bufferPtr, _audioBufferLength);
			if (!_unityGain)
			{
				ScaleAudio(_bufferPtr, _audioBufferLength);
			}
			_wavWriter.Write(_bufferPtr, _audioBufferLength);
		}
		float* localPtr = _bufferPtr;
		while (localPtr != null && _audioBuffer != null && _audioBuffer.Length >= _audioBufferLength && _audioBufferLength > 0 && !_wavWriter.IsStreamFull)
		{
			_audioBuffer.Read(localPtr, _audioBufferLength);
			if (!_unityGain)
			{
				ScaleAudio(localPtr, _audioBufferLength);
			}
			_wavWriter.Write(localPtr, _audioBufferLength);
		}
		// Закриваємо WAV-файл тут, щоб заголовок був записаний навіть якщо вийшли через IsStreamFull
		// до того, як StopRecording() встигне викликати Flush(). Close() ідемпотентний.
		_wavWriter?.Close();
		_diskWriterRunning = false;
	}

	private void Flush()
	{
		if (_wavWriter != null)
		{
			_wavWriter.Close();
		}
		_audioBuffer.Flush();
		_audioBufferLength = 0;
	}

	private unsafe void FreeBuffers()
	{
		if (_audioBuffer != null)
		{
			_audioBuffer.Close();
			_audioBuffer.Dispose();
			_audioBuffer = null;
		}
		if (_buffer != null)
		{
			_buffer.Dispose();
			_buffer = null;
			_bufferPtr = null;
		}
	}

	public void StartRecording()
	{
		if (_diskWriter == null)
		{
			_bufferEvent.Reset();
			_wavWriter = new SimpleWavWriter(_fileName, _wavSampleFormat, (uint)_sampleRate, _samplerateOut);
			_wavWriter.Open();
			_diskWriter = new Thread(DiskWriterThread);
			_diskWriterRunning = true;
			_diskWriter.Start();
			_audioProcessor.Enabled = true;
			_lostBuffers = 0;
		}
	}

	public void StopRecording()
	{
		_audioProcessor.Enabled = false;
		_diskWriterRunning = false;
		try
		{
			if (_diskWriter != null)
			{
				_bufferEvent.Set();
				_diskWriter.Join();
			}
		}
		finally
		{
			Flush();
			_diskWriter = null;
			_wavWriter = null;
			_bufferUsage = 0;
		}
	}
}
