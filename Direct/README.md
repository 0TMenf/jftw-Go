# jftw Go Direct

在线直装工具（**独立软件**）。无 U 盘、网络较好时，在线下载 jftw Go 资源整合包，解压安装到指定目录并可选启动。

> 本软件可完全独立构建与交付，不依赖 Installer。解决方案 `jftwGo.sln` 包含 Core + UI + Direct 三个项目。

## 构建与产物

```bat
cd Direct
dotnet build jftwGo.sln -c Release
```

产物目录：`Direct\JftwGo.Direct\bin\Release\`，包含：

- `jftw Go Direct.exe`（入口：`JftwGo.Direct.Program.Main`，`asInvoker` 无需管理员权限）
- `JftwGo.Core.dll`、`JftwGo.UI.dll`、`Newtonsoft.Json.dll`、`jftw Go Direct.exe.config`

运行要求：Windows 7 SP1 及以上 + .NET Framework 4.8。

## 向导页面（5 页）

1. 欢迎页
2. 资源选择页（Head 四分类 + 官方三通道 + 资源卡片，与 Installer 同设计）
3. 安装路径与启动选项（目录选择 + 是否安装后启动）
4. 下载与安装进度（进度/日志/取消/重试/错误提示）
5. 完成页（可打开安装目录）

## 关键能力

- 资源来自 `GET https://jftw.pinkoc.top/api/jftwGo/`（当前 API 未就绪，客户端已内置超时/重试/缓存回退/解析容错）。
- 图标异步加载 + 内存/磁盘二级缓存 + 占位图；相对 URL 按 API origin 解析。
- 下载器支持进度、取消、失败重试、日志、checksum 校验（存在时）。
- zip 解压带路径穿越防护；安装后自动查找 `start1.exe` 等可执行程序并可选启动。
- 高 DPI：`app.manifest` 声明 `PerMonitorV2` + `AutoScaleMode.Dpi`。
- 日志：`%LocalAppData%\jftwGo\logs\`。

## 样例数据

`samples/resources.sample.json` 为资源列表契约样例。
