using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using SDRSharp.Radio;

namespace SDRSharp.AudioRecorder;

public class SimpleWavWriter
{
	private const long MaxStreamLength = 2147483648L;

	private readonly string _filename;

	private readonly WavFormatHeader _format;

	private readonly WavSampleFormat _wavSampleFormat;

	private BinaryWriter _outputStream;

	private long _fileSizeOffs;

	private long _dataSizeOffs;

	private long _length;

	private byte[] _outputBuffer;

	private bool _isStreamFull;

	private UnsafeBuffer _resampleBuffer;

	private unsafe float* _resampleBufferPtr;

	private int _samplerateOut;

	private Resampler _resampler;

	private float _resampleCoeff;

	public WavSampleFormat SampleFormat => _wavSampleFormat;

	public WavFormatHeader WaveFormat => _format;

	public string FileName => _filename;

	public long Length => _length;

	public bool IsStreamFull => _isStreamFull;

	public SimpleWavWriter(string filename, WavSampleFormat recordingFormat, uint sampleRateIn, int samplerateOut)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected O, but got Unknown
		_filename = filename;
		_wavSampleFormat = recordingFormat;
		_samplerateOut = samplerateOut;
		_format = new WavFormatHeader(recordingFormat, (uint)_samplerateOut);
		if (sampleRateIn != samplerateOut)
		{
			_resampler = new Resampler((double)sampleRateIn, (double)samplerateOut, 160, 0.45);
			_resampleCoeff = (float)samplerateOut / (float)sampleRateIn;
		}
		else
		{
			_resampler = null;
		}
	}

	public void Open()
	{
		if (_outputStream != null)
		{
			throw new InvalidOperationException("AudioRecorder: Stream already open");
		}
		_outputStream = new BinaryWriter(File.Create(_filename));
		WriteHeader();
	}

	public void Close()
	{
		if (_outputStream == null)
		{
			return; // вже закрито — ідемпотентний виклик безпечний
		}
		UpdateLength();
		_outputStream.Flush();
		_outputStream.Close();
		_outputStream = null;
		_outputBuffer = null;
	}

	public unsafe void Write(float* data, int length)
	{
		if (_outputStream == null)
		{
			throw new InvalidOperationException("AudioRecorder: Stream not open");
		}
		int lengthOut = length;
		float* bufferOut = data;
		switch (_wavSampleFormat)
		{
		case WavSampleFormat.PCM8Stereo:
			WritePCM8(data, length);
			break;
		case WavSampleFormat.PCM16Stereo:
			WritePCM16(data, length);
			break;
		case WavSampleFormat.Float32:
			WriteFloat(data, length);
			break;
		case WavSampleFormat.PCM8Mono:
			length /= 2;
			StereoToMono(data, length);
			Resample(data, length, out bufferOut, out lengthOut);
			WritePCM8(bufferOut, lengthOut);
			break;
		case WavSampleFormat.PCM16Mono:
			length /= 2;
			StereoToMono(data, length);
			Resample(data, length, out bufferOut, out lengthOut);
			WritePCM16(bufferOut, lengthOut);
			break;
		}
	}

	private unsafe void StereoToMono(float* data, int length)
	{
		int num = 0;
		for (int i = 0; i < length; i++)
		{
			data[i] = (data[num] + data[num + 1]) * 0.5f;
			num += 2;
		}
	}

	private unsafe void Resample(float* data, int length, out float* bufferOut, out int lengthOut)
	{
		if (_resampler != null)
		{
			int num = (int)((double)length * (double)_resampleCoeff) + 100;
			if (_resampleBuffer == null || _resampleBuffer.Length != num)
			{
				_resampleBuffer = null;
				_resampleBuffer = UnsafeBuffer.Create(num, 4);
				_resampleBufferPtr = (float*)(void*)_resampleBuffer;
			}
			lengthOut = _resampler.Process(data, _resampleBufferPtr, length);
			bufferOut = _resampleBufferPtr;
		}
		else
		{
			lengthOut = length;
			bufferOut = data;
		}
	}

	private unsafe void WritePCM8(float* data, int length)
	{
		if (_outputBuffer == null || _outputBuffer.Length != length)
		{
			_outputBuffer = null;
			_outputBuffer = new byte[length];
		}
		for (int i = 0; i < length; i++)
		{
			_outputBuffer[i] = (byte)((double)data[i] * 127.0 + 128.0);
		}
		WriteStream(_outputBuffer);
	}

	private unsafe void WritePCM16(float* data, int length)
	{
		if (_outputBuffer == null || _outputBuffer.Length != length * 2)
		{
			_outputBuffer = null;
			_outputBuffer = new byte[length * 2];
		}
		int num = 0;
		for (int i = 0; i < length; i++)
		{
			short num2 = (short)((double)data[i] * 32767.0);
			_outputBuffer[num] = (byte)(num2 & 0xFF);
			num++;
			_outputBuffer[num] = (byte)((uint)num2 >> 8);
			num++;
		}
		WriteStream(_outputBuffer);
	}

	private unsafe void WriteFloat(float* data, int length)
	{
		if (_outputBuffer == null || _outputBuffer.Length != length * 4)
		{
			_outputBuffer = null;
			_outputBuffer = new byte[length * 4];
		}
		Marshal.Copy((IntPtr)data, _outputBuffer, 0, _outputBuffer.Length);
		WriteStream(_outputBuffer);
	}

	private void WriteStream(byte[] data)
	{
		if (_outputStream == null)
		{
			return;
		}
		int num = (int)Math.Min(2147483648u - _outputStream.BaseStream.Length, data.Length);
		try
		{
			_outputStream.Write(data, 0, num);
			_length += num;
			// UpdateLength() перенесено в Close() — два Seek на кожен буфер суттєво навантажували диск
			_isStreamFull = _outputStream.BaseStream.Length >= 2147483648u;
		}
		catch (Exception)
		{
			_isStreamFull = true;
		}
	}

	private void WriteHeader()
	{
		if (_outputStream != null)
		{
			_outputStream.Seek(0, SeekOrigin.Begin);
			WriteTag("RIFF");
			_fileSizeOffs = _outputStream.BaseStream.Position;
			_outputStream.Write(0u);
			WriteTag("WAVE");
			WriteTag("fmt ");
			_outputStream.Write(16u);
			_outputStream.Write(_format.FormatTag);
			_outputStream.Write(_format.Channels);
			_outputStream.Write(_format.SamplesPerSec);
			_outputStream.Write(_format.AvgBytesPerSec);
			_outputStream.Write(_format.BlockAlign);
			_outputStream.Write(_format.BitsPerSample);
			WriteTag("data");
			_dataSizeOffs = _outputStream.BaseStream.Position;
			_outputStream.Write(0u);
			_outputStream.Seek(0, SeekOrigin.End);
		}
	}

	private void UpdateLength()
	{
		if (_outputStream != null)
		{
			_outputStream.Seek((int)_fileSizeOffs, SeekOrigin.Begin);
			_outputStream.Write((uint)(_outputStream.BaseStream.Length - 8));
			_outputStream.Seek((int)_dataSizeOffs, SeekOrigin.Begin);
			_outputStream.Write((uint)_length);
			_outputStream.BaseStream.Seek(0L, SeekOrigin.End);
		}
	}

	private void WriteTag(string tag)
	{
		byte[] bytes = Encoding.ASCII.GetBytes(tag);
		_outputStream.Write(bytes, 0, bytes.Length);
	}
}
