using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;

namespace arzedit
{
    /// <summary>
    /// ArzEdit工具的统一API接口
    /// </summary>
    public static class ArzEditAPI
    {
        /// <summary>
        /// 解包ARC文件
        /// </summary>
        /// <param name="inputFile">输入的ARC文件路径</param>
        /// <param name="outputDirectory">输出目录路径</param>
        /// <param name="progress">进度回调接口</param>
        /// <returns>操作是否成功</returns>
        public static async Task<bool> UnpackArcAsync(string inputFile, string outputDirectory, IProgressCallback progress = null)
        {
            return await Task.Run(() => UnpackArc(inputFile, outputDirectory, progress));
        }

        public static bool UnpackArc(string inputFile, string outputDirectory, IProgressCallback progress = null)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                progress?.Report(0, LanguageManager.Instance.GetText("LOG.StartUnpacking", "开始解包"));

                if (!File.Exists(inputFile))
                {
                    progress?.ReportError($"{LanguageManager.Instance.GetText("LOG.InputFileNotFound", "输入文件不存在：")} {inputFile}");

                    return false;
                }

                using (FileStream fs = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
                {
                    ARCFile archive = new ARCFile();
                    archive.ReadStream(fs);
                    
                    // 自动生成输出目录（与ARC文件同名的文件夹）
                    if (string.IsNullOrEmpty(outputDirectory))
                    {
                        outputDirectory = Path.Combine(
                            Path.GetDirectoryName(inputFile),
                            Path.GetFileNameWithoutExtension(inputFile)
                        );
                    }

                    // 确保输出目录存在
                    if (!Directory.Exists(outputDirectory))
                    {
                        Directory.CreateDirectory(outputDirectory);
                    }

                    //progress?.Report(10, $"正在解包 {archive.toc.Count} ...");
                    progress?.Report(10, $"{LanguageManager.Instance.GetText("LOG.Unpacking", "正在解包")} {archive.toc.Count}");

                    
                    // 逐个解包并报告进度
                    int successCount = 0;
                    int skipCount = 0;
                    for (int i = 0; i < archive.toc.Count; i++)
                    {
                        try
                        {
                            ARCTocEntry entry = archive.toc[i];
                            
                            // 先计算进度百分比
                            int percentage = (int)((double)(i + 1) / archive.toc.Count * 80) + 10;
                            
                            string entryName = entry.GetEntryString(archive.strs);
                            
                            // 跳过空名称记录
                            if (string.IsNullOrEmpty(entryName))
                            {
                                skipCount++;
                                progress?.ReportWarning($"{LanguageManager.Instance.GetText("LOG.Unpacking", "正在解包")} ({i + 1}/{archive.toc.Count}) {LanguageManager.Instance.GetText("LOG.EmptyFileName", "空文件名，跳过")}");
                                continue;
                            }
                            
                            // 报告进度
                            progress?.Report(percentage, $"{LanguageManager.Instance.GetText("LOG.Unpacking", "正在解包")} ({i + 1}/{archive.toc.Count}) {entryName}");
                            
                            // 解包单个文件
                            string filename = Path.Combine(outputDirectory, entryName.Replace('/', Path.DirectorySeparatorChar));
                            
                            // 确保目录存在
                            string directoryName = Path.GetDirectoryName(filename);
                            if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
                            {
                                Directory.CreateDirectory(directoryName);
                            }
                            
                            using (FileStream output = new FileStream(filename, FileMode.Create))
                            {
                                archive.UnpackToStream(entry, output);
                            }
                            
                            // 设置文件时间
                            File.SetLastWriteTime(filename, entry.FileTime);
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            // 记录错误但继续解包其他文件
                            skipCount++;
                            progress?.ReportError($"{LanguageManager.Instance.GetText("LOG.Unpacking", "正在解包")} ({i + 1}/{archive.toc.Count}) {LanguageManager.Instance.GetText("LOG.FileExceptionSkip", "文件异常，跳过")}: {ex.Message}");
                        }
                    }
                    
                    // 报告跳过的文件数量
                    if (skipCount > 0)
                    {
                        progress?.ReportWarning($"{LanguageManager.Instance.GetText("LOG.SkippedFileCount", "共跳过")} {skipCount} {LanguageManager.Instance.GetText("LOG.FilesDueToError", "个文件")}");
                    }
                }

                TimeSpan duration = DateTime.Now - startTime;
                //progress?.Report(100, $"ARC解包完成，耗时: {duration.TotalSeconds:F2}秒");
                progress?.Report(100, $"{LanguageManager.Instance.GetText("LOG.UnpackingCompleteUseTime", "解包完成，耗时:")} {duration.TotalSeconds:F2}s");

                return true;
            }
            catch (Exception ex)
            {
                TimeSpan duration = DateTime.Now - startTime;
                //progress?.ReportError($"解包ARC失败: {ex.Message}，耗时: {duration.TotalSeconds:F2}秒");
                progress?.ReportError($"{LanguageManager.Instance.GetText("LOG.UnpackingFailedUseTime", "解包失败，耗时:")} {duration.TotalSeconds:F2}s , {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 打包ARC文件
        /// </summary>
        /// <param name="inputDirectory">输入目录路径</param>
        /// <param name="outputFile">输出的ARC文件路径</param>
        /// <param name="progress">进度回调接口</param>
        /// <returns>操作是否成功</returns>
        public static async Task<bool> PackArcAsync(string inputDirectory, string outputFile, IProgressCallback progress = null)
        {
            return await Task.Run(() => PackArc(inputDirectory, outputFile, progress));
        }

        public static bool PackArc(string inputDirectory, string outputFile, IProgressCallback progress = null)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                //progress?.Report(0, "开始打包ARC文件...");
                progress?.Report(0, $"{LanguageManager.Instance.GetText("LOG.StartPacking", "开始打包")}...");


                if (!Directory.Exists(inputDirectory))
                {
                    //progress?.ReportError($"输入目录不存在: {inputDirectory}");
                    progress?.ReportError($"{LanguageManager.Instance.GetText("LOG.InputDirectoryNotFound", "输入目录不存在")}: {inputDirectory}");

                    return false;
                }

                // 自动生成输出文件名
                if (string.IsNullOrEmpty(outputFile))
                {
                    outputFile = Path.Combine(
                        Path.GetDirectoryName(inputDirectory),
                        Path.GetFileName(inputDirectory) + ".arc"
                    );
                }

                // 确保输出文件的目录存在
                string outputDir = Path.GetDirectoryName(outputFile);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // 扫描文件
                //progress?.Report(10, "正在扫描文件...");

                progress?.Report(10, $"{LanguageManager.Instance.GetText("LOG.ScanningFiles", "正在扫描文件")}...");

                string[] allFiles = Directory.GetFiles(inputDirectory, "*", SearchOption.AllDirectories);
                //progress?.Report(15, $"找到 {allFiles.Length} 个文件，开始打包...");
                progress?.Report(15, $"{allFiles.Length} {LanguageManager.Instance.GetText("LOG.FoundFilesAndPack", "个文件已找到，开始打包...")}");

                
                using (FileStream fs = new FileStream(outputFile, FileMode.Create))
                using (ARCWriter awriter = new ARCWriter(fs))
                {
                    for (int i = 0; i < allFiles.Length; i++)
                    {
                        string afile = allFiles[i];
                        DateTime afiletime = File.GetLastWriteTime(afile);
                        string relativePath = Path.GetFullPath(afile).Substring(Path.GetFullPath(inputDirectory).Length)
                            .Replace(Path.DirectorySeparatorChar, '/').ToLower().TrimStart('/');
                        
                        // 报告进度
                        int percentage = (int)((double)(i + 1) / allFiles.Length * 80) + 15;
                        //progress?.Report(percentage, $"正在打包: ({i + 1}/{allFiles.Length}) {relativePath}");
                        progress?.Report(percentage, $"{LanguageManager.Instance.GetText("LOG.Packing", "正在打包")}: ({i + 1}/{allFiles.Length}) {relativePath}");

                        using (FileStream ifs = new FileStream(afile, FileMode.Open))
                        {
                            ifs.Seek(0, SeekOrigin.Begin);
                            awriter.WriteFromStream(relativePath, afiletime, ifs);
                        }
                    }
                }

                TimeSpan duration = DateTime.Now - startTime;
                //progress?.Report(100, $"ARC打包完成，耗时: {duration.TotalSeconds:F2}秒");
                progress?.Report(100, $"{LanguageManager.Instance.GetText("LOG.PackingCompleteUseTime", "打包完成，耗时:")} {duration.TotalSeconds:F2}s");

                return true;
            }
            catch (Exception ex)
            {
                TimeSpan duration = DateTime.Now - startTime;
                //progress?.ReportError($"打包ARC失败: {ex.Message}，耗时: {duration.TotalSeconds:F2}秒");
                progress?.ReportError($"{LanguageManager.Instance.GetText("LOG.PackingFailedUseTime", "打包失败，耗时:")} {duration.TotalSeconds:F2}s , {ex.Message}");

                return false;
            }
        }

