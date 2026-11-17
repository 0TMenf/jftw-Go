# jftw Go

基于 **.NET Framework 4.8 + C# (WinForms)** 的 Windows 桌面工具，包含两个独立子程序，共享一套核心类库与 UI 控件。

| 子程序 | 用途 | 产物 |
|---|---|---|
| **jftw Go Installer** | 插入 U 盘后，把选定的 jftw Go 资源写入 U 盘，制作可携带的启动介质 | `jftw Go Installer.exe` |
| **jftw Go Direct** | 无 U 盘、网络较好时，在线下载并解压安装到指定目录并启动 | `jftw Go Direct.exe` |

> Installer **不是** jftw Go 的普通安装程序，而是 U 盘启动盘制作器；Direct 指「无需 U 盘的在线直装/直启」。

---

## 一、解决方案结构

```
jftwGo/
├─ jftwGo.sln
├─ JftwGo.Core/            共享核心类库（纯逻辑，无 UI 依赖）
│  ├─ Models/              ResourceCatalog / ResourceContainer / ResourceItem / ChecksumInfo / ChannelKind
│  ├─ Api/                 ApiClient / ApiOptions / ResourceLoadResult / ApiException
│  ├─ Icons/               IconCache（异步 + 内存/磁盘二级缓存 + 缩略图）
│  ├─ Download/            Downloader（进度 / 取消 / 重试 / 校验）
│  ├─ Usb/                 UsbScanner / UsbDeviceInfo（WMI 枚举可移动盘）
│  ├─ Verify/              ChecksumVerifier（SHA256 / SHA1 / MD5）
│  ├─ Logging/             ILogger / EventLogger / FileLogger / NullLogger
│  ├─ Packaging/           ArchiveExtractor（zip 解压，含路径穿越防护）
│  └─ AppPaths.cs          缓存 / 日志 / 临时 / 下载目录
├─ JftwGo.UI/              共享 WinForms 控件
│  ├─ Controls/            ResourceBrowser / ResourceCard / LogPanel / ProgressPanel
│  ├─ Wizard/              WizardForm（向导壳）/ WizardPage（页面基类）
│  └─ UiTheme.cs           配色、字体、按钮工厂
├─ JftwGo.Installer/       U 盘制作器（WinExe，requireAdministrator）
│  ├─ Program.cs           入口
│  ├─ FrmMain.cs           向导壳 + InstallContext
│  ├─ UsbWriter.cs         清盘 / 释放资源 / 写标记
│  └─ Pages/               Welcome / Resource / UsbSelect / FormatWarning / WriteProgress / Done
├─ JftwGo.Direct/          在线直装（WinExe，asInvoker）
│  ├─ Program.cs           入口
│  ├─ FrmMain.cs           向导壳 + DirectContext
│  └─ Pages/               Welcome / Resource / InstallOptions / DownloadInstall / Done
├─ packages/               Newtonsoft.Json.13.0.3（已随仓库提供，无需联网还原）
└─ samples/                resources.sample.json（契约样例）
```

依赖方向单向：`Installer / Direct → JftwGo.UI → JftwGo.Core`。

---

## 二、职责边界

| 维度 | Installer | Direct |
|---|---|---|
| 运行场景 | 高配电脑 + 可插 U 盘 | 无 U 盘 + 网络较好 |
| 资源来源 | 同一 API、同一 `ResourceItem` 模型 | 同左 |
| 资源去向 | 写入 U 盘，做成启动/便携介质 | 解压到指定目录并启动 |
| 独有能力 | WMI 枚举可移动盘、清盘、写盘标记 | 目录选择、解压安装、进程启动 |
| 权限 | `requireAdministrator` | `asInvoker` |
| 明确不做 | 本机安装/启动 | 枚举/写入 U 盘 |

---

## 三、UI 页面清单

**Installer（6 页）**
1. 欢迎页 — 说明用途与前提
2. 资源选择页 — Head 栏 + 官方三通道切换 + 资源卡片
3. U 盘选择与检测 — 盘符/卷标/文件系统/总容量/可用空间，可重新检测
4. 格式化/写入警告 — 红色警示 + **二次确认**（勾选 + 手动输入盘符）
5. 制作进度 — 进度条、速度、状态、可展开日志、取消、失败重试
6. 完成页 — 结果摘要

