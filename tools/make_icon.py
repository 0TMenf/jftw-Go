# -*- coding: utf-8 -*-
"""把白底 jftw Go 图标处理成透明 PNG 与多尺寸 ICO。依赖 Pillow。

关键点：图标是「蓝色 U 盘 + 白色 GO 字 + 蓝色速度线」。
不能简单按颜色抠白，否则内部白字会一起消失。
做法：从四边 flood fill 找出连通到边界的「外部白底」→ 透明；
被蓝色包围的内部白字保留为白色。
用法：python tools/make_icon.py <输入图> <输出目录>
"""
import os, sys
from collections import deque
from PIL import Image

BRAND = (20, 120, 245)  # #1478F5 品牌蓝


def is_white(p):
    r, g, b = p[0], p[1], p[2]
    return r > 235 and g > 235 and b > 235


def process(src_path, outdir):
    im = Image.open(src_path).convert('RGBA')
    w, h = im.size
    px = im.load()

    # 1) 从四边 flood fill，标记连通到边界的白色（外部背景）
    outside = bytearray(w * h)
    q = deque()
    for x in range(w):
        for y in (0, h - 1):
            if is_white(px[x, y]) and not outside[y * w + x]:
                outside[y * w + x] = 1; q.append((x, y))
    for y in range(h):
        for x in (0, w - 1):
            if is_white(px[x, y]) and not outside[y * w + x]:
                outside[y * w + x] = 1; q.append((x, y))

    while q:
        x, y = q.popleft()
        for nx, ny in ((x-1,y), (x+1,y), (x,y-1), (x,y+1)):
            if 0 <= nx < w and 0 <= ny < h and not outside[ny*w+nx] and is_white(px[nx, ny]):
                outside[ny*w+nx] = 1
                q.append((nx, ny))
    print('outside pixels:', sum(outside))

    # 2) 生成 alpha：外部白 -> 0，主体 -> 255，边缘过渡 -> 介于两者
    #    边缘判定：非外部但靠近外部的像素，按与白色距离算 alpha
    bbox = [w, h, -1, -1]
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    op = out.load()
    for y in range(h):
        for x in range(w):
            r, g, b, _ = px[x, y]
            idx = y * w + x
            if outside[idx]:
                op[x, y] = (0, 0, 0, 0)
                continue

            near_outside = any(
                0 <= nx < w and 0 <= ny < h and outside[ny*w+nx]
                for nx, ny in ((x-1,y), (x+1,y), (x,y-1), (x,y+1))
            )
            dist = min(255 - r, 255 - g, 255 - b)

            if near_outside and not is_white((r, g, b)):
                # 抗锯齿边缘：按与白色距离给 alpha，颜色用品牌蓝（消除白边）
                alpha = max(0, min(255, int(255.0 * dist / 120.0)))
                if alpha < 200:
                    op[x, y] = (BRAND[0], BRAND[1], BRAND[2], alpha)
                else:
                    op[x, y] = (r, g, b, 255)
            else:
                # 主体内部：保留原色（蓝色主体 + 白色 GO 字都正确保留）
                op[x, y] = (r, g, b, 255)

            if op[x, y][3] > 24:
                bbox[0] = min(bbox[0], x); bbox[1] = min(bbox[1], y)
                bbox[2] = max(bbox[2], x); bbox[3] = max(bbox[3], y)

    print('content bbox:', bbox)
    pad = max(6, min(w, h) // 60)
    l = max(0, bbox[0] - pad); t = max(0, bbox[1] - pad)
    rr = min(w, bbox[2] + pad + 1); bb = min(h, bbox[3] + pad + 1)
    cropped = out.crop((l, t, rr, bb))
    print('cropped:', cropped.size)

    os.makedirs(outdir, exist_ok=True)
    png_path = os.path.join(outdir, 'jftw-go.png')
    cropped.save(png_path, 'PNG')

    # 3) 正方画布（ICO 用）：横向图标等比缩放居中
    side = max(cropped.size)
    canvas = Image.new('RGBA', (side, side), (0, 0, 0, 0))
    scale = 1.06
    dw = int(cropped.width * scale); dh = int(cropped.height * scale)
    dx = (side - dw) // 2; dy = (side - dh) // 2
    resized = cropped.resize((dw, dh), Image.LANCZOS)
    canvas.paste(resized, (dx, dy), resized)

    ico_path = os.path.join(outdir, 'jftw-go.ico')
    canvas.save(ico_path, format='ICO',
                sizes=[(16,16), (32,32), (48,48), (64,64), (128,128), (256,256)])
    print('saved:', png_path, os.path.getsize(png_path), 'bytes')
    print('saved:', ico_path, os.path.getsize(ico_path), 'bytes')


if __name__ == '__main__':
    if len(sys.argv) < 3:
        print('usage: python make_icon.py <input> <outdir>')
        sys.exit(1)
    process(sys.argv[1], sys.argv[2])