        /// <summary>
        /// 解包ARZ文件
        /// </summary>
        /// <param name="inputFile">输入的ARZ文件路径</param>
        /// <param name="outputDirectory">输出目录路径</param>
        /// <param name="progress">进度回调接口</param>
        /// <returns>操作是否成功</returns>
        public static async Task<bool> UnpackArzAsync(string inputFile, string outputDirectory, IProgressCallback progress = null)
        {
            return await Task.Run(() => UnpackArz(inputFile, outputDirectory, progress));
        }

        public static bool UnpackArz(string inputFile, string outputDirectory, IProgressCallback progress = null)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                //progress?.Report(0, "开始解包ARZ文件...");
                progress?.Report(0, $"{LanguageManager.Instance.GetText("LOG.StartUnpacking", "开始解包")}...");


                if (!File.Exists(inputFile))
                {
                    //progress?.ReportError($"输入文件不存在: {inputFile}");
                    progress?.ReportError($"{LanguageManager.Instance.GetText("LOG.InputFileNotFound", "输入文件不存在")}: {inputFile}");

                    return false;
                }

                byte[] mdata = File.ReadAllBytes(inputFile);
                using (MemoryStream memory = new MemoryStream(mdata))
                {
                    ARZReader arzr = new ARZReader(memory);
                    
                    // 自动生成输出目录
                    if (string.IsNullOrEmpty(outputDirectory))
                    {
                        outputDirectory = Path.GetDirectoryName(inputFile);
                    }

                    // 确保输出目录存在
                    if (!Directory.Exists(outputDirectory))
                    {
                        Directory.CreateDirectory(outputDirectory);
                    }

                    //progress?.Report(10, $"正在解包 {arzr.Count} 个文件...");
                    progress?.Report(10, $"{LanguageManager.Instance.GetText("LOG.Unpacking", "正在解包")} {arzr.Count} ...");

                    
                    for (int i = 0; i < arzr.Count; i++)
                    {
                        // 使用按需加载机制，避免一次性加载所有记录数据
                        ARZRecord rec = arzr.GetRecordLazy(i);
                        // 加载当前记录的数据
                        arzr.LoadRecordData(rec);
                        
                        string filename = Path.Combine(outputDirectory, rec.Name.Replace('/', Path.DirectorySeparatorChar));
                        Directory.CreateDirectory(Path.GetDirectoryName(filename));
                        
                        // 先计算百分比
                        int percentage = (int)((double)(i + 1) / arzr.Count * 80) + 10;
                        
                        // 报告正在处理的文件（包含文件路径）
                        //progress?.Report(percentage, $"正在解包: ({i + 1}/{arzr.Count}) {rec.Name} ");
                        progress?.Report(percentage, $"{LanguageManager.Instance.GetText("LOG.Unpacking", "正在解包")}: ({i + 1}/{arzr.Count}) {rec.Name} ");

                        
                        rec.SaveToFile(filename);
                        rec.DiscardData();
                    }
                }

