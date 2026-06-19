using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SDRSharp.AudioRecorder;

public class DialogConfigure : Form
{
	private AudioRecorderPanel _recorder;

	private int _samplerateOut;

	private IContainer components;

	private Button btnOk;

	private TabControl tabControl1;

	private TabPage fileOptions;

	private Label label4;

	private CheckBox createNewFileCheckBox;

	private TextBox folderTextBox;

	private NumericUpDown maxWriteLengthMbNumericUpDown;

	private Label label1;

	private ComboBox sampleRateComboBox;

	private ComboBox sampleFormatComboBox;

	private Label sampleFormatLbl;

	private TabPage recorderTabPage;

	private NumericUpDown continueRecordTimeNumericUpDown;

	private CheckBox continueRecordEnableCheckBox;

	private CheckBox dontWritePauseCheckBox;

	private NumericUpDown minWriteLengthNumericUpDown;

	private CheckBox deleteSmallFilesCheckBox;

	private CheckBox newFileTimeEnableCheckBox;

	private CheckBox writeOneFileCheckBox;

	private Label resultLabel;

	private Timer displayTimer;

	private CheckBox createFileIfFrequencyCheckBox;

	private Label label2;

	private NumericUpDown NewFileTimeNumericUpDown;

	private CheckBox autoStartCheckBox;

	private Label label5;

	private Label label3;

	private CheckBox useMuteCheckBox;

	private CheckBox useSquelchCheckBox;

	private Label pluginVersionLabel;

	private Label label9;

	private Label label8;

	private Label label7;

	private Label label6;

	private Label label10;

	private CheckBox useUtcTimestampsCheckBox;

	public DialogConfigure(AudioRecorderPanel recorder)
	{
		_recorder = recorder;
		InitializeComponent();
		autoStartCheckBox.Checked = _recorder.AutoStartRecording;
		AddSamplerate(_recorder.OutputSamplerateArray);
		_samplerateOut = _recorder.SamplerateOut;
		((ListControl)sampleRateComboBox).SelectedIndex = SamplerateSelect(_samplerateOut);
		sampleRateComboBox_SelectedIndexChanged(null, null);
		((ListControl)sampleFormatComboBox).SelectedIndex = _recorder.SampleFormatSelectedIndex;
		writeOneFileCheckBox.Checked = _recorder.WriteOneFile;
		dontWritePauseCheckBox.Checked = _recorder.DontWritePause;
		useSquelchCheckBox.Checked = _recorder.UseSquelch;
		useMuteCheckBox.Checked = _recorder.UseMute;
		continueRecordEnableCheckBox.Checked = _recorder.ContinueRecordTimeEnable;
		continueRecordTimeNumericUpDown.Value = _recorder.ContinueRecordTime / 1000;
		newFileTimeEnableCheckBox.Checked = _recorder.NewFileTimeEnable;
		NewFileTimeNumericUpDown.Value = _recorder.NewFileTime / 1000;
		createFileIfFrequencyCheckBox.Checked = _recorder.NewFileFrequencyEnable;
		createNewFileCheckBox.Checked = _recorder.MaxFileSizeEnable;
		maxWriteLengthMbNumericUpDown.Value = _recorder.MaxWriteLength;
		deleteSmallFilesCheckBox.Checked = _recorder.DeleteSmallFiles;
		minWriteLengthNumericUpDown.Value = (decimal)_recorder.MinWriteLength;
		useUtcTimestampsCheckBox.Checked = _recorder.UseUtcTimestamp;
		((Control)folderTextBox).Text = _recorder.FileNameRules;
		folderTextBox_TextChanged(null, null);
		((Control)pluginVersionLabel).Text = _recorder.PluginVersion;
	}

	private void AddSamplerate(string outputSamplerate)
	{
		sampleRateComboBox.Items.Add((object)"no re-sampling");
		sampleRateComboBox.Items.AddRange((object[])outputSamplerate.Split(new char[1] { ',' }));
	}

