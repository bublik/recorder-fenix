using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SDRSharp.Common;
using SDRSharp.PluginsCom;
using SDRSharp.Radio;

namespace SDRSharp.AudioRecorder;

public class AudioRecorderPanel : UserControl
{
	private IContainer components;

	private Button recBtn;

	private Timer recDisplayTimer;

	private Label allLabel;

	private Panel panel1;

	private Timer recordTimer;

	private Label recordLabel;

	private FolderBrowserDialog writeFolderBrowserDialog;

	private Button selectFolderDialog;

	private Button openFolderButton;

	private Button configureButton;

	private Label currentLabel;

	private ProgressBar progressBar;

	private Label label1;

	private ToolTip toolTip1;

	private Label label3;

	private const string recorderStartCom = "Audio_recorder_Start";

	private const string recorderStopCom = "Audio_recorder_Stop";

	private const string ctcssToneCom = "CTCSS_tone<";

	private const string dcsCodeCom = "DCS_code<";

	private const float bytesToMb = 9.536743E-07f;

	private string tempFileName = "\\temporaryAudioRecord" + DateTime.Now.ToString("HHmmss") + ".wav";

	private const long MIN_ALLOWED_HDD_REMAIN = 10000000L;

	private readonly ISharpControl _control;

	private readonly SimpleRecorder _simpleRecorder;

	private readonly RecordingAudioProcessor _audioProcessor;

	private readonly SettingsPersister _settingsPersister;

	private PluginsComProxy _pluginsCom;

	private List<MemoryEntry> _entriesInManager;

	private DateTime _startTime;

	private DateTime _endTime;

	private long _oldFrequency;

	private float _writeAllMbyte;

	private int _filesCounter;

	private int _waitTime;

	private bool _recordIsStarted;

	private int _newFileWaitTime;

	private int _recordSamplerate;

	private float _writeLength;

	private float _writeAllSecond;

	private int _fileIndexer;

	private string _ctcss = string.Empty;

	private string _dcs = string.Empty;

	private bool _debugEnable;

	private bool _comCmdError;

	private bool _simpleRecorderError;

	private bool _createFolderError;

	private bool _renameError;

	private Color _foreColor = SystemColors.ControlText;

	private Color _backColor = SystemColors.Control;

	private bool _isTelerikSDRSharp;

	public bool AutoStartRecording { get; set; }

	public bool WriteOneFile { get; set; }

	public bool DeleteSmallFiles { get; set; }

	public float MinWriteLength { get; set; }

	public WavSampleFormat SampleFormat { get; set; }

	public int SampleFormatSelectedIndex { get; set; }

	public int SamplerateOut { get; set; }

	public bool DontWritePause { get; set; }

	public bool ContinueRecordTimeEnable { get; set; }

	public int ContinueRecordTime { get; set; }

	public bool NewFileTimeEnable { get; set; }

	public int NewFileTime { get; set; }

	public bool NewFileFrequencyEnable { get; set; }

	public bool MaxFileSizeEnable { get; set; }

	public long MaxWriteLength { get; set; }

	public string OutputSamplerateArray { get; set; }

	public string ScaleAudioModeArray { get; set; }

	public bool UseUtcTimestamp { get; set; }

	public string FileNameRules { get; set; }

	public bool UseSquelch { get; set; }

	public bool UseMute { get; set; }

