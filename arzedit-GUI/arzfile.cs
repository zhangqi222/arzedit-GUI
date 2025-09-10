using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using K4os.Compression.LZ4;
using System.Text;
using System.Threading.Tasks;


namespace arzedit {
    // Classes responsible for Reading/Writing arz database, and database objects themselves

    /// <summary>
    /// ARZ文件操作主类，封装读取和条目列表功能
    /// </summary>
    public class ARZFile
    {
        private ARZReader reader;
        
        /// <summary>
        /// 从流中读取ARZ文件内容
        /// </summary>
        /// <param name="stream">ARZ文件流</param>
        public void ReadStream(Stream stream)
        {
            reader = new ARZReader(stream);
        }
        
        /// <summary>
        /// 获取ARZ文件中所有条目名称列表
        /// </summary>
        /// <returns>条目名称列表</returns>
        public List<string> ListAllEntries()
        {
            if (reader == null)
                return new List<string>();
                
            return reader.GetAllRecordNames();
        }
        
        /// <summary>
        /// 分批获取ARZ文件中所有条目名称
        /// </summary>
        /// <param name="batchSize">每批处理的记录数量</param>
        /// <returns>条目名称的迭代器</returns>
        public IEnumerable<string> ListAllEntriesInBatches(int batchSize = 1000)
        {
            if (reader == null)
                yield break;
                
            foreach (string recordName in reader.GetRecordNamesInBatches(batchSize))
            {
                yield return recordName;
            }
        }
        
        /// <summary>
        /// 通过索引范围获取ARZ文件中条目名称
        /// </summary>
        /// <param name="startIndex">起始索引</param>
        /// <param name="count">要获取的记录数量</param>
        /// <returns>指定范围的条目名称列表</returns>
        public List<string> ListEntriesByRange(int startIndex, int count)
        {
            if (reader == null)
                return new List<string>();
                
            return reader.GetRecordNamesByRange(startIndex, count);
        }
        
        /// <summary>
        /// 处理ARZ文件中的所有记录（移除了分批和并行限制）
        /// </summary>
        /// <param name="processor">处理单个记录的函数</param>
        public void ProcessRecordsInParallel(Action<string, ARZRecord> processor)
        {
            if (reader == null || processor == null)
                return;
            
            // 确保记录表已加载
            reader.EnsureRecordTableIsLoaded();
            int totalRecords = reader.Count;
            
            // 顺序处理所有记录，不使用分批和并行处理
            for (int i = 0; i < totalRecords; i++)
            {
                try
                {
                    // 获取记录名称
                    string recordName = reader.GetRecordName(i);
                    
                    // 按需加载记录数据
                    ARZRecord record = reader.GetRecord(i);
                    
                    // 处理记录
                    processor(recordName, record);
                    
                    // 使用完后释放记录数据，节省内存
                    record.DiscardData();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("处理记录时出错: " + ex.Message);
                }
            }
        }
        
        /// <summary>
        /// 异步获取记录总数
        /// </summary>
        /// <returns>记录总数</returns>
        public Task<int> GetRecordCountAsync()
        {
            return Task.Run(() =>
            {
                if (reader == null)
                    return 0;
                
                reader.EnsureRecordTableIsLoaded();
                return reader.Count;
            });
        }
    }

    public class ARZWriter
    {
        public ARZStrings strtable = null;
        int rcount = 0;
        MemoryStream mrecdata;
        MemoryStream mrectable;
        ARZHeader hdr;
        public Dictionary<string, TemplateNode> ftemplates = null;
        public ARZWriter(Dictionary<string, TemplateNode> atemplates = null)
        {
            ftemplates = atemplates;
            hdr = new ARZHeader();
            strtable = new ARZStrings();
            mrectable = new MemoryStream();
            mrecdata = new MemoryStream();
        }

        public void BeginWrite()
        {
            // hdr.WriteToStream(ost);
        }

        public void WriteFromRecord(ARZRecord rec)
        {
            ARZRecord clone = new ARZRecord(rec, strtable);
            WriteRecord(clone);
        }

        public void WriteRecord(ARZRecord rec)
        {
            rec.rdOffset = (int)mrecdata.Position; 
            mrecdata.Write(rec.cData, 0, rec.rdSizeCompressed); // Write Record Data
            rec.WriteToStream(mrectable); // Write to record table
            rcount += 1;
        }

        public void WriteFromLines(string recordname, string[] lines)
        {
            // if (recordname.Contains(@"petskill_demo_attack.dbr"))
               // Console.WriteLine("BUG:");
            ARZRecord nrec = new ARZRecord(recordname, lines, ftemplates, strtable);
            WriteRecord(nrec);
        }

        public void SaveToStream(Stream ost)
        {
            // Prepare header
            hdr.RecordTableStart = ARZHeader.HEADER_SIZE + (int)mrecdata.Length;
            hdr.RecordTableSize = (int)mrectable.Length;
            hdr.RecordTableEntries = rcount;
            MemoryStream mstrtable = new MemoryStream();
            strtable.WriteToStream(mstrtable);
            hdr.StringTableStart = hdr.RecordTableStart + hdr.RecordTableSize;
            hdr.StringTableSize = (int)mstrtable.Length;
            // Write header
            ost.Seek(0, SeekOrigin.Begin);
            MemoryStream mhdr = new MemoryStream(ARZHeader.HEADER_SIZE);
            hdr.WriteToStream(mhdr);

            // Checksums:
            Adler32 hashall = new Adler32();
            byte[] buf = mhdr.GetBuffer();
            hashall.ComputeHash(buf, 0, (int)mhdr.Length);
            buf = mrecdata.GetBuffer();
            hashall.ComputeHash(buf, 0, (int)mrecdata.Length);
            uint hrdata = (new Adler32()).ComputeHash(buf, 0, (int)mrecdata.Length);
            buf = mrectable.GetBuffer();
            hashall.ComputeHash(buf, 0, (int)mrectable.Length);
            uint hrtable = (new Adler32()).ComputeHash(buf, 0, (int)mrectable.Length);
            buf = mstrtable.GetBuffer();
            hashall.ComputeHash(buf, 0, (int)mstrtable.Length);
            uint hstable = (new Adler32()).ComputeHash(buf, 0, (int)mstrtable.Length);
            uint hall = hashall.checksum;
            
            // Write data
            mhdr.WriteTo(ost);
            mrecdata.WriteTo(ost);
            mrectable.WriteTo(ost);
            mstrtable.WriteTo(ost);

            // Write Footer:
            using (BinaryWriter bw = new BinaryWriter(ost, Encoding.GetEncoding("GBK"), true))
            {
                bw.Write(hashall.checksum);
                bw.Write(hstable);
                bw.Write(hrdata);
                bw.Write(hrtable);
            }
        }        
    }

