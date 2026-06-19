using System.Windows.Forms;
using SDRSharp.Common;

namespace SDRSharp.AudioRecorder;

public class AudioRecorderPlugin : ISharpPlugin
{
	private const string DefaultDisplayName = "Audio Recorder";

	private RecordingAudioProcessor _audioProcessor;

	private AudioRecorderPanel _guiControl;

	public UserControl Gui => (UserControl)(object)_guiControl;

	public string DisplayName => "Audio Recorder";

	public void Initialize(ISharpControl control)
	{
		_audioProcessor = new RecordingAudioProcessor(control);
		_audioProcessor.Enabled = false;
		_guiControl = new AudioRecorderPanel(control, _audioProcessor);
	}

	public void Close()
	{
		if (_guiControl != null)
		{
			_guiControl.AbortRecording();
			_guiControl.SaveSettings();
		}
	}
}
