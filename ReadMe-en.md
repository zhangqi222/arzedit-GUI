🌏 [Chinese](./ReadMe.md) / English

## 📖 Introduction:
- This tool is a modified version based on the original command-line tool [arzedit](https://github.com/rossknudsen/arzedit).
- This tool is only an auxiliary mod-editing utility. For complete mod creation, it's better to use AssetManager.
- Please download and use **arzedit-GUI** at [Releases](../releases).

## 🟢 Main Purpose:
To provide a GUI interface for more convenient and faster packing and unpacking operations.
- Quick operations, avoiding lengthy parameter inputs—just drag and drop.
- Easy to use for people unfamiliar with command-line operations.
- Accessible for beginners who don't know how to use AssetManager.
- Convenient for simple mod modifications.
- This tool is built with .net8, please follow the prompts after opening and install the .net8 runtime.

### 📑 Added and Modified Features:
- arz
  - Added directory selection for packing
  - Added viewing functionality
  - Added 837 built-in templates (provided by TT300)
  - Modified to continue execution after ignoring invalid templates during packing
  - Modified to skip asset and resource packing when creating mods
  - Modified to remove the need for specifying the game root directory
- arc
  - Fixed to continue execution if empty entries exist in arc files
  - Added viewing functionality
- Others
  - Added GUI
  - Added multi-language support
  - Added support for Chinese filenames and directories
  - Upgraded compilation environment to .NET 8
  - Replaced LZ4 with K4os.Compression.LZ4
  - Changed NLog to run asynchronously

## 🐸 Screenshots:
![Pack ARZ](./screenshot/Pasted%20image%2020250910093734.png)  
![PackARZ](./screenshot/Pasted%20image%2020250910093749.png)  
![View arc](./screenshot/Pasted%20image%2020250910094342.png)  
![View arz](./screenshot/Pasted%20image%2020250910094135.png)

## 📈 Future Plans:
- [ ] Asset packing feature (may take a long time; no immediate plans)