    public class ARZReader {        
        private Stream fstream = null;
        public ARZHeader hdr = null;
        private ARZStrings strtable = null;
        private List<ARZRecordInfo> recordInfos = null; // 只存储记录的元信息，不加载完整记录
        private Dictionary<int, ARZRecord> loadedRecords = new Dictionary<int, ARZRecord>(); // 已加载的记录缓存
        private Queue<int> recordAccessOrder = new Queue<int>(); // 记录访问顺序，用于LRU缓存
        private const int MAX_LOADED_RECORDS = 0; // 不限制缓存记录数量，0表示无限制
        private long recordTablePosition = 0; // 记录表位置，用于流式读取
        private bool isRecordTableLoaded = false; // 记录表是否已加载
        
        public ARZReader(Stream astream)
        {
            fstream = astream;
            Initialize();
        }

        public ARZRecord this[int i] { get { return GetRecord(i); } }

        public int Count { get { EnsureRecordTableLoaded(); return recordInfos != null ? recordInfos.Count : 0; } }

        /// <summary>
        /// 初始化，只读取文件头部信息
        /// </summary>
        private void Initialize()
        {
            // 只读取文件头部
            fstream.Seek(0, SeekOrigin.Begin);
            hdr = new ARZHeader(fstream);
            
            // 保存记录表位置，但不立即读取
            recordTablePosition = hdr.RecordTableStart;
            
            // 延迟初始化字符串表
            strtable = new ARZStrings(fstream, hdr.StringTableStart, hdr.StringTableSize);
        }

        /// <summary>
        /// 确保记录表已加载
        /// </summary>
        private void EnsureRecordTableLoaded()
        {
            if (!isRecordTableLoaded && fstream != null && hdr != null)
            {
                fstream.Seek(recordTablePosition, SeekOrigin.Begin);
                recordInfos = ReadRecordInfoTable(fstream, hdr.RecordTableEntries);
                isRecordTableLoaded = true;
            }
        }
        
        /// <summary>
        /// 公开方法：确保记录表已加载（供外部调用）
        /// </summary>
        public void EnsureRecordTableIsLoaded()
        {
            EnsureRecordTableLoaded();
        }
        
        /// <summary>
        /// 读取记录信息表，只存储元数据，不创建完整的ARZRecord对象
        /// </summary>
        private List<ARZRecordInfo> ReadRecordInfoTable(Stream astream, int rcount)
        {
            List<ARZRecordInfo> infoList = new List<ARZRecordInfo>();
            infoList.Capacity = (int)rcount;
            
            // 保存当前流位置
            long currentPosition = astream.Position;
            
            using (BinaryReader br = new BinaryReader(astream, Encoding.GetEncoding("GBK"), true))
            {
                for (int i = 0; i < rcount; i++)
                {
                    // 只读取记录的元数据，不创建完整的ARZRecord对象
                    int rfid = br.ReadInt32();
                    int rtypelen = br.ReadInt32();
                    // 使用GBK编码读取字节数组并解码为字符串
                    byte[] typeBytes = br.ReadBytes(rtypelen);
                    string rtype = Encoding.GetEncoding("GBK").GetString(typeBytes);
                    int rdOffset = br.ReadInt32();
                    int rdSizeCompressed = br.ReadInt32();
                    int rdSizeDecompressed = br.ReadInt32();
                    DateTime rdFileTime = DateTime.FromFileTimeUtc(br.ReadInt64());
                    
                    infoList.Add(new ARZRecordInfo(rfid, rtype, rdOffset, rdSizeCompressed, rdSizeDecompressed, rdFileTime));
                }
            }
            
            // 恢复流位置
            astream.Seek(currentPosition, SeekOrigin.Begin);
            return infoList;
        }
        
        /// <summary>
        /// 记录元信息类，用于存储记录的基本信息而不加载完整数据
        /// </summary>
        private class ARZRecordInfo
        {
            public int rfid;
            public string rtype;
            public int rdOffset;
            public int rdSizeCompressed;
            public int rdSizeDecompressed;
            public DateTime rdFileTime;
            
            public ARZRecordInfo(int rfid, string rtype, int rdOffset, int rdSizeCompressed, int rdSizeDecompressed, DateTime rdFileTime)
            {
                this.rfid = rfid;
                this.rtype = rtype;
                this.rdOffset = rdOffset;
                this.rdSizeCompressed = rdSizeCompressed;
                this.rdSizeDecompressed = rdSizeDecompressed;
                this.rdFileTime = rdFileTime;
            }
            
            /// <summary>
            /// 根据元信息创建完整的ARZRecord对象
            /// </summary>
            public ARZRecord CreateRecord(ARZStrings strtable)
            {
                ARZRecord record = new ARZRecord();
                record.rfid = rfid;
                record.rtype = rtype;
                record.rdOffset = rdOffset;
                record.rdSizeCompressed = rdSizeCompressed;
                record.rdSizeDecompressed = rdSizeDecompressed;
                record.rdFileTime = rdFileTime;
                record.strtable = strtable;
                return record;
            }
        }

