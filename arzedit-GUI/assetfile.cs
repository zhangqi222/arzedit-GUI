using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace arzedit
{
    // Classes for reading asset files and compiling resources into resource folder

    public class AssetDataGeneric
    {
        public string srcfile = null;

        protected string ReadString(BinaryReader br)
        {
            int strlen = br.ReadInt32();
            return new string(br.ReadChars(strlen));
        }

        public virtual void ReadBytes(BinaryReader br)
        {
            srcfile = ReadString(br);
        }
    }

    public enum TexFormat : byte { Uncompressed, DXT1, DXT3, DXT5, DSDT }

    public enum AssetType { Unknown, Generic, Text, Quest, Bitmap, Texture, ParticleFX, Map, Mesh, Wave, Ogg }

    public class AssetDataTex : AssetDataGeneric
    {
        public bool CreateMipmaps = false;
        public bool ConvertToNormalMap = false;
        public TexFormat Format = 0;
        public byte[] Unknown1 = null;
        public byte[] Unknown2 = null;
        public int FPS = 0;
        private readonly string[] fmtstrs = new string[5] { "", "dxt1", "dxt3", "dxt5", "dsdt" };
        public string FormatStr()
        {
            return fmtstrs[(byte)Format];
        }

        public override void ReadBytes(BinaryReader br)
        {
            base.ReadBytes(br);
            CreateMipmaps = Convert.ToBoolean(br.ReadByte());
            Format = (TexFormat)br.ReadByte();
            Unknown1 = br.ReadBytes(3);
            ConvertToNormalMap = Convert.ToBoolean(br.ReadByte());
            FPS = br.ReadInt32();
            Unknown2 = br.ReadBytes(4);
        }
    }

    public class AssetDataMsh : AssetDataGeneric
    {
        public string miffile = null;
        public bool TangentSpaceVectors = false;
        public bool VertexColors = false;

        public override void ReadBytes(BinaryReader br)
        {
            base.ReadBytes(br);
            miffile = ReadString(br);
            TangentSpaceVectors = Convert.ToBoolean(br.ReadByte());
            VertexColors = Convert.ToBoolean(br.ReadByte());
        }
    }

    public class AssetBuilder
    {
        static readonly char[] AST_MAGICK = new char[4] { 'A', 'S', 'T', (char)0x02 };
        static readonly char[] TYPE_NUL = new char[3] { '\0', '\0', '\0' };
        static readonly char[] TYPE_TXT = new char[3] { 'T', 'X', 'T' };
        static readonly char[] TYPE_QST = new char[3] { 'Q', 'S', 'T' };
        static readonly char[] TYPE_MAP = new char[3] { 'M', 'A', 'P' };
        static readonly char[] TYPE_TEX = new char[3] { 'T', 'E', 'X' };
        static readonly char[] TYPE_BIT = new char[3] { 'B', 'I', 'T' };
        static readonly char[] TYPE_MSH = new char[3] { 'M', 'S', 'H' };
        static readonly char[] TYPE_PFX = new char[3] { 'P', 'F', 'X' };
        static readonly char[] TYPE_WAV = new char[3] { 'W', 'A', 'V' };
        static readonly char[] TYPE_OGG = new char[3] { 'O', 'G', 'G' };
        char[] Magick = null;
        int Unknown1;
        int Unknown2;
        short Unknown3;
        char[] TypeMagick;
        string resname = "";
        string toolfolder = "";
        AssetType Type = AssetType.Generic;
        AssetDataGeneric data = null;

        public AssetBuilder(string afile, string abase, string gamefolder)
        {
            toolfolder = gamefolder;
            ReadFile(afile, abase);
        }

        public void ReadFile(string afile, string abase)
        {
            afile = Path.GetFullPath(afile);
            abase = Path.GetFullPath(abase);
            resname = afile.Substring(abase.Length).TrimStart(Path.DirectorySeparatorChar);
            using (FileStream fs = new FileStream(afile, FileMode.Open))
                ReadStream(fs);
        }

        public void ReadStream(Stream astream)
        {
            using (BinaryReader br = new BinaryReader(astream, Encoding.ASCII, true))
            {
                Magick = br.ReadChars(4);
                Unknown1 = br.ReadInt32(); // Not sure what, may be few separate fields in this
                Unknown2 = br.ReadInt32(); // Usually one
                Unknown3 = br.ReadInt16(); // Usually zero
                TypeMagick = br.ReadChars(3); // like MAP+0x01, QST+0x01, etc
                byte count = br.ReadByte();
                if (TypeMagick.SequenceEqual(TYPE_NUL))
                {
                    Type = AssetType.Generic;
                    data = new AssetDataGeneric();
                    data.ReadBytes(br);
                }
                else
                if (TypeMagick.SequenceEqual(TYPE_TEX))
                {
                    Type = AssetType.Texture;
                    data = new AssetDataTex();
                    data.ReadBytes(br);
                }
                else
                if (TypeMagick.SequenceEqual(TYPE_MAP))
                {
                    Type = AssetType.Map;
                    data = new AssetDataGeneric(); // TODO: Make non generic
                    data.ReadBytes(br);
                }
                else
                if (TypeMagick.SequenceEqual(TYPE_BIT))
                {
                    Type = AssetType.Bitmap;
                    data = new AssetDataGeneric();
                    data.ReadBytes(br);
                }
                else
                if (TypeMagick.SequenceEqual(TYPE_MSH))
                {
                    Type = AssetType.Mesh;
                    data = new AssetDataMsh();
                    data.ReadBytes(br);
                }
                else
                if (TypeMagick.SequenceEqual(TYPE_TXT))
                {
                    Type = AssetType.Text;
                    data = new AssetDataGeneric();
                    data.ReadBytes(br);
                }
                else
                if (TypeMagick.SequenceEqual(TYPE_QST))
                {
                    Type = AssetType.Quest;
                    data = new AssetDataGeneric();
                    data.ReadBytes(br);
                }
                else
                if (TypeMagick.SequenceEqual(TYPE_PFX))
                {
                    Type = AssetType.ParticleFX;
                    data = new AssetDataGeneric();
                    data.ReadBytes(br);
                }
                else
                if (TypeMagick.SequenceEqual(TYPE_WAV))
                {
                    Type = AssetType.Wave;
                    data = new AssetDataGeneric();
                    data.ReadBytes(br);
                }
                else
                if (TypeMagick.SequenceEqual(TYPE_OGG))
                {
                    Type = AssetType.Wave;
                    data = new AssetDataGeneric();
                    data.ReadBytes(br);
                }
                else
                {
                    Type = AssetType.Unknown;
                    data = new AssetDataGeneric();
                    data.ReadBytes(br);
                }
            }
        }

        public string CompileTexture(string arguments)
        {
            Process TextureCompilerP = new Process();
            TextureCompilerP.StartInfo.FileName = Path.Combine(toolfolder, "TextureCompiler.exe");
            TextureCompilerP.StartInfo.Arguments = arguments;
            // Console.WriteLine(TextureCompilerP.StartInfo.Arguments);
            Program.Log.Debug("Call: TextureCompiler.exe {0}", TextureCompilerP.StartInfo.Arguments);
            TextureCompilerP.StartInfo.UseShellExecute = false;
            TextureCompilerP.StartInfo.RedirectStandardOutput = true;
            TextureCompilerP.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            TextureCompilerP.StartInfo.CreateNoWindow = true; //not diplay a windows
            TextureCompilerP.Start();
            string output = TextureCompilerP.StandardOutput.ReadToEnd(); //The output result
            TextureCompilerP.WaitForExit();
            return output;
        }

        public void ExtractMDL(string infile, string outfile)
        {
            byte[] buffer = File.ReadAllBytes(infile);
            byte[] headerMagic = Encoding.ASCII.GetBytes(new char[4] { 'M', 'D', 'L', (char)0x07 });
            byte[] endString = Encoding.ASCII.GetBytes("ExportDataMDL");
            int headerOffset = BoyerMoore.indexOf(buffer, headerMagic);
            int endOffset = BoyerMoore.indexOf(buffer, endString);
            // Console.WriteLine("Header = 0x{0:X}, Footer = 0x{1:X}, Out: {2}", headerOffset, endOffset, outfile);
            using (FileStream ofs = new FileStream(outfile, FileMode.Create))
            {
                ofs.Write(buffer, headerOffset, endOffset - headerOffset);
            }
        }

        public void CompileAsset(string srcfolder, string resfolder)
        {
            if (!Directory.Exists(resfolder))
                Directory.CreateDirectory(resfolder);
            string src = Path.Combine(srcfolder, data.srcfile);
            string tgt = Path.Combine(resfolder, resname);
            string srcm = src.Replace(Path.DirectorySeparatorChar, '/');
            string tgtm = tgt.Replace(Path.DirectorySeparatorChar, '/');
            string srcfm = srcfolder.Replace(Path.DirectorySeparatorChar, '/');
            if (!srcfm.EndsWith("/")) srcfm = srcfm + "/";

            if (Type == AssetType.Map)
            {

                Process MapCompilerP = new Process();
                MapCompilerP.StartInfo.FileName = Path.Combine(toolfolder, "MapCompiler.exe");
                MapCompilerP.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\"", srcm, srcfm, tgtm);
                // Console.WriteLine(MapCompilerP.StartInfo.Arguments);
                Program.Log.Debug("Call: MapCompiler.exe {0}", MapCompilerP.StartInfo.Arguments);
                MapCompilerP.StartInfo.UseShellExecute = false;
                MapCompilerP.StartInfo.RedirectStandardOutput = true;
                MapCompilerP.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                MapCompilerP.StartInfo.CreateNoWindow = true; //not diplay a windows
                MapCompilerP.Start();
                string output = MapCompilerP.StandardOutput.ReadToEnd(); //The output result
                MapCompilerP.WaitForExit();
                // Console.WriteLine("Output:\n{0}\nExit Code: {1}", output, MapCompilerP.ExitCode); // Debug                
                // Console.WriteLine("MAP: {0}", data.srcfile);
                // TODO: May Be Check output file exists
            }
            else if (Type == AssetType.Bitmap)
            {
                // Console.WriteLine("Compiling Bitmap {0} to {1}", data.srcfile, tgtm);
                string output = CompileTexture(string.Format("\"{0}\" \"{1}\" -nopoweroftwo -nomipmaps", srcm, tgtm));
                // Console.WriteLine(output);
            }
            else if (Type == AssetType.Texture)
            {
                AssetDataTex tex = data as AssetDataTex;
                // Console.WriteLine("Texture {0}, Type: {1}, Mipmaps={2}, ConvertToNormalMap={3}", tex.srcfile, tex.FormatStr(), tex.CreateMipmaps, tex.ConvertToNormalMap);
                StringBuilder args = new StringBuilder(string.Format("\"{0}\" \"{1}\" -nopoweroftwo", srcm, tgtm));
                if (tex.Format != TexFormat.Uncompressed)
                    args.AppendFormat(" -format {0}", tex.FormatStr());
                if (tex.ConvertToNormalMap)
                    args.Append(" -normalmap");
                if (!tex.CreateMipmaps)
                    args.Append(" -nomipmaps");
                if (tex.FPS > 0 && tex.FPS != 20)
                    args.AppendFormat(" -fps {0}", tex.FPS);
                string output = CompileTexture(args.ToString());
                // Console.WriteLine("Output:\n{0}", output); // Debug
            }
            else if (Type == AssetType.Mesh)
            {
                AssetDataMsh msh = data as AssetDataMsh;
                string miffile = Path.Combine(srcfolder, msh.miffile).Replace(Path.DirectorySeparatorChar, '/');
                string tempfile = Path.Combine(Path.GetTempPath(), string.Format("temp-{0}.mdl", Path.GetFileNameWithoutExtension(src)));
                string basefolder = Path.GetFullPath(Path.Combine(srcfolder, ".."));
                // Console.WriteLine("Compiling model \"{0}\"", resname);
                ExtractMDL(src, tempfile);
                Process ModelCompilerP = new Process();
                ModelCompilerP.StartInfo.FileName = Path.Combine(toolfolder, "ModelCompiler.exe");
                string addFlags = "";
                if (msh.TangentSpaceVectors)
                    addFlags += " -tangents";
                if (msh.VertexColors)
                    addFlags += " -vertexColors";
                string srcfoldermod = srcfolder.TrimEnd(Path.DirectorySeparatorChar);
                string mifparam = "";
                if (Path.DirectorySeparatorChar == '\\')
                    srcfoldermod += @"\\";
                else
                    srcfoldermod += Path.DirectorySeparatorChar;
                if (!string.IsNullOrWhiteSpace(msh.miffile))
                    mifparam = string.Format(" -mif \"{0}\" \"{1}\"", msh.miffile, srcfoldermod);
                // ModelCompilerP.StartInfo.Arguments = string.Format("\"{0}\" {1} -mif \"{2}\" \"{3}\" \"{4}\"", tempfile.Replace(Path.DirectorySeparatorChar, '/'), addFlags, msh.miffile, srcfoldermod, tgtm);
                ModelCompilerP.StartInfo.Arguments = string.Format("\"{0}\"{1}{2} \"{3}\"", tempfile.Replace(Path.DirectorySeparatorChar, '/'), addFlags, mifparam, tgtm);
                Program.Log.Debug("Call: ModelCompiler.exe {0}", ModelCompilerP.StartInfo.Arguments); // DEBUG:
                ModelCompilerP.StartInfo.UseShellExecute = false;
                ModelCompilerP.StartInfo.RedirectStandardOutput = true;
                ModelCompilerP.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                ModelCompilerP.StartInfo.CreateNoWindow = true; //not diplay a windows
                ModelCompilerP.Start();
                string output = ModelCompilerP.StandardOutput.ReadToEnd(); //The output result
                // Console.WriteLine("Output:\n{0}", output); // DEBUG:
                ModelCompilerP.WaitForExit();
                File.Delete(tempfile);
            }
            // Direct copy assets:
            else if (Type == AssetType.Generic || Type == AssetType.Text || Type == AssetType.Quest || Type == AssetType.Wave || Type == AssetType.Ogg || Type == AssetType.ParticleFX)
            {
                // Console.WriteLine("Copying {0} to {1}", src, tgt);
                string tgtpath = Path.GetDirectoryName(tgt);
                if (!Directory.Exists(tgtpath))
                    Directory.CreateDirectory(tgtpath);
                File.Copy(src, tgt, true);
            }
            else if (Type == AssetType.Unknown)
            {
                Program.Log.Warn("Cannot read asset {0}, unknown type \"{1}\" or not implemented. Please use AssetManager.", this.resname, new string(TypeMagick));
            }
            else
            {
                Program.Log.Debug("Asset \"{0}\" of type {1} slipped through build sieve.", resname, new string(TypeMagick));
            }
        }
    }



}