	public string PluginVersion => "Audio Recorder - v" + ((Control)this).ProductVersion;

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected O, but got Unknown
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Expected O, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Expected O, but got Unknown
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Expected O, but got Unknown
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Expected O, but got Unknown
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Expected O, but got Unknown
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Expected O, but got Unknown
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Expected O, but got Unknown
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Expected O, but got Unknown
		//IL_056f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0579: Expected O, but got Unknown
		//IL_06bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c6: Expected O, but got Unknown
		//IL_0724: Unknown result type (might be due to invalid IL or missing references)
		//IL_072e: Expected O, but got Unknown
		components = new Container();
		recBtn = new Button();
		recDisplayTimer = new Timer(components);
		allLabel = new Label();
		panel1 = new Panel();
		label3 = new Label();
		label1 = new Label();
		progressBar = new ProgressBar();
		currentLabel = new Label();
		configureButton = new Button();
		openFolderButton = new Button();
		selectFolderDialog = new Button();
		recordLabel = new Label();
		recordTimer = new Timer(components);
		writeFolderBrowserDialog = new FolderBrowserDialog();
		toolTip1 = new ToolTip(components);
		((Control)panel1).SuspendLayout();
		((Control)this).SuspendLayout();
		((Control)recBtn).Anchor = (AnchorStyles)9;
		((Control)recBtn).Enabled = false;
		((ButtonBase)recBtn).FlatStyle = (FlatStyle)0;
		((Control)recBtn).Location = new Point(111, 125);
		((Control)recBtn).Name = "recBtn";
		((Control)recBtn).Size = new Size(93, 23);
		((Control)recBtn).TabIndex = 0;
		((Control)recBtn).Text = "Record";
		((ButtonBase)recBtn).UseVisualStyleBackColor = true;
		((Control)recBtn).Click += recBtn_Click;
		((Control)recBtn).Paint += new PaintEventHandler(recBtn_Paint);
		recDisplayTimer.Tick += recDisplayTimer_Tick;
		((Control)allLabel).Anchor = (AnchorStyles)13;
		((Control)allLabel).Location = new Point(6, 29);
		((Control)allLabel).Name = "allLabel";
		((Control)allLabel).Size = new Size(205, 13);
		((Control)allLabel).TabIndex = 3;
		((Control)allLabel).Text = "all 0 file(s) 00:00:00 - 0.000 MB";
		allLabel.TextAlign = (ContentAlignment)2;
		((Control)panel1).Anchor = (AnchorStyles)13;
		((Control)panel1).Controls.Add((Control)(object)label3);
		((Control)panel1).Controls.Add((Control)(object)label1);
		((Control)panel1).Controls.Add((Control)(object)progressBar);
		((Control)panel1).Controls.Add((Control)(object)currentLabel);
		((Control)panel1).Controls.Add((Control)(object)configureButton);
		((Control)panel1).Controls.Add((Control)(object)openFolderButton);
		((Control)panel1).Controls.Add((Control)(object)selectFolderDialog);
		((Control)panel1).Controls.Add((Control)(object)allLabel);
		((Control)panel1).Controls.Add((Control)(object)recordLabel);
		((Control)panel1).Controls.Add((Control)(object)recBtn);
		((Control)panel1).Location = new Point(0, 0);
		((Control)panel1).Name = "panel1";
		((Control)panel1).Size = new Size(217, 190);
		((Control)panel1).TabIndex = 7;
		((Control)label3).AutoSize = true;
		((Control)label3).Location = new Point(33, 152);
		((Control)label3).Name = "label3";
		((Control)label3).Size = new Size(37, 13);
		((Control)label3).TabIndex = 24;
		((Control)label3).Text = "debug";
		((Control)label3).Visible = false;
		((Control)label1).Anchor = (AnchorStyles)13;
		((Control)label1).Location = new Point(6, 48);
		((Control)label1).Name = "label1";
		((Control)label1).Size = new Size(205, 13);
		((Control)label1).TabIndex = 23;
		((Control)label1).Text = "Dropped buffers: 0";
		label1.TextAlign = (ContentAlignment)2;
		((Control)progressBar).Anchor = (AnchorStyles)13;
		((Control)progressBar).Location = new Point(111, 73);
		((Control)progressBar).Name = "progressBar";
		((Control)progressBar).Size = new Size(93, 12);
		((Control)progressBar).TabIndex = 8;
		((Control)currentLabel).Anchor = (AnchorStyles)13;
		((Control)currentLabel).Location = new Point(6, 9);
		((Control)currentLabel).Name = "currentLabel";
		((Control)currentLabel).Size = new Size(205, 13);
		((Control)currentLabel).TabIndex = 22;
		((Control)currentLabel).Text = "Write: current 00:00:00 - 0.000 MB";
		currentLabel.TextAlign = (ContentAlignment)2;
		((ButtonBase)configureButton).FlatStyle = (FlatStyle)0;
		((Control)configureButton).Location = new Point(12, 125);
		((Control)configureButton).Name = "configureButton";
		((Control)configureButton).Size = new Size(93, 23);
		((Control)configureButton).TabIndex = 21;
		((Control)configureButton).Text = "Configure";
		((ButtonBase)configureButton).UseVisualStyleBackColor = true;
		((Control)configureButton).Click += configureButton_Click;
		((Control)configureButton).Paint += new PaintEventHandler(configureButton_Paint);
		((ButtonBase)openFolderButton).FlatStyle = (FlatStyle)0;
		((Control)openFolderButton).Location = new Point(12, 96);
		((Control)openFolderButton).Name = "openFolderButton";
		((Control)openFolderButton).Size = new Size(93, 23);
		((Control)openFolderButton).TabIndex = 20;
		((Control)openFolderButton).Text = "Open folder";
		((ButtonBase)openFolderButton).UseVisualStyleBackColor = true;
		((Control)openFolderButton).Click += openFolderButton_Click;
		((Control)openFolderButton).MouseEnter += openFolderButton_MouseEnter;
		((Control)openFolderButton).MouseLeave += openFolderButton_MouseLeave;
		((ButtonBase)selectFolderDialog).FlatStyle = (FlatStyle)0;
		((Control)selectFolderDialog).Location = new Point(12, 67);
		((Control)selectFolderDialog).Name = "selectFolderDialog";
		((Control)selectFolderDialog).Size = new Size(93, 23);
		((Control)selectFolderDialog).TabIndex = 12;
		((Control)selectFolderDialog).Text = "Folder select";
		((ButtonBase)selectFolderDialog).UseVisualStyleBackColor = true;
		((Control)selectFolderDialog).Click += SelectFolderDialog_Click;
		((Control)selectFolderDialog).Paint += new PaintEventHandler(selectFolderDialog_Paint);
		((Control)selectFolderDialog).MouseEnter += selectFolderDialog_MouseEnter;
		((Control)selectFolderDialog).MouseLeave += selectFolderDialog_MouseLeave;
		((Control)recordLabel).Anchor = (AnchorStyles)9;
		((Control)recordLabel).AutoSize = true;
		((Control)recordLabel).Font = new Font("Microsoft Sans Serif", 8.25f, (FontStyle)1, (GraphicsUnit)3, (byte)204);
		((Control)recordLabel).ForeColor = Color.Black;
		((Control)recordLabel).Location = new Point(111, 101);
		((Control)recordLabel).Name = "recordLabel";
		((Control)recordLabel).Size = new Size(92, 13);
		((Control)recordLabel).TabIndex = 9;
		((Control)recordLabel).Text = "Create new file";
		((Control)recordLabel).Visible = false;
		((Control)recordLabel).DoubleClick += recordLabel_DoubleClick;
		recordTimer.Tick += recordTimer_Tick;
		((ContainerControl)this).AutoScaleDimensions = new SizeF(6f, 13f);
		((ContainerControl)this).AutoScaleMode = (AutoScaleMode)1;
		((Control)this).Controls.Add((Control)(object)panel1);
		((Control)this).Name = "AudioRecorderPanel";
		((Control)this).Size = new Size(217, 193);
		((UserControl)this).Load += AudioRecorderPanel_Load;
		((Control)this).VisibleChanged += AudioRecorderPanel_VisibleChanged;
		((Control)panel1).ResumeLayout(false);
		((Control)panel1).PerformLayout();
		((Control)this).ResumeLayout(false);
	}

	public AudioRecorderPanel(ISharpControl control, RecordingAudioProcessor audioProcessor)
	{
		InitializeComponent();
		_control = control;
		_audioProcessor = audioProcessor;
		_control.PropertyChanged += PropertyChangedHandler;
		if (((object)_control).GetType().GetProperty("ThemeIsDark") != null)
		{
			_isTelerikSDRSharp = true;
		}
		_settingsPersister = new SettingsPersister();
		_entriesInManager = _settingsPersister.ReadStoredFrequencies();
		_simpleRecorder = new SimpleRecorder(_audioProcessor);
		string[] array = Application.ProductVersion.Split(new char[1] { '.' });
		try
		{
			_simpleRecorder.UseNewAudioLevel = Convert.ToInt32(array[0]) == 1 && Convert.ToInt32(array[1]) == 0 && Convert.ToInt32(array[2]) == 0 && Convert.ToInt32(array[3]) >= 1918;
		}
		catch (Exception)
		{
			_simpleRecorder.UseNewAudioLevel = false;
		}
		AutoStartRecording = Utils.GetBooleanSetting("AudioRecorder.AutoStartRecording");
		OutputSamplerateArray = Utils.GetStringSetting("AudioRecorder.WriterOutputSampleRate", "48 kHz,32 kHz,16 kHz,8 kHz");
		ScaleAudioModeArray = Utils.GetStringSetting("AudioRecorder.ScaleAudioMode", _simpleRecorder.UseNewAudioLevel ? "10,10,10,10,10,10,10,10" : "115000,250000,400000,175000,175000,175000,60000,60000");
		ScaleAudioModeArray = _simpleRecorder.SetScalingValues(ScaleAudioModeArray);
		FileNameRules = Utils.GetStringSetting("AudioRecorder.FileName", "/ date / group / frequency + \" \" + name / time + \" \" + name + \" \" + frequency");
		UseUtcTimestamp = Utils.GetBooleanSetting("AudioRecorder.UseUtcTimestamp");
		SampleFormatSelectedIndex = Utils.GetIntSetting("AudioRecorder.RecordSampleFormat", 0);
		SampleFormat = (WavSampleFormat)SampleFormatSelectedIndex;
		DontWritePause = Utils.GetBooleanSetting("AudioRecorder.DontWritePause");
		UseMute = Utils.GetBooleanSetting("AudioRecorder.UseMute");
		UseSquelch = Utils.GetBooleanSetting("AudioRecorder.UseSquelch");
		writeFolderBrowserDialog.SelectedPath = Utils.GetStringSetting("AudioRecorder.WriteFolder", Path.GetDirectoryName(Application.ExecutablePath));
		SamplerateOut = Utils.GetIntSetting("AudioRecorder.WriteSamplerate", 0);
		MinWriteLength = (float)Utils.GetDoubleSetting("AudioRecorder.MinimumWriteLengthInSecond", 0.0);
		DeleteSmallFiles = Utils.GetBooleanSetting("AudioRecorder.DeleteSmallFile");
		ContinueRecordTimeEnable = Utils.GetBooleanSetting("AudioRecorder.ContinueRecordTimeEnabled");
		ContinueRecordTime = Utils.GetIntSetting("AudioRecorder.ContinueRecordTime", 1000);
		NewFileTimeEnable = Utils.GetBooleanSetting("AudioRecorder.NewFileWaitTimeEnable");
		NewFileTime = Utils.GetIntSetting("AudioRecorder.NewFileWaitTime", 10000);
		WriteOneFile = Utils.GetBooleanSetting("AudioRecorder.WriteOneFile");
		NewFileFrequencyEnable = Utils.GetBooleanSetting("AudioRecorder.NewFileIfFrequencyChange");
		MaxFileSizeEnable = Utils.GetBooleanSetting("AudioRecorder.NewFileIfMaxWriteLength");
		MaxWriteLength = Utils.GetLongSetting("AudioRecorder.MaxWriteLength", 2048L);
		if (writeFolderBrowserDialog.SelectedPath.StartsWith("\\\\"))
		{
			writeFolderBrowserDialog.SelectedPath = Application.ExecutablePath;
		}
		ConfigureGUI();
		_pluginsCom = new PluginsComProxy(NewCommandAvailable);
		_pluginsCom.AddAvailableCommands("Audio_recorder_Start");
		_pluginsCom.AddAvailableCommands("Audio_recorder_Stop");
	}

	public void SaveSettings()
	{
		Utils.SaveSetting("AudioRecorder.FileName", FileNameRules);
		Utils.SaveSetting("AudioRecorder.UseUtcTimestamp", (object)UseUtcTimestamp);
		Utils.SaveSetting("AudioRecorder.DeleteSmallFile", (object)DeleteSmallFiles);
		Utils.SaveSetting("AudioRecorder.MinimumWriteLengthInSecond", (object)MinWriteLength);
		Utils.SaveSetting("AudioRecorder.DontWritePause", (object)DontWritePause);
		Utils.SaveSetting("AudioRecorder.UseMute", (object)UseMute);
		Utils.SaveSetting("AudioRecorder.UseSquelch", (object)UseSquelch);
		Utils.SaveSetting("AudioRecorder.WriteFolder", writeFolderBrowserDialog.SelectedPath);
		Utils.SaveSetting("AudioRecorder.WriteSamplerate", (object)SamplerateOut);
		Utils.SaveSetting("AudioRecorder.RecordSampleFormat", (object)SampleFormatSelectedIndex);
		Utils.SaveSetting("AudioRecorder.AutoStartRecording", (object)AutoStartRecording);
		Utils.SaveSetting("AudioRecorder.ContinueRecordTimeEnabled", (object)ContinueRecordTimeEnable);
		Utils.SaveSetting("AudioRecorder.ContinueRecordTime", (object)ContinueRecordTime);
		Utils.SaveSetting("AudioRecorder.WriteOneFile", (object)WriteOneFile);
		Utils.SaveSetting("AudioRecorder.NewFileWaitTimeEnable", (object)NewFileTimeEnable);
		Utils.SaveSetting("AudioRecorder.NewFileWaitTime", (object)NewFileTime);
		Utils.SaveSetting("AudioRecorder.NewFileIfFrequencyChange", (object)NewFileFrequencyEnable);
		Utils.SaveSetting("AudioRecorder.NewFileIfMaxWriteLength", (object)MaxFileSizeEnable);
		Utils.SaveSetting("AudioRecorder.MaxWriteLength", (object)MaxWriteLength);
		Utils.SaveSetting("AudioRecorder.ScaleAudioMode", ScaleAudioModeArray);
	}

	public void AbortRecording()
	{
		if (_recordIsStarted)
		{
			recBtn_Click(null, null);
		}
	}

	public string ParseStringToPath(string nameString)
	{
		string frequency = GetFrequency(_oldFrequency);
		MemoryEntry entryFromManager = GetEntryFromManager(_oldFrequency);
		string text = ReplaceInvalidChars(entryFromManager.Name);
		string text2 = ReplaceInvalidChars(entryFromManager.GroupName);
		string text3 = _startTime.ToString("yyyy_MM_dd");
		string text4 = _startTime.ToString("HH-mm-ss");
		string text5 = _endTime.ToString("HH-mm-ss");
		string text6 = SecondToDate((int)_writeLength).ToString("HH-mm-ss");
		string text7 = "";
		int num = 0;
		while (num < nameString.Length)
		{
			if (CompareString(nameString, "date", num))
			{
				num += "date".Length;
				text7 += text3;
				continue;
			}
			if (CompareString(nameString, "time", num))
			{
				num += "time".Length;
				text7 += text4;
				continue;
			}
			if (CompareString(nameString, "start_time", num))
			{
				num += "start_time".Length;
				text7 += text4;
				continue;
			}
			if (CompareString(nameString, "end_time", num))
			{
				num += "end_time".Length;
				text7 += text5;
				continue;
			}
			if (CompareString(nameString, "length", num))
			{
				num += "length".Length;
				text7 += text6;
				continue;
			}
			if (CompareString(nameString, "frequency", num))
			{
				num += "frequency".Length;
				text7 += frequency;
				continue;
			}
			if (CompareString(nameString, "name", num))
			{
				num += "name".Length;
				text7 += text;
				continue;
			}
			if (CompareString(nameString, "group", num))
			{
				num += "group".Length;
				text7 += text2;
				continue;
			}
			if (CompareString(nameString, "\\", num) || CompareString(nameString, "/", num))
			{
				num++;
				text7 += "\\";
				continue;
			}
			if (CompareString(nameString, "+", num) || CompareString(nameString, " ", num))
			{
				num++;
				continue;
			}
			if (CompareString(nameString, "tones", num))
			{
				num += "tones".Length;
				if (!(_ctcss == string.Empty) || !(_dcs == string.Empty) || recordTimer.Enabled)
				{
					if (_ctcss != string.Empty)
					{
						text7 = text7 + "[CTCSS " + _ctcss.Replace(" Hz", "") + "]";
					}
					if (_dcs != string.Empty)
					{
						text7 = text7 + "[DCS " + _dcs.Replace(" / ", "_") + "]";
					}
				}
				continue;
			}
			if (!CompareString(nameString, "\"", num))
			{
				return text7 + "-error!";
			}
			int num2 = num + 1;
			int num3 = nameString.IndexOf('"', num2);
			if (num3 <= 0)
			{
				return text7 + "-error!";
			}
			text7 += ReplaceInvalidChars(nameString.Substring(num2, num3 - num2));
			num = num3 + 1;
		}
		if (text7.Length > 5)
		{
			int length = ".wav".Length;
			if (text7.Substring(text7.Length - length, length) != ".wav")
			{
				text7 += ".wav";
			}
			if (text7.Substring(0, 1) != "\\")
			{
				text7 = "\\" + text7;
			}
		}
		return text7;
	}

	private void NewCommandAvailable(string command)
	{
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		int num = command.IndexOf('<');
		int num2 = command.IndexOf('>');
		string text = command;
		if (num > 0)
		{
			text = command.Substring(0, num + 1).Trim();
		}
		string text2 = string.Empty;
		if (num2 > 0)
		{
			text2 = command.Substring(num + 1, num2 - num - 1).Trim();
		}
		if (_comCmdError)
		{
			return;
		}
		try
		{
			switch (text)
			{
			case "Audio_recorder_Start":
				if (!_recordIsStarted)
				{
					recBtn_Click(null, null);
				}
				break;
			case "Audio_recorder_Stop":
				if (_recordIsStarted)
				{
					recBtn_Click(null, null);
				}
				break;
			case "CTCSS_tone<":
				_ctcss = text2;
				break;
			case "DCS_code<":
				_dcs = text2;
				break;
			}
		}
		catch
		{
			_comCmdError = true;
			MessageBox.Show("AudioRecorder: Error command translate - " + command);
			_comCmdError = false;
		}
	}

	private void RecordStart()
	{
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		if (UseUtcTimestamp)
		{
			_startTime = DateTime.UtcNow;
		}
		else
		{
			_startTime = DateTime.Now;
		}
		_endTime = _startTime;
		_oldFrequency = _control.Frequency;
		_simpleRecorder.Format = (WavSampleFormat)SampleFormatSelectedIndex;
		string text = writeFolderBrowserDialog.SelectedPath;
		if (writeFolderBrowserDialog.SelectedPath.Substring(writeFolderBrowserDialog.SelectedPath.Length - 1, 1) == "\\")
		{
			text = writeFolderBrowserDialog.SelectedPath.Substring(0, writeFolderBrowserDialog.SelectedPath.Length - 1);
		}
		_simpleRecorder.FileName = text + tempFileName;
		_simpleRecorder.SampleRate = _audioProcessor.SampleRate;
		if (SamplerateOut == 0 || SampleFormatSelectedIndex < 3)
		{
			_simpleRecorder.SamplerateOut = (int)_audioProcessor.SampleRate;
			_recordSamplerate = (int)_audioProcessor.SampleRate;
		}
		else
		{
			_simpleRecorder.SamplerateOut = SamplerateOut;
			_recordSamplerate = SamplerateOut;
		}
		if (SampleFormatSelectedIndex < 3)
		{
			_recordSamplerate *= 2;
		}
		if (_simpleRecorderError)
		{
			return;
		}
		try
		{
			_simpleRecorder.StartRecording();
		}
		catch
		{
			_simpleRecorderError = true;
			MessageBox.Show("AudioRecorder: Unable to start recording", "Error", (MessageBoxButtons)0, (MessageBoxIcon)64);
			_simpleRecorderError = false;
		}
	}

	private void RecordStop()
	{
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		long bytesWritten = _simpleRecorder.BytesWritten;
		string text = writeFolderBrowserDialog.SelectedPath;
		if (writeFolderBrowserDialog.SelectedPath.Substring(writeFolderBrowserDialog.SelectedPath.Length - 1, 1) == "\\")
		{
			text = writeFolderBrowserDialog.SelectedPath.Substring(0, writeFolderBrowserDialog.SelectedPath.Length - 1);
		}
		if (UseUtcTimestamp)
		{
			_endTime = DateTime.UtcNow;
		}
		else
		{
			_endTime = DateTime.Now;
		}
		int bytesPerFrame = SampleFormatSelectedIndex switch
		{
			0 => 2, // PCM8Stereo:  1 байт/семпл * 2 канали
			1 => 4, // PCM16Stereo: 2 байти/семпл * 2 канали
			2 => 8, // Float32:     4 байти/семпл * 2 канали
			3 => 1, // PCM8Mono:    1 байт/семпл * 1 канал
			4 => 2, // PCM16Mono:   2 байти/семпл * 1 канал
			_ => 2,
		};
		int baseSampleRate = SampleFormatSelectedIndex < 3 ? _recordSamplerate / 2 : _recordSamplerate;
		_writeLength = baseSampleRate > 0 ? (float)bytesWritten / (float)(bytesPerFrame * baseSampleRate) : 0f;
		_simpleRecorder.StopRecording();
		if ((double)_writeLength > (double)MinWriteLength || !DeleteSmallFiles)
		{
			_writeAllMbyte += (float)bytesWritten * 9.536743E-07f;
			_writeAllSecond += _writeLength;
			_filesCounter++;
			string text2 = MakeFileName();
			string text3 = text2;
			int length = text2.LastIndexOf('.');
			while (File.Exists(text3))
			{
				text3 = text2.Substring(0, length) + "(" + _fileIndexer.ToString("D3") + ").wav";
				_fileIndexer++;
			}
			if (!_renameError)
			{
				try
				{
					File.Move(text + tempFileName, text3);
				}
				catch (Exception ex)
				{
					_renameError = true;
					MessageBox.Show("AudioRecorder: Unable to rename file.\n " + text3 + "\n\n" + ex.ToString(), "Error", (MessageBoxButtons)0, (MessageBoxIcon)64);
					_renameError = false;
					try
					{
						File.Delete(text + tempFileName);
					}
					catch (Exception)
					{
					}
				}
			}
		}
		else
		{
			File.Delete(text + tempFileName);
		}
		_ctcss = string.Empty;
		_dcs = string.Empty;
	}

	private void ConfigureGUI()
	{
		if (_control.IsPlaying)
		{
			((Control)recBtn).Enabled = true;
			((Control)recBtn).Text = (_recordIsStarted ? "Stop" : "Record");
		}
		else
		{
			((Control)recBtn).Enabled = false;
			((Control)recBtn).Text = "Record";
		}
		((Control)recordLabel).Visible = _recordIsStarted;
		((Control)selectFolderDialog).Enabled = !_recordIsStarted;
		((Control)configureButton).Enabled = !_recordIsStarted;
	}

	private string MakeFileName()
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		string text = writeFolderBrowserDialog.SelectedPath;
		if (text.EndsWith("\\"))
		{
			text = writeFolderBrowserDialog.SelectedPath.Substring(0, writeFolderBrowserDialog.SelectedPath.Length - 1);
		}
		string text2 = text + ParseStringToPath(FileNameRules);
		string text3 = text2.Substring(0, text2.LastIndexOf("\\"));
		if (_createFolderError)
		{
			return "";
		}
		try
		{
			Directory.CreateDirectory(text3);
		}
		catch
		{
			_createFolderError = true;
			MessageBox.Show("AudioRecorder: Unable to create directory", text3, (MessageBoxButtons)0, (MessageBoxIcon)64);
			_createFolderError = false;
		}
		return text2;
	}

	private string ReplaceInvalidChars(string sourceString)
	{
		char[] invalidPathChars = Path.GetInvalidPathChars();
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		while (true)
		{
			int num = sourceString.IndexOfAny(invalidPathChars);
			if (num == -1)
			{
				break;
			}
			sourceString = sourceString.Replace(sourceString[num], '_');
		}
		while (true)
		{
			int num2 = sourceString.IndexOfAny(invalidFileNameChars);
			if (num2 == -1)
			{
				break;
			}
			sourceString = sourceString.Replace(sourceString[num2], '_');
		}
		return sourceString;
	}

	private bool CompareString(string source, string compare, int index)
	{
		if (index + compare.Length > source.Length)
		{
			return false;
		}
		return source.Substring(index, compare.Length) == compare;
	}

	private MemoryEntry GetEntryFromManager(long frequency)
	{
		MemoryEntry memoryEntry = new MemoryEntry();
		memoryEntry.Name = "Unknown";
		memoryEntry.GroupName = "No group";
		if (_entriesInManager == null)
		{
			return memoryEntry;
		}
		foreach (MemoryEntry item in _entriesInManager)
		{
			if (item.Frequency == frequency)
			{
				memoryEntry.Name = item.Name;
				memoryEntry.GroupName = item.GroupName;
				break;
			}
		}
		return memoryEntry;
	}

	public static string GetFrequency(long frequency, bool isRange = false)
	{
		long num = Math.Abs(frequency);
		if (num == 0L)
		{
			return "DC";
		}
		if (num > 1500000000)
		{
			return $"{(double)frequency / 1000000000.0:#,0.000 000} GHz";
		}
		if (num >= 30000000)
		{
			if (!isRange)
			{
				return $"{(double)frequency / 1000000.0:0,0.000###} MHz";
			}
			return $"{(double)frequency / 1000000.0:0,0.######} MHz";
		}
		if (num >= 1000)
		{
			return $"{(double)frequency / 1000.0:#,#.###} KHz";
		}
		return frequency.ToString();
	}

	private bool SignalIsActive()
	{
		bool flag = true;
		if (UseSquelch)
		{
			flag = _control.IsSquelchOpen || !_control.SquelchEnabled;
		}
		if (UseMute)
		{
			flag &= !_control.AudioIsMuted;
		}
		return flag;
	}

	private void RecordLabel(string text, Color color)
	{
		((Control)recordLabel).Text = text;
		((Control)recordLabel).ForeColor = color;
		((Control)recordLabel).Visible = true;
	}

	private DateTime SecondToDate(int second)
	{
		return new DateTime(1, 1, 1, 0, 0, 0).AddSeconds(second);
	}

	private long GetTotalFreeSpace()
	{
		DriveInfo driveInfo = new DriveInfo(writeFolderBrowserDialog.SelectedPath.Substring(0, 1).ToString());
		if (driveInfo.IsReady)
		{
			return driveInfo.AvailableFreeSpace;
		}
		return -1L;
	}

	private void changeColors()
	{
		//IL_0313: Unknown result type (might be due to invalid IL or missing references)
		if (!_isTelerikSDRSharp)
		{
			return;
		}
		int num = 0;
		if (((ContainerControl)this).ParentForm == null || !((Control)((ContainerControl)this).ParentForm).Visible || !((Control)this).Visible || ((Control)this).Width == 0 || ((Control)this).Height == 0)
		{
			return;
		}
		try
		{
			num = 1;
			Color bgColor = GetBgColor(0, 0);
			num = 2;
			if ((double)bgColor.GetBrightness() < 0.5)
			{
				_foreColor = Color.Gainsboro;
				_backColor = bgColor;
			}
			else
			{
				_foreColor = SystemColors.ControlText;
				_backColor = SystemColors.Control;
			}
			Color gray = Color.Gray;
			num = 3;
			((Control)this).BackColor = _backColor;
			((Control)panel1).BackColor = _backColor;
			((Control)label1).BackColor = _backColor;
			((Control)label1).ForeColor = _foreColor;
			((Control)label3).BackColor = _backColor;
			((Control)label3).ForeColor = _foreColor;
			((Control)currentLabel).BackColor = _backColor;
			((Control)currentLabel).ForeColor = _foreColor;
			((Control)allLabel).BackColor = _backColor;
			((Control)allLabel).ForeColor = _foreColor;
			((Control)recordLabel).BackColor = _backColor;
			((Control)recordLabel).ForeColor = _foreColor;
			((Control)progressBar).BackColor = _backColor;
			((Control)progressBar).ForeColor = _foreColor;
			((Control)selectFolderDialog).BackColor = _backColor;
			((Control)selectFolderDialog).ForeColor = _foreColor;
			((ButtonBase)selectFolderDialog).FlatAppearance.BorderSize = 1;
			((ButtonBase)selectFolderDialog).FlatAppearance.MouseOverBackColor = gray;
			((ButtonBase)selectFolderDialog).FlatAppearance.MouseDownBackColor = _backColor;
			((Control)openFolderButton).BackColor = _backColor;
			((Control)openFolderButton).ForeColor = _foreColor;
			((ButtonBase)openFolderButton).FlatAppearance.BorderSize = 1;
			((ButtonBase)openFolderButton).FlatAppearance.MouseOverBackColor = gray;
			((ButtonBase)openFolderButton).FlatAppearance.MouseDownBackColor = _backColor;
			((Control)configureButton).BackColor = _backColor;
			((Control)configureButton).ForeColor = _foreColor;
			((ButtonBase)configureButton).FlatAppearance.BorderSize = 1;
			((ButtonBase)configureButton).FlatAppearance.MouseOverBackColor = gray;
			((ButtonBase)configureButton).FlatAppearance.MouseDownBackColor = _backColor;
			((Control)recBtn).BackColor = _backColor;
			((Control)recBtn).ForeColor = _foreColor;
			((ButtonBase)recBtn).FlatAppearance.BorderSize = 1;
			((ButtonBase)recBtn).FlatAppearance.MouseOverBackColor = gray;
			((ButtonBase)recBtn).FlatAppearance.MouseDownBackColor = _backColor;
		}
		catch (Exception ex)
		{
			MessageBox.Show("AudioRecorder: Looks like there is a problem figuring out the plug-in colors [" + num + "]\n\n " + ex.Message, "ALERT", (MessageBoxButtons)0);
		}
	}

	public Color GetBgColor(int x, int y)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		int num = 1;
		if (((Control)this).Width < x + num || ((Control)this).Height < y + num)
		{
			return Color.Black;
		}
		Bitmap val = new Bitmap(num, num);
		((Control)this).DrawToBitmap(val, new Rectangle(x, y, num, num));
		return val.GetPixel(0, 0);
	}

	private void PropertyChangedHandler(object sender, PropertyChangedEventArgs e)
	{
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		switch (e.PropertyName)
		{
		case "StartRadio":
			ConfigureGUI();
			if (AutoStartRecording)
			{
				recBtn_Click(null, null);
			}
			break;
		case "StopRadio":
			if (_recordIsStarted)
			{
				recBtn_Click(null, null);
			}
			break;
		case "UnityGain":
			if (_simpleRecorder != null)
			{
				_simpleRecorder.UnityGain = _control.UnityGain;
			}
			break;
		case "DetectorType":
			if (_simpleRecorder != null)
			{
				_simpleRecorder.Detector = _control.DetectorType;
			}
			break;
		case "ThemeIsDark":
			changeColors();
			break;
		}
	}

	private void recBtn_Click(object sender, EventArgs e)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		if (!_recordIsStarted)
		{
			long totalFreeSpace = GetTotalFreeSpace();
			if (totalFreeSpace < 10000000)
			{
				if (totalFreeSpace == -1)
				{
					MessageBox.Show("AudioRecorder: There was an unexpected error detecting the drives HDD space to start recording\nCheck the selected base folder", "ALERT", (MessageBoxButtons)0);
				}
				else
				{
					MessageBox.Show("AudioRecorder: You do not have enough HDD space to start recording", "ALERT", (MessageBoxButtons)0);
				}
				return;
			}
			_entriesInManager = _settingsPersister.ReadStoredFrequencies();
			recordTimer.Enabled = true;
			recDisplayTimer.Enabled = true;
			_recordIsStarted = true;
			_simpleRecorder.DiskWriterPause = true;
			_writeAllMbyte = 0f;
			_filesCounter = 0;
			_writeAllSecond = 0f;
			_fileIndexer = 0;
			RecordLabel("Create new file", _foreColor);
		}
		else
		{
			_recordIsStarted = false;
			recordTimer.Enabled = false;
			recDisplayTimer.Enabled = false;
			if (_simpleRecorder.IsRecording)
			{
				RecordStop();
			}
			((Control)recordLabel).Visible = false;
		}
		ConfigureGUI();
	}

	private void recBtn_Paint(object sender, PaintEventArgs e)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Expected O, but got Unknown
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Expected O, but got Unknown
		if (!_isTelerikSDRSharp)
		{
			return;
		}
		Button val = recBtn;
		Rectangle clientRectangle = ((Control)val).ClientRectangle;
		int num = (((int)((ButtonBase)val).FlatStyle == 0) ? (((ButtonBase)val).FlatAppearance.BorderSize * -1) : 0);
		if (num == 0)
		{
			clientRectangle.Inflate(-3, -3);
		}
		else
		{
			clientRectangle.Inflate(num, num);
		}
		string text = ((Control)val).Text;
		Font font = ((Control)val).Font;
		SolidBrush val2 = new SolidBrush(((Control)val).BackColor);
		try
		{
			if (!((Control)val).Enabled)
			{
				e.Graphics.FillRectangle((Brush)(object)val2, clientRectangle);
			}
		}
		finally
		{
			((IDisposable)val2)?.Dispose();
		}
		StringFormat val3 = new StringFormat();
		try
		{
			val3.Alignment = (StringAlignment)1;
			val3.LineAlignment = (StringAlignment)1;
			if (!((Control)val).Enabled)
			{
				ControlPaint.DrawStringDisabled(e.Graphics, text, font, SystemColors.Control, (RectangleF)clientRectangle, val3);
			}
		}
		finally
		{
			((IDisposable)val3)?.Dispose();
		}
	}

	private void SelectFolderDialog_Click(object sender, EventArgs e)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Invalid comparison between Unknown and I4
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		string selectedPath = writeFolderBrowserDialog.SelectedPath;
		Environment.SpecialFolder rootFolder = writeFolderBrowserDialog.RootFolder;
		SendKeys.Send("{TAB}{TAB}{RIGHT}");
		if ((int)((CommonDialog)writeFolderBrowserDialog).ShowDialog() != 1)
		{
			return;
		}
		if (writeFolderBrowserDialog.SelectedPath.StartsWith("\\\\"))
		{
			writeFolderBrowserDialog.SelectedPath = selectedPath;
			writeFolderBrowserDialog.RootFolder = rootFolder;
			MessageBox.Show("AudioRecorder: Use of UNC paths are not supported.\n\nTry using a mapped network path.\nThe previous folder will remain selected.", "Warning", (MessageBoxButtons)0, (MessageBoxIcon)16);
			return;
		}
		DriveInfo driveInfo = new DriveInfo(writeFolderBrowserDialog.SelectedPath.Substring(0, 1).ToString());
		if (driveInfo.DriveType != DriveType.Fixed && driveInfo.DriveType != DriveType.Network)
		{
			writeFolderBrowserDialog.SelectedPath = selectedPath;
			writeFolderBrowserDialog.RootFolder = rootFolder;
			MessageBox.Show("AudioRecorder: Selected folder MUST be a fixed drive\nPlease select again or previous folder will remain selected.", "Warning", (MessageBoxButtons)0, (MessageBoxIcon)16);
		}
	}

	private void selectFolderDialog_Paint(object sender, PaintEventArgs e)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Expected O, but got Unknown
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Expected O, but got Unknown
		if (!_isTelerikSDRSharp)
		{
			return;
		}
		Button val = selectFolderDialog;
		Rectangle clientRectangle = ((Control)val).ClientRectangle;
		int num = (((int)((ButtonBase)val).FlatStyle == 0) ? (((ButtonBase)val).FlatAppearance.BorderSize * -1) : 0);
		if (num == 0)
		{
			clientRectangle.Inflate(-3, -3);
		}
		else
		{
			clientRectangle.Inflate(num, num);
		}
		string text = ((Control)val).Text;
		Font font = ((Control)val).Font;
		SolidBrush val2 = new SolidBrush(((Control)val).BackColor);
		try
		{
			if (!((Control)val).Enabled)
			{
				e.Graphics.FillRectangle((Brush)(object)val2, clientRectangle);
			}
		}
		finally
		{
			((IDisposable)val2)?.Dispose();
		}
		StringFormat val3 = new StringFormat();
		try
		{
			val3.Alignment = (StringAlignment)1;
			val3.LineAlignment = (StringAlignment)1;
			if (!((Control)val).Enabled)
			{
				ControlPaint.DrawStringDisabled(e.Graphics, text, font, SystemColors.Control, (RectangleF)clientRectangle, val3);
			}
		}
		finally
		{
			((IDisposable)val3)?.Dispose();
		}
	}

	private void openFolderButton_Click(object sender, EventArgs e)
	{
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		string text = writeFolderBrowserDialog.SelectedPath;
		if (writeFolderBrowserDialog.SelectedPath.Substring(writeFolderBrowserDialog.SelectedPath.Length - 1, 1) == "\\")
		{
			text = writeFolderBrowserDialog.SelectedPath.Substring(0, writeFolderBrowserDialog.SelectedPath.Length - 1);
		}
		if (Directory.Exists(text))
		{
			Process.Start("explorer.exe", text);
		}
		else
		{
			MessageBox.Show((IWin32Window)(object)this, "Path does not seem valid", "Error", (MessageBoxButtons)0, (MessageBoxIcon)16);
		}
	}

	private void selectFolderDialog_MouseEnter(object sender, EventArgs e)
	{
		toolTip1.SetToolTip((Control)(object)selectFolderDialog, writeFolderBrowserDialog.SelectedPath);
	}

	private void selectFolderDialog_MouseLeave(object sender, EventArgs e)
	{
		toolTip1.SetToolTip((Control)(object)selectFolderDialog, string.Empty);
	}

	private void openFolderButton_MouseEnter(object sender, EventArgs e)
	{
		toolTip1.SetToolTip((Control)(object)openFolderButton, writeFolderBrowserDialog.SelectedPath);
	}

	private void openFolderButton_MouseLeave(object sender, EventArgs e)
	{
		toolTip1.SetToolTip((Control)(object)openFolderButton, string.Empty);
	}

	private void configureButton_Click(object sender, EventArgs e)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		((Form)new DialogConfigure(this)).ShowDialog();
	}

	private void configureButton_Paint(object sender, PaintEventArgs e)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Expected O, but got Unknown
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Expected O, but got Unknown
		if (!_isTelerikSDRSharp)
		{
			return;
		}
		Button val = configureButton;
		Rectangle clientRectangle = ((Control)val).ClientRectangle;
		int num = (((int)((ButtonBase)val).FlatStyle == 0) ? (((ButtonBase)val).FlatAppearance.BorderSize * -1) : 0);
		if (num == 0)
		{
			clientRectangle.Inflate(-3, -3);
		}
		else
		{
			clientRectangle.Inflate(num, num);
		}
		string text = ((Control)val).Text;
		Font font = ((Control)val).Font;
		SolidBrush val2 = new SolidBrush(((Control)val).BackColor);
		try
		{
			if (!((Control)val).Enabled)
			{
				e.Graphics.FillRectangle((Brush)(object)val2, clientRectangle);
			}
		}
		finally
		{
			((IDisposable)val2)?.Dispose();
		}
		StringFormat val3 = new StringFormat();
		try
		{
			val3.Alignment = (StringAlignment)1;
			val3.LineAlignment = (StringAlignment)1;
			if (!((Control)val).Enabled)
			{
				ControlPaint.DrawStringDisabled(e.Graphics, text, font, SystemColors.Control, (RectangleF)clientRectangle, val3);
			}
		}
		finally
		{
			((IDisposable)val3)?.Dispose();
		}
	}

	private void AudioRecorderPanel_Load(object sender, EventArgs e)
	{
		changeColors();
	}

	private void AudioRecorderPanel_VisibleChanged(object sender, EventArgs e)
	{
		changeColors();
	}

	private void recordTimer_Tick(object sender, EventArgs e)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		if (!_recordIsStarted)
		{
			return;
		}
		if (GetTotalFreeSpace() < 10000000)
		{
			recBtn_Click(null, null);
			MessageBox.Show("AudioRecorder: You do not have enough HDD space to continue recording", "ALERT", (MessageBoxButtons)0);
			return;
		}
		if (MaxFileSizeEnable && (double)_simpleRecorder.BytesWritten * 9.5367431640625E-07 >= (double)MaxWriteLength)
		{
			RecordStop();
			RecordStart();
		}
		if (_simpleRecorder.IsStreamFull)
		{
			RecordStop();
			RecordStart();
		}
		if (!WriteOneFile && _simpleRecorder.IsRecording)
		{
			if (_oldFrequency != _control.Frequency && NewFileFrequencyEnable)
			{
				RecordStop();
				RecordLabel("Create new file", _foreColor);
			}
			if (NewFileTimeEnable)
			{
				if (SignalIsActive())
				{
					_newFileWaitTime = NewFileTime;
				}
				else if (_newFileWaitTime > 0)
				{
					_newFileWaitTime -= recordTimer.Interval;
				}
				else
				{
					RecordStop();
					RecordLabel("Create new file", _foreColor);
				}
			}
		}
		if (!DontWritePause && !_simpleRecorder.IsRecording)
		{
			RecordStart();
			_simpleRecorder.DiskWriterPause = false;
			RecordLabel("Record", Color.Red);
		}
		if (!DontWritePause)
		{
			return;
		}
		if (SignalIsActive())
		{
			if (!_simpleRecorder.IsRecording)
			{
				RecordStart();
			}
			_simpleRecorder.DiskWriterPause = false;
			RecordLabel("Record", Color.Red);
			_waitTime = ContinueRecordTime;
		}
		if (!SignalIsActive() && _simpleRecorder.IsRecording)
		{
			if (ContinueRecordTimeEnable && _waitTime > 0)
			{
				_waitTime -= recordTimer.Interval;
				RecordLabel($"Record {(float)((double)_waitTime / 1000.0):f1}", Color.Green);
			}
			else
			{
				_simpleRecorder.DiskWriterPause = true;
				RecordLabel("Pause", _foreColor);
			}
		}
	}

	private void recDisplayTimer_Tick(object sender, EventArgs e)
	{
		float num = (float)_simpleRecorder.BytesWritten * 9.536743E-07f;
		int num2 = 0;
		switch (_simpleRecorder.Format)
		{
		case WavSampleFormat.PCM8Stereo:
			num2 = 2;
			break;
		case WavSampleFormat.PCM16Stereo:
			num2 = 4;
			break;
		case WavSampleFormat.Float32:
			num2 = 8;
			break;
		case WavSampleFormat.PCM8Mono:
			num2 = 1;
			break;
		case WavSampleFormat.PCM16Mono:
			num2 = 2;
			break;
		}
		int second = 0;
		int num3 = (int)_writeAllSecond;
		int num4 = _recordSamplerate;
		if (_recordSamplerate != 0)
		{
			if (SampleFormatSelectedIndex < 3)
			{
				num4 /= 2;
				// num3 /= 2 видалено: _writeAllSecond тепер акумулює коректні значення після виправлення _writeLength
			}
			second = (int)(_simpleRecorder.BytesWritten / num2 / num4);
		}
		((Control)allLabel).Text = $"all {_filesCounter} file(s) {SecondToDate(num3):HH:mm:ss} - {_writeAllMbyte:f3} MB";
		((Control)currentLabel).Text = $"Write: current {SecondToDate(second):HH:mm:ss} - {num:f3} MB ";
		progressBar.Value = Math.Min(_simpleRecorder.BufferUsage, progressBar.Maximum);
		((Control)label1).Text = "Dropped buffers: " + _simpleRecorder.LostBuffers;
		((Control)label3).Visible = _debugEnable;
		if (_debugEnable)
		{
			((Control)label3).Text = "REC SR: " + num4 + " - REC level dB: " + _simpleRecorder.Decibels.ToString("#");
		}
	}

	private void recordLabel_DoubleClick(object sender, EventArgs e)
	{
		_debugEnable = !_debugEnable;
	}
}