        public ARZRecord GetRecord(int id) {            
            EnsureRecordTableLoaded();
            
            // 检查记录是否已经加载
            if (loadedRecords.TryGetValue(id, out ARZRecord loadedRecord))
            {
                // 更新记录访问顺序
                UpdateRecordAccessOrder(id);
                return loadedRecord;
            }
            
            // 按需创建并加载单个记录
            ARZRecordInfo info = recordInfos[id];
            ARZRecord record = info.CreateRecord(strtable);
            
            // 加载记录数据
            LoadRecordData(record);
            
            // 不限制缓存大小，只管理访问顺序用于LRU缓存（当需要手动清理时使用）
            // 当MAX_LOADED_RECORDS为0时不进行缓存清理
            
            // 缓存已加载的记录
            loadedRecords[id] = record;
            recordAccessOrder.Enqueue(id);
            
            return record;
        }

        // 原方法：一次性获取所有记录名称
        public List<string> GetAllRecordNames()
        {
            List<string> recordNames = new List<string>();
            EnsureRecordTableLoaded();
            if (recordInfos == null || strtable == null)
                return recordNames;

            // 确保字符串表已加载
            strtable.EnsureLoaded();

            foreach (var recordInfo in recordInfos)
            {
                // 通过字符串表获取记录名称
                string recordName = strtable[recordInfo.rfid];
                recordNames.Add(recordName);
            }
            return recordNames;
        }
        
        // 新增：分批获取记录名称，避免一次性加载所有名称导致内存不足
        public IEnumerable<string> GetRecordNamesInBatches(int batchSize = 1000)
        {
            EnsureRecordTableLoaded();
            if (recordInfos == null || strtable == null)
                yield break;

            // 确保字符串表已加载
            strtable.EnsureLoaded();

            // 分批处理记录名称
            List<string> batch = new List<string>(batchSize);
            for (int i = 0; i < recordInfos.Count; i++)
            {
                string recordName = strtable[recordInfos[i].rfid];
                batch.Add(recordName);
                
                // 当批次达到指定大小时，返回批次数据
                if (batch.Count >= batchSize)
                {
                    foreach (string name in batch)
                    {
                        yield return name;
                    }
                    batch.Clear();
                }
            }
            
            // 返回剩余的记录名称
            foreach (string name in batch)
            {
                yield return name;
            }
        }
        
        // 新增：通过索引范围获取记录名称
        public List<string> GetRecordNamesByRange(int startIndex, int count)
        {
            List<string> recordNames = new List<string>();
            EnsureRecordTableLoaded();
            if (recordInfos == null || strtable == null)
                return recordNames;

            // 确保字符串表已加载
            strtable.EnsureLoaded();
            
            // 计算实际的结束索引
            int endIndex = Math.Min(startIndex + count, recordInfos.Count);
            
            // 只加载指定范围的记录名称
            for (int i = startIndex; i < endIndex; i++)
            {
                string recordName = strtable[recordInfos[i].rfid];
                recordNames.Add(recordName);
            }
            
            return recordNames;
        }
        
        // 新增：通过索引获取单个记录名称
        public string GetRecordName(int index)
        {
            EnsureRecordTableLoaded();
            if (recordInfos == null || strtable == null || index < 0 || index >= recordInfos.Count)
                return string.Empty;

            // 确保字符串表已加载
            strtable.EnsureLoaded();
            
            // 获取指定索引的记录名称
            return strtable[recordInfos[index].rfid];
        }

        // 新增：按需获取记录，避免一次性加载所有记录数据
        public ARZRecord GetRecordLazy(int id)
        {
            EnsureRecordTableLoaded();
            
            // 检查记录是否已经加载
            if (loadedRecords.TryGetValue(id, out ARZRecord loadedRecord))
            {
                // 更新记录访问顺序
                UpdateRecordAccessOrder(id);
                return loadedRecord;
            }
            
            // 按需创建单个记录，但不加载数据
            ARZRecordInfo info = recordInfos[id];
            ARZRecord record = info.CreateRecord(strtable);
            
            // 不限制缓存大小，当MAX_LOADED_RECORDS为0时不进行缓存清理
            
            // 缓存已创建但未加载数据的记录
            loadedRecords[id] = record;
            recordAccessOrder.Enqueue(id);
            
            return record;
        }
        
        // 更新记录访问顺序，用于LRU缓存策略
        private void UpdateRecordAccessOrder(int recordId)
        {
            // 创建一个新的队列，移除旧的记录ID并添加到队列末尾
            Queue<int> newOrder = new Queue<int>();
            bool recordFound = false;
            
            foreach (int id in recordAccessOrder)
            {
                if (id != recordId)
                {
                    newOrder.Enqueue(id);
                }
                else
                {
                    recordFound = true;
                }
            }
            
            // 将当前记录ID添加到队列末尾
            newOrder.Enqueue(recordId);
            
            // 更新访问顺序队列
            recordAccessOrder = newOrder;
        }

        // 新增：按需加载记录数据
        public void LoadRecordData(ARZRecord record)
        {
            if (record.entries == null)
            {
                // 确保字符串表已加载，因为记录数据可能引用字符串表
                if (strtable != null)
                {
                    strtable.EnsureLoaded();
                }
                
                fstream.Seek(ARZHeader.HEADER_SIZE + record.rdOffset, SeekOrigin.Begin);
                using (BinaryReader br = new BinaryReader(fstream, Encoding.GetEncoding("GBK"), true))
                    record.ReadData(br);
            }
        }

    }

    public class ARZStrings {
        private List<string> strtable = null;
        private SortedDictionary<string, int> strsearchlist = null;
        private Stream stream = null;
        private long stringTableStart = 0;
        private int stringTableSize = 0;
        private bool isStringTableLoaded = false;

        public ARZStrings()
        {
            strtable = new List<string>();
        }

        public ARZStrings(Stream astream, int size) : this()
        {
            ReadStream(astream, size);
        }
        
        /// <summary>
        /// 延迟加载构造函数
        /// </summary>
        /// <param name="astream">文件流</param>
        /// <param name="start">字符串表开始位置</param>
        /// <param name="size">字符串表大小</param>
        public ARZStrings(Stream astream, long start, int size) : this()
        {
            stream = astream;
            stringTableStart = start;
            stringTableSize = size;
            isStringTableLoaded = false;
        }

