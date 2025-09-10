using System;
using System.IO;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using System.Diagnostics;

namespace arzedit
{
    public partial class GuiMainForm : Form
    {
        // 用于保存database子目录选择列表的控件引用
        private CheckedListBox databaseSubDirsCheckedListBox;
        
        // 语言选择下拉框
        private ComboBox languageCombo;
        
        // 版本号标签
        private Label versionLabel;
        
        public GuiMainForm()
        {
            SetupUI();
            InitLanguageSupport();
            
        }

        private void SetupUI()
        {
            // 设置主窗口属性
            //this.Text = "ArzEdit GUI 工具";
            this.Text = "arzedit GUI Tool";
            this.Size = new System.Drawing.Size(900, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new System.Drawing.Size(600, 500);
            this.AllowDrop = true;
            //this.TopMost = true;
            // 设置图标（需要先添加图标文件）
            try
            {
                // 从嵌入的资源中加载图标
                Assembly assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream("arzedit.app.ico"))
                {
                    if (stream != null)
                    {
                        this.Icon = new System.Drawing.Icon(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录错误，但不影响程序运行
                Console.WriteLine("Failed to load icon: " + ex.Message);
            }

            // 创建主面板
            TableLayoutPanel mainPanel = new TableLayoutPanel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.ColumnCount = 1;
            mainPanel.RowCount = 4;
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // 语言选择行
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // 标题行
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 选项卡行
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // 状态栏行versionLabel

            // 创建大标题
            Label titleLabel = new Label();
            titleLabel.Text = "arzedit GUI 工具";
            titleLabel.Name = "MainForm.Title";
            titleLabel.Font = new System.Drawing.Font("微软雅黑", 18, System.Drawing.FontStyle.Bold);
            titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            titleLabel.Dock = DockStyle.Fill;
            //titleLabel.BackColor = System.Drawing.Color.LightBlue;
            titleLabel.ForeColor = System.Drawing.Color.DarkBlue;



            // 创建选项卡控件
            TabControl tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            //tabControl.Font = new System.Drawing.Font("微软雅黑", 10); // 增大选项卡字体
            // 设置选项卡项的大小模式
            tabControl.SizeMode = TabSizeMode.Fixed;
            tabControl.ItemSize = new System.Drawing.Size(120, 36); // 固定选项卡大小
            tabControl.Appearance = TabAppearance.Normal;
            

            // 创建选项卡页面
            TabPage unpackArcTab = CreateUnpackArcTab();
            TabPage packArcTab = CreatePackArcTab();
            TabPage unpackArzTab = CreateUnpackArzTab();
            TabPage packArzTab = CreatePackArzTab();

            // 添加选项卡到控件
            tabControl.TabPages.Add(unpackArcTab);
            tabControl.TabPages.Add(packArcTab);
            tabControl.TabPages.Add(unpackArzTab);
            tabControl.TabPages.Add(packArzTab);


            // 语言切换区域
            var languagePanel = new TableLayoutPanel();
            languagePanel.Dock = DockStyle.Right;
            languagePanel.AutoSize = true;
            languagePanel.ColumnCount = 2;
            languagePanel.RowCount = 1;
            languagePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            languagePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            
            // 地球图标
            var globeLabel = new Label();
            globeLabel.Text = "🌏"; // Unicode地球图标
            globeLabel.Font = new System.Drawing.Font("Segoe UI", 10);
            globeLabel.AutoSize = true;
            globeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            globeLabel.Dock = DockStyle.Fill;
            
            // 语言切换按钮
            languageCombo = new ComboBox();
            languageCombo.Items.Add(new LanguageItem("zh-CN", "简体中文"));
            languageCombo.Items.Add(new LanguageItem("en-US", "English"));

            // 获取注册表值，设置默认选中项
            string initialLangCode = LanguageManager.Instance.GetInitialLanguageCode();
            languageCombo.SelectedItem = languageCombo.Items.Cast<LanguageItem>()
                .First(item => item.Code == initialLangCode);
            
            languageCombo.Width = 100;
            languageCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            
            languagePanel.Controls.Add(globeLabel, 0, 0);
            languagePanel.Controls.Add(languageCombo, 1, 0);

            // 语言切换事件
            languageCombo.SelectedIndexChanged += (s, e) =>
            {
                if (languageCombo.SelectedItem is LanguageItem selectedItem)
                {
                    LanguageManager.Instance.LoadLanguage(selectedItem.Code);
                }
            };
            
          


            // 创建状态栏面板
            TableLayoutPanel statusBarPanel = new TableLayoutPanel();
            statusBarPanel.Dock = DockStyle.Fill;
            statusBarPanel.ColumnCount = 4;
            statusBarPanel.RowCount = 1;
            statusBarPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // 作者信息
            statusBarPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // 弹性空间
            statusBarPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // 使用说明
            statusBarPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // 版本号
            statusBarPanel.BackColor = System.Drawing.SystemColors.Control;
            statusBarPanel.BorderStyle = BorderStyle.None;
            statusBarPanel.Padding = new Padding(0, 0, 0, 3); // 上下各添加3像素内边距，使内容居中显示

            // 创建作者标签
            Label authorLabel = new Label();
            authorLabel.Text = LanguageManager.Instance.GetText("MainForm.AuthorLabel","作者: laozhangggg");
            authorLabel.Name = "MainForm.AuthorLabel";
            authorLabel.AutoSize = true;
            authorLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            authorLabel.Dock = DockStyle.Left;
            authorLabel.Padding = new Padding(10, 0, 0, 0);
            authorLabel.ForeColor = System.Drawing.Color.Gray;

            // 创建使用说明标签
            Label howToUseLabel = new Label();
            howToUseLabel.Text = LanguageManager.Instance.GetText("MainForm.HowToUseLabel","使用说明");
            howToUseLabel.Name = "MainForm.HowToUseLabel";
            howToUseLabel.AutoSize = true;
            howToUseLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            howToUseLabel.Dock = DockStyle.Right;
            howToUseLabel.Padding = new Padding(0, 0, 10, 0);
            
            // 设置使用说明标签可点击样式
            howToUseLabel.Cursor = Cursors.Hand;
            howToUseLabel.Font = new System.Drawing.Font(howToUseLabel.Font, System.Drawing.FontStyle.Underline);
            howToUseLabel.ForeColor = System.Drawing.Color.Gray;
            
            // 添加使用说明点击事件
            howToUseLabel.Click += (s, e) => ShowHowToUse();

            // 创建版本号标签
            versionLabel = new Label();
            versionLabel.Text = $"{Program.GUI_VERSION}";
            versionLabel.Name = "MainForm.VersionLabel";
            versionLabel.AutoSize = true;
            versionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            versionLabel.Dock = DockStyle.Right;
            versionLabel.Padding = new Padding(0, 0, 10, 0);
            
            // 设置版本号标签可点击样式
            versionLabel.Cursor = Cursors.Hand;
            versionLabel.Font = new System.Drawing.Font(versionLabel.Font, System.Drawing.FontStyle.Underline);
            versionLabel.ForeColor = System.Drawing.Color.Gray;
            
            // 添加版本号点击事件
            versionLabel.Click += (s, e) => ShowChangelog();
            
            // 添加标签到状态栏
            statusBarPanel.Controls.Add(authorLabel, 0, 0);
            statusBarPanel.Controls.Add(new Label(), 1, 0); // 占位
            statusBarPanel.Controls.Add(howToUseLabel, 2, 0);
            statusBarPanel.Controls.Add(versionLabel, 3, 0);

            // 添加控件到主面板
            mainPanel.Controls.Add(languagePanel, 0, 0);
            mainPanel.Controls.Add(titleLabel, 0, 1);
            mainPanel.Controls.Add(tabControl, 0, 2);
            mainPanel.Controls.Add(statusBarPanel, 0, 3);
            
            this.Controls.Add(mainPanel);
        }

        private void EnableDragDrop(TextBox textBox)
        {
            textBox.AllowDrop = true;
            textBox.DragEnter += (s, e) => {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.Copy;
                else
                    e.Effect = DragDropEffects.None;
            };
            textBox.DragDrop += (s, e) => {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files.Length > 0)
                    {
                        textBox.Text = files[0];
                    }
                }
            };
        }




        #region 解包ARC选项卡
        
        private TabPage CreateUnpackArcTab()
        {
            TabPage tabPage = new TabPage("解包ARC");
            tabPage.Name = "UnpackArcTab.Title";
            
            // 创建布局面板
            TableLayoutPanel mainPanel = new TableLayoutPanel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.ColumnCount = 1;
            mainPanel.RowCount = 5;
            mainPanel.Padding = new Padding(10);
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            
            // 输入文件选择
            GroupBox inputGroup = new GroupBox();
            inputGroup.Text = "输入文件";
            inputGroup.Name = "Common.InputFilePathGroup";
            inputGroup.Dock = DockStyle.Fill;
            
            TableLayoutPanel inputPanel = new TableLayoutPanel();
            inputPanel.Dock = DockStyle.Fill;
            inputPanel.ColumnCount = 3; // 修改为3列
            inputPanel.RowCount = 1;
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 76));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12)); // 调整浏览按钮宽度
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12)); // 新增查看按钮列
            
            TextBox arcFilePathTextBox = new TextBox();
            arcFilePathTextBox.Name = "arcFilePath";
            arcFilePathTextBox.Dock = DockStyle.Fill;
            EnableDragDrop(arcFilePathTextBox); // 启用拖放
            
            Button browseArcButton = new Button();
            browseArcButton.Text = "浏览";
            browseArcButton.Name = "Common.BrowseButton";
            browseArcButton.Click += (s, e) => BrowseFile(arcFilePathTextBox, "选择ARC文件", "ARC文件|*.arc");
            
            // 添加查看按钮
            Button viewArcButton = new Button();
            viewArcButton.Text = "查看";
            viewArcButton.Name = "Common.ViewButton";
            viewArcButton.Click += (s, e) => ShowArcFileList(arcFilePathTextBox.Text, tabPage);
            
            inputPanel.Controls.Add(arcFilePathTextBox, 0, 0);
            inputPanel.Controls.Add(browseArcButton, 1, 0);
            inputPanel.Controls.Add(viewArcButton, 2, 0); // 添加到第三列
            
            inputGroup.Controls.Add(inputPanel);
            
            // 输出目录显示
            GroupBox outputGroup = new GroupBox();
            outputGroup.Text = "输出目录";
            outputGroup.Name = "Common.OutputDirPathGroup";
            outputGroup.Dock = DockStyle.Fill;
            
            TextBox outputDirTextBox = new TextBox();
            outputDirTextBox.Name = "outputDir";
            outputDirTextBox.ReadOnly = true;
            outputDirTextBox.Dock = DockStyle.Fill;
            
            outputGroup.Controls.Add(outputDirTextBox);
            
            // 自动更新输出目录
            arcFilePathTextBox.TextChanged += (s, e) => {
                if (!string.IsNullOrEmpty(arcFilePathTextBox.Text))
                {
                    string dir = Path.Combine(
                        Path.GetDirectoryName(arcFilePathTextBox.Text),
                        Path.GetFileNameWithoutExtension(arcFilePathTextBox.Text)
                    );
                    outputDirTextBox.Text = dir;
                }
                else
                {
                    outputDirTextBox.Text = "";
                }
            };
            
            // 执行按钮
            Button unpackArcButton = new Button();
            unpackArcButton.Text = "解包ARC";
            unpackArcButton.Name = "UnpackArcTab.ExecuteButton";
            unpackArcButton.Height = 40;
            unpackArcButton.AutoSize = true;
            unpackArcButton.Anchor = AnchorStyles.None;
            unpackArcButton.Font = new System.Drawing.Font(unpackArcButton.Font, System.Drawing.FontStyle.Bold);
            unpackArcButton.Click += async (s, e) => await ExecuteUnpackArc(arcFilePathTextBox.Text, outputDirTextBox.Text, tabPage);
            
            // 进度条
            System.Windows.Forms.ProgressBar progressBar = new System.Windows.Forms.ProgressBar();
            progressBar.Name = "progressBar";
            //progressBar.Height = 5;
            progressBar.Visible = false;
            progressBar.Dock = DockStyle.Fill; // 占满宽度
            
            // 日志输出
            TextBox logTextBox = new TextBox();
            logTextBox.Name = "logOutput";
            logTextBox.Multiline = true;
            logTextBox.ScrollBars = ScrollBars.Both;
            logTextBox.ReadOnly = true;
            logTextBox.Dock = DockStyle.Fill;
            
            // 状态标签
            Label statusLabel = new Label();
            statusLabel.Text = "就绪";
            statusLabel.Name = "Common.StatusLabel";
            statusLabel.Height = 25;
            statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            statusLabel.Dock = DockStyle.Fill; 
            
            // 添加控件到主面板
            mainPanel.Controls.Add(inputGroup, 0, 0);
            mainPanel.Controls.Add(outputGroup, 0, 1);
            mainPanel.Controls.Add(unpackArcButton, 0, 2);
            mainPanel.Controls.Add(progressBar, 0, 3);
            mainPanel.Controls.Add(statusLabel, 0, 4);
            mainPanel.Controls.Add(logTextBox, 0, 5);
            
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // inputGroup
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // outputGroup
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // button
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 10)); // progress
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // status
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // log
            
            tabPage.Controls.Add(mainPanel);
            
            return tabPage;
        }
        
        #endregion

        #region 打包ARC选项卡
        
        private TabPage CreatePackArcTab()
        {
            TabPage tabPage = new TabPage("打包ARC");
            tabPage.Name = "PackArcTab.Title";


            
            // 创建布局面板
            TableLayoutPanel mainPanel = new TableLayoutPanel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.ColumnCount = 1;
            mainPanel.RowCount = 5;
            mainPanel.Padding = new Padding(10);
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            
            // 输入目录选择
            GroupBox inputGroup = new GroupBox();
            inputGroup.Text = "输入目录";
            inputGroup.Name = "Common.InputDirPathGroup";
            inputGroup.Dock = DockStyle.Fill;
            
            
            TableLayoutPanel inputPanel = new TableLayoutPanel();
            inputPanel.Dock = DockStyle.Fill;
            inputPanel.ColumnCount = 2;
            inputPanel.RowCount = 1;
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 88));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12));
            
            TextBox inputDirTextBox = new TextBox();
            inputDirTextBox.Name = "inputDir";
            inputDirTextBox.Dock = DockStyle.Fill;
            EnableDragDrop(inputDirTextBox); // 启用拖放
            
            Button browseDirButton = new Button();
            browseDirButton.Text = "浏览";
            browseDirButton.Name = "Common.BrowseButton";
            browseDirButton.Click += (s, e) => BrowseFolder(
                inputDirTextBox, 
                LanguageManager.Instance.GetText("Common.BrowseFolderDescription", "选择文件夹")
                );


            
            inputPanel.Controls.Add(inputDirTextBox, 0, 0);
            inputPanel.Controls.Add(browseDirButton, 1, 0);
            
            inputGroup.Controls.Add(inputPanel);
            
            // 输出文件显示
            GroupBox outputGroup = new GroupBox();
            outputGroup.Text = "输出文件";
            outputGroup.Name = "Common.OutputFilePathGroup";
            outputGroup.Dock = DockStyle.Fill;
            
            TableLayoutPanel outputPanel = new TableLayoutPanel();
            outputPanel.Dock = DockStyle.Fill;
            outputPanel.ColumnCount = 2;
            outputPanel.RowCount = 1;
            outputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 88));
            outputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12));
            
            TextBox outputFileTextBox = new TextBox();
            outputFileTextBox.Name = "outputFile";
            outputFileTextBox.ReadOnly = true;
            outputFileTextBox.Dock = DockStyle.Fill;
            
            // 添加查看按钮
            Button viewArcButton = new Button();
            viewArcButton.Text = "查看";
            viewArcButton.Name = "Common.ViewButton";

            viewArcButton.Click += (s, e) => {
                if (File.Exists(outputFileTextBox.Text))
                {
                    ShowArcFileList(outputFileTextBox.Text, tabPage);
                }
                else
                {
                    MessageBox.Show(
                        LanguageManager.Instance.GetText("Common.MessageArcFileNotFound", "ARC文件不存在，请先打包"),
                        LanguageManager.Instance.GetText("Common.MessageBoxTitle.Info", "消息"), 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            
            outputPanel.Controls.Add(outputFileTextBox, 0, 0);
            outputPanel.Controls.Add(viewArcButton, 1, 0);
            outputGroup.Controls.Add(outputPanel);
            
            // 自动更新输出文件路径
            inputDirTextBox.TextChanged += (s, e) => {
                if (!string.IsNullOrEmpty(inputDirTextBox.Text))
                {
                    string file = Path.Combine(
                        Path.GetDirectoryName(inputDirTextBox.Text),
                        Path.GetFileName(inputDirTextBox.Text) + ".arc"
                    );
                    outputFileTextBox.Text = file;
                }
                else
                {
                    outputFileTextBox.Text = "";
                }
            };
            
            // 执行按钮
            Button packArcButton = new Button();
            packArcButton.Text = "打包ARC";
            packArcButton.Name = "PackArcTab.ExecuteButton";
            packArcButton.Height = 40;
            packArcButton.AutoSize = true;
            packArcButton.Anchor = AnchorStyles.None;
            packArcButton.Font = new System.Drawing.Font(packArcButton.Font, System.Drawing.FontStyle.Bold);
            packArcButton.Click += async (s, e) => await ExecutePackArc(inputDirTextBox.Text, outputFileTextBox.Text, tabPage);
            
            // 进度条
            System.Windows.Forms.ProgressBar progressBar = new System.Windows.Forms.ProgressBar();
            progressBar.Name = "progressBar";
            //progressBar.Height = 5;
            progressBar.Visible = false;
            progressBar.Dock = DockStyle.Fill; // 占满宽度
            
            // 日志输出
            TextBox logTextBox = new TextBox();
            logTextBox.Name = "logOutput";
            logTextBox.Multiline = true;
            logTextBox.ScrollBars = ScrollBars.Both;
            logTextBox.ReadOnly = true;
            logTextBox.Dock = DockStyle.Fill;
            
            // 状态标签
            Label statusLabel = new Label();
            statusLabel.Text = "就绪";
            statusLabel.Name = "Common.StatusLabel";
            statusLabel.Height = 25;
            statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            statusLabel.Dock = DockStyle.Fill;
            
            // 添加控件到主面板
            mainPanel.Controls.Add(inputGroup, 0, 0);
            mainPanel.Controls.Add(outputGroup, 0, 1);
            mainPanel.Controls.Add(packArcButton, 0, 2);
            mainPanel.Controls.Add(progressBar, 0, 3);
            mainPanel.Controls.Add(statusLabel, 0, 4);
            mainPanel.Controls.Add(logTextBox, 0, 5);
            
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // inputGroup
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // outputGroup
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // button
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 10)); // progress
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // status
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // log
            
            tabPage.Controls.Add(mainPanel);
            
            return tabPage;
        }
        
        #endregion

        #region 解包ARZ选项卡
        
        private TabPage CreateUnpackArzTab()
        {
            TabPage tabPage = new TabPage("解包ARZ");
            tabPage.Name = "UnpackArzTab.Title";

            
            // 创建布局面板
            TableLayoutPanel mainPanel = new TableLayoutPanel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.ColumnCount = 1;
            mainPanel.RowCount = 5;
            mainPanel.Padding = new Padding(10);
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            
            // 输入文件选择
            GroupBox inputGroup = new GroupBox();
            inputGroup.Text = "输入文件";
            inputGroup.Name = "Common.InputFilePathGroup";
            inputGroup.Dock = DockStyle.Fill;
            
            TableLayoutPanel inputPanel = new TableLayoutPanel();
            inputPanel.Dock = DockStyle.Fill;
            inputPanel.ColumnCount = 3;  // 修改为3列以容纳查看按钮
            inputPanel.RowCount = 1;
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 76));  // 调整比例
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12));  // 新增列
            
            TextBox arzFilePathTextBox = new TextBox();
            arzFilePathTextBox.Name = "arzFilePath";
            arzFilePathTextBox.Dock = DockStyle.Fill;
            EnableDragDrop(arzFilePathTextBox); // 启用拖放
            
            Button browseArzButton = new Button();
            browseArzButton.Text = "浏览";
            browseArzButton.Name = "Common.BrowseButton";
            browseArzButton.Click += (s, e) => BrowseFile(arzFilePathTextBox, "选择ARZ文件", "ARZ文件|*.arz");
            
            // 新增：ARZ查看按钮
            Button viewArzButton = new Button();
            viewArzButton.Text = "查看";
            viewArzButton.Name = "Common.ViewButton";
            viewArzButton.Click += (s, e) => {
                if (File.Exists(arzFilePathTextBox.Text))
                {
                    ShowArzFileStructure(arzFilePathTextBox.Text, tabPage);
                }
                else
                {
                    MessageBox.Show(
                        LanguageManager.Instance.GetText("Common.MessageArzFileNotFound","ARZ文件未找到。请检查文件路径是否正确。"),
                        LanguageManager.Instance.GetText("Common.MessageTitle.Error","错误"),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                }
            };
            
            inputPanel.Controls.Add(arzFilePathTextBox, 0, 0);
            inputPanel.Controls.Add(browseArzButton, 1, 0);
            inputPanel.Controls.Add(viewArzButton, 2, 0);  // 添加到新列
            
            inputGroup.Controls.Add(inputPanel);
            
            // 输出目录显示
            GroupBox outputGroup = new GroupBox();
            outputGroup.Text = "输出目录";
            outputGroup.Name = "Common.OutputDirPathGroup";
            outputGroup.Dock = DockStyle.Fill;
            
            TextBox outputDirTextBox = new TextBox();
            outputDirTextBox.Name = "outputDir";
            outputDirTextBox.ReadOnly = true;
            outputDirTextBox.Dock = DockStyle.Fill;
            
            outputGroup.Controls.Add(outputDirTextBox);
            
            // 自动更新输出目录
            arzFilePathTextBox.TextChanged += (s, e) => {
                if (!string.IsNullOrEmpty(arzFilePathTextBox.Text))
                {
                    string dir = Path.GetDirectoryName(arzFilePathTextBox.Text);
                    outputDirTextBox.Text = dir;
                }
                else
                {
                    outputDirTextBox.Text = "";
                }
            };
            
            // 执行按钮
            Button unpackArzButton = new Button();
            unpackArzButton.Text = "解包ARZ";
            unpackArzButton.Name = "UnpackArzTab.ExecuteButton";
            unpackArzButton.Height = 40;
            unpackArzButton.AutoSize = true;
            unpackArzButton.Anchor = AnchorStyles.None;
            unpackArzButton.Font = new System.Drawing.Font(unpackArzButton.Font, System.Drawing.FontStyle.Bold);
            unpackArzButton.Click += async (s, e) => await ExecuteUnpackArz(arzFilePathTextBox.Text, outputDirTextBox.Text, tabPage);
            
            // 进度条
            System.Windows.Forms.ProgressBar progressBar = new System.Windows.Forms.ProgressBar();
            progressBar.Name = "progressBar";
            //progressBar.Height = 5;
            progressBar.Visible = false;
            progressBar.Dock = DockStyle.Fill; // 占满宽度
            
            // 日志输出
            TextBox logTextBox = new TextBox();
            logTextBox.Name = "logOutput";
            logTextBox.Multiline = true;
            logTextBox.ScrollBars = ScrollBars.Both;
            logTextBox.ReadOnly = true;
            logTextBox.Dock = DockStyle.Fill;
            
            // 状态标签
            Label statusLabel = new Label();
            statusLabel.Text = "就绪";
            statusLabel.Name = "Common.StatusLabel";
            statusLabel.Height = 25;
            statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            statusLabel.Dock = DockStyle.Fill;
            
            // 添加控件到主面板
            mainPanel.Controls.Add(inputGroup, 0, 0);
            mainPanel.Controls.Add(outputGroup, 0, 1);
            mainPanel.Controls.Add(unpackArzButton, 0, 2);
            mainPanel.Controls.Add(progressBar, 0, 3);
            mainPanel.Controls.Add(statusLabel, 0, 4);
            mainPanel.Controls.Add(logTextBox, 0, 5);
            
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // inputGroup
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // outputGroup
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // button
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 10)); // progress
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // status
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // log
            
            tabPage.Controls.Add(mainPanel);
            
            return tabPage;
        }
        
        #endregion

        #region 打包ARZ选项卡
        
        private TabPage CreatePackArzTab()
        {
            TabPage tabPage = new TabPage("打包ARZ");
            tabPage.Name = "PackArzTab.Title";

            
            // 创建布局面板 - 上中下三部分
            TableLayoutPanel mainPanel = new TableLayoutPanel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Padding = new Padding(10);
            
            // 顶部使用介绍说明面板
            TableLayoutPanel introPanel = new TableLayoutPanel();
            introPanel.Dock = DockStyle.Fill;
            introPanel.RowCount = 1;
            
            Label introLabel = new Label();
            introLabel.Text = "模板说明：本工具内置模板837个，包含游戏模板和大部分mod模板，由tt300提供。版本2025-7-8.\n优先顺序：自定义模板（高） > mod模板（中）> 内置模板（低）";
            introLabel.Name = "PackArzTab.TemplateInfoLabel";
            introLabel.ForeColor = System.Drawing.Color.Red;
            introLabel.AutoSize = false;
            introLabel.Dock = DockStyle.Fill;
            introLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            introLabel.MaximumSize = new System.Drawing.Size(int.MaxValue, 60);
            introLabel.AutoSize = true;
            
            introPanel.Controls.Add(introLabel);
            
            // 输入目录选择
            GroupBox inputGroup = new GroupBox();
            inputGroup.Text = "输入mod目录 - 一般为MOD根目录如：Grim Dawn/mods/modName，会自动加载该目录下的*.tpl模板文件";
            inputGroup.Name = "PackArzTab.InputDirPathGroup";
            inputGroup.Dock = DockStyle.Fill;
            
            TableLayoutPanel inputPanel = new TableLayoutPanel();
            inputPanel.Dock = DockStyle.Fill;
            inputPanel.ColumnCount = 2;
            inputPanel.RowCount = 1;
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 86));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14));
            
            TextBox inputDirTextBox = new TextBox();
            inputDirTextBox.Name = "inputDir";
            inputDirTextBox.Dock = DockStyle.Fill;
            EnableDragDrop(inputDirTextBox); // 启用拖放
            
            Button browseDirButton = new Button();
            browseDirButton.Text = "浏览";
            browseDirButton.Name = "Common.BrowseButton";
            browseDirButton.Click += (s, e) => BrowseFolder(
                inputDirTextBox, 
                LanguageManager.Instance.GetText("Common.BrowseFolderDescription", "选择文件夹")
                );
            
            inputPanel.Controls.Add(inputDirTextBox, 0, 0);
            inputPanel.Controls.Add(browseDirButton, 1, 0);
            
            inputGroup.Controls.Add(inputPanel);
            
            // 模板目录选择（可选）
            GroupBox templateGroup = new GroupBox();
            templateGroup.Text = "模板目录（可选）";
            templateGroup.Name = "PackArzTab.TemplateDirPathGroup";

            templateGroup.Dock = DockStyle.Fill;
            
            TableLayoutPanel templatePanel = new TableLayoutPanel();
            templatePanel.Dock = DockStyle.Fill;
            templatePanel.ColumnCount = 2;
            templatePanel.RowCount = 1;
            templatePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 86));
            templatePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14));
            
            TextBox templateDirTextBox = new TextBox();
            templateDirTextBox.Name = "templateDir";
            templateDirTextBox.Dock = DockStyle.Fill;
            EnableDragDrop(templateDirTextBox); // 启用拖放
            
            Button browseTemplateButton = new Button();
            browseTemplateButton.Text = "浏览";
            browseTemplateButton.Name = "Common.BrowseButton";

            browseTemplateButton.Click += (s, e) => BrowseFolder(
                templateDirTextBox, 
                LanguageManager.Instance.GetText("Common.BrowseFolderDescription", "选择文件夹")
                );
            
            templatePanel.Controls.Add(templateDirTextBox, 0, 0);
            templatePanel.Controls.Add(browseTemplateButton, 1, 0);
            
            templateGroup.Controls.Add(templatePanel);
            
            // 输出文件显示
            GroupBox outputGroup = new GroupBox();
            outputGroup.Text = "输出文件";
            outputGroup.Name = "Common.OutputFilePathGroup";
            outputGroup.Dock = DockStyle.Fill;
            
            TableLayoutPanel outputPanel = new TableLayoutPanel();  // 新增面板
            outputPanel.Dock = DockStyle.Fill;
            outputPanel.ColumnCount = 2;
            outputPanel.RowCount = 1;
            outputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 86));
            outputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14));
            
            TextBox outputFileTextBox = new TextBox();
            outputFileTextBox.Name = "outputFile";
            outputFileTextBox.ReadOnly = true;
            outputFileTextBox.Dock = DockStyle.Fill;
            
            // 新增：ARZ查看按钮
            Button viewArzButton = new Button();
            viewArzButton.Text = "查看";
            viewArzButton.Name = "Common.ViewButton";
            viewArzButton.Click += (s, e) => {
                if (File.Exists(outputFileTextBox.Text))
                {
                    ShowArzFileStructure(outputFileTextBox.Text, tabPage);
                }
                else
                {
                    MessageBox.Show(
                        LanguageManager.Instance.GetText("Common.MessageArzFileNotFound", "ARZ文件不存在，请检查"), 
                        LanguageManager.Instance.GetText("Common.MessageBoxTitle.Info", "消息"), 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            
            outputPanel.Controls.Add(outputFileTextBox, 0, 0);
            outputPanel.Controls.Add(viewArzButton, 1, 0);
            outputGroup.Controls.Add(outputPanel);
            
            // 自动更新输出文件路径
            inputDirTextBox.TextChanged += (s, e) => {
                if (!string.IsNullOrEmpty(inputDirTextBox.Text))
                {
                    string modName = Path.GetFileName(inputDirTextBox.Text);
                    string outputFile = Path.Combine(inputDirTextBox.Text, "database", modName + ".arz");
                    outputFileTextBox.Text = outputFile;
                    
                    // 加载database子目录
                    LoadDatabaseSubDirectories(inputDirTextBox.Text);
                }
                else
                {
                    outputFileTextBox.Text = "";
                    databaseSubDirsCheckedListBox.Items.Clear();
                }
            };
            
            // 加载数据库子目录列表
            void LoadDatabaseSubDirectories(string modDir)
            {
                databaseSubDirsCheckedListBox.Items.Clear();
                
                if (string.IsNullOrEmpty(modDir) || !Directory.Exists(modDir))
                    return;
                
                string databaseDir = Path.Combine(modDir, "database");
                if (!Directory.Exists(databaseDir))
                    return;
                
                try
                {
                    // 获取database目录下的所有直接子目录
                    string[] subDirs = Directory.GetDirectories(databaseDir, "*", SearchOption.TopDirectoryOnly);
                    
                    foreach (string subDir in subDirs)
                    {
                        string subDirName = Path.GetFileName(subDir);
                        // 不包括.arz文件所在的目录（如果有）
                        if (subDirName != "output")
                        {
                            // 只默认选中records文件夹，其他不选
                            bool isChecked = subDirName.Equals("records", StringComparison.OrdinalIgnoreCase);
                            databaseSubDirsCheckedListBox.Items.Add(subDirName, isChecked);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error loading database subdirectories: " + ex.Message);
                }
            };
            
            // 执行按钮
            Button packArzButton = new Button();
            packArzButton.Text = "打包ARZ";
            packArzButton.Name = "PackArzTab.ExecuteButton";
            packArzButton.Height = 40;
            packArzButton.AutoSize = true;
            packArzButton.Anchor = AnchorStyles.None;
            packArzButton.Font = new System.Drawing.Font(packArzButton.Font, System.Drawing.FontStyle.Bold);
            packArzButton.Click += async (s, e) => await ExecutePackArz(inputDirTextBox.Text, "", templateDirTextBox.Text, outputFileTextBox.Text, tabPage);
            
            // 数据库子目录选择
            TableLayoutPanel databaseSubDirsPanel = new TableLayoutPanel();
            databaseSubDirsPanel.Dock = DockStyle.Fill;
            databaseSubDirsPanel.MaximumSize = new System.Drawing.Size(int.MaxValue, 260); // 设置最大高度为120像素
            databaseSubDirsPanel.AutoSize = true;
            databaseSubDirsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            databaseSubDirsPanel.ColumnCount = 1;
            databaseSubDirsPanel.RowCount = 2;
            databaseSubDirsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
            databaseSubDirsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            
            // 数据库子目录标签
            Label databaseSubDirsLabel = new Label();
            databaseSubDirsLabel.Text = LanguageManager.Instance.GetText("PackArzTab.DatabaseSubDirsLabel", "选择要打包的子目录:");
            databaseSubDirsLabel.Name = "PackArzTab.DatabaseSubDirsLabel";
            databaseSubDirsLabel.AutoSize = true;
            databaseSubDirsLabel.Dock = DockStyle.Left;
            
            // 数据库子目录选择列表
            databaseSubDirsCheckedListBox = new CheckedListBox();
            databaseSubDirsCheckedListBox.Name = "databaseSubDirsList";
            databaseSubDirsCheckedListBox.Dock = DockStyle.Fill;
            databaseSubDirsCheckedListBox.MultiColumn = false;
            databaseSubDirsCheckedListBox.CheckOnClick = true;
            
            // 添加控件到子目录面板
            databaseSubDirsPanel.Controls.Add(databaseSubDirsLabel, 0, 0);
            databaseSubDirsPanel.Controls.Add(databaseSubDirsCheckedListBox, 0, 1);
            
            // 进度条
            System.Windows.Forms.ProgressBar progressBar = new System.Windows.Forms.ProgressBar();
            progressBar.Name = "progressBar";
            //progressBar.Height = 5;
            progressBar.Visible = false;
            progressBar.Dock = DockStyle.Fill;
            
            // 日志输出
            TextBox logTextBox = new TextBox();
            logTextBox.Name = "logOutput";
            logTextBox.Multiline = true;
            logTextBox.ScrollBars = ScrollBars.Both;
            logTextBox.ReadOnly = true;
            logTextBox.Dock = DockStyle.Fill;
            
            // 状态标签
            Label statusLabel = new Label();
            statusLabel.Text = "就绪";
            statusLabel.Name = "Common.StatusLabel";
            statusLabel.Height = 25;
            statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            statusLabel.Dock = DockStyle.Fill;
            
            // 修改为左右布局
            mainPanel.ColumnCount = 2;
            mainPanel.ColumnStyles.Clear(); // 清除原有样式
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20)); // 右侧窄一些
            
            // 左侧面板 - 放置主要功能控件
            TableLayoutPanel leftPanel = new TableLayoutPanel();
            leftPanel.Dock = DockStyle.Fill;
            leftPanel.RowCount = 6;
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // inputGroup
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // templateGroup
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // outputGroup
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // button
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // progress + status
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // log
            
            // 添加控件到左侧面板
            leftPanel.Controls.Add(inputGroup, 0, 0);
            leftPanel.Controls.Add(templateGroup, 0, 1);
            leftPanel.Controls.Add(outputGroup, 0, 2);
            leftPanel.Controls.Add(packArzButton, 0, 3);
            
            // 进度条和状态标签面板
            TableLayoutPanel progressStatusPanel = new TableLayoutPanel();
            progressStatusPanel.Dock = DockStyle.Fill;
            progressStatusPanel.ColumnCount = 1;
            progressStatusPanel.RowCount = 2;
            progressStatusPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 10)); // progress
            progressStatusPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // status
            progressStatusPanel.Controls.Add(progressBar, 0, 0);
            progressStatusPanel.Controls.Add(statusLabel, 0, 1);
            
            leftPanel.Controls.Add(progressStatusPanel, 0, 4);
            
            // 添加控件到主面板 - 上中下布局
            // 1. 顶部介绍说明
            mainPanel.Controls.Add(introPanel, 0, 0);
            mainPanel.SetColumnSpan(introPanel, 2);
            
            // 2. 中间主要功能部分
            mainPanel.Controls.Add(leftPanel, 0, 1);
            mainPanel.Controls.Add(databaseSubDirsPanel, 1, 1); // 右侧放置数据库子目录选择
            
            // 3. 底部日志部分
            mainPanel.Controls.Add(logTextBox, 0, 2);
            mainPanel.SetColumnSpan(logTextBox, 2);
            
            // 设置主面板行样式 - 上中下三部分
            mainPanel.RowCount = 3;
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // 顶部介绍说明部分
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40)); // 中间主要功能部分
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 26)); // 底部日志部分
            
            tabPage.Controls.Add(mainPanel);
            
            return tabPage;
        }
        
        #endregion

        #region ARZ文件结构查看相关方法
        /// <summary>
        /// 显示ARZ文件的树形结构
        /// </summary>
        private void ShowArzFileStructure(string arzFilePath, TabPage tabPage)
        {
            if (string.IsNullOrEmpty(arzFilePath) || !File.Exists(arzFilePath))
            {
                MessageBox.Show(
                    LanguageManager.Instance.GetText("Common.MessageArzFileNotFound", "ARZ文件不存在，请检查"), 
                    LanguageManager.Instance.GetText("Common.MessageBoxTitle.Error", "错误"), 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var statusLabel = tabPage.Controls.Find("Common.StatusLabel", true)[0] as Label;
            //statusLabel.Text = "正在解析ARZ文件结构...";
            statusLabel.Text = LanguageManager.Instance.GetText("Common.StatusLabel.Parsing", "解析中...");
            
            try
            {
                // 1. 读取ARZ所有条目
                List<string> allEntries;
                using (FileStream fs = new FileStream(arzFilePath, FileMode.Open, FileAccess.Read))
                {
                    ARZFile arzFile = new ARZFile();
                    arzFile.ReadStream(fs);
                    allEntries = arzFile.ListAllEntries();
                }

                if (allEntries.Count == 0)
                {
                    //MessageBox.Show("ARZ文件中没有找到任何条目", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    MessageBox.Show(
                        LanguageManager.Instance.GetText("Common.MessageFileNotFoundrecord", "ARZ文件中没有找到任何条目"), 

                        LanguageManager.Instance.GetText("Common.MessageBoxTitle.Info", "消息"), 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    return;
                }

                // 2. 构建层级目录结构并保存原始节点（用于搜索过滤）
                var originalRootNodes = BuildHierarchicalStructure(allEntries);

                // 3. 创建带TreeView的弹窗
                using (Form structureForm = new Form())
                {
                    //structureForm.Text = $"ARZ文件结构 - {Path.GetFileName(arzFilePath)} ({allEntries.Count}个条目)";
                    structureForm.Text = $"{LanguageManager.Instance.GetText("Common.MessageArzFileStructureTitle", "ARZ文件结构")} - {Path.GetFileName(arzFilePath)} ({allEntries.Count})";
                    structureForm.Size = new System.Drawing.Size(800, 600);
                    structureForm.StartPosition = FormStartPosition.CenterParent;
                    structureForm.Owner = this;
                    //structureForm.TopMost = true;

                    // 创建搜索框容器面板（用于放置标签和搜索框）
                    TableLayoutPanel searchPanel = new TableLayoutPanel();
                    searchPanel.Dock = DockStyle.Top;
                    searchPanel.Height = 30;
                    searchPanel.ColumnCount = 2;
                    searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50)); // 标签固定宽度
                    searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // 搜索框占剩余宽度
                    searchPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 设置行样式以确保垂直对齐

                    // 创建"搜索："标签
                    Label searchLabel = new Label();
                    searchLabel.Text = LanguageManager.Instance.GetText("Common.SearchLabel", "搜索：");
                    searchLabel.Name = "Common.SearchLabel";
                    searchLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                    searchLabel.Dock = DockStyle.Fill; // 设置标签停靠以确保垂直填充
                    searchLabel.AutoSize = false; // 允许自定义大小
                    searchLabel.Height = 22; // 设置合适的高度
                    
                    // 创建搜索框
                    TextBox searchBox = new TextBox();
                    searchBox.Dock = DockStyle.Fill; // 设置搜索框停靠以确保垂直填充
                    searchBox.Margin = new Padding(0, 4, 0, 4); // 设置上下边距以调整垂直位置
                    searchBox.Height = 22; // 设置合适的高度
                   // searchBox.ForeColor = Color.Black;
                   // 将标签和搜索框添加到容器面板
                    searchPanel.Controls.Add(searchLabel, 0, 0);
                    searchPanel.SetRowSpan(searchLabel, 1); // 确保标签只占用一行
                    searchPanel.Controls.Add(searchBox, 1, 0);
                    searchPanel.SetRowSpan(searchBox, 1); // 确保搜索框只占用一行

                    // 创建TreeView控件
                    TreeView treeView = new TreeView();
                    treeView.Dock = DockStyle.Fill;
                    treeView.ShowLines = true;
                    treeView.ShowPlusMinus = true;
                    treeView.ShowRootLines = true;
                    treeView.PathSeparator = "/";
                    treeView.ItemHeight = 22;

                    // 初始加载根节点
                    PopulateTreeView(treeView, originalRootNodes);

                    // 搜索文本变化事件（通过重建节点实现过滤）
                    searchBox.TextChanged += (s, e) =>
                    {
                        string searchText = searchBox.Text.Trim().ToLower();
                        var filteredNodes = FilterNodeData(originalRootNodes, searchText);
                        PopulateTreeView(treeView, filteredNodes);
                    };

                    // 节点展开事件（延迟加载子节点）
                    treeView.BeforeExpand += (s, e) =>
                    {
                        //if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Text == "加载中...")
                        if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Text == LanguageManager.Instance.GetText("Common.Loading", "加载中..."))
                        {
                            e.Node.Nodes.Clear();
                            var currentNodeData = e.Node.Tag as NodeData;
                            if (currentNodeData?.Children != null)
                            {
                                foreach (var childData in currentNodeData.Children)
                                {
                                    e.Node.Nodes.Add(CreateTreeNode(childData));
                                }
                            }
                        }
                    };

                    // 添加控件到窗体
                    structureForm.Controls.Add(treeView);
                    structureForm.Controls.Add(searchPanel);

                    structureForm.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"读取ARZ文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show(
                    LanguageManager.Instance.GetText("Common.MessageBoxReadFail", "读取ARZ文件失败"), 
                    LanguageManager.Instance.GetText("Common.MessageBoxTitle.Error", "错误"), 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                statusLabel.Text = LanguageManager.Instance.GetText("Common.StatusLabel", "就绪");
            }
        }



        // 辅助类：树形节点数据
        private class NodeData
        {
            public string Name { get; set; }
            public bool IsDirectory { get; set; }
            public List<NodeData> Children { get; set; } = new List<NodeData>();
            public int TotalItemCount { get; set; } // 包含子目录的总条目数
        }

        // 构建层级目录结构
        private List<NodeData> BuildHierarchicalStructure(List<string> entries)
        {
            var root = new List<NodeData>();
            var pathCache = new Dictionary<string, NodeData>(); // 缓存路径对应的节点，避免重复创建

            foreach (var entry in entries)
            {
                var parts = entry.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                NodeData currentLevel = null;
                List<NodeData> currentParent = root;
                string currentPath = "";

                for (int i = 0; i < parts.Length; i++)
                {
                    bool isLastPart = i == parts.Length - 1;
                    currentPath = string.IsNullOrEmpty(currentPath) ? parts[i] : $"{currentPath}/{parts[i]}";

                    // 检查缓存中是否已有该路径节点
                    if (pathCache.TryGetValue(currentPath, out var existingNode))
                    {
                        currentLevel = existingNode;
                        currentParent = existingNode.Children;
                        existingNode.TotalItemCount++; // 更新总条目数
                        continue;
                    }

                    // 创建新节点
                    var newNode = new NodeData
                    {
                        Name = parts[i],
                        IsDirectory = !isLastPart,
                        TotalItemCount = 1
                    };

                    // 添加到父节点
                    currentParent.Add(newNode);
                    pathCache[currentPath] = newNode;

                    currentLevel = newNode;
                    currentParent = newNode.Children;
                }
            }

            return root;
        }

        // 创建TreeView节点 (包含延迟加载占位符)
        private TreeNode CreateTreeNode(NodeData data)
        {
            var node = new TreeNode();
            node.Text = data.IsDirectory 
                ? $"{data.Name} ({data.TotalItemCount}项)" // 目录显示条目数
                : data.Name;
            node.Tag = data;
            node.ImageIndex = data.IsDirectory ? 0 : 1; // 可添加图标区分目录/文件 (需提前准备ImageList)
            node.SelectedImageIndex = node.ImageIndex;

            // 目录节点添加"加载中..."占位符，触发BeforeExpand时替换为实际子节点
            if (data.IsDirectory && data.Children.Count > 0)
            {
                node.Nodes.Add(new TreeNode(LanguageManager.Instance.GetText("Common.Loading", "加载中...")));

            }

            return node;
        }

        // 重新填充TreeView节点
        private void PopulateTreeView(TreeView treeView, List<NodeData> rootNodes)
        {
            treeView.Nodes.Clear();
            foreach (var nodeData in rootNodes)
            {
                var treeNode = CreateTreeNode(nodeData);
                treeView.Nodes.Add(treeNode);
            }
        }

        // 过滤节点数据（递归）
        private List<NodeData> FilterNodeData(List<NodeData> nodes, string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
                return nodes;

            var filteredNodes = new List<NodeData>();
            foreach (var node in nodes)
            {
                bool nodeMatches = node.Name.ToLower().Contains(searchText);
                var filteredChildren = FilterNodeData(node.Children, searchText);
                bool hasMatchingChildren = filteredChildren.Count > 0;

                if (nodeMatches || hasMatchingChildren)
                {
                    var newNode = new NodeData
                    {
                        Name = node.Name,
                        IsDirectory = node.IsDirectory,
                        TotalItemCount = nodeMatches ? 1 + filteredChildren.Sum(c => c.TotalItemCount) : filteredChildren.Sum(c => c.TotalItemCount), // 更新过滤后的条目数
                        Children = filteredChildren // 只包含匹配的子节点
                    };
                    filteredNodes.Add(newNode);
                }
            }
            return filteredNodes;
        }

        #endregion

        #region 多语言支持
        
        // 初始化语言支持（在构造函数或SetupUI末尾调用）
        
        private void InitLanguageSupport()
        {
            // 订阅语言变更事件，触发UI刷新
            LanguageManager.Instance.LanguageChanged += RefreshUI;
            
            // 初始加载默认语言
            RefreshUI();
        }

        // 递归刷新所有控件文本（核心方法）
        private void RefreshUI()
        {
            // 从主窗口开始递归刷新所有控件
            RefreshControlText(this);
        }

        // 递归处理所有子控件
        private void RefreshControlText(Control parent)
        {
            if (parent == null) return;

            // 仅处理设置了Name且需要多语言的控件
            if (!string.IsNullOrEmpty(parent.Name))
            {
                // 根据控件类型更新文本（支持Label/Button/GroupBox/TabPage等）
                switch (parent)
                {
                    case Label label:
                        label.Text = LanguageManager.Instance.GetText(label.Name, label.Text); // 第二个参数为默认值（开发时的中文）
                        break;
                    case Button button:
                        button.Text = LanguageManager.Instance.GetText(button.Name, button.Text);
                        break;
                    case GroupBox groupBox:
                        groupBox.Text = LanguageManager.Instance.GetText(groupBox.Name, groupBox.Text);
                        break;
                    case TabPage tabPage:
                        tabPage.Text = LanguageManager.Instance.GetText(tabPage.Name, tabPage.Text);
                        break;
                    case Form form:
                        form.Text = LanguageManager.Instance.GetText(form.Name, form.Text);
                        break;
                    case TextBox textBox:
                        // TextBox的多语言支持通常用于水印/占位符文本，但WinForms原生不支持
                        // 如果需要，可以通过自定义控件或额外属性实现
                        break;
                    case ComboBox comboBox:
                        // ComboBox本身的文本可以通过SelectedText或Items处理
                        // 但通常不需要整体翻译
                        break;
                    case CheckBox checkBox:
                        checkBox.Text = LanguageManager.Instance.GetText(checkBox.Name, checkBox.Text);
                        break;
                    case RadioButton radioButton:
                        radioButton.Text = LanguageManager.Instance.GetText(radioButton.Name, radioButton.Text);
                        break;
                    case TabControl tabControl:
                        // TabControl本身不显示文本，但其TabPages会单独处理
                        break;
                    case Panel panel:
                        // Panel通常不显示文本
                        break;
                    // 可根据需要添加更多控件类型
                }
            }

            // 递归刷新子控件
            foreach (Control child in parent.Controls)
            {
                RefreshControlText(child);
            }
        }
        #endregion

        #region 辅助方法


        /// <summary>
        /// 显示ARC文件中的文件列表
        /// </summary>
        private void ShowArcFileList(string arcFilePath, TabPage tabPage)
        {
            if (string.IsNullOrEmpty(arcFilePath) || !File.Exists(arcFilePath))
            {
                //MessageBox.Show("ARC文件不存在", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show(
                    LanguageManager.Instance.GetText("Common.MessageFileNotFound", "文件不存在，请检查"), 
                    LanguageManager.Instance.GetText("Common.MessageBoxTitle.Error", "错误"), 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            var statusLabel = tabPage.Controls.Find("Common.StatusLabel", true)[0] as Label;
            //statusLabel.Text = "正在读取ARC文件列表...";
            statusLabel.Text = LanguageManager.Instance.GetText("Common.StatusLabel.Parsing", "解析中...");

            
            try
            {
                using (FileStream fs = new FileStream(arcFilePath, FileMode.Open, FileAccess.Read))
                {
                    ARCFile arcFile = new ARCFile();
                    arcFile.ReadStream(fs);
                    
                    List<string> entries = arcFile.ListAllEntries();
                    
                    if (entries.Count == 0)
                    {
                        //MessageBox.Show("ARC文件中没有找到任何条目", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        MessageBox.Show(
                            LanguageManager.Instance.GetText("Common.MessageFileNotFoundrecord", "文件中没有找到任何条目"), 
                            LanguageManager.Instance.GetText("Common.MessageBoxTitle.Information", "信息"), 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);

                        return;
                    }
                    
                    // 创建带行号的文件列表字符串
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.AppendLine($"{Path.GetFileName(arcFilePath)} > {entries.Count} {LanguageManager.Instance.GetText("LOG.Files", "个文件")}:");

                    // 循环添加行号和文件路径
                    for (int i = 0; i < entries.Count; i++)
                    {
                        // 行号从1开始，格式为 "行号. 文件路径"
                        sb.AppendLine($"{entries[i]}");
                    }
                    
                    string fileList = sb.ToString();
                    
                    // 创建一个可滚动的消息框
                    using (Form msgForm = new Form())
                    {
                        msgForm.Text = LanguageManager.Instance.GetText("Common.FileList", "文件列表");
                        msgForm.Name = "Common.FileList";
                        msgForm.Size = new System.Drawing.Size(600, 400);
                        msgForm.StartPosition = FormStartPosition.CenterParent;
                        msgForm.Owner = this;
                        //msgForm.TopMost = true;
                        
                        TextBox textBox = new TextBox();
                        textBox.Multiline = true;
                        textBox.ScrollBars = ScrollBars.Both;
                        textBox.ReadOnly = true;
                        textBox.Dock = DockStyle.Fill;
                        textBox.Text = fileList;
                        textBox.WordWrap = false; // 禁用自动换行，确保行号对齐
                        
                        Button okButton = new Button();
                        okButton.Text = LanguageManager.Instance.GetText("Common.OkButton", "确定");
                        okButton.Name = "Common.OkButton";

                        okButton.Dock = DockStyle.Bottom;
                        okButton.Click += (s, e) => msgForm.Close();
                        
                        msgForm.Controls.Add(textBox);
                        msgForm.Controls.Add(okButton);
                        
                        msgForm.ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"读取文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show(
                    LanguageManager.Instance.GetText("Common.MessageBoxReadFail", "读取文件失败"), 
                    LanguageManager.Instance.GetText("Common.MessageBoxTitle.Error", "错误"), 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                statusLabel.Text = LanguageManager.Instance.GetText("Common.StatusLabel", "就绪");
            }
        }



        // 添加静态变量记住上次选择的位置
        private static string lastSelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        private void BrowseFile(TextBox textBox, string title, string filter)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = title;
                dialog.Filter = filter;
                if (!string.IsNullOrEmpty(textBox.Text))
                {
                    dialog.InitialDirectory = Path.GetDirectoryName(textBox.Text);
                }
                else if (Directory.Exists(lastSelectedPath))
                {
                    dialog.InitialDirectory = lastSelectedPath;
                }
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    textBox.Text = dialog.FileName;
                    lastSelectedPath = Path.GetDirectoryName(dialog.FileName);
                }
            }
        }

        private void BrowseFolder(TextBox textBox, string description)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = description;
                if (!string.IsNullOrEmpty(textBox.Text) && Directory.Exists(textBox.Text))
                {
                    dialog.SelectedPath = textBox.Text;
                }
                else if (Directory.Exists(lastSelectedPath))
                {
                    dialog.SelectedPath = lastSelectedPath;
                }
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    textBox.Text = dialog.SelectedPath;
                    lastSelectedPath = dialog.SelectedPath;
                }
            }
        }
        
        #endregion

        #region 执行方法
        
        private async Task ExecuteUnpackArc(string inputFile, string outputDir, TabPage tabPage)
        {
            if (string.IsNullOrEmpty(inputFile))
            {
                //MessageBox.Show("请选择要解包的文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                MessageBox.Show(
                    LanguageManager.Instance.GetText("Common.MessageBoxNoSelectUnPackfile", "请选择要解包的文件"), 
                    LanguageManager.Instance.GetText("Common.MessageBoxTitle.Warning", "提示"), 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            if (!File.Exists(inputFile))
            {
                //MessageBox.Show("选择的文件不存在", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show(
                    LanguageManager.Instance.GetText("Common.MessageFileNotFound", "文件不存在，请检查"), 
                    LanguageManager.Instance.GetText("Common.MessageBoxTitle.Error", "错误"), 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            var progressBar = tabPage.Controls.Find("progressBar", true)[0] as System.Windows.Forms.ProgressBar;
            var logTextBox = tabPage.Controls.Find("logOutput", true)[0] as TextBox;
            logTextBox.Clear();
            var statusLabel = tabPage.Controls.Find("Common.StatusLabel", true)[0] as Label;
            
            var progress = new GuiProgressCallback(progressBar, logTextBox, statusLabel);
            
            progressBar.Visible = true;
            try
            {
                bool success = await ArzEditAPI.UnpackArcAsync(inputFile, outputDir, progress);
                if (success)
                {
                    //MessageBox.Show("解包完成！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    MessageBox.Show(
                        LanguageManager.Instance.GetText("Common.MessageBoxUnPackSuccess", "解包完成！"), 
                        LanguageManager.Instance.GetText("Common.MessageBoxTitle.Information", "成功"), 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                }
                else
                {
                    //MessageBox.Show("解包失败，请查看日志", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    MessageBox.Show(
                        LanguageManager.Instance.GetText("Common.MessageBoxUnPackFail", "解包失败，请查看日志"), 
                        LanguageManager.Instance.GetText("Common.MessageBoxTitle.Error", "错误"), 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    

                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"操作过程中发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show(
                    LanguageManager.Instance.GetText("Common.MessageBoxProcessError", "操作过程中发生错误：" + ex.Message), 
                    LanguageManager.Instance.GetText("Common.MessageBoxTitle.Error", "错误"), 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                

            }
            finally
            {
                // progressBar.Visible = false;
            }
        }
        
        private async Task ExecutePackArc(string inputDir, string outputFile, TabPage tabPage)
        {
            if (string.IsNullOrEmpty(inputDir))
            {
                //MessageBox.Show("请选择要打包的目录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                MessageBox.Show(
                    LanguageManager.Instance.GetText("Common.MessageBoxNoSelectPackDir", "请选择要打包的目录"), 
                    LanguageManager.Instance.GetText("Common.MessageBoxTitle.Warning", "提示"), 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                
                return;
            }
            
            if (!Directory.Exists(inputDir))
            {
                //MessageBox.Show("选择的目录不存在", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show(
                    LanguageManager.Instance.GetText("Common.MessageBoxDirNotFound", "选择的目录不存在"), 
                    LanguageManager.Instance.GetText("Common.MessageBoxTitle.Error", "错误"), 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                return;
            }
            
            var progressBar = tabPage.Controls.Find("progressBar", true)[0] as System.Windows.Forms.ProgressBar;
            var logTextBox = tabPage.Controls.Find("logOutput", true)[0] as TextBox;
            logTextBox.Clear();
            var statusLabel = tabPage.Controls.Find("Common.StatusLabel", true)[0] as Label;
            
            var progress = new GuiProgressCallback(progressBar, logTextBox, statusLabel);
            
            progressBar.Visible = true;
            try
            {
                bool success = await ArzEditAPI.PackArcAsync(inputDir, outputFile, progress);
                if (success)
                {
                    //MessageBox.Show("打包完成！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    MessageBox.Show(
                        LanguageManager.Instance.GetText("Common.MessageBoxPackSuccess", "打包完成！"), 
                        LanguageManager.Instance.GetText("Common.MessageBoxTitle.Information", "成功"), 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                }
                else
                {
                    //MessageBox.Show("打包失败，请查看日志", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    MessageBox.Show(
                        LanguageManager.Instance.GetText("Common.MessageBoxPackFail", "打包失败，请查看日志"), 
                        LanguageManager.Instance.GetText("Common.MessageBoxTitle.Error", "错误"), 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    
                    

                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"操作过程中发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show(
                    LanguageManager.Instance.GetText("Common.MessageBoxProcessError", "操作过程中发生错误：" + ex.Message), 
                    LanguageManager.Instance.GetText("Common.MessageBoxTitle.Error", "错误"), 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                
            }
            finally
            {
                //progressBar.Visible = false;
            }
        }
        
        private async Task ExecuteUnpackArz(string inputFile, string outputDir, TabPage tabPage)
        {
            if (string.IsNullOrEmpty(inputFile))
            {
                //MessageBox.Show("请选择要解包的文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                MessageBox.Show(
                    LanguageManager.Instance.GetText("Common.MessageBoxNoSelectUnPackfile", "请选择要解包的文件"), 
                    LanguageManager.Instance.GetText("Common.MessageBoxTitle.Warning", "提示"), 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            if (!File.Exists(inputFile))
            {
                //MessageBox.Show("选择的文件不存在", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);  
                MessageBox.Show(
                    LanguageManager.Instance.GetText("Common.MessageFileNotFound", "文件不存在，请检查"), 
                    LanguageManager.Instance.GetText("Common.MessageBoxTitle.Error", "错误"), 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                return;
            }
            
            var progressBar = tabPage.Controls.Find("progressBar", true)[0] as System.Windows.Forms.ProgressBar;
            var logTextBox = tabPage.Controls.Find("logOutput", true)[0] as TextBox;
            logTextBox.Clear();
            var statusLabel = tabPage.Controls.Find("Common.StatusLabel", true)[0] as Label;
            
            var progress = new GuiProgressCallback(progressBar, logTextBox, statusLabel);
            
            progressBar.Visible = true;
            try
            {
                bool success = await ArzEditAPI.UnpackArzAsync(inputFile, outputDir, progress);
                if (success)
                {
                    //MessageBox.Show("解包完成！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    MessageBox.Show(
                        LanguageManager.Instance.GetText("Common.MessageBoxUnPackSuccess", "解包完成！"), 
                        LanguageManager.Instance.GetText("Common.MessageBoxTitle.Information", "成功"), 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                else
                {
                    //MessageBox.Show("解包失败，请查看日志", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    MessageBox.Show(
                        LanguageManager.Instance.GetText("Common.MessageBoxUnPackFail", "解包失败，请查看日志"), 
                        LanguageManager.Instance.GetText("Common.MessageBoxTitle.Error", "错误"), 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"操作过程中发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show(
                    LanguageManager.Instance.GetText("Common.MessageBoxProcessError", "操作过程中发生错误：" + ex.Message), 
                    LanguageManager.Instance.GetText("Common.MessageBoxTitle.Error", "错误"), 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                
            }
            finally
            {
                // progressBar.Visible = false;
            }
        }
        
        private async Task ExecutePackArz(string modDir, string gameDir, string templateDir, string outputFile, TabPage tabPage)
        {
            if (string.IsNullOrEmpty(modDir))
            {
                //MessageBox.Show("请选择目录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                MessageBox.Show(
                    LanguageManager.Instance.GetText("Common.MessageBoxNoSelectPackDir", "请选择要打包的目录"), 
                    LanguageManager.Instance.GetText("Common.MessageBoxTitle.Warning", "提示"), 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // 游戏目录为空时跳过检查，允许打包
            if (!string.IsNullOrEmpty(gameDir))
            {
                if (!File.Exists(Path.Combine(gameDir, "Grim Dawn.exe")))
                {
                    //MessageBox.Show("选择的游戏目录不正确，未找到Grim Dawn.exe", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    MessageBox.Show(
                        LanguageManager.Instance.GetText("Common.MessageBoxGameDirError", "选择的游戏目录不正确，未找到Grim Dawn.exe"), 
                        LanguageManager.Instance.GetText("Common.MessageBoxTitle.Error", "错误"), 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            
            if (!Directory.Exists(modDir))
            {
                //MessageBox.Show("选择的mod目录不存在", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show(
                    LanguageManager.Instance.GetText("Common.MessageBoxModDirNotFound", "选择的mod目录不存在"), 
                    LanguageManager.Instance.GetText("Common.MessageBoxTitle.Error", "错误"), 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            string[] templatePaths = string.IsNullOrEmpty(templateDir) ? null : new string[] { templateDir };
            
            var progressBar = tabPage.Controls.Find("progressBar", true)[0] as System.Windows.Forms.ProgressBar;
            var logTextBox = tabPage.Controls.Find("logOutput", true)[0] as TextBox;
            
            // 收集用户选择的子目录
            List<string> selectedSubDirs = new List<string>();
            foreach (var item in databaseSubDirsCheckedListBox.Items)
            {
                int index = databaseSubDirsCheckedListBox.Items.IndexOf(item);
                if (databaseSubDirsCheckedListBox.GetItemChecked(index))
                {
                    selectedSubDirs.Add(item.ToString());
                }
            }
            
            logTextBox.Clear();
            // 显示用户选择的子目录信息
            // string selectedDirsMsg = "将打包以下数据库子目录：\n" + string.Join("\n", selectedSubDirs);
            // logTextBox.Text = selectedDirsMsg + "\n\n";
            
            var statusLabel = tabPage.Controls.Find("Common.StatusLabel", true)[0] as Label;
            
            var progress = new GuiProgressCallback(progressBar, logTextBox, statusLabel);
            
            progressBar.Visible = true;
            try
            {
                // 直接使用原始mod目录进行打包，不创建临时目录
                
                // 确保模板目录存在（如果指定了模板目录）
                // if (!string.IsNullOrEmpty(templateDir))
                // {
                //     if (Directory.Exists(templateDir))
                //     {
                //         Log.Info("自定义目录：" + templateDir);
                //     }
                //     else
                //     {
                //         Log.Warn("警告：指定的模板目录不存在：" + templateDir);
                        
                //     }
                // }
                
                // 执行打包 - 使用用户选择的子目录
                bool success = await ArzEditAPI.BuildArzWithSelectedDirsAsync(modDir, null, gameDir, selectedSubDirs, templatePaths, progress);
                
                // 检查输出文件是否存在
                // if (!string.IsNullOrEmpty(outputFile))
                // {
                //     if (File.Exists(outputFile))
                //     {
                //         logTextBox.Text += "\nARZ文件已成功生成：" + outputFile;
                //         logTextBox.Text += "\n文件大小：" + new FileInfo(outputFile).Length + " 字节";
                //     }
                //     else
                //     {
                //         logTextBox.Text += "\n警告：未找到预期的ARZ文件：" + outputFile;
                //         success = false;
                //     }
                // }
                // else
                // {
                //     logTextBox.Text += "\n未指定输出文件路径";
                //     success = false;
                // }
                
                if (success)
                {
                    //MessageBox.Show("打包完成！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    MessageBox.Show(
                        LanguageManager.Instance.GetText("Common.MessageBoxPackSuccess", "打包完成！"), 
                        LanguageManager.Instance.GetText("Common.MessageBoxTitle.Information", "成功"), 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                }
                else
                {
                    //MessageBox.Show("打包失败，请查看日志", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    MessageBox.Show(
                        LanguageManager.Instance.GetText("Common.MessageBoxPackFail", "打包失败，请查看日志"), 
                        LanguageManager.Instance.GetText("Common.MessageBoxTitle.Error", "错误"), 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"操作过程中发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show(
                    LanguageManager.Instance.GetText("Common.MessageBoxProcessError", "操作过程中发生错误：" + ex.Message), 
                    LanguageManager.Instance.GetText("Common.MessageBoxTitle.Error", "错误"), 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                
            }
            finally
            {
                //progressBar.Visible = false;
            }
        }
        
        // 复制目录的辅助方法
        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            // 创建目标目录
            Directory.CreateDirectory(destinationDir);
            
            // 复制文件
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            
            // 递归复制子目录
            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string destDir = Path.Combine(destinationDir, Path.GetFileName(dir));
                CopyDirectory(dir, destDir);
            }
        }
        
        #endregion
        
        // 显示更新日志窗口
        private void ShowChangelog()
        {
            // 创建新窗口
            Form changelogForm = new Form();
            changelogForm.Text = "更新日志 - Changelog";
            changelogForm.Size = new System.Drawing.Size(600, 800);
            changelogForm.StartPosition = FormStartPosition.CenterParent;
            changelogForm.MinimumSize = new System.Drawing.Size(400, 300);
            
            // 创建富文本框用于显示更新日志
            RichTextBox changelogBox = new RichTextBox();
            changelogBox.Dock = DockStyle.Fill;
            changelogBox.ReadOnly = true;
            changelogBox.ScrollBars = RichTextBoxScrollBars.Both;
            
            try
            {
                // 尝试从嵌入资源读取更新日志RTF文件
                Assembly assembly = Assembly.GetExecutingAssembly();
                // 获取嵌入资源的完整名称
                string resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(name => name.EndsWith("!ChangeLog.rtf", StringComparison.OrdinalIgnoreCase));
                
                if (!string.IsNullOrEmpty(resourceName))
                {
                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        // RichTextBox直接加载RTF格式内容
                        changelogBox.LoadFile(stream, RichTextBoxStreamType.RichText);
                    }
                }
                else
                {
                    // 如果没有找到嵌入的RTF资源，显示默认内容
                    changelogBox.Text = "更新日志文件未找到。";
                }
            }
            catch (Exception ex)
            {
                changelogBox.Text = "加载更新日志时出错：" + ex.Message;
            }
            
            // 添加按钮
            Button closeButton = new Button();
            closeButton.Text = "关闭";
            closeButton.Dock = DockStyle.Bottom;
            closeButton.Height = 30;
            closeButton.Click += (s, e) => changelogForm.Close();
            
            // 添加控件到窗口
            changelogForm.Controls.Add(changelogBox);
            changelogForm.Controls.Add(closeButton);
            
            // 显示窗口
            changelogForm.ShowDialog(this);
        }

        // 显示使用说明窗口
        private void ShowHowToUse()
        {
            // 创建新窗口
            Form howToUseForm = new Form();
            howToUseForm.Text = "使用说明 - How To Use";
            howToUseForm.Size = new System.Drawing.Size(600, 800);
            howToUseForm.StartPosition = FormStartPosition.CenterParent;
            howToUseForm.MinimumSize = new System.Drawing.Size(400, 300);
            
            // 创建富文本框用于显示使用说明
            RichTextBox howToUseBox = new RichTextBox();
            howToUseBox.Dock = DockStyle.Fill;
            howToUseBox.ReadOnly = true;
            howToUseBox.ScrollBars = RichTextBoxScrollBars.Both;
            
            try
            {
                // 尝试从嵌入资源读取使用说明RTF文件
                Assembly assembly = Assembly.GetExecutingAssembly();
                // 获取嵌入资源的完整名称
                string resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(name => name.EndsWith("!HowToUse.rtf", StringComparison.OrdinalIgnoreCase));
                
                if (!string.IsNullOrEmpty(resourceName))
                {
                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        // RichTextBox直接加载RTF格式内容
                        howToUseBox.LoadFile(stream, RichTextBoxStreamType.RichText);
                    }
                }
                else
                {
                    // 如果没有找到嵌入的RTF资源，显示默认内容
                    howToUseBox.Text = "使用说明文件未找到。";
                }
            }
            catch (Exception ex)
            {
                howToUseBox.Text = "加载使用说明时出错：" + ex.Message;
            }
            
            // 添加按钮
            Button closeButton = new Button();
            closeButton.Text = "关闭";
            closeButton.Dock = DockStyle.Bottom;
            closeButton.Height = 30;
            closeButton.Click += (s, e) => howToUseForm.Close();
            
            // 添加控件到窗口
            howToUseForm.Controls.Add(howToUseBox);
            howToUseForm.Controls.Add(closeButton);
            
            // 显示窗口
            howToUseForm.ShowDialog(this);
        }
    }
}