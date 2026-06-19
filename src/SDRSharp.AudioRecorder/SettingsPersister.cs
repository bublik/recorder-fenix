using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace SDRSharp.AudioRecorder;

public class SettingsPersister
{
	private const string FreqMgrFilename = "frequencies.xml";

	private readonly string _settingsFolder;

	public SettingsPersister()
	{
		_settingsFolder = Path.GetDirectoryName(Application.ExecutablePath);
	}

	public List<MemoryEntry> ReadStoredFrequencies()
	{
		List<MemoryEntry> list = ReadObject<List<MemoryEntry>>("frequencies.xml");
		if (list != null)
		{
			list.Sort((MemoryEntry e1, MemoryEntry e2) => e1.Frequency.CompareTo(e2.Frequency));
			return list;
		}
		return new List<MemoryEntry>();
	}

	public void PersistStoredFrequencies(List<MemoryEntry> entries)
	{
		WriteObject(entries, "frequencies.xml");
	}

	private T ReadObject<T>(string fileName)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		string path = Path.Combine(_settingsFolder, fileName);
		if (File.Exists(path))
		{
			using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
			{
				return (T)new XmlSerializer(typeof(T)).Deserialize((Stream)fileStream);
			}
		}
		return default(T);
	}

	private void WriteObject<T>(T obj, string fileName)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		using FileStream fileStream = new FileStream(Path.Combine(_settingsFolder, fileName), FileMode.Create);
		new XmlSerializer(obj.GetType()).Serialize((Stream)fileStream, (object)obj);
	}
}