        public string this[int i] 
        { 
            get 
            { 
                EnsureStringTableLoaded();
                if (i < 0 || i >= strtable.Count) 
                {
                    Program.Log.Error("ARZStrings: 索引越界 - 索引={0}, 表大小={1}", i, strtable.Count);
                    return string.Format("[Invalid String: index={0}]", i);
                }
                return strtable[i]; 
            } 
            set 
            { 
                EnsureStringTableLoaded();
                if (i < 0 || i >= strtable.Count) 
                {
                    Program.Log.Error("ARZStrings: 索引越界 - 索引={0}, 表大小={1}", i, strtable.Count);
                    return;
                }
                strtable[i] = value; 
            } 
        }

        public int Count 
        { 
            get 
            { 
                EnsureStringTableLoaded();
                return strtable.Count; 
            } 
        }
        
        /// <summary>
        /// 确保字符串表已加载
        /// </summary>
        private void EnsureStringTableLoaded()
        {
            if (!isStringTableLoaded && stream != null)
            {
                stream.Seek(stringTableStart, SeekOrigin.Begin);
                ReadStreamInternal(stream, stringTableSize);
                isStringTableLoaded = true;
            }
        }
        
        /// <summary>
        /// 公共方法：确保字符串表已加载
        /// </summary>
        public void EnsureLoaded()
        {
            EnsureStringTableLoaded();
        }
        
        public void ReadStream(Stream astream, int size)
        {
            stream = astream;
            stringTableStart = astream.Position;
            stringTableSize = size;
            ReadStreamInternal(astream, size);
            isStringTableLoaded = true;
        }
        
