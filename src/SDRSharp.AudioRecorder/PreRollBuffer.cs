using System;

namespace SDRSharp.AudioRecorder;

// Кільцевий буфер останніх семплів аудіо (інтерлівд-стерео), поки запис на паузі.
// Коли VOX відкривається, вміст дописується перед поточним буфером — так не губиться
// початок передачі, що прийшов до спрацювання гейта. Використовується лише з аудіопотоку.
public sealed class PreRollBuffer
{
	private readonly float[] _data;

	private int _pos;

	private int _count;

	public PreRollBuffer(int capacity)
	{
		if (capacity <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(capacity));
		}
		_data = new float[capacity];
	}

	public int Capacity => _data.Length;

	public int Count => _count;

	public float[] Data => _data;

	public unsafe void Write(float* audio, int length)
	{
		if (length <= 0)
		{
			return;
		}
		int capacity = _data.Length;
		if (length >= capacity)
		{
			new ReadOnlySpan<float>(audio + (length - capacity), capacity).CopyTo(_data);
			_pos = 0;
			_count = capacity;
			return;
		}
		int first = Math.Min(length, capacity - _pos);
		new ReadOnlySpan<float>(audio, first).CopyTo(new Span<float>(_data, _pos, first));
		if (first < length)
		{
			new ReadOnlySpan<float>(audio + first, length - first).CopyTo(_data);
		}
		_pos = (_pos + length) % capacity;
		_count = Math.Min(_count + length, capacity);
	}

	// Вміст від найстарішого семпла до найновішого: Data[start..start+len1) + Data[0..len2).
	public void GetSegments(out int start, out int len1, out int len2)
	{
		int capacity = _data.Length;
		start = (_pos - _count + capacity) % capacity;
		len1 = Math.Min(_count, capacity - start);
		len2 = _count - len1;
	}

	public void Clear()
	{
		_pos = 0;
		_count = 0;
	}
}
