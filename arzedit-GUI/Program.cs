using System;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using K4os.Compression.LZ4;
using System.Linq;
using System.Text;
using CommandLine;
using CommandLine.Text;
using NLog;
using System.Globalization;
using System.Reflection;




namespace arzedit
{   
    class Program
    {   
        

        public const string VERSION = "0.2b5.2";
        public const string GUI_VERSION = "GUI_v1.0";
        static byte[] footer = new byte[16];
        public static Logger Log = LogManager.GetCurrentClassLogger();
        public static List<string> strtable = null;
        public static ARZStrings astrtable = null;
        public static List<int> strrefcount = null;
        public static SortedDictionary<string, int> strsearchlist = null;
        public static HashSet<string> resfiles = null;
        public static HashSet<string> dbrfiles = null;
        [STAThread]
        static int Main(string[] args)
        {
            // 注册编码提供程序以支持GBK编码
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            
            // 如果没有命令行参数，或者包含--gui参数，则启动GUI模式
            if (args.Length == 0 || args.Contains("--gui"))
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new GuiMainForm());
                return 0;
            }

            // 解析命令行参数
            return Parser.Default.ParseArguments<SetOptions, GetOptions, ExtractOptions, PackOptions, BuildOptions, UnarcOptions, ArcOptions>(args)
                .MapResult(
                    (SetOptions opts) => ProcessSetVerb(opts),
                    // (GetOptions opts) => ProcessGetVerb(opts), // 注释掉不存在的方法
                    (ExtractOptions opts) => ProcessExtractVerb(opts),
                    (PackOptions opts) => ProcessPackVerb(opts),
                    (BuildOptions opts) => ProcessBuildVerb(opts),
                    (UnarcOptions opts) => ProcessUnarcVerb(opts),
                    (ArcOptions opts) => ProcessArcVerb(opts),
                    errors => 1 // 如果解析失败，返回错误代码1
                );
        }

        // 保留原来的GUI入口点作为兼容性支持
        [STAThread]
        static void MainGUI()
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GuiMainForm());
        }


        static int ProcessSetVerb(SetOptions opt)
        {
            Log.Error("Setting records is not implemented yet");
            return 1;
        }

        static int ProcessExtractVerb(ExtractOptions opt) {
            //using ()
            byte[] mdata = File.ReadAllBytes(opt.InputFile);
            // DateTime start = DateTime.Now;
            using (MemoryStream memory = new MemoryStream(mdata))
            {
                ARZReader arzr = new ARZReader(memory);

                string outpath = null;
                if (string.IsNullOrEmpty(opt.OutputPath))
                    outpath = Directory.GetCurrentDirectory();
                else
                    outpath = Path.GetFullPath(opt.OutputPath);

                //Console.WriteLine("Extracting to \"{0}\" ...", outpath);
                Log.Info("Extracting to \"{0}\" ...", outpath);

                char ans = 'n';
                bool overwriteall = opt.ForceOverwrite;
                using (ProgressBar progress = new ProgressBar())
                {
                    // foreach (ARZRecord rec in rectable)
                    for (int i = 0; i < arzr.Count; i++)
                    {
                        ARZRecord rec = arzr[i]; 
                        string filename = Path.Combine(outpath, rec.Name.Replace('/', Path.DirectorySeparatorChar));
                        // Console.WriteLine("Writing \"{0}\"", filename); // Debug
                        Directory.CreateDirectory(Path.GetDirectoryName(filename));
                        bool fileexists = File.Exists(filename);
                        if (!overwriteall && fileexists)
                        {
                            progress.SetHidden(true);
                            ans = char.ToLower(Ask(string.Format("File \"{0}\" exists, overwrite? yes/no/all/cancel (n): ", filename), "yYnNaAcC", 'n'));
                            if (ans == 'c')
                            {
                                //Console.WriteLine("Aborted by user");
                                Log.Error("Aborted by user");
                                return 1;
                            };
                            progress.SetHidden(false);
                            overwriteall = ans == 'a';
                        }

                        if (!fileexists || overwriteall || ans == 'y')
                        {
                            try
                            {
                                rec.SaveToFile(filename);
                                File.SetCreationTime(filename, rec.rdFileTime); // TODO: Check if this is needed
                            }
                            catch (Exception e)
                            {
                                // Console.WriteLine("Error writing file \"{0}\", Message: ", filename, e.Message);
                                Log.Error("Could not write file \"{0}\", Message: ", filename, e.Message);
                                return 1;
                            }
                        }

                        rec.DiscardData();
                        progress.Report(((double)i) / arzr.Count);
                    }
                }
            }
            // DateTime end = DateTime.Now;
            // Console.WriteLine("Done ({0:c})", (TimeSpan)(end - start));
            return 0;
        }

        static int BuildAssets(string assetfolder, string sourcefolder, string buildfolder, string gamefolder)
        {
            if (!Directory.Exists(assetfolder))
            {
                Log.Warn("No asset folder {0}. Skipping compilation.", assetfolder);
                return 1;
            }
            string[] assetfiles = Directory.GetFiles(assetfolder, "*", SearchOption.AllDirectories);
            int ci = 0;
            using (ProgressBar progress = new ProgressBar())
            {
                foreach (string afile in assetfiles)
                {
                    AssetBuilder abuilder = new AssetBuilder(afile, assetfolder, gamefolder);
                    abuilder.CompileAsset(sourcefolder, buildfolder);
                    progress.Report((double)ci++ / assetfiles.Length);
                }
            }
            return 0;
        }

        /// <summary>
        /// 兼容层方法，保持向后兼容性
        /// </summary>
        internal static int ProcessBuildVerbWithSelectedFiles(BuildOptions options)
        {
            // 设置默认选项以确保正确处理
            options.SkipAssets = true;
            options.SkipDB = false;
            options.SkipResources = true;
            
            // 直接调用ProcessBuildVerb，它已经支持SelectedDbrFiles
            return ProcessBuildVerb(options);
        }

        internal static int ProcessBuildVerb(BuildOptions options)
        {
            string packfolder = ""; string modname = ""; string buildfolder = ""; string gamefolder = "";
            try
            {
                packfolder = Path.GetFullPath(options.ModPath);
                modname = Path.GetFileName(packfolder);
                buildfolder = string.IsNullOrEmpty(options.BuildPath) ? packfolder : Path.GetFullPath(options.BuildPath);
                //gamefolder = string.IsNullOrEmpty(options.GameFolder) ? Path.GetFullPath(".") : Path.GetFullPath(options.GameFolder); // TODO: Make game folder detection more robust, check dir one level up
            } catch {
                // Console.WriteLine("Error parsing parameters, check for escape characters, especially \\" (folders should not end in \\).");
                Log.Error("Error parsing parameters, check for escape characters, especially \" (folders should not end in ).");
            }
            
            if (!options.SkipAssets)
            {
                // Assets
                // Console.Write("Compiling Assets ... ");
                // if (BuildAssets(Path.Combine(packfolder, "assets"), Path.Combine(packfolder, "source"), Path.Combine(buildfolder, "resources"), gamefolder) != 0)
                //     Log.Warn("Error building assets.");
                // Console.WriteLine("Done");
            }
            
            // Process templates and database
            if (!options.SkipDB)
            {
                Log.Info(LanguageManager.Instance.GetText("LOG.ParsingTemplates", "解析模板 ..."));
                // DateTime start = DateTime.Now;

                Dictionary<string, TemplateNode> templates = new Dictionary<string, TemplateNode>();
                
                // 从嵌入式资源加载内置模板
                int builtInTemplateCount = LoadBuiltInTemplates(templates);
                Log.Info($"{LanguageManager.Instance.GetText("LOG.BuiltInTemplateCount", "内置模板：")} {builtInTemplateCount} ");

                List<string> templateFolders = new List<string>();
                int actualModCount = 0;
                int actualCustomCount = 0;

                // 总是先添加MOD模板路径
                templateFolders.Add(packfolder);
                
                // 然后添加自定义模板（如果有），这样自定义模板会覆盖同名的MOD模板
                if (options.TemplatePaths != null)
                {
                    templateFolders.AddRange(options.TemplatePaths);
                }
                
                string[] tfolders = templateFolders.ToArray();
                
                // 统计实际模板文件数量
                foreach (string tfolder in tfolders)
                {
                    try
                    {
                        string[] alltemplates = Directory.GetFiles(Path.GetFullPath(tfolder), "*.tpl", SearchOption.AllDirectories);
                        bool isModFolder = Path.GetFullPath(tfolder).Equals(Path.GetFullPath(packfolder), StringComparison.OrdinalIgnoreCase);
                          
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

                Log.Info($"{LanguageManager.Instance.GetText("LOG.ModTemplateCount", "mod模板：")} {actualModCount} ");
                
                Log.Info($"{LanguageManager.Instance.GetText("LOG.CustomTemplateCount", "自定义模板：")} {actualCustomCount} ");   

                try
                {
                    templates = BuildTemplateDict(tfolders, templates); // Build template dictionary from folders
                }
                catch (Exception e)
                {
                    Log.Error("Error parsing templates, reason - {0}\nStackTrace:\n{1}", e.Message, e.StackTrace);
                    return 1;
                }

                // Fill template include list
                foreach (KeyValuePair<string, TemplateNode> tpln in templates)
                {
                    tpln.Value.FillIncludes(templates);
                }

                Log.Info($"{LanguageManager.Instance.GetText("LOG.PackingDatabaseStart", "开始打包数据库 ... ")}");
                string[] alldbrs = null;
                
                // 优先使用用户选择的文件列表
                if (options.SelectedDbrFiles != null && options.SelectedDbrFiles.Length > 0)
                {
                    alldbrs = options.SelectedDbrFiles;
                    Log.Info($"{LanguageManager.Instance.GetText("LOG.UsingSelectedFilesCount", "使用用户选择的文件数：")} {alldbrs.Length}");
                }
                else
                {
                    try
                    {
                        alldbrs = Directory.GetFiles(packfolder, "*.dbr", SearchOption.AllDirectories);
                        Log.Info($"{LanguageManager.Instance.GetText("LOG.FoundAllDbrFilesCount", "找到所有dbr文件数：")} {alldbrs.Length}");
                    }
                    catch (Exception e)
                    {
                        Log.Error("Error listing *.dbr files, reason - {0} ", e.Message);
                        return 1;
                    }
                }
                


                // 使用并行处理替代原有的 for 循环
                ARZWriter arzw = new ARZWriter(templates);
                try
                {
                    int processedCount = 0;
                       
                    // 直接顺序处理所有dbr文件，移除分批和并行处理
                    foreach (string dbrfile in alldbrs)
                    {
                        string[] recstrings = File.ReadAllLines(dbrfile, Encoding.GetEncoding("GBK"));
                        string recordName = DbrFileToRecName(dbrfile, packfolder);
                           
                        arzw.WriteFromLines(recordName, recstrings);
                        processedCount++;
                        string packfolderDB = Path.Combine(packfolder, "database"); // 添加固定目录
                        string relativePath = dbrfile.Substring(packfolderDB.Length).TrimStart(Path.DirectorySeparatorChar);
                        Log.Info($"{LanguageManager.Instance.GetText("LOG.Packing", "正在打包")}：({processedCount}/{alldbrs.Length}) {relativePath}");
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"{LanguageManager.Instance.GetText("LOG.ErrorWhileParsingRecords", "解析记录时出错，请检查dbr文件与模板是否匹配")}");
                    return 1;
                }

                // 保存数据库文件
                string dbfolder = Path.Combine(buildfolder, "database");
                if (!Directory.Exists(dbfolder))
                    Directory.CreateDirectory(dbfolder);
                string outputfile = Path.Combine(dbfolder, modname + ".arz");
                using (FileStream fs = new FileStream(outputfile, FileMode.Create))
                    arzw.SaveToStream(fs);

                Log.Info($"{LanguageManager.Instance.GetText("LOG.BuildSuccessful", "构建成功")}");
                
                // 确保函数有正确的返回值
                return 0;
            }

            if (!options.SkipResources)
            {
                Log.Info("Packing resources ...");
                string resfolder = Path.GetFullPath(Path.Combine(buildfolder, "resources"));
                if (Directory.Exists(resfolder))
                {
                    string[] resdirs = Directory.GetDirectories(resfolder, "*", SearchOption.TopDirectoryOnly);
                    foreach (string resdir in resdirs)
                    {
                        string outfile = Path.Combine(resfolder, Path.GetFileName(resdir) + ".arc");
                        Log.Info("Packing folder \"{0}\", to: \"{1}\"", resdir, outfile);
                        ArcFolder(resdir, "*", outfile);
                    }
                }
                else {
                    Log.Warn("Resource folder {0} not found, skipping.", resfolder);
                }
                //Console.WriteLine("Done");
                Log.Info("Done");
            }
            return 0;
        }

        static int ProcessPackVerb(PackOptions opt)
        {
            string packfolder = Path.GetFullPath(opt.InputPath);
            Log.Info("Packing is broken right now.");
            return 1;
            
            if (string.IsNullOrEmpty(opt.OutputFile))
            {
                //Console.WriteLine("Please specify output file as second parameter!");
                Log.Error("Please specify output file as second parameter!");
                PrintUsage();
                return 1;
            }

            // Process templates
            string[] tfolders = null;
            if (opt.TemplatePaths != null)
            {
                tfolders = opt.TemplatePaths.ToArray();
            }
            else
            {
                tfolders = new string[1] { opt.InputPath };
            }

            Log.Info($"{LanguageManager.Instance.GetText("LOG.ParsingTemplates", "解析模板")} ... ");

            Dictionary<string, TemplateNode> templates = null;
            try
            {
                templates = BuildTemplateDict(tfolders);
            } catch (Exception e)
            {
                Console.WriteLine("Error parsing templates, reason - {0}\nStackTrace:\n{1}", e.Message, e.StackTrace);
                return 1;
            }

            Log.Info($"{LanguageManager.Instance.GetText("LOG.Packing", "正在打包")} ... ");
            // start = DateTime.Now;
            string[] alldbrs = null;
            try
            {
                alldbrs = Directory.GetFiles(packfolder, "*.dbr", SearchOption.AllDirectories);
            }
            catch (Exception e) {
                Log.Error("Error listing *.dbr files, reason - {0} ", e.Message);
                return 1;
            }

            /*
            if (opt.CheckReferences)
            {
                dbrfiles = new HashSet<string>();
                foreach (string dbrfile in alldbrs)
                {
                    dbrfiles.Add(DbrFileToRecName(dbrfile, packfolder));
                }
            }*/

            ARZWriter arzw = new ARZWriter(templates);
            try
            {
                using (ProgressBar progress = new ProgressBar())
                {
                    for (int cf = 0; cf < alldbrs.Length; cf++)
                    {
                        string dbrfile = alldbrs[cf];
                        string[] recstrings = File.ReadAllLines(dbrfile, Encoding.GetEncoding("GBK"));
                        arzw.WriteFromLines(DbrFileToRecName(dbrfile, packfolder), recstrings);
                        progress.Report((double)cf / alldbrs.Length);
                    } // for int cf = 0 ...
                } // using progress
            }
            catch (Exception e)
            {
                Log.Error("Error while parsing records. Message: {0}\nStack Trace:\n{1}", e.Message, e.StackTrace);
                return 1;
            }

            // Save Data
            if (!AskSaveData(arzw, opt.OutputFile, opt.ForceOverwrite)) return 1;

            // Pack Resources
            /*
            string resfolder = Path.GetFullPath(Path.Combine(packfolder, "resources"));
            if (Directory.Exists(resfolder)) {
                Console.WriteLine("Packing resources:");
                string[] resdirs = Directory.GetDirectories(resfolder, "*", SearchOption.TopDirectoryOnly);
                foreach (string resdir in resdirs)
                {
                    string outfile = Path.Combine(resfolder, Path.GetFileName(resdir) + ".arc");
                    Console.WriteLine("Packing \"{0}\", to: \"{1}\"", resdir, outfile);
                    ArcFolder(resdir, "*", outfile);
                }
            }
            */
            return 0;
        } // ProcessPackVerb

        static int ProcessUnarcVerb(UnarcOptions opt)
        {
            string outpath = ".";
            if (!string.IsNullOrEmpty(opt.OutPath)) outpath = opt.OutPath;
            outpath = Path.GetFullPath(outpath);

            if (opt.ArcFiles.Count() == 0)
            {
                Log.Info("Please supply at least one arc file for extraction!");
                return 1;
            }
            
            foreach (string arcfilename in opt.ArcFiles)
            {
                Log.Info($"{LanguageManager.Instance.GetText("LOG.Unpacking", "正在解包")} \"{arcfilename}\"");
                string arcsub = "";
                if (opt.ArcFiles.Count() > 1 || string.IsNullOrEmpty(opt.OutPath))
                    arcsub = Path.Combine(outpath, Path.GetFileNameWithoutExtension(arcfilename));
                else
                    arcsub = outpath;
                ARCFile archive = new ARCFile();
                using (FileStream arcstream = new FileStream(arcfilename, FileMode.Open))
                {
                    archive.ReadStream(arcstream);
                    archive.UnpackAll(arcsub);
                }

            }
            return 0;
        }

        static int ProcessArcVerb(ArcOptions opt)
        {
            if (!string.IsNullOrEmpty(opt.Folder))
            {
                string afolder = Path.GetFullPath(opt.Folder);
                if (!Directory.Exists(afolder)) {
                    Log.Error($"{LanguageManager.Instance.GetText("LOG.CannotFindFolder", "无法找到文件夹")} \"{afolder}\"");
                    return 1;
                }
                if (string.IsNullOrEmpty(opt.FileMask))
                    opt.FileMask = "*";

                string outfile = Path.GetFullPath(Path.GetFileName(afolder)+".arc");
                if (!string.IsNullOrEmpty(opt.OutFile))
                    outfile = Path.GetFullPath(opt.OutFile);
                Log.Info("Packing \"{0}\" mask: \"{1}\", out: \"{2}\"", afolder, opt.FileMask, outfile);
                ArcFolder(afolder, opt.FileMask, outfile);
            } else return 1;
            return 0;
        }

        public static string DbrFileToRecName(string dbrfile, string packfolder)
        {
            string recname = dbrfile.Substring(packfolder.Length).ToLower().Replace(Path.DirectorySeparatorChar, '/').TrimStart('/');
            if (recname.StartsWith("database/")) recname = recname.Substring("database/".Length);
            return recname;
        }

        static bool AskSaveData(ARZWriter arzw, string outputfile, bool force = false)
        {
            if (force || !File.Exists(outputfile) || char.ToUpper(Ask(string.Format("Output file \"{0}\" exists, overwrite? [y/n] n: ", Path.GetFullPath(outputfile)), "yYnN", 'N')) == 'Y')
            {
                using (FileStream fs = new FileStream(outputfile, FileMode.Create))
                    arzw.SaveToStream(fs);
                return true;
            }
            else
            {
                Log.Info("Aborted by user.");
                return false;
            }
        }


        // 2025-8-27修改，当遇到无法解析的模板时，将无法解析的模板记录下来，跳过
        public static Dictionary<string, TemplateNode> BuildTemplateDict(string[] tfolders, Dictionary<string, TemplateNode> templates = null)
        {
            if (templates == null) templates = new Dictionary<string, TemplateNode>();
            foreach (string tfolder in tfolders)
            {
                string tfullpath = Path.GetFullPath(tfolder).TrimEnd(Path.DirectorySeparatorChar);
                // string tfullpath = Path.GetFullPath(tfolder);
                string tbasepath = tfullpath;
                // If we are not starting at the base, go up.
                // TODO: what about mods not using /database for templates?
                string dirname = Path.GetFileName(tbasepath);
                if (Path.GetFileName(tbasepath) == "templates")
                    tbasepath = Path.GetFullPath(Path.Combine(tbasepath, ".."));
                if (Path.GetFileName(tbasepath) == "database")
                    tbasepath = Path.GetFullPath(Path.Combine(tbasepath, ".."));

                // string debugpath = Path.Combine(tbasepath, "templates");
                if (!Directory.Exists(Path.Combine(tbasepath, "database")) && !Directory.Exists(Path.Combine(tbasepath, "templates")))
                {
                    Log.Info($"{LanguageManager.Instance.GetText("LOG.PossiblyWrongTemplateBaseFolder", "可能是错误的模板基础文件夹")} \"{tbasepath}\"");
                }

                string[] alltemplates = Directory.GetFiles(tfullpath, "*.tpl", SearchOption.AllDirectories);
                foreach (string tfile in alltemplates)
                {
                    try
                    {
                        string[] tstrings = File.ReadAllLines(tfile);
                        string tpath = tfile.Substring(tbasepath.Length).ToLower().Replace(Path.DirectorySeparatorChar, '/');
                        if (tpath.StartsWith("/")) tpath = tpath.Substring(1);
                        // TODO: sort out database prefix                            

                        if (!tpath.StartsWith("database/"))
                        {
                            tpath = "database/" + tpath;
                        }

                        TemplateNode ntemplate = new TemplateNode(null, tpath);
                        ntemplate.ParseNode(tstrings, 0);
                        templates[tpath] = ntemplate;
                    }
                    catch (Exception ex)
                    {
                        Log.Warn($"\"{tfile}\" {LanguageManager.Instance.GetText("LOG.TemplateParseError", "模板解析错误，将跳过：")} {ex.Message}");
                        continue;
                    }
                }
            }
            return templates;
        } // BuildTemplateDict



        // Utility function
        static char Ask(string question, string answers, char adefault) {
            Console.Write(question);
            ConsoleKeyInfo key = Console.ReadKey();
            while (!answers.Contains(key.KeyChar) && key.Key != ConsoleKey.Enter)
                key = Console.ReadKey();
            //Console.WriteLine();
            if (key.Key == ConsoleKey.Enter) return adefault;
            return key.KeyChar;
        }

        // Packs all files in a folder to arc file: 

        internal static void ArcFolder(string afolder, string afilemask, string outfilename)
        {
            afolder = Path.GetFullPath(afolder);
            using (FileStream ofs = new FileStream(outfilename, FileMode.Create))
            using (ARCWriter awriter = new ARCWriter(ofs))
            {
                string[] afiles = Directory.GetFiles(afolder, afilemask, SearchOption.AllDirectories);
                foreach (string afile in afiles)
                {
                    DateTime afiletime = File.GetLastWriteTime(afile);
                    string aentry = Path.GetFullPath(afile).Substring(afolder.Length).Replace(Path.DirectorySeparatorChar, '/').ToLower().TrimStart('/');
                    // string fname;
                    // if (Path.GetFileName(afile) == "dermapteranwall01_nml.tex")
                    // fname = afile;
                    using (FileStream ifs = new FileStream(afile, FileMode.Open))
                    {
                        ifs.Seek(0, SeekOrigin.Begin); // Go to start
                        awriter.WriteFromStream(aentry, afiletime, ifs); // Pack and write
                    }
                }
            }
        }


        static void PrintUsage()
        {
            Console.WriteLine("\nGrim Dawn Arz Editor, v{0}", VERSION);
            Console.WriteLine("\nUsage:");
            Console.WriteLine();
            Console.WriteLine("{0} <build|extract|arc|unarc> <suboptions>\n", Path.GetFileName(System.AppDomain.CurrentDomain.FriendlyName));
            Console.WriteLine("build <mod base> [<build path>] [-g <Grim Dawn folder>] [-t <additional template folders>] [-ADRvs] [-l <log file>]");
            Console.WriteLine("  <mod base>         folder that contains mod sources");
            Console.Write("  <build path>       folder where to put built mod files");
            var ht = new HelpText();
            ht.AddDashesToOption = true;
            // 移除不必要的AddOptions调用
            Console.WriteLine(ht);
            Console.WriteLine("extract <input file> [<output path>] [-y]");
            Console.WriteLine("<input file>         arz file to be extracted");
            Console.Write("<output path>        where to store dbr files");
            ht = new HelpText();
            ht.AddDashesToOption = true;
            // 移除不必要的AddOptions调用
            Console.WriteLine(ht);
            Console.Write("arc <arc folder> <arc file> [-m <file mask>]");
            ht = new HelpText();
            ht.AddDashesToOption = true;
            // 移除不必要的AddOptions调用
            Console.WriteLine(ht);
            Console.Write("unarc <arc file1> [<arc file2> ...] [-o <output path>]");
            ht = new HelpText();
            ht.AddDashesToOption = true;
            // 移除不必要的AddOptions调用
            Console.WriteLine(ht);
            // Console.ReadKey(true);
        }

        /// <summary>
        /// 从嵌入式资源加载内置模板
        /// </summary>
        /// <param name="templates">模板字典，加载的模板将添加到这里</param>
        /// <returns>加载的模板数量</returns>
        public static int LoadBuiltInTemplates(Dictionary<string, TemplateNode> templates)
        {
            int count = 0;
            try
            {
                // 获取当前程序集
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                
                // 获取所有嵌入式资源名称
                var resourceNames = assembly.GetManifestResourceNames();
                
                // 查找嵌入式ARC文件资源
                string arcResourceName = resourceNames.FirstOrDefault(r => r.EndsWith("templates.arc", StringComparison.OrdinalIgnoreCase));
                if (arcResourceName != null)
                {
                    try
                    {
                        // 读取ARC文件资源
                        using (Stream arcStream = assembly.GetManifestResourceStream(arcResourceName))
                        {
                            if (arcStream != null)
                            {
                                ARCFile arcFile = new ARCFile();
                                arcFile.ReadStream(arcStream);
                                
                                // 遍历ARC文件中的所有条目
                                foreach (ARCTocEntry aentry in arcFile.toc)
                                {
                                    try
                                    {
                                        string entryname = aentry.GetEntryString(arcFile.strs);
                                        // 只处理.tpl文件
                                        if (!string.IsNullOrEmpty(entryname) && entryname.ToLower().EndsWith(".tpl"))
                                        {
                                            List<string> strlist = new List<string>();
                                            using (MemoryStream tplmem = new MemoryStream())
                                            {
                                                arcFile.UnpackToStream(aentry, tplmem);
                                                tplmem.Seek(0, SeekOrigin.Begin);
                                                using (StreamReader sr = new StreamReader(tplmem))
                                                {
                                                    while (!sr.EndOfStream)
                                                    {
                                                        strlist.Add(sr.ReadLine());
                                                    }
                                                }
                                            }

                                            if (strlist.Count > 0)
                                            {
                                                // 解析模板
                                            TemplateNode node = new TemplateNode(null, entryname);
                                            node.ParseNode(strlist.ToArray());
                                             
                                            // 标准化模板键格式，确保与BuildTemplateDict方法一致
                                            string templateKey = entryname.ToLower().Replace('\\', '/');
                                            if (!templateKey.StartsWith("database/templates/"))
                                            {
                                                templateKey = "database/templates/" + templateKey;
                                            }
                                             
                                            // 添加到模板字典中
                                            if (!templates.ContainsKey(templateKey))
                                            {
                                                templates.Add(templateKey, node);
                                                count++;
                                            }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error("Failed to load built-in template: {0}. Error: {1}", aentry.GetEntryString(arcFile.strs), ex.Message);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error loading built-in ARC template file: {0}", ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error loading built-in templates: {0}", ex.Message);
            }
            
            return count;
        }
    }
    
}