        private void ReadStreamInternal(Stream astream, int size)
        {
            if (strtable == null)
                strtable = new List<string>();
            strtable.Capacity = 0;
            int pos = 0;
            using (BinaryReader br = new BinaryReader(astream, Encoding.GetEncoding("GBK"), true))
            {
                try
                {
                    while (pos < size)
                    {
                        // 安全检查：确保有足够的字节读取count
                        if (pos + 4 > size)
                        {
                            Program.Log.Error("ARZStrings: 字符串表数据格式错误，剩余字节不足读取count");
                            break;
                        }
                        
                        int count = br.ReadInt32(); pos += 4; // Read Count
                        
                        // 不限制count值（只检查负数）
                        if (count < 0)
                        {
                            Program.Log.Error("ARZStrings: 发现异常的count值: {0}", count);
                            break;
                        }
                        
                        strtable.Capacity += count;
                        
                        for (int i = 0; i < count; i++)
                        {
                            // 安全检查：确保有足够的字节读取length
                            if (pos + 4 > size)
                            {
                                Program.Log.Error("ARZStrings: 字符串表数据格式错误，剩余字节不足读取length");
                                break;
                            }
                            
                            int length = br.ReadInt32(); pos += 4;
                            
                            // 不限制length值（只检查负数）
                            if (length < 0)
                            {
                                Program.Log.Error("ARZStrings: 发现异常的length值: {0}", length);
                                break;
                            }
                            
                            // 安全检查：确保有足够的字节读取字符数据
                            if (pos + length > size)
                            {
                                Program.Log.Error("ARZStrings: 字符串表数据格式错误，剩余字节不足读取字符数据");
                                // 调整pos到流的末尾，退出循环
                                pos = size;
                                break;
                            }
                            
                            // 使用正确的编码读取字符串
                            byte[] bytes = br.ReadBytes(length);
                            pos += length;
                            
                            try
                            {
                                // 确保使用GBK编码正确解码字符串
                                string str = Encoding.GetEncoding("GBK").GetString(bytes);
                                strtable.Add(str);
                            }
                            catch (Exception ex)
                            {
                                Program.Log.Error("ARZStrings: 解码字符串时出错: {0}", ex.Message);
                                // 添加一个占位符字符串，继续处理
                                strtable.Add("[解码错误]");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Program.Log.Error("ARZStrings: 读取字符串表时发生错误: {0}", ex.Message);
                }
            }
        }

        public void WriteToStream(Stream astream)
        {
            using (BinaryWriter bw = new BinaryWriter(astream, Encoding.GetEncoding("GBK"), true))
            {
                bw.Write((int)strtable.Count);
                foreach (string s in strtable)
                {
                    // 使用GBK编码计算实际字节数，而不是字符数
                    bw.Write((int)Encoding.GetEncoding("GBK").GetByteCount(s));
                    bw.Write(Encoding.GetEncoding("GBK").GetBytes(s));
                }
            }
        }        

        public int AddString(string newvalue)
        {
            if (strsearchlist == null) // Build string search list on first access
                BuildStringSearchList();

            if (strsearchlist.ContainsKey(newvalue))
                return strsearchlist[newvalue];
            else // New value
            {
               strtable.Add(newvalue);
               strsearchlist.Add(newvalue, strtable.Count - 1); // Add to searchlist
               return strtable.Count - 1;
            }
        }

        public int GetStringID(string searchstr)
        {
            if (strsearchlist == null)
                BuildStringSearchList();

            if (strsearchlist.ContainsKey(searchstr))
                return strsearchlist[searchstr];
            else
                return -1;
        }

        public void BuildStringSearchList()
        {
            strsearchlist = new SortedDictionary<string, int>();
            for (int i = 0; i < strtable.Count; i++)
            {
                strsearchlist.Add(strtable[i], i);
            }
        }


    }

    public class ARZRecord
    {
        public int rfid;
        public string rtype;
        public int rdOffset;
        public int rdSizeCompressed;
        public int rdSizeDecompressed;
        public DateTime rdFileTime;
        public byte[] cData;
        public byte[] aData;
        public List<ARZEntry> entries = null;
        public ARZStrings strtable = null;
        private HashSet<string> entryset = null;
        public string Name { get { return strtable != null && rfid >= 0 && rfid < strtable.Count ? strtable[rfid] : string.Format("[Invalid Name: rfid={0}]", rfid); } }

        private static ARZEntryComparer NameComparer = new ARZEntryComparer();
   
        // 添加默认构造函数，用于按需创建记录对象
        public ARZRecord()
        {
            // 初始化必要的字段为默认值
            rfid = -1;
            rtype = string.Empty;
            rdOffset = 0;
            rdSizeCompressed = 0;
            rdSizeDecompressed = 0;
            rdFileTime = DateTime.MinValue;
            cData = null;
            aData = null;
            entries = null;
            strtable = null;
            entryset = null;
        }
        
        public ARZRecord(string rname, string[] rstrings, Dictionary<string, TemplateNode> templates, ARZStrings astrtable)
        {
            strtable = astrtable;
            rfid = strtable.AddString(rname);
            rtype = ""; // TODO: IMPORTANT: How record type is determined, last piece of info
            rdFileTime = DateTime.Now; // TODO: Correct file time should be passed here somehow
            entries = new List<ARZEntry>();
            entryset = new HashSet<string>();
            TemplateNode tpl = null;
            foreach (string line in rstrings) {
                if (line.StartsWith("templateName"))
                {
                    string[] eexpl = line.Split(',');
                    try
                    {
                        string templateName = eexpl[1];
                        
                        // 首先尝试直接使用templateName查找
                        if (templates.TryGetValue(templateName, out tpl))
                        {
                            // 找到模板，使用它
                        }
                        // 如果没找到，尝试添加"database/"前缀后查找
                        else if (!templateName.StartsWith("database/templates/") && templates.TryGetValue("database/templates/" + templateName, out tpl))
                        {
                            // 找到带前缀的模板，使用它
                        }
                        // 如果还没找到，尝试将templateName标准化后查找
                        else
                        {
                            string normalizedName = templateName.ToLower().Replace('\\', '/');
                            if (!normalizedName.StartsWith("database/templates/") && templates.TryGetValue("database/templates/" + normalizedName, out tpl))
                            {
                                // 找到标准化后的模板，使用它
                            }
                        }
                        
                        // 如果找到了模板
                        if (tpl != null)
                        {
                            ARZEntry newentry = new ARZEntry(eexpl[0], TemplateNode.TemplateNameVar, rname, this);
                            if (newentry.TryAssign(eexpl[0], eexpl[1], strtable, ""))
                            {
                                entries.Add(newentry);
                                entryset.Add(eexpl[0]);
                            }
                        }
                        else
                        {
                            // 模板未找到，抛出异常
                            throw new KeyNotFoundException(string.Format("Template '{0}' not found", templateName));
                        }
                    }
                    catch (KeyNotFoundException e)
                    {
                        // 修复：将参数整合到字符串插值中
                        Program.Log.Error($"{LanguageManager.Instance.GetText("LOG.ErrorTemplateNotFound", "使用的模板未找到：")} \"{eexpl[1]}\"");
                        //throw e;
                        throw;
                    }
                    break;
                }
            }
            if (tpl == null)
            {
                Program.Log.Error("Record {0} has no template!", rname); // DEBUG
                throw new Exception(string.Format("Record {0} has no template!", rname));
            }
            foreach (string estr in rstrings)
            {
                TemplateNode vart = null;
                string[] eexpl = estr.Split(',');
                if (eexpl.Length != 3)
                {
                    // Console.WriteLine("Record \"{0}\" - Malformed assignment string \"{1}\"", Name, estr);
                    if (eexpl.Length == 2)
                        Program.Log.Warn("Record \"{0}\" - Malformed assignment string \"{1}\", No comma at the end, Recoverable."); // DEBUG:
                    else
                    {
                        Program.Log.Warn("Record \"{0}\" - Malformed assignment string \"{1}\", Skipping");
                        continue;
                    }
                }
                string varname = eexpl[0];
                string vvalue = eexpl[1];
                string defaultsto = "";
                if (string.IsNullOrEmpty(varname))
                {
                    Program.Log.Warn("Record \"{0}\" - Has empty entry name, Skipping");
                    continue;
                }
                if (varname == "templateName")
                {
                    continue; // templateName should have been assigned already
                }
                else
                {
                    // Find variable in templates
                    // TODO: All this is rubbish, introduces ambiguity, find root cause, not hack & tack
                    vart = tpl.FindVariable(varname); 
                    if (vart != null)
                    {
                        if (vart.values.ContainsKey("defaultValue"))
                            defaultsto = vart.values["defaultValue"];
                        if (entryset.Contains(varname))
                        {
                            // Console.WriteLine("Record {0} duplicate entry {1} - Overwriting.", rname, varname); // TODO: Do not ignore, Overwrite
                            Program.Log.Info("Record {0} duplicate entry {1} - Overwriting.", rname, varname);
                            ARZEntry entry = entries.Find(e => e.Name == varname);
                            entry.TryAssign(varname, vvalue, strtable, defaultsto);
                            continue;
                        }
                        else
                        {

                            if (varname.ToLower() == "class") rtype = vvalue;
                            ARZEntry newentry = new ARZEntry(varname, vart, rname, this);
                            if (newentry.TryAssign(varname, vvalue, strtable, defaultsto))
                            {
                                entries.Add(newentry);
                                entryset.Add(varname);
                            }
                        }
                    }
                    else {
                        Program.Log.Debug("Entry {0}/{1} template not found. Skipping.", rname, varname);
                        // notfoundvars.Add(varname);
                    }
                }
                    
            }
            entries.Sort(1, entries.Count - 1, NameComparer); // Sort all except first "templateName" entry
            PackData();
        }

        public ARZRecord(ARZRecord tocopy, ARZStrings astrtable) {
            strtable = astrtable;
            rfid = strtable.AddString(tocopy.Name);
            rtype = tocopy.rtype;
            rdFileTime = tocopy.rdFileTime;
            entries = new List<ARZEntry>();
            foreach (ARZEntry tce in tocopy.entries)
            {
                entries.Add(new ARZEntry(tce, this));
            }
            PackData();
        }

        public ARZRecord(BinaryReader rdata, ARZStrings astrtable)
        {
            strtable = astrtable;
            ReadBytes(rdata);
        }

        public void ReadBytes(BinaryReader rdata)
        {
            // read record info
            rfid = rdata.ReadInt32();
            // string record_file = strtable[rfid];
            int rtypelen = rdata.ReadInt32();
            // 使用GBK编码读取字节数组并解码为字符串，与WriteRecord方法匹配
            byte[] typeBytes = rdata.ReadBytes(rtypelen);
            rtype = Encoding.GetEncoding("GBK").GetString(typeBytes);
            rdOffset = rdata.ReadInt32();
            rdSizeCompressed = rdata.ReadInt32();
            rdSizeDecompressed = rdata.ReadInt32();
            rdFileTime = DateTime.FromFileTimeUtc(rdata.ReadInt64());
        }

        public List<ARZEntry> ReadData(BinaryReader brdata) // Reads record entry data from reader and creates entries list
        {
            try
            {
                // 安全检查：确保压缩和解压缩大小合理（不限制最大大小）
                if (rdSizeCompressed <= 0 || rdSizeDecompressed <= 0)
                {
                    Program.Log.Error("ARZRecord: 发现异常的大小值 - 压缩: {0}, 解压: {1}", rdSizeCompressed, rdSizeDecompressed);
                    entries = new List<ARZEntry>();
                    return entries;
                }
                
                cData = brdata.ReadBytes(rdSizeCompressed);
                
                // 安全检查：确保读取的字节数正确
                if (cData.Length != rdSizeCompressed)
                {
                    Program.Log.Error("ARZRecord: 读取的压缩数据长度不匹配 - 预期: {0}, 实际: {1}", rdSizeCompressed, cData.Length);
                    entries = new List<ARZEntry>();
                    return entries;
                }
                
                // 使用标准老式LZ4Codec.Decode方法解压数据
                try
                {
                    aData = new byte[rdSizeDecompressed];
                    // 使用LZ4Codec.Decode进行解压
                    int decompressedLength = LZ4Codec.Decode(cData, 0, rdSizeCompressed, aData, 0, rdSizeDecompressed);
                    
                    // 验证解压结果
                    if (decompressedLength != rdSizeDecompressed)
                    {
                        throw new Exception(string.Format("解压长度不匹配: 预期 {0}, 实际 {1}", rdSizeDecompressed, decompressedLength));
                    }
                }
                catch (Exception ex)
                {
                    Program.Log.Error("ARZRecord: 解压数据时出错: {0}", ex.Message);
                    entries = new List<ARZEntry>();
                    return entries;
                }
                
                entries = new List<ARZEntry>();
                // 设置合理的容量预估（不限制条目数量）
                int estimatedCapacity = rdSizeDecompressed / 12; // 假设每个条目至少12字节
                entries.Capacity = estimatedCapacity;
                
                using (MemoryStream eMem = new MemoryStream(aData))
                {
                    using (BinaryReader eDbr = new BinaryReader(eMem, Encoding.GetEncoding("GBK"), true))
                    {
                        int entryCount = 0;
                        while (eMem.Position < eMem.Length)
                        {
                            // 不限制条目数量
                            
                            // 安全检查：确保至少有8字节可读取（ARZEntry头部大小）
                            if (eMem.Position + 8 > eMem.Length)
                            {
                                Program.Log.Error("ARZRecord: 剩余数据不足，无法读取完整条目");
                                break;
                            }
                            
                            try
                            {
                                ARZEntry entry = new ARZEntry(eDbr, this);
                                entries.Add(entry);
                                entryCount++;
                            }
                            catch (Exception ex)
                            {
                                Program.Log.Error("ARZRecord: 创建条目时出错: {0}", ex.Message);
                                // 尝试跳过当前条目，继续处理下一个
                                break; // 由于无法确定当前条目的大小，最好的做法是中断处理
                            }
                        }
                    }
                }
                
                // 清理内存
                aData = null;
                
                return entries;
            }
            catch (Exception ex)
            {
                Program.Log.Error("ARZRecord: 读取记录数据时发生错误: {0}", ex.Message);
                entries = new List<ARZEntry>();
                return entries;
            }
        }

        public void DiscardData()
        {
            entries = null;
            cData = null;
        }

        public void PackData()
        {
            int datasize = entries.Count * 8; // Headers
            foreach (ARZEntry e in entries)
                datasize += e.values.Length * 4; // + Data
            using (MemoryStream mStream = new MemoryStream(datasize))
            {
                using (BinaryWriter bWriter = new BinaryWriter(mStream, Encoding.GetEncoding("GBK"), true))
                {
                    mStream.Seek(0, SeekOrigin.Begin);
                    foreach (ARZEntry e in entries)
                    {
                        // e.dcount;
                        e.dcount = (ushort)e.values.Length;
                        // Console.WriteLine("Packing {0} - Len: {1} ", Program.strtable[e.dstrid], e.dcount); // DEBUG
                        e.WriteBytes(bWriter);
                    }
                }

                // Replace data
                aData = mStream.GetBuffer();
                rdSizeDecompressed = aData.Length;
                
                // 使用LZ4Codec.Encode进行压缩
                try
                {
                    // 为压缩数据分配足够的空间
                    cData = new byte[LZ4Codec.MaximumOutputSize(rdSizeDecompressed)];
                    
                    // 使用LZ4Codec.Encode进行压缩
                    int compressedLength = LZ4Codec.Encode(aData, 0, rdSizeDecompressed, cData, 0, cData.Length);
                    
                    // 如果实际压缩大小小于分配的空间，调整数组大小
                    if (compressedLength < cData.Length)
                    {
                        byte[] temp = new byte[compressedLength];
                        Buffer.BlockCopy(cData, 0, temp, 0, compressedLength);
                        cData = temp;
                    }
                }
                catch (Exception ex)
                {
                    Program.Log.Error("ARZRecord: 压缩数据时出错: {0}", ex.Message);
                    cData = new byte[0];
                    rdSizeCompressed = 0;
                }
                
                aData = null;
                rdSizeCompressed = cData.Length;
            }
        }

        public void SaveToFile(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Create))
                SaveToStream(fs);
        }

        public void SaveToStream(Stream astream)
        {
            using (StreamWriter sr = new StreamWriter(astream, Encoding.GetEncoding("GBK")))
            {
                sr.NewLine = "\n";
                foreach (ARZEntry etr in entries)
                {
                    string estring = etr.ToString();
                    if (estring.Contains('\n') || estring.Contains(Environment.NewLine))
                    {
                        Program.Log.Info("Record \"{0}\" entry \"{1}\" contains newline(s), fixing.", strtable[rfid], strtable[etr.dstrid]);
                        estring = System.Text.RegularExpressions.Regex.Replace(estring, @"\r\n?|\n", "");
                    }
                    sr.WriteLine(estring);
                }
            }
        }

        public void WriteToStream(Stream rtstream)
        {
            using (BinaryWriter bw = new BinaryWriter(rtstream, Encoding.GetEncoding("GBK"), true))
            {
                WriteRecord(bw, rdOffset);
            }
        }

        public void WriteRecord(BinaryWriter rtable, int dataoffset)
        {
            rdOffset = dataoffset;
            rtable.Write(rfid);
            // 使用GBK编码计算实际字节数
            byte[] typeBytes = Encoding.GetEncoding("GBK").GetBytes(rtype);
            rtable.Write(typeBytes.Length);
            rtable.Write(typeBytes);
            rtable.Write(rdOffset);
            rtable.Write(rdSizeCompressed);
            rtable.Write(rdSizeDecompressed);
            rtable.Write(rdFileTime.ToFileTimeUtc());
        }

    }

