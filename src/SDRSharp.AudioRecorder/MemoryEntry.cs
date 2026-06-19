using SDRSharp.Radio;

namespace SDRSharp.AudioRecorder;

public class MemoryEntry
{
	private long _frequency;

	private DetectorType _detectorType;

	private string _name;

	private long _shift;

	private long _centerFrequency;

	private string _groupName;

	private long _filterBandwidth;

	private bool _isFavourite;

	public bool IsFavourite
	{
		get
		{
			return _isFavourite;
		}
		set
		{
			_isFavourite = value;
		}
	}

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
		}
	}

	public string GroupName
	{
		get
		{
			return _groupName;
		}
		set
		{
			_groupName = value;
		}
	}

	public long Frequency
	{
		get
		{
			return _frequency;
		}
		set
		{
			_frequency = value;
		}
	}

	public DetectorType DetectorType
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return _detectorType;
		}
		set
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			_detectorType = value;
		}
	}

	public long Shift
	{
		get
		{
			return _shift;
		}
		set
		{
			_shift = value;
		}
	}

	public long FilterBandwidth
	{
		get
		{
			return _filterBandwidth;
		}
		set
		{
			_filterBandwidth = value;
		}
	}

	public long CenterFrequency
	{
		get
		{
			return _centerFrequency;
		}
		set
		{
			_centerFrequency = value;
		}
	}

	public MemoryEntry()
	{
	}

	public MemoryEntry(MemoryEntry memoryEntry)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		_name = memoryEntry._name;
		_groupName = memoryEntry._groupName;
		_detectorType = memoryEntry._detectorType;
		_frequency = memoryEntry._frequency;
		_shift = memoryEntry._shift;
		_centerFrequency = memoryEntry._centerFrequency;
		_filterBandwidth = memoryEntry._filterBandwidth;
		_isFavourite = memoryEntry._isFavourite;
	}
}
