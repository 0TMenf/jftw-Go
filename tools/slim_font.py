# -*- coding: utf-8 -*-
"""把 Noto Sans SC 可变 TTF 固定为 wght=400 静态实例（保留全部字符，不子集化）。
   用途：可变字体在 GDI/PrivateFontCollection 下兼容性不确定，固定为静态更稳。
   用法：python tools/slim_font.py <可变字体.ttf>
         默认把结果写入 Installer/fonts 与 Direct/fonts。依赖 fonttools。"""
import os, sys
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

if len(sys.argv) < 2:
    print("用法: python tools/slim_font.py <NotoSansSC-VF.ttf>")
    sys.exit(1)

SRC = os.path.abspath(sys.argv[1])
if not os.path.exists(SRC):
    print("源文件不存在:", SRC)
    sys.exit(1)

font = TTFont(SRC)
font = instantiateVariableFont(font, {"wght": 400}, inplace=False)

for app in ("Installer", "Direct"):
    outdir = os.path.join(ROOT, app, "fonts")
    os.makedirs(outdir, exist_ok=True)
    out = os.path.join(outdir, "NotoSansSC-Regular.ttf")
    font.save(out)
    print("输出:", out, os.path.getsize(out), "bytes")
