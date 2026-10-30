"""生成创意工坊预览图（800x450 PNG，体积远小于 1MB）。"""
from PIL import Image, ImageDraw, ImageFont

W, H = 800, 450
BG_TOP = (22, 22, 30)
BG_BOTTOM = (46, 24, 24)
ACCENT = (198, 40, 40)
TEXT = (240, 236, 230)
MUTED = (168, 160, 152)

img = Image.new("RGB", (W, H), BG_TOP)
draw = ImageDraw.Draw(img)

# 竖向渐变背景
for y in range(H):
    t = y / (H - 1)
    color = tuple(int(BG_TOP[i] + (BG_BOTTOM[i] - BG_TOP[i]) * t) for i in range(3))
    draw.line([(0, y), (W, y)], fill=color)

# 左侧竖条与细边框
draw.rectangle([0, 0, 10, H], fill=ACCENT)
draw.rectangle([1, 1, W - 2, H - 2], outline=(70, 62, 58), width=1)

yahei = r"C:\Windows\Fonts\msyh.ttc"
yahei_bold = r"C:\Windows\Fonts\msyhbd.ttc"
try:
    f_title = ImageFont.truetype(yahei_bold, 66)
    f_sub = ImageFont.truetype(yahei, 26)
    f_item = ImageFont.truetype(yahei, 24)
except OSError:
    f_title = f_sub = f_item = ImageFont.load_default()

draw.text((56, 96), "三国杀无双音效", font=f_title, fill=TEXT)
draw.text((60, 182), "Slay the Spire 2 · 伤害达到阈值时播放音效", font=f_sub, fill=MUTED)

items = [
    ("我方造成伤害", "低档 / 高档：20 / 50"),
    ("敌方造成伤害", "低档 / 高档：15 / 30"),
]
y = 262
for name, detail in items:
    draw.ellipse([62, y + 8, 74, y + 20], fill=ACCENT)
    draw.text((90, y), name, font=f_item, fill=TEXT)
    draw.text((268, y), detail, font=f_item, fill=MUTED)
    y += 46

draw.text((60, 388), "阈值 / 音量 / 统计口径可在游戏内 Mod 设置页调整", font=f_sub, fill=(140, 134, 128))

out = r"D:\sls2_wswjqsvoice\workshop\image.png"
img.save(out, "PNG", optimize=True)
print(out)
