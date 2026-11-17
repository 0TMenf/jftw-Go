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

## API 状态

资源接口 `https://jftw.pinkoc.top/api/jftwGo/` 由服务端提供，当前**尚未就绪**。客户端已实现超时/重试/本地缓存回退/解析容错；API 上线后无需改动客户端即可接入，联调待接口就绪后进行。

## 许可证

[Apache License 2.0](LICENSE)
