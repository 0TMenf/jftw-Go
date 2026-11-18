/*
 * start1.exe — 机房偷玩整合包 · 游戏启动器
 * ------------------------------------------------------------
 * 纯 Win32 C 实现，不依赖 .NET / 运行时，目标机可直接双击运行。
 *
 * 功能：
 *   1) 启动时结束极域电子教室学生端进程（参考 imengyu/JiYuTrainer 的做法：
 *      强杀 StudentMain.exe，结束本地进程以解除控制）。仅结束本机进程，
 *      不做远程注入/UDP 攻击教师端等激进操作。
 *   2) 显示游戏列表，双击或点"启动"按钮运行所选游戏，自动设置工作目录。
 *
 * 编译（需要任意 C 编译器，如 zig cc / mingw gcc / MSVC）：
 *   zig cc -target x86_64-windows-gnu -O2 -municode start1.c -o start1.exe -lcomctl32 -lshell32
 *   gcc -municode -O2 start1.c -o start1.exe -lcomctl32 -lshell32
 *
 * 说明：游戏清单写在下面的 GAMES[] 中，按实际整合包目录调整即可。
 */
#ifndef UNICODE
#define UNICODE
#endif
#ifndef _UNICODE
#define _UNICODE
#endif
#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0600
#endif

#include <windows.h>
#include <commctrl.h>
#include <shellapi.h>
#include <tlhelp32.h>   /* CreateToolhelp32Snapshot / Process32First/Next */
#include <tchar.h>
#include <stdio.h>

/* ---------------- 配置 ---------------- */

/* 极域相关进程名（小写比较）。StudentMain.exe 是主进程。
   其余为常见守护/辅助进程名，命中即结束。 */
static const wchar_t *JIYU_PROCESSES[] = {
    L"studentmain.exe",
    L"studentmain.exe ",     /* 容错：带空格的镜像名 */
    L"jyuclient.exe",
    L"student.exe",
    NULL
};

/* 游戏项：显示名、可执行文件（相对 start1.exe 目录）、命令行参数、是否用 ShellExecute 协议 */
typedef struct {
    const wchar_t *name;      /* 菜单显示名 */
    const wchar_t *exe;       /* 相对路径 */
    const wchar_t *args;      /* 命令行参数，可为 NULL */
    const wchar_t *workdir;   /* 工作目录（相对），NULL 表示取 exe 所在目录 */
} GameEntry;

static const GameEntry GAMES[] = {
    { L"Minecraft（HMCL 启动器）", L"Minecraft\\启动游戏双击我.bat", NULL, L"Minecraft" },
    { L"植物大战僵尸 PVZ",         L"PVZ\\PlantsVsZombies.exe",      NULL, L"PVZ" },
    { L"植物大战僵尸 · 杂交版",    L"PVZ杂交版\\植物大战僵尸杂交版发布版0.27.0.Csharp.exe", NULL, L"PVZ杂交版" },
    { L"穿越迷雾 Through The Fog", L"TTF穿越迷雾\\Through The Fog!.exe", NULL, L"TTF穿越迷雾" },
    { L"Undertale",               L"Undertale\\UNDERTALE.exe",     NULL, L"Undertale" },
    { L"PVZ 修改器",              L"傻子PVZ修改器\\PvZ_Toolkit_v1.22.0_ZH.exe", NULL, L"傻子PVZ修改器" },
    { L"图吧工具箱 2026",         L"图吧工具箱\\图吧工具箱2026.exe", NULL, L"图吧工具箱" },
    { L"米塔 MiSide",             L"米塔MiSide\\MiSideFull.exe",   NULL, L"米塔MiSide" },
};
#define GAME_COUNT (sizeof(GAMES) / sizeof(GAMES[0]))

/* ---------------- 全局 ---------------- */
#define ID_LIST   1001
#define ID_BTN    1002
#define ID_STATUS 1003

static HWND g_hList = NULL;
static HWND g_hStatus = NULL;
static HINSTANCE g_hInst = NULL;

/* ---------------- 工具函数 ---------------- */

/* 取 start1.exe 所在目录（不含末尾反斜杠则补上） */
static void GetAppDir(wchar_t *buf, DWORD cch)
{
    GetModuleFileNameW(NULL, buf, cch);
    wchar_t *p = wcsrchr(buf, L'\\');
    if (p) *(p + 1) = L'\0';
}

/* 拼接 目录 + 相对路径（用标准 swprintf，兼容 mingw/zig/MSVC） */
static void JoinPath(wchar_t *out, DWORD cch, const wchar_t *dir, const wchar_t *rel)
{
    if (!out || cch == 0) return;
    swprintf(out, cch, L"%s%s", dir, rel);
    out[cch - 1] = L'\0';
}