    public enum ARZEntryType : ushort { Int=0, Real=1, String=2, Bool=3 };

    public class ARZEntryComparer : IComparer<ARZEntry>
    {
        public int Compare(ARZEntry first, ARZEntry second) 
        {
              return String.CompareOrdinal(first.Name, second.Name);
        }
    }


    public class ARZEntry
    {
        public ARZEntryType dtype;
        public ushort dcount;
        public int dstrid;
        public int[] values;
        public bool changed = false; // TODO: overhead
        public bool isarray = false;
        public bool isfile = false;
        private ARZRecord parent = null;
        // private ARZStrings strtable = null;
        // static SortedList<string, int> strsearchlist = null;
        public ARZStrings StrTable { get { return parent?.strtable; } }
        public string Name { get { return StrTable[dstrid]; } }
        // public static SortedList<string, int> StrSearchList { get { if (strsearchlist == null) strsearchlist = Program.strsearchlist; return strsearchlist; } set { strsearchlist = value; } }

        public ARZEntry(ARZEntry tocopy, ARZRecord aparent)
        {
            parent = aparent;
            dstrid = StrTable.AddString(tocopy.Name);
            dtype = tocopy.dtype;
            dcount = tocopy.dcount;
            values = (int[])tocopy.values.Clone();
            if (dtype == ARZEntryType.String)
                for (int i = 0; i < values.Length; i++) {
                    values[i] = StrTable.AddString(tocopy.AsString(i));
                }
        }

