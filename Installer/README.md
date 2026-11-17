# jftw Go Installer

U 盘资源部署器（**独立软件**）。把选定的 jftw Go 资源整合包部署到 U 盘，生成可携带的启动介质。

> 本软件可完全独立构建与交付，不依赖 Direct。解决方案 `jftwGo.sln` 包含 Core + UI + Installer 三个项目。

## 构建与产物

```bat
cd Installer
dotnet build jftwGo.sln -c Release
```

产物目录：`Installer\JftwGo.Installer\bin\Release\`，包含：

- `jftw Go Installer.exe`（入口：`JftwGo.Installer.Program.Main`，需管理员权限）
- `JftwGo.Core.dll`、`JftwGo.UI.dll`、`Newtonsoft.Json.dll`、`jftw Go Installer.exe.config`

运行要求：Windows 7 SP1 及以上 + .NET Framework 4.8。

## 向导页面（6 页）

1. 欢迎页
2. 资源选择页（Head 四分类 + 官方三通道 + 资源卡片）
3. U 盘选择与检测（盘符/卷标/文件系统/总容量/可用空间）
4. 确认部署（说明目录结构；检测 `jftw_Go_Assets` 是否占用；勾选确认）
5. 部署进度（下载 → 部署 → 进度/日志/取消/重试）
6. 完成页

## U 盘部署结构（不格式化）

**本工具不会格式化 U 盘，也不会删除盘中现有文件。** 部署后在 U 盘上生成：

```
U:\
├─ jftw_Go_Assets\
│   └─ version\
│       ├─ <整合包ID-1>\        ← 每个整合包一个独立文件夹（类似 Minecraft .minecraft/version）
│       │   └─ start1.exe
│       └─ <整合包ID-2>\
│           └─ start1.exe
├─ jftw Go.lnk                   ← 快捷方式，指向本次写入包的 start1.exe
└─ jftw Go.bat                   ← 启动脚本，用相对路径启动 start1.exe（盘符变化仍可用）
```

- **文件夹占用**：若 U 盘根目录已存在 `jftw_Go_Assets`，确认页会明确提示"文件夹被占用"；此时采用追加模式——保留已有内容，仅在 `version\` 下创建/更新本次整合包文件夹，不删除任何其他文件。
- **多整合包共存**：每次部署一个整合包，各自占用 `version\` 下的一个子文件夹；根目录的 `jftw Go.lnk` / `jftw Go.bat` 每次更新为指向最近一次部署的整合包。
- **start1.exe**：部署器会在整合包目录（含子目录）中查找 `start1.exe`；找到则创建快捷方式与 bat，未找到则资源照常解压、仅跳过启动入口并给出提示。
- **路径可移植**：bat 使用 `%~dp0` + 相对路径定位，U 盘换电脑后盘符变化仍能启动。

## 数据流与关键能力

- 资源来自 `GET https://jftw.pinkoc.top/api/jftwGo/`（当前 API 未就绪，客户端已内置超时/重试/缓存回退/解析容错）。
- 图标异步加载 + 内存/磁盘二级缓存 + 占位图；相对 URL 按 API origin 解析。
- 下载器支持进度、取消、失败重试、日志、checksum 校验（存在时）。
- 高 DPI：`app.manifest` 声明 `PerMonitorV2` + `AutoScaleMode.Dpi`。
- 日志：`%LocalAppData%\jftwGo\logs\`。

## 多语言

语言包位于 `lang/`（`zh_cn` 简体、`zh_tw` 繁体、`zh_hk` 繁体香港），构建后复制到 exe 同级的 `lang/` 目录。所有 UI 文案均通过本地化 key 从 YAML 读取。向导窗口右上角可切换语言，选择后自动重启生效；选择保存在 `%LocalAppData%\jftwGo\language.txt`。

## 样例数据

`samples/resources.sample.json` 为资源列表契约样例。
