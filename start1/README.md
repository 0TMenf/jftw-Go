# start1.exe — 整合包游戏启动器

纯 **C / Win32** 实现的整合包启动器，不依赖 .NET Framework，目标机可直接双击运行。

## 功能

1. **启动即解除极域控制**：进程启动时枚举并结束极域电子教室学生端进程（`StudentMain.exe` 等），做法参考开源项目 [imengyu/JiYuTrainer](https://github.com/imengyu/JiYuTrainer) 的"强杀"功能——仅结束本机进程，不做远程注入、UDP 攻击教师端等激进操作。需要 `SeDebugPrivilege` 时会尝试自动提权（若极域进程受保护，请以管理员身份运行）。
2. **游戏选择 UI**：窗口内列出整合包中的全部游戏，双击条目或点"启动"按钮即可运行；自动设置工作目录，兼容依赖相对路径的游戏。

## 编译

无需 Visual Studio，用自带的 zig 工具链即可（自带 C 编译器与链接器）：

```
start1\build.bat
```

产物：`start1\start1.exe`。构建脚本会依次尝试 `tools\ziglang\ziglang\zig.exe` → `gcc` → `cl`。

若本机没有 zig 工具链，从 PyPI 下载 `ziglang` 的 win_amd64 wheel 解压到 `tools\ziglang\` 即可得到 `ziglang\zig.exe`（工具链体积约 400MB，已加入 .gitignore，不随仓库分发）。

手动编译：

```
zig cc -target x86_64-windows-gnu -O2 -municode start1.c -o start1.exe -lcomctl32 -lshell32 -luser32 -lgdi32
```

## 定制

### 增删游戏

编辑 `start1.c` 顶部的 `GAMES[]` 数组，每项四个字段：

```c
{ L"菜单显示名", L"相对路径\\程序.exe", L"命令行参数或NULL", L"工作目录相对路径或NULL" },
```

路径都相对于 `start1.exe` 所在目录。工作目录建议显式填写（很多游戏依赖相对路径读取资源）。

### 增删要结束的极域进程

编辑 `JIYU_PROCESSES[]` 数组，进程名用**小写**填写（比较时不区分大小写）。默认是 `studentmain.exe` 等常见名称。

## 部署

把编译好的 `start1.exe` 放进整合包根目录（与各游戏文件夹同级），双击运行即可。

> 注意：结束受保护进程可能需要管理员权限；若发现极域未被结束，右键 `start1.exe` → 以管理员身份运行。