/* ---------------- 结束极域进程 ---------------- */

static BOOL IsJiYuProcess(const wchar_t *imageName)
{
    for (int i = 0; JIYU_PROCESSES[i]; ++i) {
        if (_wcsicmp(imageName, JIYU_PROCESSES[i]) == 0) return TRUE;
    }
    return FALSE;
}

/* 提升当前进程令牌权限（SeDebugPrivilege），否则可能无权结束受保护进程 */
static void EnableDebugPrivilege(void)
{
    HANDLE hToken = NULL;
    if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &hToken))
        return;

    TOKEN_PRIVILEGES tp;
    LUID luid;
    if (LookupPrivilegeValueW(NULL, SE_DEBUG_NAME, &luid)) {
        tp.PrivilegeCount = 1;
        tp.Privileges[0].Luid = luid;
        tp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
        AdjustTokenPrivileges(hToken, FALSE, &tp, sizeof(tp), NULL, NULL);
    }
    CloseHandle(hToken);
}

/* 结束所有匹配的极域进程，返回结束数量 */
static int KillJiYuProcesses(void)
{
    EnableDebugPrivilege();

    int killed = 0;
    HANDLE snap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (snap == INVALID_HANDLE_VALUE) return 0;

    PROCESSENTRY32W pe;
    pe.dwSize = sizeof(pe);
    if (Process32FirstW(snap, &pe)) {
        do {
            if (IsJiYuProcess(pe.szExeFile)) {
                HANDLE h = OpenProcess(PROCESS_TERMINATE, FALSE, pe.th32ProcessID);
                if (h) {
                    if (TerminateProcess(h, 0)) killed++;
                    CloseHandle(h);
                }
            }
        } while (Process32NextW(snap, &pe));
    }
    CloseHandle(snap);
    return killed;
}

/* ---------------- 启动游戏 ---------------- */

static void LaunchGame(int index)
{
    if (index < 0 || index >= (int)GAME_COUNT) return;

    wchar_t dir[MAX_PATH], exeFull[MAX_PATH], workFull[MAX_PATH];
    GetAppDir(dir, MAX_PATH);
    JoinPath(exeFull, MAX_PATH, dir, GAMES[index].exe);

    const wchar_t *work = GAMES[index].workdir;
    if (work) JoinPath(workFull, MAX_PATH, dir, work);
    else {
        wcsncpy(workFull, exeFull, MAX_PATH - 1);
        workFull[MAX_PATH - 1] = L'\0';
        wchar_t *p = wcsrchr(workFull, L'\\');
        if (p) *(p + 1) = L'\0';
    }

    /* 检查文件是否存在 */
    if (GetFileAttributesW(exeFull) == INVALID_FILE_ATTRIBUTES) {
        MessageBoxW(NULL,
            L"找不到该游戏的可执行文件。\n请检查 start1.exe 是否放在整合包根目录。",
            L"启动失败", MB_ICONWARNING | MB_OK);
        return;
    }

    SHELLEXECUTEINFOW sei;
    ZeroMemory(&sei, sizeof(sei));
    sei.cbSize = sizeof(sei);
    sei.lpVerb = L"open";
    sei.lpFile = exeFull;
    sei.lpParameters = GAMES[index].args;
    sei.lpDirectory = workFull;
    sei.nShow = SW_SHOWNORMAL;
    sei.fMask = SEE_MASK_NOCLOSEPROCESS;

    if (!ShellExecuteExW(&sei)) {
        DWORD err = GetLastError();
        wchar_t msg[512];
        swprintf(msg, 512, L"启动失败，错误码：%lu\n\n路径：%s", err, exeFull);
        MessageBoxW(NULL, msg, L"启动失败", MB_ICONERROR | MB_OK);
    }
}

/* ---------------- GUI ---------------- */

