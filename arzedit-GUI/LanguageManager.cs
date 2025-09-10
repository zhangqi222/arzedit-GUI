using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Microsoft.Win32;

namespace arzedit
{
    public class LanguageItem
    {
        public string Code { get; set; }
        public string Name { get; set; }

        public LanguageItem(string code, string name)
        {
            Code = code;
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class LanguageManager
    {
        private static readonly Lazy<LanguageManager> _instance = new Lazy<LanguageManager>(() => new LanguageManager());
        public static LanguageManager Instance => _instance.Value;

        private Dictionary<string, string> _currentLanguage = new Dictionary<string, string>();
        private string _currentLangCode = "zh-CN"; // 默认中文
        private const string REGISTRY_PATH = "SOFTWARE\\arzedit-gui"; // 注册表路径

        public event Action LanguageChanged;

        private LanguageManager()
        {
            // 从注册表读取语言设置，并直接加载对应的语言
            string initialLangCode = GetInitialLanguageCode();
            _currentLangCode = initialLangCode; // 直接从注册表获取值
            LoadLanguage(initialLangCode);
        }
        
        /// <summary>
        /// 获取初始语言代码（从注册表读取，如果为空则使用默认中文）
        /// </summary>
        /// <returns>应该使用的语言代码</returns>
        public string GetInitialLanguageCode()
        {
            string registryLangCode = ReadLanguageFromRegistry();
            return !string.IsNullOrEmpty(registryLangCode) ? registryLangCode : "zh-CN";
        }
        
        /// <summary>
        /// 从注册表读取语言设置
        /// </summary>
        /// <returns>保存的语言代码，如果没有则返回null</returns>
        public string ReadLanguageFromRegistry()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(REGISTRY_PATH))
                {
                    if (key != null)
                    {
                        return key.GetValue("language") as string;
                    }
                }
            }
            catch { }
            return null;
        }
        
        /// <summary>
        /// 将语言设置写入注册表
        /// </summary>
        /// <param name="langCode">要保存的语言代码</param>
        private void WriteLanguageToRegistry(string langCode)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(REGISTRY_PATH))
                {
                    if (key != null)
                    {
                        key.SetValue("language", langCode);
                    }
                }
            }
            catch { }
        }

        public void LoadLanguage(string langCode)
        {
            // 读取嵌入式资源（资源名格式：项目命名空间.文件夹名.文件名）
            // 注意：如果你的项目默认命名空间不是"arzedit"，需要替换为实际命名空间
            var resourceName = $"arzedit.languages.{langCode}.json";

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var json = reader.ReadToEnd();
                        _currentLanguage = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                        _currentLangCode = langCode;
                        
                        // 将当前语言保存到注册表
                        WriteLanguageToRegistry(langCode);
                        
                        LanguageChanged?.Invoke(); // 触发语言变更事件
                    }
                }
                else
                {
                    // 资源不存在时使用默认中文
                    _currentLanguage = new Dictionary<string, string>();
                    _currentLangCode = "zh-CN";
                    
                    // 保存默认语言到注册表
                    WriteLanguageToRegistry("zh-CN");
                }
            }
        }

        public string GetText(string key, string defaultValue = "")
        {
            return _currentLanguage.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public string CurrentLangCode => _currentLangCode;
    }
}