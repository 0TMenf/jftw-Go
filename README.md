# jftw Go

基于 **.NET Framework 4.8 + C# (WinForms)** 的 Windows 桌面工具集。

> 这是**两个彼此独立**的软件，各自位于独立目录、独立解决方案，可单独打开、构建、发布和交付；互不依赖、互不引用。二者各自携带一份 Core/UI 源码副本，以保证完全独立。

| 软件 | 目录 | 用途 | 产物 |
|---|---|---|---|
| **jftw Go Installer** | [`Installer/`](Installer/) | 把选定的 jftw Go 资源整合包部署到 U 盘，生成可携带的启动介质 | `jftw Go Installer.exe` |
| **jftw Go Direct** | [`Direct/`](Direct/) | 无 U 盘、网络较好时，在线下载并解压安装到指定目录并启动 | `jftw Go Direct.exe` |

- **Installer** 不是 jftw Go 的普通安装程序，而是 U 盘资源部署工具；**Direct** 指「无需 U 盘的在线直装/直启」。
- 两者共享同一资源 API 契约与同一套资源选择界面设计，但作为两个独立软件分别维护。

## 目录结构

```
jftwGo/
├─ Installer/                    ← 独立软件 1：U 盘资源部署器
│   ├─ jftwGo.sln                （Core + UI + Installer 三个项目）
│   ├─ JftwGo.Core/              核心类库副本
│   ├─ JftwGo.UI/                共享 UI 控件副本
│   ├─ JftwGo.Installer/         主程序（6 页向导）
│   ├─ packages/                 Newtonsoft.Json.13.0.3（net45）
│   └─ samples/                  资源列表 JSON 样例
└─ Direct/                       ← 独立软件 2：在线直装
    ├─ jftwGo.sln                （Core + UI + Direct 三个项目）
    ├─ JftwGo.Core/              核心类库副本
    ├─ JftwGo.UI/                共享 UI 控件副本
    ├─ JftwGo.Direct/            主程序（5 页向导）
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
```

也可用 Visual Studio 2019/2022 分别打开两个目录下的 `jftwGo.sln`。目标框架均为 .NET Framework 4.8。

各软件的详细说明、UI 页面、API 契约与关键实现见对应目录的 README：

- [Installer 说明](Installer/README.md)
- [Direct 说明](Direct/README.md)

## 多语言

两个软件均内置多语言，语言包为各自目录下的 `lang/*.yaml`：

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

## API 状态

资源接口 `https://jftw.pinkoc.top/api/jftwGo/` 由服务端提供，当前**尚未就绪**。客户端已实现超时/重试/本地缓存回退/解析容错；API 上线后无需改动客户端即可接入，联调待接口就绪后进行。

## 许可证

[Apache License 2.0](LICENSE)