                TimeSpan duration = DateTime.Now - startTime;
                //progress?.Report(100, $"ARZ解包完成，耗时: {duration.TotalSeconds:F2}秒");
                progress?.Report(100, $"{LanguageManager.Instance.GetText("LOG.UnpackingCompleteUseTime", "解包完成，耗时")}:{duration.TotalSeconds:F2}s");

                return true;
            }
            catch (Exception ex)
            {
                TimeSpan duration = DateTime.Now - startTime;
                //progress?.ReportError($"解包ARZ失败: {ex.Message}，耗时: {duration.TotalSeconds:F2}秒");
                progress?.ReportError($"{LanguageManager.Instance.GetText("LOG.UnpackingFailedUseTime", "解包失败，耗时")}:{duration.TotalSeconds:F2}s , {ex.Message}");

                return false;
            }
        }

        
        
        /// <summary>
        /// 构建ARZ文件（仅打包指定子目录）
        /// </summary>
        /// <param name="modPath">Mod源码目录路径</param>
        /// <param name="buildPath">构建输出目录路径</param>
        /// <param name="gameFolder">游戏根目录路径</param>
        /// <param name="selectedSubDirs">用户选择的子目录列表</param>
        /// <param name="templatePaths">额外的模板目录路径数组（可选）</param>
        /// <param name="progress">进度回调接口</param>
        /// <returns>操作是否成功</returns>
        public static async Task<bool> BuildArzWithSelectedDirsAsync(string modPath, string buildPath, string gameFolder, 
                                                                    List<string> selectedSubDirs, string[] templatePaths = null, 
                                                                    IProgressCallback progress = null)
        {
            return await Task.Run(() => BuildArzWithSelectedDirs(modPath, buildPath, gameFolder, selectedSubDirs, templatePaths, progress));
        }

        public static bool BuildArzWithSelectedDirs(string modPath, string buildPath, string gameFolder, 
                                           List<string> selectedSubDirs, string[] templatePaths = null, 
                                           IProgressCallback progress = null)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                progress?.Report(0, $"{LanguageManager.Instance.GetText("LOG.Packing", "正在打包")}...");

                if (!Directory.Exists(modPath))
                {
                    progress?.ReportError($"{LanguageManager.Instance.GetText("LOG.ModPathNotExist", "Mod路径不存在")}: {modPath}");
                    return false;
                }

                // 游戏目录为空时跳过检查，允许打包
                if (!string.IsNullOrEmpty(gameFolder) && !File.Exists(Path.Combine(gameFolder, "Grim Dawn.exe")))
                {
                    progress?.ReportError($"{LanguageManager.Instance.GetText("LOG.GamePathNotRight", "游戏路径不正确")}: {gameFolder}");
                    return false;
                }

                // 收集用户选择的子目录下的所有dbr文件
                List<string> selectedDbrFiles = new List<string>();
                string databaseDir = Path.Combine(modPath, "database");
                
                // 检查selectedSubDirs是否有效
                if (selectedSubDirs == null || selectedSubDirs.Count == 0)
                {
                    progress?.ReportWarning($"{LanguageManager.Instance.GetText("LOG.MessageBoxNoDirSelected", "未选择子目录")}");
                }
                
                if (Directory.Exists(databaseDir))
                {
                    
                    if (selectedSubDirs == null || selectedSubDirs.Count == 0)
                    {
                        progress?.ReportWarning($"{LanguageManager.Instance.GetText("LOG.MessageBoxNoDirSelected", "未选择子目录")}");
                    }
                    else
                    {
                        foreach (string subDir in selectedSubDirs)
                        {
                            string fullSubDirPath = Path.Combine(databaseDir, subDir);
                            if (Directory.Exists(fullSubDirPath))
                            {
                                string[] dbrFiles = Directory.GetFiles(fullSubDirPath, "*.dbr", SearchOption.AllDirectories);
                                selectedDbrFiles.AddRange(dbrFiles);
                                progress?.ReportLog($"{LanguageManager.Instance.GetText("LOG.AddingFilesFromDir", "添加目录中的文件")}: {fullSubDirPath} ({dbrFiles.Length} {LanguageManager.Instance.GetText("LOG.Files", "个文件")})");
                            }
                            else
                            {
                                progress?.ReportWarning($"{LanguageManager.Instance.GetText("LOG.MessageBoxDirNotFound", "目录不存在")}: {fullSubDirPath}");
                            }
                        }
                    }
                }
                else
                {
                    progress?.ReportError($"{LanguageManager.Instance.GetText("LOG.DatabaseDirNotExist", "database目录不存在")}: {databaseDir}");
                    return false;
                }
                

                
                if (selectedDbrFiles.Count == 0)
                {
                    progress?.ReportError($"{LanguageManager.Instance.GetText("LOG.NoFilesToPack", "没有找到要打包的文件")}");
                    return false;
                }

                // 解析模板
                progress?.Report(5, $"{LanguageManager.Instance.GetText("LOG.ParsingTemplates", "解析模板")}...");
                
                Dictionary<string, TemplateNode> templates = new Dictionary<string, TemplateNode>();
                
                // 从嵌入式资源加载内置模板
                int builtInTemplateCount = Program.LoadBuiltInTemplates(templates);
                progress?.ReportLog($"{LanguageManager.Instance.GetText("LOG.BuiltInTemplateCount", "内置模板：")} {builtInTemplateCount}");

                List<string> templateFolders = new List<string>();
                int actualModCount = 0;
                int actualCustomCount = 0;

                // 总是先添加MOD模板路径
                templateFolders.Add(modPath);
                
                // 然后添加自定义模板（如果有），这样自定义模板会覆盖同名的MOD模板
                if (templatePaths != null)
                {
                    templateFolders.AddRange(templatePaths);
                }
                
                string[] tfolders = templateFolders.ToArray();
                
                // 统计实际模板文件数量
                foreach (string tfolder in tfolders)
                {
                    try
                    {
                        string[] alltemplates = Directory.GetFiles(Path.GetFullPath(tfolder), "*.tpl", SearchOption.AllDirectories);
                        bool isModFolder = Path.GetFullPath(tfolder).Equals(Path.GetFullPath(modPath), StringComparison.OrdinalIgnoreCase);
                           
                        if (isModFolder)
                        {
                            actualModCount += alltemplates.Length;
                        }
                        else
                        {
                            actualCustomCount += alltemplates.Length;
                        }
                    }
                    catch
                    {
                        // 忽略无法访问的目录
                    }
                }

                progress?.ReportLog($"{LanguageManager.Instance.GetText("LOG.ModTemplateCount", "mod模板：")} {actualModCount}");
                progress?.ReportLog($"{LanguageManager.Instance.GetText("LOG.CustomTemplateCount", "自定义模板：")} {actualCustomCount}");

                try
                {
                    templates = Program.BuildTemplateDict(tfolders, templates); // 构建模板字典
                }
                catch (Exception e)
                {
                    progress?.ReportError($"{LanguageManager.Instance.GetText("LOG.TemplateParseError", "解析模板时出错")}: {e.Message}");
                    return false;
                }

                // 填充模板包含列表
                foreach (KeyValuePair<string, TemplateNode> tpln in templates)
                {
                    tpln.Value.FillIncludes(templates);
                }

                progress?.Report(20, $"{LanguageManager.Instance.GetText("LOG.PackingDatabaseStart", "开始打包数据库")}...");

                string outputBuildPath = buildPath ?? modPath;
                string modName = Path.GetFileName(modPath);
                string outputFile = Path.Combine(outputBuildPath, "database", modName + ".arz");

                // 确保输出目录存在
                string outputDir = Path.GetDirectoryName(outputFile);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // 打包dbr文件
                ARZWriter arzw = new ARZWriter(templates);
                try
                {
                    int processedCount = 0;
                    
                    foreach (string dbrfile in selectedDbrFiles)
                    {
                        try
                        {
                            string[] recstrings = File.ReadAllLines(dbrfile, Encoding.GetEncoding("GBK"));
                            string recordName = Program.DbrFileToRecName(dbrfile, modPath);
                            
                            arzw.WriteFromLines(recordName, recstrings);
                            processedCount++;
                            
                            // 计算进度百分比
                            int percentage = (int)((double)processedCount / selectedDbrFiles.Count * 70) + 20;
                            string packfolderDB = Path.Combine(modPath, "database");
                            string relativePath = dbrfile.Substring(packfolderDB.Length).TrimStart(Path.DirectorySeparatorChar);
                            
                            progress?.Report(percentage, $"{LanguageManager.Instance.GetText("LOG.Packing", "正在打包")} ({processedCount}/{selectedDbrFiles.Count}) {relativePath}");
                        }
                        catch (Exception e)
                        {
                            progress?.ReportWarning($"{LanguageManager.Instance.GetText("LOG.ErrorPackingFile", "打包文件时出错")}: {dbrfile}, {e.Message}");
                            // 继续处理下一个文件
                        }
                    }
                }
                catch (Exception e)
                {
                    progress?.ReportError($"{LanguageManager.Instance.GetText("LOG.ErrorWhileParsingRecords", "解析记录时出错，请检查dbr文件与模板是否匹配")}: {e.Message}");
                    return false;
                }

                // 保存数据
                progress?.Report(90, $"{LanguageManager.Instance.GetText("LOG.SavingDatabase", "保存数据库")}...");
                try
                {
                    using (FileStream fs = new FileStream(outputFile, FileMode.Create))
                    {
                        arzw.SaveToStream(fs);
                    }
                }
                catch (Exception e)
                {
                    progress?.ReportError($"{LanguageManager.Instance.GetText("LOG.ErrorSavingDatabase", "保存数据库时出错")}: {e.Message}");
                    return false;
                }

                TimeSpan duration = DateTime.Now - startTime;
                progress?.Report(100, $"{LanguageManager.Instance.GetText("LOG.PackingCompleteUseTime", "打包完成，耗时")}: {duration.TotalSeconds:F2}s");
                return true;
            }
            catch (Exception ex)
            {
                TimeSpan duration = DateTime.Now - startTime;
                progress?.ReportError($"{LanguageManager.Instance.GetText("LOG.PackingFailedUseTime", "打包失败，耗时")}:{duration.TotalSeconds:F2}s , {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// 进度回调接口
    /// </summary>
    public interface IProgressCallback
    {
        /// <summary>
        /// 报告进度
        /// </summary>
        /// <param name="percentage">进度百分比</param>
        /// <param name="message">进度消息</param>
        void Report(int percentage, string message);

        /// <summary>
        /// 报告普通日志信息
        /// </summary>
        /// <param name="message">日志消息</param>
        void ReportLog(string message);

        /// <summary>
        /// 报告错误
        /// </summary>
        /// <param name="error">错误消息</param>
        void ReportError(string error);
        
        /// <summary>
        /// 报告警告
        /// </summary>
        /// <param name="warning">警告消息</param>
        void ReportWarning(string warning);
    }
}