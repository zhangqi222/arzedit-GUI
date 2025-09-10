using CommandLine;
using System;
using System.Collections.Generic;

namespace arzedit
{
    // Classes used by CommandLine parameter parsing library
    [Verb("set", HelpText = "Set values in database")]
    class SetOptions
    {
        [Value(0, Required = true, HelpText = "Input file path")]
        public string InputFile { get; set; }
        
        [Option('o', "output", HelpText = "Write changes to specified file. NOTE: If not provided - overwrites input file, make backups!")]
        public string OutputFile { get; set; }
        
        [Option('y', "overwrite", HelpText = "Force overwrite output file, be careful, make backups!")]
        public bool ForceOverwrite { get; set; }
        
        [Option('b', "base", HelpText = "Pack records based at this folder (usually one which has /records/ folder)")]
        public string SetBase { get; set; }
        
        [Option('s', "subfolder", HelpText = "Pack only records in subfolder and below, relative to base being packed, like \"records\\game\\\"")]
        public string SetSubfolder { get; set; }
        
        [Option('r', "record", HelpText = "Record to be changed, format example: \"records/game/gameengine.dbr\" if it contains spaces - enclose in double quotes (")]
        public string SetRecord { get; set; }
        
        [Option('p', "patch", HelpText = "Use patch file to update multiple records and entries", Separator = ',')]
        public IEnumerable<string> SetPatches { get; set; }
        
        [Option('f', "file", HelpText = "Record file (*.dbr) to be assigned to the record.")]
        public string SetFile { get; set; }
        
        [Option('e', "entries", HelpText = "Entry names with values. Entry example: \"playerDevotionCap,56,\", Multiple entries are separated by spaces, if entry contains spaces it must be enclosed in doublequotes (\").", Separator = ',')]
        public IEnumerable<string> SetEntries { get; set; }
    }

    [Verb("get", HelpText = "Get records/values in database")]
    class GetOptions
    {
        [Value(0, Required = true, HelpText = "Input file path")]
        public string InputFile { get; set; }
    }

    [Verb("extract", HelpText = "Extract records from database")]
    class ExtractOptions
    {
        [Value(0, Required = true, HelpText = "Input file path")]
        public string InputFile { get; set; }
        
        [Value(1, HelpText = "Output directory path")]
        public string OutputPath { get; set; }
        
        [Option('y', "overwrite", HelpText = "Force overwrite files in target folder, be careful, make backups!")]
        public bool ForceOverwrite { get; set; }
    }

    [Verb("pack", HelpText = "Pack records to database")]
    class PackOptions
    {
        [Value(0, Required = true, HelpText = "Input directory path")]
        public string InputPath { get; set; }
        
        [Value(1, Required = true, HelpText = "Output file path")]
        public string OutputFile { get; set; }
        
        [Option('t', "tbase", HelpText = "Folder(s) containing templates, if not specified - assumes templates are in mod folder. Order matters - later templates override prior. You would like game templates go first and your own templates second.", Separator = ',')]
        public IEnumerable<string> TemplatePaths { get; set; }
        
        [Option('p', "peek", HelpText = "Peek at database and compare results - debugging option")]
        public string PeekFile { get; set; }
        
        [Option('y', "overwrite", HelpText = "Force overwrite target file, make backups!")]
        public bool ForceOverwrite { get; set; }
        
        [Option('r', "refs", HelpText = "Check if referenced files exist. Needs \"resources\" folder in <mod base>. May generate a lot of messages.")]
        public bool CheckReferences { get; set; }
    }

    [Verb("build", HelpText = "Build mod")]
    class BuildOptions
    {
        [Value(0, Required = true, HelpText = "Mod directory path")]
        public string ModPath { get; set; }
        
        [Value(1, Required = true, HelpText = "Build output directory path")]
        public string BuildPath { get; set; }
        
        [Option('g', "game-folder", HelpText = "Grim Dawn folder (tools folder)")]
        public string GameFolder { get; set; }
        
        [Option('t', "tbase", HelpText = "Folder(s) containing additional templates.", Separator = ',')]
        public IEnumerable<string> TemplatePaths { get; set; }
        
        [Option('D', "skip-db", HelpText = "Skip building database")]
        public Boolean SkipDB { get; set; }
        
        [Option('A', "skip-assets", HelpText = "Skip compiling assets")]
        public Boolean SkipAssets { get; set; }
        
        [Option('R', "skip-res", HelpText = "Skip compressing Resources folder")]
        public Boolean SkipResources { get; set; }
        
        [Option('v', "verbose", HelpText = "Output Debug level messages")]
        public Boolean EnableVerbose { get; set; }
        
        [Option('s', "silent", HelpText = "Output only Error messages")]
        public Boolean EnableSilent { get; set; }
        
        [Option('l', "log-file", HelpText = "Log all messages (of any level) to file")]
        public string LogFile { get; set; }
        
        /// <summary>
        /// 存储用户选择的dbr文件列表（用于选择性打包）
        /// </summary>
        public string[] SelectedDbrFiles { get; set; }
    }

    [Verb("unarc", HelpText = "Unpack arc file(s)")]
    class UnarcOptions
    {
        [Value(0, Required = true, HelpText = "Arc file(s) to unpack")]
        public IEnumerable<string> ArcFiles { get; set; }
        
        [Option('o', "out-path", HelpText = "Path where to store unpacked files")]
        public string OutPath { get; set; }
    }

    [Verb("arc", HelpText = "pack arc file(s)")]
    class ArcOptions
    {
        [Value(0, Required = true, HelpText = "Folder to pack")]
        public string Folder { get; set; }
        
        [Value(1, Required = true, HelpText = "Output arc file path")]
        public string OutFile { get; set; }
        
        [Option('m', "mask", HelpText = "Mask for file inclusion, All files are added if not specified")]
        public string FileMask { get; set; }
    }
}