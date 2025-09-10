using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace arzedit
{
    // Template Objects
    public class TemplateNode
    {
        public static readonly TemplateNode TemplateNameVar;
        public string TemplateFile = null;
        public TemplateNode parent = null;
        public string kind = "";
        public Dictionary<string, string> values = new Dictionary<string, string>();
        public SortedDictionary<string, TemplateNode> varsearch = null;
        public List<TemplateNode> subitems = new List<TemplateNode>();
        public List<TemplateNode> includes = new List<TemplateNode>();

        static TemplateNode()
        {
            TemplateNameVar = new TemplateNode();
            TemplateNameVar.kind = "variable";
            TemplateNameVar.values["name"] = "templateName";
            TemplateNameVar.values["class"] = "variable";
            TemplateNameVar.values["type"] = "string";
        }

        public TemplateNode(TemplateNode aparent = null, string aTemplateFile = null)
        {
            parent = aparent;
            TemplateFile = aTemplateFile;
            if (aparent == null) // I am root
                varsearch = new SortedDictionary<string, TemplateNode>();
        }

        public string GetTemplateFile()
        {
            if (!string.IsNullOrEmpty(TemplateFile))
                return TemplateFile;
            else
                if (parent != null) return parent.GetTemplateFile();
            else return null;
        }

        
        public int ParseNode(string[] parsestrings, int parsestart = 0)
        {
            int i = parsestart;
            
            // 检查起始索引是否有效
            if (i < 0 || i >= parsestrings.Length)
            {
                throw new InvalidOperationException(
                    $"模板文件 \"{TemplateFile}\" 解析错误: 起始索引 {i} 超出数组范围 (数组长度: {parsestrings.Length})");
            }

            // 跳过空行，查找kind定义
            while (i < parsestrings.Length && string.IsNullOrWhiteSpace(parsestrings[i])) 
                i++;
            
            // 检查是否已到达数组末尾
            if (i >= parsestrings.Length)
            {
                throw new InvalidOperationException(
                    $"模板文件 \"{TemplateFile}\" 解析错误: 文件内容不完整，未找到模板类型定义");
            }
            
            // 读取模板类型
            kind = (parsestrings[i++].Trim().ToLower());
            
            // 查找开始括号 "{"
            while (i < parsestrings.Length && parsestrings[i].Trim() != "{") 
                i++;
            
            // 检查是否找到开始括号
            if (i >= parsestrings.Length)
            {
                throw new InvalidOperationException(
                    $"模板文件 \"{TemplateFile}\" 解析错误: 未找到开始括号 \"{{\"，模板类型: {kind}");
            }
            
            // 跳过开始括号
            i++;
            
            // 跳过开始括号后的空行
            while (i < parsestrings.Length && string.IsNullOrWhiteSpace(parsestrings[i])) 
                i++;
            
            // 解析内容直到结束括号 "}"
            while (i < parsestrings.Length && parsestrings[i].Trim() != "}")
            {
                // 跳过空行
                if (string.IsNullOrWhiteSpace(parsestrings[i])) 
                { 
                    i++; 
                    continue; 
                }
                
                // 检查是否有等号，判断是键值对还是子项
                if (parsestrings[i].Trim().Contains('='))
                {
                    // 这是键值对
                    try
                    {
                        string[] sval = parsestrings[i].Split('=');
                        if (sval.Length >= 2)
                        {
                            string akey = (sval[0].Trim());
                            string aval = (sval[1].Trim().Trim('"'));
                            values[akey] = aval;
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                $"模板文件 \"{TemplateFile}\" 解析错误: 键值对格式不正确，行内容: {parsestrings[i]}");
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"模板文件 \"{TemplateFile}\" 解析错误: 处理键值对时发生异常，行内容: {parsestrings[i]}", ex);
                    }
                }
                else
                {
                    // 这是子项
                    TemplateNode sub = new TemplateNode(this);
                    try
                    {
                        i = sub.ParseNode(parsestrings, i);
                        subitems.Add(sub);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"模板文件 \"{TemplateFile}\" 解析错误: 处理子项时发生异常，子项类型: {sub.kind}", ex);
                    }
                }
                i++;
            }
            
            // 检查是否正常结束（找到结束括号）
            if (i >= parsestrings.Length)
            {
                throw new InvalidOperationException(
                    $"模板文件 \"{TemplateFile}\" 解析错误: 未找到结束括号 \"}}\"，模板类型: {kind}");
            }
            
            return i;
        }

        public List<TemplateNode> findValue(string aval)
        {
            List<TemplateNode> res = new List<TemplateNode>();
            if (values.ContainsValue(aval)) res.Add(this);
            foreach (TemplateNode sub in subitems)
            {
                res.AddRange(sub.findValue(aval));
            }
            return res;
        }

        public TemplateNode FindVariable(string aname)
        {
            if (parent == null && varsearch.ContainsKey(aname))
            {
                return varsearch[aname];
            }
            // 修改，忽略大小写
            if (kind == "variable" && values.ContainsKey("name") && string.Equals(values["name"], aname, StringComparison.OrdinalIgnoreCase))
            {
                if (parent == null) varsearch.Add(aname, this);
                return this;
            }
            // Not this, recurse subitems:
            TemplateNode res = null;
            foreach (TemplateNode sub in subitems)
            {
                res = sub.FindVariable(aname);
                if (res != null)
                {
                    if (parent == null) varsearch.Add(aname, res);
                    return res;
                }
            }
            // No entry in subitems, check includes
            foreach (TemplateNode incl in includes)
            {
                res = incl.FindVariable(aname);
                if (res != null)
                {
                    if (parent == null) varsearch.Add(aname, res);
                    return res;
                }
            }
            // Giving up:
            return null;
        }

        public void FillIncludes(Dictionary<string, TemplateNode> alltempl)
        {
            foreach (TemplateNode sub in subitems)
            {
                if (sub.kind == "variable" && sub.values.ContainsKey("type") && sub.values["type"] == "include")
                {
                    string incstr = sub.values.ContainsKey("value") ? sub.values["value"] : "";
                    if (incstr == "")
                        incstr = sub.values.ContainsKey("defaultValue") ? sub.values["defaultValue"] : "";
                    incstr = incstr.ToLower().Replace("%template_dir%", "").Replace(Path.DirectorySeparatorChar, '/');
                    if (incstr.StartsWith("/")) incstr = incstr.Substring(1);
                    if (alltempl.ContainsKey(incstr))
                    {
                        // Console.WriteLine("Include {0}", incstr);
                        // Check for cycles
                        TemplateNode itemplate = alltempl[incstr];
                        // DEBUG:
                        if (itemplate == this || includes.Contains(itemplate))
                            Program.Log.Warn("WARNING: When parsing template {0} include \"{1}\" found out it's already included by another file, include might be cyclic.", GetTemplateFile(), incstr);
                           // Console.WriteLine("WARNING: When parsing template {0} include \"{1}\" found out it's already included by another file, include might be cyclic.", GetTemplateFile(), incstr);
                        includes.Add(itemplate);
                    }
                    else
                    {
                        TemplateNode tproot = this;
                        while (tproot.parent != null) tproot = tproot.parent;
                        string intemplate = alltempl.First(t => t.Value == tproot).Key;
                        // Console.WriteLine("Cannot find include {0} referenced in {1}", incstr, intemplate); // Debug
                        //Program.Log.Info("Cannot find include {0} referenced in {1}", incstr, intemplate);
                        Program.Log.Warn($"{intemplate} {LanguageManager.Instance.GetText("LOG.ReferenceTemplateNotFound", "文件中引用了外部文件，但未找到：")} {incstr}");
                    }
                }
                else if (sub.kind == "group")
                {
                    sub.FillIncludes(alltempl);
                }
            }
        }
    }
}