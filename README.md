# jftw Go

基于 **.NET Framework 4.8 + C# (WinForms)** 的 jftw for Windows 分支。

> **jftw Go Installer** 和 **jftw Go Direct" 以及 **jftw Go Start1Dev** 是**三个彼此独立**的软件，各自位于独立目录，如果你是开发者，请注意。

| 软件 | 目录 | 用途 | 产物 |
|---|---|---|---|
| **jftw Go Installer** | [`Installer/`](Installer/) | 把选定的 jftw Go 资源整合包部署到 U 盘，生成可携带的启动介质 | `jftw Go Installer.exe` |
| **jftw Go Direct** | [`Direct/`](Direct/) | 无 U 盘、网络较好时，在线下载并解压安装到指定目录并启动 | `jftw Go Direct.exe` |  
| **jftw Go Start1Dev** | [`Start1Dev/`](Start1Dev/) | 于 jftw ModPack 及 jftw Go 贡献者 的开发工具 | `jftw Go Start1Dev.exe` |  

- **Installer** 不是 jftw Go 的普通安装程序，而是 U 盘资源部署工具；**Direct** 指「无需 U 盘的在线直装/直启」。
- **Start1Dev** 是于 jftw ModPack 及 jftw Go 贡献者 的开发工具，如果你是普通用户请勿下载。  
- 三者共享同一资源 API 契约与同一套资源选择界面设计，但作为三个独立软件分别维护。

## 目录结构

```
jftwGo/
├─ Installer/
│   ├─ jftwGo.sln             
│   ├─ JftwGo.Core/        
│   ├─ JftwGo.UI/        
│   ├─ JftwGo.Installer/    
│   ├─ packages/            
│   └─ samples/           
└─ Direct/              
    ├─ jftwGo.sln           
    ├─ JftwGo.Core/       
    ├─ JftwGo.UI/           
    ├─ JftwGo.Direct/          
    ├─ packages/
    └─ samples/
└─ Direct/              
    ├─ jftwGo.sln           
    ├─ JftwGo.Core/       
    ├─ JftwGo.UI/           
    ├─ JftwGo.Start1Dev/          
    ├─ packages/
    └─ samples/
```

## 构建

进入任一软件目录单独构建：

```bat
cd Installer
dotnet build jftwGo.sln -c Release
:: 产物：Installer\JftwGo.Installer\bin\Release\jftw Go Installer.exe

cd ..\Direct
dotnet build jftwGo.sln -c Release
:: 产物：Direct\JftwGo.Direct\bin\Release\jftw Go Direct.exe
  
cd ..\Start1Dev
dotnet build jftwGo.sln -c Release
:: 产物：Direct\JftwGo.Direct\bin\Release\Start1Dev_jftw_Go.exe
```

也可用 Visual Studio 2019/2022 分别打开三个目录下的 `jftwGo.sln`。  
目标框架均为 .NET Framework 4.8。

各软件的详细说明、UI 页面、API 契约与关键实现见对应目录的 README：

- [Installer 说明](Installer/README.md)
- [Direct 说明](Direct/README.md)
- [Start1Dev 说明](Start1Dev/README.md)

## 多语言

三个软件均内置多语言，语言包为各自目录下的 `lang/*.yaml`：

| 语言 | 包名 |
|---|---|
| 简体中文 | `zh_cn` |
| 繁体中文 | `zh_tw` |
| 繁体中文（香港） | `zh_hk` |

- 所有面向用户的文案（界面、状态、日志、错误）均通过本地化 key 从 YAML 读取，代码中无硬编码 UI 中文。
- 首次启动按系统区域选择语言（中文系统默认简体中文），之后记住用户选择（存于 `%LocalAppData%\jftwGo\language.txt`）。
- 运行时在向导窗口顶部右侧的下拉框切换语言，选择后自动重启以重建全部界面文本。
- `lang/` 目录随程序输出到 exe 同级目录，可直接编辑 YAML 调整措辞或新增语言（新增后需在下拉框代码 `langCodes` 数组中登记）。YAML 为扁平 `key: value`，`{0}` 为占位符，`\n` 为换行。

## 内置字体

界面统一使用**思源黑体（Noto Sans SC）**，以 SIL Open Font License 1.1 开源，可合法随软件分发，规避微软雅黑等系统字体的商用授权风险。

- 字体文件：`fonts/NotoSansSC-Regular.ttf`（由可变字体固定为 wght=400 静态实例，保留全部字符，约 10 MB），许可证见 `fonts/OFL.txt`。
- 构建后复制到 exe 同级 `fonts/` 目录，通过 GDI `PrivateFontCollection` 私有加载，**无需在系统中安装字体**。
- 字体缺失或损坏时，自动回退到系统 sans 字体，程序仍可正常运行。
- 如需重新生成字体文件：`python tools/slim_font.py`（依赖 fonttools；输入为 Noto Sans SC 可变 TTF）。

## API 与 JSON 文件

客户端从一个 URL 拉取资源列表 JSON，地址在 exe 同级 `*.exe.config` 的 `appSettings` 中配置：

```xml
<add key="ApiEndpoint" value="https://jftw.pinkoc.top/api/jftwGo/" />
<add key="ApiOrigin" value="https://jftw.pinkoc.top" />
```

**"JSON 文件名填什么"**：
- `ApiEndpoint` 填的是**完整 URL**，不是本地文件名。它既可以是接口路径（如 `/api/jftwGo/`，由后端返回 JSON），也可以直接指向一个静态文件。
- 若用静态 JSON 文件，推荐命名为 **`resources.json`**，并把 `ApiEndpoint` 填成该文件的完整地址，例如：
  `https://jftw.pinkoc.top/api/jftwGo/resources.json`
- 可直接把 `samples/resources.json` 作为模板放到服务器对应位置，按实际资源修改内容。
- `ApiOrigin` 是相对图标 URL（以 `/` 开头）的拼接基准域名。

JSON 结构样例见 `samples/resources.json`（顶层 `schemaVersion` / `generatedAt` / `resources`，其中 `resources` 含 `official.stable|preview|dev`、`historical[]`、`community[]`、`games[]`）。

客户端已实现超时/重试/本地缓存回退/解析容错；接口就绪后改 config 即可接入，**无需重新编译**。当前接口尚未就绪，联调待后端完成。

## 许可证

[Apache License 2.0](LICENSE)