	private void sampleRateComboBox_SelectedIndexChanged(object sender, EventArgs e)
	{
		int samplerateOut = 0;
		Match match = Regex.Match(((Control)sampleRateComboBox).Text, "([0-9\\.]+) ([k]?)Hz", RegexOptions.IgnoreCase);
		if (match.Success)
		{
			samplerateOut = (int)(double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) * 1000.0);
		}
		_samplerateOut = samplerateOut;
	}

	private int SamplerateSelect(int samplerate)
	{
		int num = 0;
		foreach (object item in sampleRateComboBox.Items)
		{
			Match match = Regex.Match(item.ToString(), "([0-9\\.]+) ([k]?)Hz", RegexOptions.IgnoreCase);
			if (match.Success && (int)(double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) * 1000.0) == samplerate)
			{
				return num;
			}
			num++;
		}
		return 0;
	}

	private void btnOk_Click(object sender, EventArgs e)
	{
		_recorder.DontWritePause = dontWritePauseCheckBox.Checked;
		_recorder.WriteOneFile = writeOneFileCheckBox.Checked;
		_recorder.UseMute = useMuteCheckBox.Checked;
		_recorder.UseSquelch = useSquelchCheckBox.Checked;
		_recorder.ContinueRecordTimeEnable = continueRecordEnableCheckBox.Checked;
		_recorder.ContinueRecordTime = (int)continueRecordTimeNumericUpDown.Value * 1000;
		_recorder.NewFileTimeEnable = newFileTimeEnableCheckBox.Checked;
		_recorder.NewFileTime = (int)NewFileTimeNumericUpDown.Value * 1000;
		_recorder.NewFileFrequencyEnable = createFileIfFrequencyCheckBox.Checked;
		_recorder.SampleFormatSelectedIndex = ((ListControl)sampleFormatComboBox).SelectedIndex;
		_recorder.SamplerateOut = _samplerateOut;
		_recorder.MaxFileSizeEnable = createNewFileCheckBox.Checked;
		_recorder.MaxWriteLength = (long)maxWriteLengthMbNumericUpDown.Value;
		_recorder.DeleteSmallFiles = deleteSmallFilesCheckBox.Checked;
		_recorder.MinWriteLength = (float)minWriteLengthNumericUpDown.Value;
		_recorder.UseUtcTimestamp = useUtcTimestampsCheckBox.Checked;
		_recorder.FileNameRules = ((Control)folderTextBox).Text;
		_recorder.AutoStartRecording = autoStartCheckBox.Checked;
		((Form)this).DialogResult = (DialogResult)1;
	}

	private void folderTextBox_TextChanged(object sender, EventArgs e)
	{
		((Control)resultLabel).Text = _recorder.ParseStringToPath(((Control)folderTextBox).Text);
	}

	private void displayTimer_Tick(object sender, EventArgs e)
	{
		((Control)continueRecordEnableCheckBox).Enabled = dontWritePauseCheckBox.Checked;
		((Control)continueRecordTimeNumericUpDown).Enabled = dontWritePauseCheckBox.Checked;
		((Control)useMuteCheckBox).Enabled = dontWritePauseCheckBox.Checked;
		((Control)useSquelchCheckBox).Enabled = dontWritePauseCheckBox.Checked;
		((Control)newFileTimeEnableCheckBox).Enabled = !writeOneFileCheckBox.Checked;
		((Control)NewFileTimeNumericUpDown).Enabled = !writeOneFileCheckBox.Checked;
		((Control)createFileIfFrequencyCheckBox).Enabled = !writeOneFileCheckBox.Checked;
		((Control)sampleRateComboBox).Enabled = ((ListControl)sampleFormatComboBox).SelectedIndex > 2;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		((Form)this).Dispose(disposing);
	}

	private void InitializeComponent()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected O, but got Unknown
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected O, but got Unknown
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected O, but got Unknown
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Expected O, but got Unknown
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Expected O, but got Unknown
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Expected O, but got Unknown
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Expected O, but got Unknown
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Expected O, but got Unknown
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Expected O, but got Unknown
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Expected O, but got Unknown
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Expected O, but got Unknown
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Expected O, but got Unknown
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Expected O, but got Unknown
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Expected O, but got Unknown
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Expected O, but got Unknown
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Expected O, but got Unknown
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Expected O, but got Unknown
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Expected O, but got Unknown
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Expected O, but got Unknown
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Expected O, but got Unknown
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Expected O, but got Unknown
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Expected O, but got Unknown
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Expected O, but got Unknown
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Expected O, but got Unknown
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Expected O, but got Unknown
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Expected O, but got Unknown
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Expected O, but got Unknown
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Expected O, but got Unknown
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Expected O, but got Unknown
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_04aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d21: Unknown result type (might be due to invalid IL or missing references)
		//IL_146f: Unknown result type (might be due to invalid IL or missing references)
		components = new Container();
		btnOk = new Button();
		tabControl1 = new TabControl();
		fileOptions = new TabPage();
		useUtcTimestampsCheckBox = new CheckBox();
		label9 = new Label();
		label8 = new Label();
		label2 = new Label();
		resultLabel = new Label();
		minWriteLengthNumericUpDown = new NumericUpDown();
		deleteSmallFilesCheckBox = new CheckBox();
		label4 = new Label();
		createNewFileCheckBox = new CheckBox();
		folderTextBox = new TextBox();
		maxWriteLengthMbNumericUpDown = new NumericUpDown();
		label1 = new Label();
		sampleRateComboBox = new ComboBox();
		sampleFormatComboBox = new ComboBox();
		sampleFormatLbl = new Label();
		recorderTabPage = new TabPage();
		label10 = new Label();
		label7 = new Label();
		label6 = new Label();
		label5 = new Label();
		label3 = new Label();
		useMuteCheckBox = new CheckBox();
		useSquelchCheckBox = new CheckBox();
		autoStartCheckBox = new CheckBox();
		NewFileTimeNumericUpDown = new NumericUpDown();
		createFileIfFrequencyCheckBox = new CheckBox();
		writeOneFileCheckBox = new CheckBox();
		newFileTimeEnableCheckBox = new CheckBox();
		continueRecordTimeNumericUpDown = new NumericUpDown();
		continueRecordEnableCheckBox = new CheckBox();
		dontWritePauseCheckBox = new CheckBox();
		displayTimer = new Timer(components);
		pluginVersionLabel = new Label();
		((Control)tabControl1).SuspendLayout();
		((Control)fileOptions).SuspendLayout();
		((ISupportInitialize)minWriteLengthNumericUpDown).BeginInit();
		((ISupportInitialize)maxWriteLengthMbNumericUpDown).BeginInit();
		((Control)recorderTabPage).SuspendLayout();
		((ISupportInitialize)NewFileTimeNumericUpDown).BeginInit();
		((ISupportInitialize)continueRecordTimeNumericUpDown).BeginInit();
		((Control)this).SuspendLayout();
		((Control)btnOk).Anchor = (AnchorStyles)9;
		btnOk.DialogResult = (DialogResult)1;
		((Control)btnOk).Location = new Point(434, 255);
		((Control)btnOk).Margin = new Padding(2);
		((Control)btnOk).Name = "btnOk";
		((Control)btnOk).Size = new Size(56, 23);
		((Control)btnOk).TabIndex = 7;
		((Control)btnOk).Text = "O&K";
		((ButtonBase)btnOk).UseVisualStyleBackColor = true;
		((Control)btnOk).Click += btnOk_Click;
		((Control)tabControl1).Anchor = (AnchorStyles)13;
		((Control)tabControl1).Controls.Add((Control)(object)fileOptions);
		((Control)tabControl1).Controls.Add((Control)(object)recorderTabPage);
		((Control)tabControl1).Location = new Point(12, 3);
		((Control)tabControl1).Name = "tabControl1";
		tabControl1.SelectedIndex = 0;
		((Control)tabControl1).Size = new Size(478, 247);
		((Control)tabControl1).TabIndex = 26;
		((Control)fileOptions).BackColor = SystemColors.Menu;
		((Control)fileOptions).Controls.Add((Control)(object)useUtcTimestampsCheckBox);
		((Control)fileOptions).Controls.Add((Control)(object)label9);
		((Control)fileOptions).Controls.Add((Control)(object)label8);
		((Control)fileOptions).Controls.Add((Control)(object)label2);
		((Control)fileOptions).Controls.Add((Control)(object)resultLabel);
		((Control)fileOptions).Controls.Add((Control)(object)minWriteLengthNumericUpDown);
		((Control)fileOptions).Controls.Add((Control)(object)deleteSmallFilesCheckBox);
		((Control)fileOptions).Controls.Add((Control)(object)label4);
		((Control)fileOptions).Controls.Add((Control)(object)createNewFileCheckBox);
		((Control)fileOptions).Controls.Add((Control)(object)folderTextBox);
		((Control)fileOptions).Controls.Add((Control)(object)maxWriteLengthMbNumericUpDown);
		((Control)fileOptions).Controls.Add((Control)(object)label1);
		((Control)fileOptions).Controls.Add((Control)(object)sampleRateComboBox);
		((Control)fileOptions).Controls.Add((Control)(object)sampleFormatComboBox);
		((Control)fileOptions).Controls.Add((Control)(object)sampleFormatLbl);
		fileOptions.Location = new Point(4, 22);
		((Control)fileOptions).Name = "fileOptions";
		((Control)fileOptions).Padding = new Padding(3);
		((Control)fileOptions).Size = new Size(470, 221);
		fileOptions.TabIndex = 0;
		((Control)fileOptions).Text = "File options";
		((Control)useUtcTimestampsCheckBox).AutoSize = true;
		((Control)useUtcTimestampsCheckBox).Location = new Point(315, 126);
		((Control)useUtcTimestampsCheckBox).Name = "useUtcTimestampsCheckBox";
		((Control)useUtcTimestampsCheckBox).Size = new Size(120, 17);
		((Control)useUtcTimestampsCheckBox).TabIndex = 56;
		((Control)useUtcTimestampsCheckBox).Text = "Use UTC timestamp";
		((ButtonBase)useUtcTimestampsCheckBox).UseVisualStyleBackColor = true;
		((Control)label9).AutoSize = true;
		((Control)label9).Location = new Point(284, 91);
		((Control)label9).Name = "label9";
		((Control)label9).Size = new Size(52, 13);
		((Control)label9).TabIndex = 55;
		((Control)label9).Text = "[0 - 3600]";
		((Control)label8).AutoSize = true;
		((Control)label8).Location = new Point(284, 65);
		((Control)label8).Name = "label8";
		((Control)label8).Size = new Size(52, 13);
		((Control)label8).TabIndex = 54;
		((Control)label8).Text = "[3 - 2048]";
		((Control)label2).AutoSize = true;
		((Control)label2).Location = new Point(6, 149);
		((Control)label2).Name = "label2";
		((Control)label2).Size = new Size(459, 13);
		((Control)label2).TabIndex = 46;
		((Control)label2).Text = "You can use: date, time, start_time, end_time, length, name, group, frequency, \"any text\", +, \\, /";
		((Control)resultLabel).AutoSize = true;
		((Control)resultLabel).Location = new Point(6, 198);
		((Control)resultLabel).Name = "resultLabel";
		((Control)resultLabel).Size = new Size(13, 13);
		((Control)resultLabel).TabIndex = 45;
		((Control)resultLabel).Text = "_";
		minWriteLengthNumericUpDown.DecimalPlaces = 1;
		((Control)minWriteLengthNumericUpDown).Location = new Point(221, 86);
		minWriteLengthNumericUpDown.Maximum = new decimal(new int[4] { 3600, 0, 0, 0 });
		((Control)minWriteLengthNumericUpDown).Name = "minWriteLengthNumericUpDown";
		((Control)minWriteLengthNumericUpDown).Size = new Size(57, 20);
		((Control)minWriteLengthNumericUpDown).TabIndex = 40;
		((Control)deleteSmallFilesCheckBox).AutoSize = true;
		((Control)deleteSmallFilesCheckBox).Location = new Point(9, 87);
		((Control)deleteSmallFilesCheckBox).Name = "deleteSmallFilesCheckBox";
		((Control)deleteSmallFilesCheckBox).Size = new Size(183, 17);
		((Control)deleteSmallFilesCheckBox).TabIndex = 39;
		((Control)deleteSmallFilesCheckBox).Text = "Delete file if the file size < second";
		((ButtonBase)deleteSmallFilesCheckBox).UseVisualStyleBackColor = true;
		((Control)label4).AutoSize = true;
		((Control)label4).Location = new Point(6, 130);
		((Control)label4).Name = "label4";
		((Control)label4).Size = new Size(143, 13);
		((Control)label4).TabIndex = 37;
		((Control)label4).Text = "Rules for creating file names.";
		((Control)createNewFileCheckBox).AutoSize = true;
		((Control)createNewFileCheckBox).Location = new Point(9, 61);
		((Control)createNewFileCheckBox).Name = "createNewFileCheckBox";
		((Control)createNewFileCheckBox).Size = new Size(196, 17);
		((Control)createNewFileCheckBox).TabIndex = 33;
		((Control)createNewFileCheckBox).Text = "Create a new file if the file size > MB";
		((ButtonBase)createNewFileCheckBox).UseVisualStyleBackColor = true;
		((Control)folderTextBox).Anchor = (AnchorStyles)13;
		((Control)folderTextBox).Location = new Point(9, 168);
		((Control)folderTextBox).Name = "folderTextBox";
		((Control)folderTextBox).Size = new Size(458, 20);
		((Control)folderTextBox).TabIndex = 31;
		((Control)folderTextBox).Text = "/ date / name + \"_\" + frequency / time + \"_\" + name + \"_\" + frequency";
		((Control)folderTextBox).TextChanged += folderTextBox_TextChanged;
		((Control)maxWriteLengthMbNumericUpDown).Location = new Point(221, 60);
		maxWriteLengthMbNumericUpDown.Maximum = new decimal(new int[4] { 2048, 0, 0, 0 });
		maxWriteLengthMbNumericUpDown.Minimum = new decimal(new int[4] { 3, 0, 0, 0 });
		((Control)maxWriteLengthMbNumericUpDown).Name = "maxWriteLengthMbNumericUpDown";
		((Control)maxWriteLengthMbNumericUpDown).Size = new Size(57, 20);
		((Control)maxWriteLengthMbNumericUpDown).TabIndex = 30;
		maxWriteLengthMbNumericUpDown.Value = new decimal(new int[4] { 3, 0, 0, 0 });
		((Control)label1).AutoSize = true;
		((Control)label1).Location = new Point(4, 35);
		((Control)label1).Name = "label1";
		((Control)label1).Size = new Size(60, 13);
		((Control)label1).TabIndex = 29;
		((Control)label1).Text = "Samplerate";
		sampleRateComboBox.DropDownStyle = (ComboBoxStyle)2;
		((ListControl)sampleRateComboBox).FormattingEnabled = true;
		((Control)sampleRateComboBox).Location = new Point(103, 33);
		((Control)sampleRateComboBox).Name = "sampleRateComboBox";
		((Control)sampleRateComboBox).Size = new Size(175, 21);
		((Control)sampleRateComboBox).TabIndex = 28;
		sampleRateComboBox.SelectedIndexChanged += sampleRateComboBox_SelectedIndexChanged;
		sampleFormatComboBox.DropDownStyle = (ComboBoxStyle)2;
		sampleFormatComboBox.DropDownWidth = 120;
		((ListControl)sampleFormatComboBox).FormattingEnabled = true;
		sampleFormatComboBox.Items.AddRange(new object[5] { "8 Bit PCM Stereo", "16 Bit PCM Stereo", "32 Bit IEEE Float Stereo", "8 Bit PCM Mono", "16 Bit PCM Mono" });
		((Control)sampleFormatComboBox).Location = new Point(103, 6);
		((Control)sampleFormatComboBox).Name = "sampleFormatComboBox";
		((Control)sampleFormatComboBox).Size = new Size(175, 21);
		((Control)sampleFormatComboBox).TabIndex = 26;
		((Control)sampleFormatLbl).AutoSize = true;
		((Control)sampleFormatLbl).Location = new Point(4, 9);
		((Control)sampleFormatLbl).Name = "sampleFormatLbl";
		((Control)sampleFormatLbl).Size = new Size(77, 13);
		((Control)sampleFormatLbl).TabIndex = 27;
		((Control)sampleFormatLbl).Text = "Sample Format";
		((Control)recorderTabPage).BackColor = SystemColors.Menu;
		((Control)recorderTabPage).Controls.Add((Control)(object)label10);
		((Control)recorderTabPage).Controls.Add((Control)(object)label7);
		((Control)recorderTabPage).Controls.Add((Control)(object)label6);
		((Control)recorderTabPage).Controls.Add((Control)(object)label5);
		((Control)recorderTabPage).Controls.Add((Control)(object)label3);
		((Control)recorderTabPage).Controls.Add((Control)(object)useMuteCheckBox);
		((Control)recorderTabPage).Controls.Add((Control)(object)useSquelchCheckBox);
		((Control)recorderTabPage).Controls.Add((Control)(object)autoStartCheckBox);
		((Control)recorderTabPage).Controls.Add((Control)(object)NewFileTimeNumericUpDown);
		((Control)recorderTabPage).Controls.Add((Control)(object)createFileIfFrequencyCheckBox);
		((Control)recorderTabPage).Controls.Add((Control)(object)writeOneFileCheckBox);
		((Control)recorderTabPage).Controls.Add((Control)(object)newFileTimeEnableCheckBox);
		((Control)recorderTabPage).Controls.Add((Control)(object)continueRecordTimeNumericUpDown);
		((Control)recorderTabPage).Controls.Add((Control)(object)continueRecordEnableCheckBox);
		((Control)recorderTabPage).Controls.Add((Control)(object)dontWritePauseCheckBox);
		recorderTabPage.Location = new Point(4, 22);
		((Control)recorderTabPage).Name = "recorderTabPage";
		((Control)recorderTabPage).Padding = new Padding(3);
		((Control)recorderTabPage).Size = new Size(470, 221);
		recorderTabPage.TabIndex = 1;
		((Control)recorderTabPage).Text = "Recorder options";
		((Control)label10).AutoSize = true;
		((Control)label10).Location = new Point(217, 76);
		((Control)label10).Name = "label10";
		((Control)label10).Size = new Size(95, 13);
		((Control)label10).TabIndex = 55;
		((Control)label10).Text = "to trigger recording";
		((Control)label7).AutoSize = true;
		((Control)label7).Location = new Point(280, 127);
		((Control)label7).Name = "label7";
		((Control)label7).Size = new Size(89, 13);
		((Control)label7).TabIndex = 54;
		((Control)label7).Text = "seconds [0 - 100]";
		((Control)label6).AutoSize = true;
		((Control)label6).Location = new Point(366, 100);
		((Control)label6).Name = "label6";
		((Control)label6).Size = new Size(89, 13);
		((Control)label6).TabIndex = 53;
		((Control)label6).Text = "seconds [0 - 100]";
		((Control)label5).AutoSize = true;
		((Control)label5).Location = new Point(130, 76);
		((Control)label5).Name = "label5";
		((Control)label5).Size = new Size(25, 13);
		((Control)label5).TabIndex = 52;
		((Control)label5).Text = "and";
		((Control)label3).AutoSize = true;
		((Control)label3).Location = new Point(26, 76);
		((Control)label3).Name = "label3";
		((Control)label3).Size = new Size(26, 13);
		((Control)label3).TabIndex = 51;
		((Control)label3).Text = "Use";
		((Control)useMuteCheckBox).AutoSize = true;
		((Control)useMuteCheckBox).Location = new Point(163, 75);
		((Control)useMuteCheckBox).Name = "useMuteCheckBox";
		((Control)useMuteCheckBox).Size = new Size(49, 17);
		((Control)useMuteCheckBox).TabIndex = 50;
		((Control)useMuteCheckBox).Text = "mute";
		((ButtonBase)useMuteCheckBox).UseVisualStyleBackColor = true;
		((Control)useSquelchCheckBox).AutoSize = true;
		((Control)useSquelchCheckBox).Location = new Point(59, 75);
		((Control)useSquelchCheckBox).Name = "useSquelchCheckBox";
		((Control)useSquelchCheckBox).Size = new Size(63, 17);
		((Control)useSquelchCheckBox).TabIndex = 49;
		((Control)useSquelchCheckBox).Text = "squelch";
		((ButtonBase)useSquelchCheckBox).UseVisualStyleBackColor = true;
		((Control)autoStartCheckBox).AutoSize = true;
		((Control)autoStartCheckBox).Location = new Point(6, 6);
		((Control)autoStartCheckBox).Name = "autoStartCheckBox";
		((Control)autoStartCheckBox).Size = new Size(118, 17);
		((Control)autoStartCheckBox).TabIndex = 48;
		((Control)autoStartCheckBox).Text = "Auto-start recording";
		((ButtonBase)autoStartCheckBox).UseVisualStyleBackColor = true;
		((Control)NewFileTimeNumericUpDown).Location = new Point(212, 123);
		((Control)NewFileTimeNumericUpDown).Name = "NewFileTimeNumericUpDown";
		((Control)NewFileTimeNumericUpDown).Size = new Size(63, 20);
		((Control)NewFileTimeNumericUpDown).TabIndex = 27;
		((Control)createFileIfFrequencyCheckBox).AutoSize = true;
		((Control)createFileIfFrequencyCheckBox).Location = new Point(6, 146);
		((Control)createFileIfFrequencyCheckBox).Name = "createFileIfFrequencyCheckBox";
		((Control)createFileIfFrequencyCheckBox).Size = new Size(236, 17);
		((Control)createFileIfFrequencyCheckBox).TabIndex = 26;
		((Control)createFileIfFrequencyCheckBox).Text = "Create a new file if the frequency is changed";
		((ButtonBase)createFileIfFrequencyCheckBox).UseVisualStyleBackColor = true;
		((Control)writeOneFileCheckBox).AutoSize = true;
		((Control)writeOneFileCheckBox).Location = new Point(6, 29);
		((Control)writeOneFileCheckBox).Name = "writeOneFileCheckBox";
		((Control)writeOneFileCheckBox).Size = new Size(148, 17);
		((Control)writeOneFileCheckBox).TabIndex = 25;
		((Control)writeOneFileCheckBox).Text = "Write all activity in one file";
		((ButtonBase)writeOneFileCheckBox).UseVisualStyleBackColor = true;
		((Control)newFileTimeEnableCheckBox).AutoSize = true;
		((Control)newFileTimeEnableCheckBox).Location = new Point(6, 123);
		((Control)newFileTimeEnableCheckBox).Name = "newFileTimeEnableCheckBox";
		((Control)newFileTimeEnableCheckBox).Size = new Size(201, 17);
		((Control)newFileTimeEnableCheckBox).TabIndex = 23;
		((Control)newFileTimeEnableCheckBox).Text = "Waiting time to create a new file after";
		((ButtonBase)newFileTimeEnableCheckBox).UseVisualStyleBackColor = true;
		((Control)continueRecordTimeNumericUpDown).Location = new Point(304, 98);
		((Control)continueRecordTimeNumericUpDown).Name = "continueRecordTimeNumericUpDown";
		((Control)continueRecordTimeNumericUpDown).Size = new Size(57, 20);
		((Control)continueRecordTimeNumericUpDown).TabIndex = 22;
		continueRecordTimeNumericUpDown.Value = new decimal(new int[4] { 5, 0, 0, 0 });
		((Control)continueRecordEnableCheckBox).AutoSize = true;
		((Control)continueRecordEnableCheckBox).Location = new Point(6, 98);
		((Control)continueRecordEnableCheckBox).Name = "continueRecordEnableCheckBox";
		((Control)continueRecordEnableCheckBox).Size = new Size(293, 17);
		((Control)continueRecordEnableCheckBox).TabIndex = 21;
		((Control)continueRecordEnableCheckBox).Text = "Continue recording after the squelch has been closed for";
		((ButtonBase)continueRecordEnableCheckBox).UseVisualStyleBackColor = true;
		((Control)dontWritePauseCheckBox).AutoSize = true;
		((Control)dontWritePauseCheckBox).Location = new Point(6, 52);
		((Control)dontWritePauseCheckBox).Name = "dontWritePauseCheckBox";
		((Control)dontWritePauseCheckBox).Size = new Size(108, 17);
		((Control)dontWritePauseCheckBox).TabIndex = 20;
		((Control)dontWritePauseCheckBox).Text = "Don't write pause";
		((ButtonBase)dontWritePauseCheckBox).UseVisualStyleBackColor = true;
		displayTimer.Enabled = true;
		displayTimer.Tick += displayTimer_Tick;
		((Control)pluginVersionLabel).AutoSize = true;
		((Control)pluginVersionLabel).Location = new Point(31, 260);
		((Control)pluginVersionLabel).Name = "pluginVersionLabel";
		((Control)pluginVersionLabel).Size = new Size(81, 13);
		((Control)pluginVersionLabel).TabIndex = 27;
		((Control)pluginVersionLabel).Text = "Audio Recorder";
		((Form)this).AcceptButton = (IButtonControl)(object)btnOk;
		((ContainerControl)this).AutoScaleDimensions = new SizeF(6f, 13f);
		((ContainerControl)this).AutoScaleMode = (AutoScaleMode)1;
		((Form)this).ClientSize = new Size(501, 289);
		((Control)this).Controls.Add((Control)(object)pluginVersionLabel);
		((Control)this).Controls.Add((Control)(object)tabControl1);
		((Control)this).Controls.Add((Control)(object)btnOk);
		((Form)this).FormBorderStyle = (FormBorderStyle)6;
		((Form)this).Margin = new Padding(2);
		((Form)this).MaximizeBox = false;
		((Control)this).MaximumSize = new Size(650, 323);
		((Form)this).MinimizeBox = false;
		((Control)this).MinimumSize = new Size(517, 323);
		((Control)this).Name = "DialogConfigure";
		((Form)this).SizeGripStyle = (SizeGripStyle)2;
		((Form)this).StartPosition = (FormStartPosition)1;
		((Control)this).Text = "Configure audio recorder";
		((Control)tabControl1).ResumeLayout(false);
		((Control)fileOptions).ResumeLayout(false);
		((Control)fileOptions).PerformLayout();
		((ISupportInitialize)minWriteLengthNumericUpDown).EndInit();
		((ISupportInitialize)maxWriteLengthMbNumericUpDown).EndInit();
		((Control)recorderTabPage).ResumeLayout(false);
		((Control)recorderTabPage).PerformLayout();
		((ISupportInitialize)NewFileTimeNumericUpDown).EndInit();
		((ISupportInitialize)continueRecordTimeNumericUpDown).EndInit();
		((Control)this).ResumeLayout(false);
		((Control)this).PerformLayout();
	}
}