static LRESULT CALLBACK WndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    switch (msg) {
    case WM_CREATE: {
        INITCOMMONCONTROLSEX icc;
        icc.dwSize = sizeof(icc);
        icc.dwICC = ICC_LISTVIEW_CLASSES;
        InitCommonControlsEx(&icc);

        /* 游戏列表 */
        g_hList = CreateWindowExW(WS_EX_CLIENTEDGE, WC_LISTVIEWW, NULL,
            WS_CHILD | WS_VISIBLE | LVS_REPORT | LVS_SINGLESEL | LVS_SHOWSELALWAYS,
            16, 16, 520, 320, hwnd, (HMENU)ID_LIST, g_hInst, NULL);
        ListView_SetExtendedListViewStyle(g_hList, LVS_EX_FULLROWSELECT | LVS_EX_DOUBLEBUFFER);

        LVCOLUMNW col;
        ZeroMemory(&col, sizeof(col));
        col.mask = LVCF_TEXT | LVCF_WIDTH | LVCF_SUBITEM;
        col.pszText = (LPWSTR)L"选择要启动的游戏（双击或点下方按钮）";
        col.cx = 490;
        ListView_InsertColumn(g_hList, 0, &col);

        for (int i = 0; i < (int)GAME_COUNT; ++i) {
            LVITEMW item;
            ZeroMemory(&item, sizeof(item));
            item.mask = LVIF_TEXT | LVIF_PARAM;
            item.iItem = i;
            item.iSubItem = 0;
            item.pszText = (LPWSTR)GAMES[i].name;
            item.lParam = i;
            ListView_InsertItem(g_hList, &item);
        }
        if (GAME_COUNT > 0) ListView_SetItemState(g_hList, 0, LVIS_SELECTED, LVIS_SELECTED);

        /* 启动按钮 */
        CreateWindowExW(0, L"BUTTON", L"启 动",
            WS_CHILD | WS_VISIBLE | BS_PUSHBUTTON,
            16, 352, 120, 36, hwnd, (HMENU)ID_BTN, g_hInst, NULL);

        /* 状态栏 */
        g_hStatus = CreateWindowExW(0, STATUSCLASSNAMEW, NULL,
            WS_CHILD | WS_VISIBLE | SBARS_SIZEGRIP,
            0, 0, 0, 0, hwnd, (HMENU)ID_STATUS, g_hInst, NULL);
        SendMessageW(g_hStatus, SB_SETTEXTW, 0, (LPARAM)L"已就绪。双击游戏即可启动。");
        return 0;
    }

    case WM_SIZE: {
        int w = LOWORD(lParam), h = HIWORD(lParam);
        if (g_hList) MoveWindow(g_hList, 16, 16, w - 32, h - 88, TRUE);
        if (g_hStatus) SendMessageW(g_hStatus, WM_SIZE, 0, 0);
        HWND btn = GetDlgItem(hwnd, ID_BTN);
        if (btn) MoveWindow(btn, 16, h - 64, 120, 36, TRUE);
        return 0;
    }

    case WM_COMMAND:
        if (LOWORD(wParam) == ID_BTN) {
            int sel = ListView_GetNextItem(g_hList, -1, LVNI_SELECTED);
            if (sel >= 0) LaunchGame(sel);
            else MessageBoxW(hwnd, L"请先选择一个游戏。", L"提示", MB_ICONINFORMATION | MB_OK);
        }
        return 0;

    case WM_NOTIFY: {
        NMHDR *nm = (NMHDR *)lParam;
        if (nm->idFrom == ID_LIST && nm->code == NM_DBLCLK) {
            int sel = ListView_GetNextItem(g_hList, -1, LVNI_SELECTED);
            if (sel >= 0) LaunchGame(sel);
        }
        return 0;
    }

    case WM_DESTROY:
        PostQuitMessage(0);
        return 0;
    }
    return DefWindowProcW(hwnd, msg, wParam, lParam);
}

/* ---------------- 入口 ---------------- */

int WINAPI wWinMain(HINSTANCE hInst, HINSTANCE hPrev, LPWSTR lpCmdLine, int nCmdShow)
{
    (void)hPrev; (void)lpCmdLine;
    g_hInst = hInst;

    /* 1) 启动即结束极域进程，解除控制 */
    KillJiYuProcesses();

    /* 2) 建立 GUI */
    WNDCLASSEXW wc;
    ZeroMemory(&wc, sizeof(wc));
    wc.cbSize = sizeof(wc);
    wc.lpfnWndProc = WndProc;
    wc.hInstance = hInst;
    wc.hIcon = LoadIconW(NULL, IDI_APPLICATION);
    wc.hCursor = LoadCursorW(NULL, IDC_ARROW);
    wc.hbrBackground = (HBRUSH)(COLOR_WINDOW + 1);
    wc.lpszClassName = L"JftwGoStart1Class";
    RegisterClassExW(&wc);

    HWND hwnd = CreateWindowExW(0, L"JftwGoStart1Class", L"jftw Go · 游戏启动器",
        WS_OVERLAPPEDWINDOW | WS_VISIBLE,
        CW_USEDEFAULT, CW_USEDEFAULT, 560, 440,
        NULL, NULL, hInst, NULL);
    if (!hwnd) return 1;

    ShowWindow(hwnd, nCmdShow);
    UpdateWindow(hwnd);

    MSG msg;
    while (GetMessageW(&msg, NULL, 0, 0)) {
        TranslateMessage(&msg);
        DispatchMessageW(&msg);
    }
    return (int)msg.wParam;
}