        public ARZEntry(BinaryReader edata, ARZRecord aparent)
        {
            parent = aparent;            
            ReadBytes(edata);
        }

        public ARZEntry(string entryname, TemplateNode tpl, string recname, ARZRecord aparent)
        {
            string vtype = null;
            parent = aparent;

            try
            {
                vtype = tpl.values["type"];
            }
            catch (KeyNotFoundException e)
            {
                Program.Log.Error("Template {0} does not contain value type for entry {1}! I'm not guessing it.", tpl.GetTemplateFile(), entryname);
                throw e; // rethrow
            }

            isarray = tpl.values.ContainsKey("class") && tpl.values["class"] == "array"; // This is an array act accordingly when packing strings
                        
            if (vtype.StartsWith("file_"))
            {
                vtype = "file"; // Make it string
                isfile = true;
            }

            switch (vtype)
            {
                case "string":
                case "file":
                case "equation":
                    dtype = ARZEntryType.String; // string type
                    break;
                case "real":
                    dtype = ARZEntryType.Real;
                    break;
                case "bool":
                    dtype = ARZEntryType.Bool;
                    break;
                case "int":
                    dtype = ARZEntryType.Int;
                    break;
                default:
                    Program.Log.Error("Template {0} has unknown type {1} for entry {1}", tpl.GetTemplateFile(), tpl.values["type"], entryname);
                    throw new Exception("Unknown variable type");
                    // break;
            }

            values = new int[0];
            dstrid = StrTable.AddString(entryname);

        }

        public void ReadBytes(BinaryReader edata)
        {
            // header
            dtype = (ARZEntryType)edata.ReadUInt16();
            dcount = edata.ReadUInt16();
            dstrid = edata.ReadInt32();
            // read all entries
            values = new int[dcount];
            for (int i = 0; i < dcount; i++)
            {
                values[i] = edata.ReadInt32();
            }
        }

        public void WriteBytes(BinaryWriter edata)
        {
            edata.Write((ushort)dtype);
            edata.Write(dcount);
            edata.Write(dstrid);
            foreach (int v in values)
                edata.Write(v);
        }

        public int AsInt(int eid)
        {
            return values[eid];
        }

        public float AsFloat(int eid)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(values[eid]), 0);
        }

        public string AsString(int eid)
        {
            return AsStringEx(eid, StrTable);
        }

        public string AsStringEx(int eid, ARZStrings strtable)
        {
            return strtable[values[eid]];
        }

