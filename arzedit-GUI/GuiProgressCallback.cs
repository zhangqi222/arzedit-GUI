using System;
using System.Windows.Forms;
using NLog;
using NLog.Targets;
using NLog.Config;

namespace arzedit
{
    /// <summary>
    /// GUI进度回调实现示例
    /// </summary>
    public class GuiProgressCallback : IProgressCallback
    {
        private System.Windows.Forms.ProgressBar progressBar; // 明确使用 Windows Forms ProgressBar
        private TextBox logTextBox;
        private Label statusLabel;
        
        // 添加NLog目标，用于捕获NLog日志
        private NLogTarget nlogTarget;

        public GuiProgressCallback(System.Windows.Forms.ProgressBar progressBar = null, TextBox logTextBox = null, Label statusLabel = null)
        {
            this.progressBar = progressBar;
            this.logTextBox = logTextBox;
            this.statusLabel = statusLabel;
            
            // 初始化NLog目标并添加到配置
            InitializeNLogTarget();
        }
        
        // 初始化NLog目标，将NLog日志重定向到GUI
        private void InitializeNLogTarget()
        {
            nlogTarget = new NLogTarget(this);
            
            // 获取NLog配置
            var config = LogManager.Configuration ?? new LoggingConfiguration();
            
            // 清除所有现有规则，避免多目标冲突
            config.LoggingRules.Clear();
            
            // 添加目标和规则
            config.AddTarget("guiTarget", nlogTarget);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, nlogTarget);
            
            // 应用配置
            LogManager.Configuration = config;
        }

        public void Report(int percentage, string message)
        {
            // 确保在UI线程上执行
            if (progressBar?.InvokeRequired == true || logTextBox?.InvokeRequired == true || statusLabel?.InvokeRequired == true)
            {
                Action action = () => Report(percentage, message);
                progressBar?.Invoke(action);
                return;
            }

            if (statusLabel != null)
            {
                statusLabel.Text = message;
            }

            if (progressBar != null)
            {
                progressBar.Value = Math.Max(0, Math.Min(100, percentage));
            }

            if (logTextBox != null)
            {
                logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
                logTextBox.ScrollToCaret(); // 自动滚动到最新日志
            }
        }

        public void ReportLog(string message)
        {
            Report(0, message);
        }

        public void ReportError(string error)
        {
            Report(0, $"错误: {error}");
        }
        
        public void ReportWarning(string warning)
        {
            Report(0, $"警告: {warning}");
        }
        
        // NLog目标实现，用于捕获NLog日志
        private class NLogTarget : TargetWithLayout
        {
            private GuiProgressCallback callback;
            
            public NLogTarget(GuiProgressCallback callback)
            {
                this.callback = callback;
                Name = "GuiLogTarget";
                // 显式设置布局为仅包含日志消息，避免默认布局添加额外信息
                Layout = "${message}"; 
            }
            
            protected override void Write(LogEventInfo logEvent)
            {
                string logMessage = Layout.Render(logEvent);
                string level = logEvent.Level.ToString().ToUpper();
                
                // 统一格式：仅包含级别和消息（时间戳由Report方法统一添加）
                callback.ReportLog($"[{level}] {logMessage}");
            }
        }
    }
}