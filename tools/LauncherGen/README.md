# start1.exe 生成器（LauncherGen）

一个带**实时预览**的配置工具，用来生成整合包游戏启动器 `start1.exe`。

- **生成器本身**：WinForms（.NET Framework 4.8），只在你制作整合包时运行。
- **生成的 start1.exe**：**纯 C / Win32（GDI+）**，零 .NET 依赖，目标机房电脑可直接双击运行。

## 启动

双击 `bin\Release\LauncherGen.exe`。首次打开会加载示例配置（整合包里的 8 个游戏）。

## 界面说明

左侧是配置区，右侧是**实时预览**（和生成后的效果一致）：

| 配置项 | 说明 |
|---|---|
| 窗口标题 | 启动器窗口标题 |
| 颜色主题 | 深色 / 浅色 / 自定义；自定义可改主题色、背景、文字、卡片颜色（RRGGBB） |
| 背景 | 纯色（用主题里的背景色）或图片（选择 PNG/JPG，生成时自动复制到输出目录） |
| 每页游戏数 | 分页大小；游戏多时自动分多页，底部左右半区点击或方向键翻页 |
| 启动时结束极域进程 | 对应 JiYuTrainer 的强杀功能，结束 `StudentMain.exe` 等本机进程 |
| 游戏列表 | 点"添加文件"选择 exe/bat/sh（可多选）；双击条目改显示名/参数/工作目录；↑↓ 调整顺序 |

右侧预览下方可以翻页查看分页效果。

## 生成

点"生成 start1.exe"，选择输出目录（通常是整合包根目录），生成器会：
1. 把配置发射成 `start1.c`
2. 若选了背景图片，复制为 `launcher-bg.*`
3. 把游戏路径转成相对路径
4. 调用 `tools\ziglang\ziglang\zig.exe` 编译出 `start1.exe`

编译成功后，把 exe（和背景图片，若用了）放在整合包根目录即可。

## 命令行模式（可选）

```
LauncherGen.exe --generate <配置.json> <输出目录>
```

用于批量/无人值守生成。配置 JSON 格式见界面"保存配置"导出的 `launcher-config.json`。

## 目录结构

```
tools/LauncherGen/
├─ LauncherGen.exe        生成器（WinForms）
├─ template.c.txt         C 程序模板（嵌入资源）
├─ Model.cs               配置模型
├─ CodeEmitter.cs         配置 -> C 源码
├─ PreviewControl.cs      实时预览绘制
├─ MainForm.cs            主界面
├─ GameEditForm.cs        游戏编辑框
└─ build 输出 bin\Release\
tools/ziglang/ziglang/zig.exe   C 编译器（约 400MB，未入库）
```