        public bool TryAssign(string entryname, string valuestr, ARZStrings strtable, string defaultsto = "") {
            if (strtable[dstrid] != entryname)
            {
                Program.Log.Warn("Cannot assign \"{0}\" to \"{1}\" field (entry names differ).", entryname, strtable[dstrid]);
                return false;
            }
            if (string.IsNullOrWhiteSpace(valuestr))
            {
                valuestr = defaultsto;
                return false;
            }

            if (isfile) valuestr = valuestr.Replace(Path.DirectorySeparatorChar, '/').ToLower();
            string[] strs = valuestr.Split(';');
            if (!isarray )
            {
                if (strs.Length > 1)
                {
                    if (dtype == ARZEntryType.String)
                    {
                        strs = new string[1] { valuestr };
                    }
                    else
                    {
                        strs = new string[1] { strs[0] };
                    }
                }
            }
            else if (valuestr.Contains(";;"))
            {
                // This is when it get's weird:
                // try compacting multiple empty strings to ;;                
                List<string> cstrs = new List<string>();
                string accum = "";
                for (int i = 0; i < strs.Length; i++) // 
                {
                    if (strs[i] == "")
                    {
                        if (i + 1 < strs.Length && strs[i + 1] == "") accum += ";"; // Ignore last ; as it is a separator
                    }
                    else
                    {
                        if (accum != "")
                        {
                            // if (dtype != ARZEntryType.Real)
                            cstrs.Add(accum);
                            accum = "";
                        }
                        //if (dtype == ARZEntryType.Real && !string.IsNullOrWhiteSpace(strs[i]))
                        cstrs.Add(strs[i]);
                    }
                }
                // if (accum != "" && dtype != ARZEntryType.Real)
                if (accum != "")
                        cstrs.Add(accum);
                strs = cstrs.ToArray<string>();
            }

            float fval = (float)0.0;
            int[] nvalues = new int[strs.Length];

            for (int i = 0; i < strs.Length; i++)
            {               
                switch (dtype)
                {
                    case ARZEntryType.Int: // TODO: Move entry types to static Consts for clarity
                        if (!int.TryParse(strs[i], out nvalues[i]))
                        {
                            if (float.TryParse(strs[i], out fval))
                            {
                                nvalues[i] = (int)fval;
                                // Console.WriteLine("Int value represented as float {0} in {1}, truncating", fval, parent.Name); // DEBUG
                                Program.Log.Debug("Int value represented as float \"{0:F}\" in {1}/{2}, truncating", fval, parent.Name, entryname);
                            } else
                            if (strs[i].StartsWith("0x"))
                            {
                                try
                                {
                                    nvalues[i] = Convert.ToInt32(strs[i].Substring(2), 16);
                                }
                                catch
                                {
                                    // Console.WriteLine("Could not parse Hex number {0}", strs[i]); // DEBUG
                                    Program.Log.Debug("Could not parse Hex number \"{0}\" in {1}/{2}", strs[i], parent.Name, entryname);
                                    nvalues[i] = 0;
                                }
                            }
                            else
                            {
                                // DEBUG:
                                // Console.WriteLine("Record {3} Entry {0} Error parsing integer value #{1}=\"{2}\", Defaulting to 0", Name, i, strs[i], parent.Name);
                                // return false;
                                Program.Log.Debug("Error parsing integer value \"{0}\" in {1}/{2}", strs[i], parent.Name, entryname);
                                nvalues[i] = 0; // Set default
                            }
                        }
                        break;
                    case ARZEntryType.Real:
                        if (!float.TryParse(strs[i], out fval))
                        {
                            Program.Log.Debug("Error parsing float value \"{0}\" in {1}/{2}, Defaulting to 0.0", strs[i], parent.Name, entryname);
                            nvalues[i] = BitConverter.ToInt32(BitConverter.GetBytes(0.0), 0);
                        }
                        nvalues[i] = BitConverter.ToInt32(BitConverter.GetBytes(fval), 0);
                        break;
                    case ARZEntryType.String: // String
                        { 
                            nvalues[i] = strtable.AddString(strs[i]);
                        }
                        break;
                    case ARZEntryType.Bool:
                        if (!int.TryParse(strs[i], out nvalues[i]) || nvalues[i] > 1)
                        {
                            Program.Log.Debug("Error parsing boolean value \"{0}\" in {1}/{2}, Defaulting to False", strs[i], parent.Name, entryname);
                            nvalues[i] = 0;
                            // return false;
                        }
                        break;
                    default:
                        Program.Log.Warn("Unknown data type {2} for entry {0}/{1}", parent.Name, entryname, dtype);
                        return false;
                }
            }
            values = nvalues;
            dcount = (ushort)values.Length;
            changed = true;
            return true;
        }

        public override string ToString()
        {
            if (StrTable == null) return "";
            StringBuilder sb = new StringBuilder();
            sb.Append(StrTable[dstrid]).Append(',');
            bool firstentry = true;
            foreach (int value in values)
            {
                if (!firstentry) sb.Append(";");
                switch (dtype)
                {
                    case ARZEntryType.Int:
                    case ARZEntryType.Bool:
                    default:
                        sb.Append(value); // values are signed!
                        break;
                    case ARZEntryType.Real:
                        sb.AppendFormat("{0:0.000000}", BitConverter.ToSingle(BitConverter.GetBytes(value), 0));
                        break;
                    case ARZEntryType.String:
                        sb.Append(StrTable[value]);
                        break;
                }
                firstentry = false;
            }
            sb.Append(',');
            return sb.ToString();
        }
    }

    public class ARZHeader
    {
        public const int HEADER_SIZE = 24;
        public short Unknown = 0x02; // Magick
        public short Version = 0x03;
        public int RecordTableStart;
        public int RecordTableSize;
        public int RecordTableEntries;
        public int StringTableStart;
        public int StringTableSize;
        public ARZHeader()
        {
        }

        public ARZHeader(Stream astream)
        {
            using (BinaryReader br = new BinaryReader(astream, Encoding.GetEncoding("GBK"), true))
                ReadBytes(br);
        }

        public void WriteToStream(Stream astream) {
            using (BinaryWriter bw = new BinaryWriter(astream, Encoding.GetEncoding("GBK"), true))
            {
                bw.Write(Unknown);
                bw.Write(Version);
                bw.Write(RecordTableStart);
                bw.Write(RecordTableSize);
                bw.Write(RecordTableEntries);
                bw.Write(StringTableStart);
                bw.Write(StringTableSize);
            }
        }

        public void ReadBytes(BinaryReader bytes)
        {
            Unknown = bytes.ReadInt16();
            Version = bytes.ReadInt16();
            RecordTableStart = bytes.ReadInt32();
            RecordTableSize = bytes.ReadInt32();
            RecordTableEntries = bytes.ReadInt32();
            StringTableStart = bytes.ReadInt32();
            StringTableSize = bytes.ReadInt32();
        }

    }

}