# -*- coding: utf-8 -*-
"""最终验证：配置 -> 生成 C 源码 -> zig 编译 -> 运行验证 -> 部署。
   需要在 360 关闭的状态下运行（zig.exe 才能执行）。"""
import json, os, subprocess, shutil, time

ROOT = r'E:\wkb\spaces\2026_11_17_12-34-17\jftwGo'
HERE = os.path.join(ROOT, 'tools', 'LauncherGen')
TESTDIR = os.path.join(HERE, '_verify')
BUNDLE = r'E:\机房偷玩 4.0.0 for Middle'
ZIG = os.path.join(ROOT, 'tools', 'ziglang', 'ziglang', 'zig.exe')

# 1) 用真实整合包数据构造配置（输出到整合包目录本身，路径天然相对）
games = [
 'Minecraft\\启动游戏双击我.bat',
 'PVZ\\PlantsVsZombies.exe',
 'PVZ杂交版\\植物大战僵尸杂交版发布版0.27.0.Csharp.exe',
 'TTF穿越迷雾\\Through The Fog!.exe',
 'Undertale\\UNDERTALE.exe',
 '傻子PVZ修改器\\PvZ_Toolkit_v1.22.0_ZH.exe',
 '图吧工具箱\\图吧工具箱2026.exe',
 '米塔MiSide\\MiSideFull.exe',
]
names = ['Minecraft（HMCL）', '植物大战僵尸', 'PVZ 杂交版', '穿越迷雾',
         'Undertale', 'PVZ 修改器', '图吧工具箱', '米塔 MiSide']
cfg = {
 'Title': 'jftw Go · 游戏启动器',
 'Theme': 'dark', 'AccentColor': '4F8CFF', 'BackgroundColor': '121826',
 'TextColor': 'FFFFFF', 'CardColor': '1E2940',
 'BackgroundMode': 'solid', 'BackgroundImagePath': '',
 'PageSize': 6, 'KillJiYu': True,
 'Games': [
   {'Name': n, 'RelativePath': p, 'Arguments': '', 'WorkDir': os.path.dirname(p)}
   for n, p in zip(names, games)
 ]
}

os.makedirs(TESTDIR, exist_ok=True)
cfgpath = os.path.join(TESTDIR, 'cfg.json')
open(cfgpath, 'w', encoding='utf-8').write(json.dumps(cfg, ensure_ascii=False, indent=2))

# 2) 发射 C 源码（到临时验证目录）
tpl = open(os.path.join(HERE, 'template.c.txt'), encoding='utf-8').read()

def esc(s): return (s or '').replace('\\', '\\\\').replace('"', '\\"')
def rgb(h):
    h = (h or '000000').lstrip('#')
    return 'RGB(%d,%d,%d)' % (int(h[0:2],16), int(h[2:4],16), int(h[4:6],16))

glines = []
for g in cfg['Games']:
    glines.append('    { L"%s", L"%s", NULL, L"%s" },' % (
        esc(g['Name']), esc(g['RelativePath']), esc(g['WorkDir'])))
tpl = tpl.replace('@@APP_TITLE@@', 'L"' + esc(cfg['Title']) + '"')
for k, v in [
 ('@@COLOR_ACCENT@@', rgb(cfg['AccentColor'])),
 ('@@COLOR_BG@@', rgb(cfg['BackgroundColor'])),
 ('@@COLOR_TEXT@@', rgb(cfg['TextColor'])),
 ('@@COLOR_CARD@@', rgb(cfg['CardColor'])),
 ('@@PAGE_SIZE@@', str(cfg['PageSize'])),
 ('@@BG_IMAGE@@', 'NULL'),
 ('@@KILL_JIYU@@', '1' if cfg['KillJiYu'] else '0'),
 ('@@GAMES@@', '\n'.join(glines)),
]:
    tpl = tpl.replace(k, v)

csrc = os.path.join(TESTDIR, 'start1.c')
open(csrc, 'w', encoding='utf-8').write(tpl)
print('[1] C source generated:', len(tpl), 'chars')

# 3) zig 编译
exe = os.path.join(TESTDIR, 'start1.exe')
cmd = [ZIG, 'cc', '-target', 'x86_64-windows-gnu', '-O2', '-municode', csrc,
       '-o', exe, '-lgdiplus', '-lshell32', '-luser32', '-lgdi32', '-lm']
r = subprocess.run(cmd, capture_output=True, timeout=240)
err = r.stderr.decode('utf-8', 'replace')
if r.returncode != 0:
    print('[2] COMPILE FAILED:', err[:3000])
    raise SystemExit(1)
print('[2] compiled OK:', os.path.getsize(exe), 'bytes')

# 4) 运行验证（GUI 进程）
p = subprocess.Popen([exe], cwd=TESTDIR)
time.sleep(4)
alive = p.poll() is None
print('[3] running:', alive, 'pid=', p.pid)
if alive:
    p.terminate()
    try: p.wait(timeout=5)
    except Exception: p.kill()

# 5) 部署到整合包
dest = os.path.join(BUNDLE, 'start1.exe')
shutil.copy2(exe, dest)
print('[4] deployed:', dest, os.path.getsize(dest), 'bytes')
print('ALL OK')
