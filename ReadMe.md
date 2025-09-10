🌏 中文 / [english](./ReadMe-en.md)

## 📖 简介：
- 本工具是在原命令行工具 [arzedit](https://github.com/rossknudsen/arzedit) 的基础上修改而来。
- 本工具只是辅助修改mod工具，想完整制作mod还是使用AssetManager较好
- 请在[Releases](../releases)处进行下载使用**arzedit-GUI**。

## 🟢 主要目的：
是通过添加GUI界面，更便捷快速的操作打包和解包工作。
- 快速操作，避免输入太多的参数，一拖一拽即可
- 方便不懂命令行操作的人操作
- 方便不懂使用AssetManager的小白操作
- 方便简单修改mod

### 📑 添加及修改内容：
- arz
	- 添加打包目录选择功能
	- 添加查看功能
	- 添加内置模板837个（由TT300提供）
	- 修改打包时忽略异常模板后继续执行
	- 修改打包mod时忽略资产打包、资源打包
	- 修改不需要输入游戏根目录
- arc
	- 修改arc包中如有空记录则继续执行
	- 添加查看功能
- 其他
	- 添加GUI
	- 添加多语言支持
	- 添加中文文件名及目录支持
	- 修改编译环境为.net8
	- 修改LZ4为K4os.Compression.LZ4
	- 修改Nlog为异步执行

## 🐸 部分截图：
![打包ARZ](./screenshot/Pasted%20image%2020250910093734.png)
![PackARZ](./screenshot/Pasted%20image%2020250910093749.png)
![查看arc](./screenshot/Pasted%20image%2020250910094342.png)
![查看arz](./screenshot/Pasted%20image%2020250910094135.png)

## 📈 未来计划：
- [ ] 资产打包功能（可能时间较长，近期无打算）