**Direct（5 页）**
1. 欢迎页
2. 资源选择页（与 Installer 共用同一组件）
3. 安装路径与启动选项 — 目录选择 + 是否安装后启动
4. 下载与安装进度 — 下载/解压进度、日志、取消、重试、错误对话框
5. 完成页 — 可打开安装目录

**资源选择页 Head 栏**：官方整合包 / 历史版本 / 社区开发版本 / 单个游戏；官方整合包下切换 **稳定版 / 预览版 / 开发版**。所有分类复用同一 `ResourceCard`（图标 48×48 等比居中、名称、版本、描述、大小、更新时间、标签、选择按钮）。

---

## 四、API 与数据层

- 接口：`GET https://jftw.pinkoc.top/api/jftwGo/`
- 超时 15s，最多重试 3 次（指数退避），带 `User-Agent`
- 本地缓存：`%LocalAppData%\jftwGo\cache\resources.json`；网络失败自动回退缓存并标注「离线缓存」
- JSON：Newtonsoft.Json，`MissingMemberHandling.Ignore`；缺失可选字段全部容错
- 归一化：过滤缺 `id/name/downloadUrl` 的非法项；相对图标 URL 按 `https://jftw.pinkoc.top` 解析为绝对 URL
- 状态：`Loading / Loaded / Empty / Failed`，UI 分别渲染加载中、空列表、失败可重试

**图标**：异步加载不阻塞列表；内存字典 + 磁盘两级缓存（文件名 = URL 的 SHA256 前 8 字节）；失败/空/超时显示内置占位图；切换分类复用缓存。

**下载器**：流式写 `.part` → 进度事件 → 取消 → 失败重试 → `checksum` 存在时校验（不匹配即失败并删除）→ 通过后才落到最终路径。

---

## 五、构建

### 方式一：Visual Studio
用 VS 2019/2022 打开 `jftwGo.sln`，选择 `Release`，生成解决方案。

### 方式二：命令行

```bat
cd jftwGo
dotnet build jftwGo.sln -c Release
```

或使用 MSBuild：

```bat
msbuild jftwGo.sln /p:Configuration=Release
```

### 输出

| 可执行文件 | 路径 |
|---|---|
| `jftw Go Installer.exe` | `JftwGo.Installer\bin\Release\` |
| `jftw Go Direct.exe` | `JftwGo.Direct\bin\Release\` |

每个目录内已包含运行所需：`JftwGo.Core.dll`、`JftwGo.UI.dll`、`Newtonsoft.Json.dll`、`*.exe.config`。分发时整个目录一起拷贝即可。

**运行要求**：Windows 7 SP1 及以上，安装 .NET Framework 4.8 运行时。

---

## 六、关键实现说明

1. **U 盘写入的真实边界**：`UsbWriter` 负责「清空目标盘 → 释放资源包到盘根 → 写 `jftwgo-media.txt` 标记」。可启动引导文件由**资源包自身**提供（包内含 boot 目录/引导文件时会一并释放到盘根），本工具不伪造引导扇区。
2. **危险操作防护**：写入前二次确认（勾选 + 输入盘符）；写入前用 `UsbScanner.IsStillRemovable` 复检盘符仍是可移动盘，避免误写固定磁盘。
3. **高 DPI**：`app.manifest` 声明 `PerMonitorV2` + `AutoScaleMode.Dpi`，布局使用 Dock/Anchor 与流式布局，避免缩放错位。
4. **不写程序目录**：缓存、日志、临时文件、下载均位于 `%LocalAppData%\jftwGo`，普通用户权限即可运行 Direct。

---

## 七、样例数据

`samples/resources.sample.json` 为契约样例，可用于本地对照字段或搭建 Mock 服务。

快速 Mock（把 `ApiOptions.Endpoint` 指向本地即可）：

```bat
python -m http.server 8000
```

---

## 八、已知限制

- 当前网络环境无法访问 `https://jftw.pinkoc.top`（TLS 握手失败），因此在线联调需在可访问该域名的网络中验证；客户端已内置重试与缓存回退。
- 未签名 exe 在部分安全软件（如 360）下首次运行会被拦截，需手动放行或做代码签名。
