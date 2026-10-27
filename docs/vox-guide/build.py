"""Генерує PDF-інструкцію з налаштування VOX для Audio Recorder (SDR#).

Ілюстрації — схематичні макети вікон плагіна, намальовані за реальними
координатами контролів із DialogConfigure.cs / AudioRecorderPanel.cs (v1.3.13.0).

Запуск:  python build.py   (потрібні reportlab і pillow; шрифти DejaVu)
Результат: VOX_Setup_Guide_UA.pdf поруч зі скриптом.
"""
import math
import os
import random

from PIL import Image, ImageDraw, ImageFont
from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (Image as RLImage, KeepTogether, PageBreak, Paragraph,
                                SimpleDocTemplate, Spacer, Table, TableStyle)

HERE = os.path.dirname(os.path.abspath(__file__))
IMG = os.path.join(HERE, "img")
OUT = os.path.join(HERE, "VOX_Setup_Guide_UA.pdf")
FONT_DIR = "/usr/share/fonts/truetype/dejavu"

S = 2  # масштаб макетів відносно пікселів WinForms

RED = (220, 38, 38)
GREEN = (22, 140, 60)
ORANGE = (230, 120, 0)
TEXT = (0, 0, 0)
GRAY = (160, 160, 160)


def font(size, bold=False, cond=True):
    name = "DejaVuSans"
    if cond:
        name += "Condensed"
    if bold:
        name += "-Bold"
    return ImageFont.truetype(os.path.join(FONT_DIR, name + ".ttf"), int(size * S))


UI = font(10.5)
UI_B = font(10.5, bold=True)


# ---------------------------------------------------------------------------
# Примітиви WinForms (координати — у пікселях форми, множаться на S)
# ---------------------------------------------------------------------------
class Canvas:
    def __init__(self, w, h, pad_l=40, pad_t=10, pad_r=45, pad_b=10, bg=(255, 255, 255)):
        self.ox, self.oy = pad_l, pad_t
        self.img = Image.new("RGB", ((w + pad_l + pad_r) * S, (h + pad_t + pad_b) * S), bg)
        self.d = ImageDraw.Draw(self.img)

    def p(self, x, y):
        return ((x + self.ox) * S, (y + self.oy) * S)

    def rect(self, x, y, w, h, fill=None, outline=None, width=1):
        self.d.rectangle([self.p(x, y), self.p(x + w - 1, y + h - 1)], fill=fill, outline=outline,
                         width=width * S if outline else 0)

    def text(self, x, y, s, fnt=UI, fill=TEXT, max_w=None, anchor="la"):
        f = fnt
        if max_w:
            size = f.size
            while self.d.textlength(s, font=f) > max_w * S and size > 12:
                size -= 1
                f = ImageFont.truetype(f.path, size)
        self.d.text(self.p(x, y), s, font=f, fill=fill, anchor=anchor)

    def checkbox(self, x, y, w, label, checked=False, enabled=True):
        col = TEXT if enabled else GRAY
        self.rect(x, y + 2, 13, 13, fill=(255, 255, 255) if enabled else (245, 245, 245),
                  outline=(51, 51, 51) if enabled else (188, 188, 188))
        if checked:
            pts = [self.p(x + 3, y + 8), self.p(x + 5.5, y + 11), self.p(x + 10, y + 5)]
            self.d.line(pts, fill=col, width=2 * S, joint="curve")
        self.text(x + 17, y + 1, label, fill=col, max_w=w - 17)

    def numeric(self, x, y, w, value, enabled=True):
        self.rect(x, y, w, 20, fill=(255, 255, 255) if enabled else (240, 240, 240),
                  outline=(122, 122, 122) if enabled else (204, 204, 204))
        self.text(x + 3, y + 3, str(value), fill=TEXT if enabled else GRAY)
        bx = x + w - 17
        self.rect(bx, y + 1, 16, 18, fill=(240, 240, 240))
        c = (90, 90, 90) if enabled else GRAY
        self.d.polygon([self.p(bx + 5, y + 8), self.p(bx + 11, y + 8), self.p(bx + 8, y + 5)], fill=c)
        self.d.polygon([self.p(bx + 5, y + 12), self.p(bx + 11, y + 12), self.p(bx + 8, y + 15)], fill=c)

    def combo(self, x, y, w, value):
        self.rect(x, y, w, 21, fill=(255, 255, 255), outline=(122, 122, 122))
        self.text(x + 4, y + 3, value)
        bx = x + w - 17
        self.d.polygon([self.p(bx + 4, y + 9), self.p(bx + 12, y + 9), self.p(bx + 8, y + 13)], fill=(60, 60, 60))

    def textbox(self, x, y, w, value):
        self.rect(x, y, w, 20, fill=(255, 255, 255), outline=(122, 122, 122))
        self.text(x + 3, y + 3, value, max_w=w - 6)

    def button(self, x, y, w, h, label):
        self.rect(x, y, w, h, fill=(225, 225, 225), outline=(173, 173, 173))
        self.text(x + w / 2, y + h / 2, label, anchor="mm")

    def callout(self, n, x, y, w, h, bx, by, color=RED):
        """Червона рамка навколо контролу + номерний кружок у точці (bx, by)."""
        pad = 3
        x0, y0, x1, y1 = x - pad, y - pad, x + w + pad, y + h + pad
        cx = min(max(bx, x0), x1)
        cy = min(max(by, y0), y1)
        self.d.line([self.p(bx, by), self.p(cx, cy)], fill=color, width=2 * S)
        self.d.rounded_rectangle([self.p(x0, y0), self.p(x1, y1)], radius=3 * S, outline=color, width=2 * S)
        r = 11
        self.d.ellipse([self.p(bx - r, by - r), self.p(bx + r, by + r)], fill=color)
        self.text(bx, by + 0.5, str(n), fnt=font(12, bold=True, cond=False), fill=(255, 255, 255), anchor="mm")

    def save(self, name):
        os.makedirs(IMG, exist_ok=True)
        path = os.path.join(IMG, name)
        self.img.save(path)
        return path


def window_frame(c, w, h, title):
    """Рамка вікна Windows із заголовком; повертає зсув клієнтської області."""
    c.rect(0, 0, w, h + 31, fill=(240, 240, 240), outline=(120, 120, 120))
    c.rect(1, 1, w - 2, 30, fill=(255, 255, 255))
    c.text(10, 8, title)
    c.text(w - 22, 16, "✕", anchor="mm")
    return 31


def dialog_tabs(c, top, selected):
    """TabControl (12,3) 478x247 з двома вкладками; повертає (x, y) TabPage."""
    tx, ty = 12, top + 3
    page_x, page_y = tx + 4, ty + 22
    c.rect(tx + 2, ty + 20, 474, 225, fill=(255, 255, 255), outline=(172, 172, 172))
    tabs = [("File options", 78), ("Recorder options", 104)]
    x = tx + 2
    for i, (name, w) in enumerate(tabs):
        sel = i == selected
        y0 = ty + (0 if sel else 2)
        c.rect(x, y0, w, 21 if sel else 19, fill=(255, 255, 255) if sel else (240, 240, 240),
               outline=(172, 172, 172))
        if sel:
            c.rect(x + 1, ty + 19, w - 2, 3, fill=(255, 255, 255))
        c.text(x + w / 2, y0 + 10, name, anchor="mm")
        x += w
    return page_x, page_y, tabs


# ---------------------------------------------------------------------------
# Макет 1: Configure → Recorder options
# ---------------------------------------------------------------------------
def img_recorder_tab():
    W, H = 501, 289
    c = Canvas(W, H + 31, pad_l=42, pad_r=46)
    top = window_frame(c, W, H, "Configure audio recorder")
    px, py, _ = dialog_tabs(c, top, selected=1)

    def P(x, y):
        return px + x, py + y

    c.checkbox(*P(6, 6), 118, "Auto-start recording")
    c.checkbox(*P(6, 29), 148, "Write all activity in one file")
    c.checkbox(*P(6, 52), 108, "Don't write pause", checked=True)
    c.text(*P(26, 77), "Use")
    c.checkbox(*P(59, 75), 63, "squelch", enabled=False)
    c.text(*P(130, 77), "and")
    c.checkbox(*P(163, 75), 49, "mute", enabled=False)
    c.text(*P(217, 77), "to trigger recording")
    c.checkbox(*P(6, 98), 293, "Continue recording after the squelch has been closed for", checked=True)
    c.numeric(*P(304, 98), 57, 2)
    c.text(*P(366, 101), "seconds [0 - 100]")
    c.checkbox(*P(6, 123), 201, "Waiting time to create a new file after", checked=True)
    c.numeric(*P(212, 123), 63, 5)
    c.text(*P(280, 128), "seconds [0 - 100]")
    c.checkbox(*P(6, 146), 236, "Create a new file if the frequency is changed", checked=True)
    c.checkbox(*P(6, 169), 200, "VOX — audio level (SSB/CW/AM)", checked=True)
    c.text(*P(213, 172), "open ≥ dB")
    c.numeric(*P(272, 167), 48, -32)
    c.text(*P(326, 172), "close < dB")
    c.numeric(*P(388, 167), 48, -38)

    c.text(31, top + 261, "Audio Recorder v1.3.13.0")
    c.button(434, top + 255, 56, 23, "OK")

    L = -22  # колонка номерків ліворуч від вікна
    c.callout(1, 14 + 78, top + 3, 104, 21, 14 + 78 + 104 + 24, top + 13)
    c.callout(2, *P(6, 52), 110, 17, L, py + 61)
    c.callout(3, *P(6, 169), 202, 17, L, py + 178)
    c.callout(4, *P(211, 166), 228, 22, W + 22, py + 176)
    c.callout(5, *P(6, 97), 452, 22, L, py + 108)
    c.callout(6, *P(6, 122), 366, 22, L, py + 133)
    c.callout(7, 434, top + 255, 56, 23, W + 22, top + 266)
    return c.save("01_recorder_options.png")


# ---------------------------------------------------------------------------
# Макет 2: Configure → File options
# ---------------------------------------------------------------------------
def img_file_tab():
    W, H = 501, 289
    c = Canvas(W, H + 31, pad_l=42, pad_r=46)
    top = window_frame(c, W, H, "Configure audio recorder")
    px, py, _ = dialog_tabs(c, top, selected=0)

    def P(x, y):
        return px + x, py + y

    c.text(*P(4, 9), "Sample Format")
    c.combo(*P(103, 6), 175, "16 Bit PCM Mono")
    c.text(*P(4, 36), "Samplerate")
    c.combo(*P(103, 33), 175, "no re-sampling")
    c.checkbox(*P(9, 61), 196, "Create a new file if the file size > MB")
    c.numeric(*P(221, 60), 57, 100)
    c.text(*P(284, 65), "[3 - 2048]")
    c.checkbox(*P(9, 87), 183, "Delete file if the file size < second", checked=True)
    c.numeric(*P(221, 86), 57, 3)
    c.text(*P(284, 91), "[0 - 3600]")
    c.checkbox(*P(315, 126), 120, "Use UTC timestamp")
    c.text(*P(6, 130), "Rules for creating file names.")
    c.text(*P(6, 149), "You can use: date, time, start_time, end_time, length, name, group, frequency, \"any text\", +, \\, /",
           max_w=459)
    c.textbox(*P(9, 168), 458, "/ date / name + \"_\" + frequency / time + \"_\" + name + \"_\" + frequency")
    c.text(*P(6, 198), "\\2026_10_27\\Channel_145.500 MHz\\12-30-05_Channel_145.500 MHz")
    c.text(31, top + 261, "Audio Recorder v1.3.13.0")
    c.button(434, top + 255, 56, 23, "OK")

    c.callout(1, 14, top + 3, 78, 21, -22, top + 14)
    c.callout(2, *P(9, 86), 327, 21, -22, py + 96)
    return c.save("02_file_options.png")


# ---------------------------------------------------------------------------
# Макет 3: панель плагіна в SDR# (розширена бокова панель)
# ---------------------------------------------------------------------------
def img_panel(name, state, callouts=True):
    base_w, W, H = 217, 380, 172
    dx = W - base_w  # контроли з Anchor=Right зсуваються, з Left|Right — розтягуються
    c = Canvas(W, H, pad_l=36, pad_r=40, pad_t=12, pad_b=12)
    c.rect(0, 0, W, H, fill=(240, 240, 240), outline=(150, 150, 150))
    c.text(6, 9, "Write: current 00:00:{:02d} - {} MB".format(state["sec"], state["mb"]))
    c.text(6, 29, "all {} file(s) 00:01:12 - 1.104 MB".format(state["files"]))
    c.text(6, 48, "Dropped buffers: 0")
    c.button(12, 67, 93, 23, "Folder select")
    c.rect(111, 73, 93 + dx, 12, fill=(230, 230, 230), outline=(188, 188, 188))
    if state["progress"] > 0:
        c.rect(112, 74, int((93 + dx) * state["progress"]), 10, fill=(6, 176, 37))
    c.button(12, 96, 93, 23, "Open folder")
    c.text(111 + dx, 101, state["label"], fill=state["label_color"])
    c.button(12, 125, 93, 23, "Configure")
    c.button(111 + dx, 125, 93, 23, "Stop")
    dbg = "REC SR: 48000 - REC level dB: {0} - VOX dB: {0} [{1}]".format(state["db"], state["gate"])
    c.text(33, 152, dbg)
    if callouts:
        c.callout(1, 12, 125, 93, 23, -18, 136)
        c.callout(2, 111 + dx, 125, 93, 23, W + 20, 136)
        c.callout(3, 111 + dx, 99, c.d.textlength(state["label"], font=UI) / S + 2, 17, W + 20, 108)
        tw = c.d.textlength(dbg, font=UI) / S
        c.callout(4, 33, 151, tw + 2, 15, -18, 159)
    return c.save(name)


# ---------------------------------------------------------------------------
# Графіки рівня: правильні / неправильні пороги
# ---------------------------------------------------------------------------
def level_series(noise_db, noise_spread, seed):
    rnd = random.Random(seed)
    t = [i * 0.1 for i in range(300)]
    v = []
    for x in t:
        n = noise_db + rnd.uniform(-noise_spread, noise_spread * 0.6)
        if rnd.random() < 0.03:
            n += rnd.uniform(2, 4)  # поодинокі шумові піки
        if 5 <= x < 11 or 18 <= x < 22.5:
            n = -16 + 4 * math.sin(x * 3.1) + rnd.uniform(-3, 3)
            if rnd.random() < 0.12:
                n -= 14  # паузи між словами
        v.append(n)
    return t, v


def gate(values, open_db, close_db, tail_s=2.0, dt=0.1):
    st, out, hold = False, [], 0.0
    for v in values:
        if st:
            if v < close_db:
                st = False
        elif v >= open_db:
            st = True
        if st:
            hold = tail_s
        elif hold > 0:
            hold -= dt
        out.append(st or hold > 0)
    return out


def dashed(d, p0, p1, color, width, dash=14, gap=9):
    (x0, y0), (x1, y1) = p0, p1
    length = math.hypot(x1 - x0, y1 - y0)
    n = 0.0
    while n < length:
        a, b = n / length, min(n + dash, length) / length
        d.line([(x0 + (x1 - x0) * a, y0 + (y1 - y0) * a), (x0 + (x1 - x0) * b, y0 + (y1 - y0) * b)],
               fill=color, width=width)
        n += dash + gap


def img_chart(name, title, noise_db, spread, open_db, close_db, ok):
    Wpx, Hpx = 1500, 640
    img = Image.new("RGB", (Wpx, Hpx), (255, 255, 255))
    d = ImageDraw.Draw(img)
    f = ImageFont.truetype(os.path.join(FONT_DIR, "DejaVuSans.ttf"), 22)
    fb = ImageFont.truetype(os.path.join(FONT_DIR, "DejaVuSans-Bold.ttf"), 28)
    L, R, T, B = 110, Wpx - 210, 70, 470
    lo, hi = -60, 0

    def Y(db):
        return B - (db - lo) / (hi - lo) * (B - T)

    def X(t):
        return L + t / 30 * (R - L)

    d.text((L, 18), title, font=fb, fill=GREEN if ok else RED)
    for db in range(lo, hi + 1, 10):
        d.line([(L, Y(db)), (R, Y(db))], fill=(232, 232, 232), width=2)
        d.text((L - 12, Y(db)), str(db), font=f, fill=(90, 90, 90), anchor="rm")
    d.text((22, (T + B) / 2), "dB", font=f, fill=(90, 90, 90), anchor="lm")

    t, v = level_series(noise_db, spread, seed=7 if ok else 3)
    # смуга шуму
    d.rectangle([L, Y(noise_db + spread * 0.6 + 4), R, Y(noise_db - spread)], fill=(238, 238, 238))
    d.text((L + 8, Y(noise_db - spread) + 4), "шум", font=f, fill=(110, 110, 110), anchor="lt")
    pts = [(X(a), Y(max(lo, min(hi, b)))) for a, b in zip(t, v)]
    d.line(pts, fill=(40, 90, 200), width=3)

    dashed(d, (L, Y(open_db)), (R, Y(open_db)), RED, 4)
    d.text((R + 12, Y(open_db) - 4), f"open {open_db}", font=f, fill=RED, anchor="lb")
    dashed(d, (L, Y(close_db)), (R, Y(close_db)), ORANGE, 4)
    d.text((R + 12, Y(close_db) + 4), f"close {close_db}", font=f, fill=ORANGE, anchor="lt")

    # смуга «що записалось»
    g = gate(v, open_db, close_db)
    gy0, gy1 = 505, 545
    d.text((L - 12, (gy0 + gy1) / 2), "запис", font=f, fill=(60, 60, 60), anchor="rm")
    d.rectangle([L, gy0, R, gy1], fill=(245, 245, 245), outline=(200, 200, 200))
    for i, on in enumerate(g):
        if on:
            d.rectangle([X(t[i]), gy0, X(t[i]) + (R - L) / 300 + 1, gy1], fill=RED)
    for s in range(0, 31, 5):
        d.text((X(s), gy1 + 10), f"{s} с", font=f, fill=(90, 90, 90), anchor="mt")
    d.text((L, 600), "синя лінія — рівень VOX dB;   червоне — що потрапить у файл", font=f, fill=(90, 90, 90))
    path = os.path.join(IMG, name)
    img.save(path)
    return path


# ---------------------------------------------------------------------------
# PDF
# ---------------------------------------------------------------------------
def build_pdf(imgs):
    pdfmetrics.registerFont(TTFont("DV", os.path.join(FONT_DIR, "DejaVuSans.ttf")))
    pdfmetrics.registerFont(TTFont("DVB", os.path.join(FONT_DIR, "DejaVuSans-Bold.ttf")))
    pdfmetrics.registerFont(TTFont("DVM", os.path.join(FONT_DIR, "DejaVuSansMono.ttf")))
    from reportlab.pdfbase.pdfmetrics import registerFontFamily
    registerFontFamily("DV", normal="DV", bold="DVB", italic="DV", boldItalic="DVB")

    base = ParagraphStyle("b", fontName="DV", fontSize=10.5, leading=14.5)
    small = ParagraphStyle("s", parent=base, fontSize=8.5, leading=11, textColor=colors.HexColor("#555555"))
    h1 = ParagraphStyle("h1", parent=base, fontName="DVB", fontSize=22, leading=27, spaceAfter=4)
    sub = ParagraphStyle("sub", parent=base, fontSize=12, leading=16, textColor=colors.HexColor("#444444"))
    h2 = ParagraphStyle("h2", parent=base, fontName="DVB", fontSize=15, leading=19, spaceBefore=6, spaceAfter=6,
                        textColor=colors.HexColor("#b91c1c"))
    big = ParagraphStyle("big", parent=base, fontSize=12, leading=17)
    cell = ParagraphStyle("c", parent=base, fontSize=9.5, leading=12.5)
    cellb = ParagraphStyle("cb", parent=cell, fontName="DVB")
    cap = ParagraphStyle("cap", parent=small, alignment=TA_CENTER)

    page_w = A4[0] - 30 * mm

    def pic(path, width=page_w):
        im = Image.open(path)
        return RLImage(path, width=width, height=width * im.height / im.width)

    def legend(rows):
        """Таблиця «номер на картинці → що зробити»."""
        data = []
        for n, txt in rows:
            badge = Paragraph(f'<font color="white"><b>{n}</b></font>',
                              ParagraphStyle("n", parent=cell, alignment=TA_CENTER))
            data.append([badge, Paragraph(txt, cell)])
        t = Table(data, colWidths=[9 * mm, page_w - 9 * mm])
        st = [("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
              ("BACKGROUND", (0, 0), (0, -1), colors.HexColor("#dc2626")),
              ("LINEBELOW", (1, 0), (1, -1), 0.4, colors.HexColor("#dddddd")),
              ("TOPPADDING", (0, 0), (-1, -1), 4), ("BOTTOMPADDING", (0, 0), (-1, -1), 4)]
        t.setStyle(TableStyle(st))
        return t

    def box(flow, color="#fef3c7", border="#f59e0b"):
        t = Table([[flow]], colWidths=[page_w])
        t.setStyle(TableStyle([("BACKGROUND", (0, 0), (-1, -1), colors.HexColor(color)),
                               ("BOX", (0, 0), (-1, -1), 1, colors.HexColor(border)),
                               ("LEFTPADDING", (0, 0), (-1, -1), 10), ("RIGHTPADDING", (0, 0), (-1, -1), 10),
                               ("TOPPADDING", (0, 0), (-1, -1), 8), ("BOTTOMPADDING", (0, 0), (-1, -1), 8)]))
        return t

    def footer(cv, doc):
        cv.saveState()
        cv.setFont("DV", 7.5)
        cv.setFillColor(colors.HexColor("#888888"))
        cv.drawString(15 * mm, 10 * mm, "Audio Recorder v1.3.13.0 — режим VOX. Ілюстрації — схематичні макети вікон плагіна.")
        cv.drawRightString(A4[0] - 15 * mm, 10 * mm, str(doc.page))
        cv.restoreState()

    s = []
    # --- сторінка 1: суть ---
    s += [Paragraph("Запис без шуму: налаштування VOX", h1),
          Paragraph("Audio Recorder для SDR# · версія 1.3.13.0 · для операторів", sub),
          Spacer(1, 8)]
    s.append(box(Paragraph(
        "<b>Чому пишеться шум?</b> VOX вмикає запис, коли звук голосніший за поріг <b>open</b>. "
        "Якщо поріг нижчий за рівень шуму, запис не вимикається ніколи. "
        "<b>Рішення:</b> подивитися, який рівень має шум, і поставити пороги <b>вище</b> за нього.", big)))
    s.append(Spacer(1, 10))
    s.append(Paragraph("Коротко — 5 кроків", h2))
    steps = [
        ("1", "<b>Configure → Recorder options</b>: увімкнути <b>Don't write pause</b> і <b>VOX</b>."),
        ("2", "Натиснути <b>Record</b>, двічі клацнути по написі стану, щоб побачити <b>VOX dB</b>."),
        ("3", "На порожній частоті запам'ятати найвищий рівень шуму — це <b>N</b>."),
        ("4", "Поставити <b>close = N + 4</b>, <b>open = N + 10</b> (dB)."),
        ("5", "Хвіст <b>2 с</b>, видаляти файли <b>&lt; 3 с</b>. У SDR# — AGC повільний, вузький фільтр."),
    ]
    s.append(legend(steps))
    s.append(Spacer(1, 10))
    s.append(pic(imgs["good"], page_w * 0.92))
    s.append(Paragraph("Правильно: шум (сіра смуга) нижче обох порогів, у файл потрапляють лише передачі.", cap))
    s.append(Spacer(1, 6))
    s.append(pic(imgs["bad"], page_w * 0.92))
    s.append(Paragraph("Неправильно: пороги за замовчуванням (−30/−40) нижчі за шум — запис іде безперервно.", cap))
    s.append(PageBreak())

    # --- крок 1 ---
    s.append(Paragraph("Крок 1. Увімкнути VOX", h2))
    s.append(Paragraph("На панелі плагіна натисніть <b>Configure</b> і відкрийте вкладку <b>Recorder options</b>.", base))
    s.append(Spacer(1, 6))
    s.append(pic(imgs["rec"]))
    s.append(Spacer(1, 6))
    s.append(legend([
        ("1", "Вкладка <b>Recorder options</b>."),
        ("2", "☑ <b>Don't write pause</b> — без цієї галочки VOX не працює (пишеться все підряд)."),
        ("3", "☑ <b>VOX — audio level (SSB/CW/AM)</b>. Галочки <i>squelch</i> і <i>mute</i> стануть сірими — так і має бути."),
        ("4", "Пороги <b>open ≥ dB</b> і <b>close &lt; dB</b>. Цифри на картинці — лише приклад, свої виставите на кроці 3."),
        ("5", "☑ <b>Continue recording … for</b> = <b>1–3 с</b>. Хвіст, щоб не різало паузи між словами. "
              "Працює і для VOX, хоча в назві написано «squelch». Не ставте 10+ с — буде довгий шумовий хвіст."),
        ("6", "☑ <b>Waiting time to create a new file after</b> = <b>5–10 с</b> — кожна передача в окремому файлі."),
        ("7", "<b>OK</b> — зберегти."),
    ]))
    s.append(PageBreak())

    # --- крок 2 ---
    s.append(Paragraph("Крок 2. Побачити рівень VOX dB", h2))
    s.append(pic(imgs["panel_closed"], page_w * 0.85))
    s.append(Spacer(1, 6))
    s.append(legend([
        ("1", "<b>Configure</b> — вікно налаштувань (крок 1)."),
        ("2", "<b>Record</b> — почати чергування (кнопка стане <b>Stop</b>)."),
        ("3", "<b>Двічі клацніть по цьому напису</b> (Pause / Record / Create new file) — з'явиться рядок налагодження. "
              "Повторний подвійний клік ховає його."),
        ("4", "Рядок з <b>VOX dB</b> і станом <b>[closed]</b> / <b>[open]</b>. Число оновлюється кілька разів на секунду."),
    ]))
    s.append(Spacer(1, 6))
    s.append(box(Paragraph(
        "<b>Не видно «VOX dB»?</b> Рядок довгий і обрізається на вузькій панелі. "
        "<b>Розширте бокову панель SDR#</b> (потягніть її край мишею).", base)))
    s.append(Spacer(1, 8))
    s.append(Paragraph("Так виглядає момент передачі — гейт відкритий, іде запис:", base))
    s.append(Spacer(1, 4))
    s.append(pic(imgs["panel_open"], page_w * 0.85))
    s.append(Spacer(1, 4))
    s.append(Paragraph("Після кінця передачі напис стає зеленим «Record 1.6» — це відлік хвоста (крок 1, пункт 5), "
                       "потім — «Pause».", base))
    s.append(PageBreak())

    # --- крок 3 ---
    s.append(Paragraph("Крок 3. Заміряти шум і виставити пороги", h2))
    s.append(legend([
        ("а", "Станьте на <b>порожню частоту</b> (або дочекайтесь паузи в ефірі), натисніть <b>Record</b>."),
        ("б", "<b>1–2 хвилини</b> дивіться на <b>VOX dB</b>. Запам'ятайте <b>найбільше</b> число — це <b>N</b> "
              "(наприклад, −42). Пам'ятайте: −40 більше, ніж −42."),
        ("в", "Дочекайтесь передачі й подивіться рівень голосу — <b>S</b> (наприклад, −15)."),
        ("г", "Відкрийте <b>Configure</b> і впишіть пороги за таблицею нижче."),
    ]))
    s.append(Spacer(1, 8))
    tbl = Table([
        [Paragraph("Поле", cellb), Paragraph("Формула", cellb), Paragraph("Приклад (N = −42, S = −15)", cellb)],
        [Paragraph("close &lt; dB", cell), Paragraph("N + 3…5", cell), Paragraph("<b>−38</b>", cell)],
        [Paragraph("open ≥ dB", cell), Paragraph("N + 8…12, але нижче за S", cell), Paragraph("<b>−32</b>", cell)],
    ], colWidths=[page_w * 0.25, page_w * 0.4, page_w * 0.35])
    tbl.setStyle(TableStyle([("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#f3f4f6")),
                             ("GRID", (0, 0), (-1, -1), 0.5, colors.HexColor("#cccccc")),
                             ("TOPPADDING", (0, 0), (-1, -1), 5), ("BOTTOMPADDING", (0, 0), (-1, -1), 5)]))
    s.append(tbl)
    s.append(Spacer(1, 8))
    s.append(box(Paragraph(
        "<b>Перевірка:</b> на порожній частоті має бути <b>[closed]</b> і напис <b>Pause</b>. "
        "Якщо гейт сам відкривається — підніміть обидва пороги ще на 3 dB.<br/>"
        "<b>Правило:</b> open завжди більший за close (наприклад, −32 &gt; −38).", base),
        color="#ecfdf5", border="#10b981"))
    s.append(Spacer(1, 8))
    s.append(pic(imgs["good"], page_w * 0.9))
    s.append(Paragraph("Значення N залежить від частоти, антени й налаштувань SDR#. Змінили щось — заміряйте знову.", cap))
    s.append(PageBreak())

    # --- крок 4 ---
    s.append(Paragraph("Крок 4. Автоматично видаляти «клацання»", h2))
    s.append(Paragraph("Configure → вкладка <b>File options</b>:", base))
    s.append(Spacer(1, 6))
    s.append(pic(imgs["file"]))
    s.append(Spacer(1, 6))
    s.append(legend([
        ("1", "Вкладка <b>File options</b>."),
        ("2", "☑ <b>Delete file if the file size &lt; second</b> = <b>2–3</b>. "
              "Файли від коротких шумових сплесків видалятимуться самі."),
    ]))
    s.append(Spacer(1, 12))

    # --- крок 5 ---
    s.append(Paragraph("Крок 5. Налаштування самого SDR#", h2))
    s.append(Paragraph("Найчастіше шум «накачує» сам приймач. Перевірте:", base))
    s.append(Spacer(1, 4))
    s.append(legend([
        ("1", "<b>AGC</b> — увімкнений, але <b>повільний</b> (Slow / з Hang). Швидкий AGC у паузі "
              "піднімає шум майже до рівня голосу — і VOX його пише."),
        ("2", "<b>Ширина фільтра</b> — не ширше потрібного: SSB ≈ 2,4–2,7 кГц, CW ≈ 300–500 Гц. "
              "Вужче = менше шуму."),
        ("3", "<b>RF / LNA gain</b> — не на максимумі, якщо від цього росте лише шум."),
    ]))
    s.append(Spacer(1, 6))
    s.append(box(Paragraph("Змінили AGC, фільтр або підсилення → <b>повторіть крок 3</b> (рівень шуму зміниться).", base)))
    s.append(PageBreak())

    # --- діагностика ---
    s.append(Paragraph("Що робити, якщо…", h2))
    rows = [
        ("Пише безперервно, весь час [open]", "Шум вище порогу close. Підніміть <b>close</b> і <b>open</b> на 5–10 dB."),
        ("Багато коротких файлів із шумом", "Підніміть <b>open</b>; увімкніть видалення файлів &lt; 3 с (крок 4)."),
        ("Обрізає початок фраз", "Опустіть <b>open</b> на 2–3 dB."),
        ("Обрізає кінець фраз", "Збільште <b>Continue recording</b> (2–3 с)."),
        ("Рве фразу на шматки", "Опустіть <b>close</b> (більша різниця open–close) і збільште Continue recording."),
        ("Слабкі станції не пишуться", "Мало запасу між шумом і голосом: звузьте фільтр, повільний AGC, "
                                       "менше RF gain — і крок 3 заново."),
        ("Взагалі не пише", "Перевірте, що натиснуто <b>Record</b> і стоять галочки <b>Don't write pause</b> + <b>VOX</b>; "
                            "чи не завищено <b>open</b> (має бути нижче рівня голосу S)."),
        ("Не видно VOX dB", "Подвійний клік по написі стану; розширте бокову панель SDR#."),
    ]
    data = [[Paragraph("Симптом", cellb), Paragraph("Рішення", cellb)]] + \
           [[Paragraph(a, cellb), Paragraph(b, cell)] for a, b in rows]
    t = Table(data, colWidths=[page_w * 0.36, page_w * 0.64])
    t.setStyle(TableStyle([("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#f3f4f6")),
                           ("GRID", (0, 0), (-1, -1), 0.5, colors.HexColor("#cccccc")),
                           ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
                           ("TOPPADDING", (0, 0), (-1, -1), 6), ("BOTTOMPADDING", (0, 0), (-1, -1), 6)]))
    s.append(t)
    s.append(Spacer(1, 12))
    s.append(KeepTogether([
        Paragraph("Шпаргалка (типові значення)", h2),
        box(Paragraph(
            "Don't write pause ☑ · VOX ☑ · close = <b>N + 4</b> · open = <b>N + 10</b> · "
            "Continue recording <b>2 с</b> · New file after <b>5 с</b> · Delete file &lt; <b>3 с</b><br/>"
            "SDR#: AGC <b>Slow</b> · фільтр SSB <b>2,7 кГц</b> / CW <b>500 Гц</b>", big),
            color="#eff6ff", border="#3b82f6"),
    ]))
    s.append(Spacer(1, 10))
    s.append(Paragraph(
        "Як це працює: плагін бере пікове значення рівня звуку кілька разів на секунду. "
        "Рівень ≥ open — запис іде; рівень &lt; close — гейт закривається, ще «Continue recording» секунд "
        "дописується хвіст, далі пауза. Між open і close гейт тримає попередній стан (гістерезис), "
        "тому запис не «тріпоче» на межі.", small))

    doc = SimpleDocTemplate(OUT, pagesize=A4, leftMargin=15 * mm, rightMargin=15 * mm,
                            topMargin=14 * mm, bottomMargin=16 * mm,
                            title="Налаштування VOX — Audio Recorder (SDR#)", author="recorder-rew")
    doc.build(s, onFirstPage=footer, onLaterPages=footer)


def main():
    imgs = {
        "rec": img_recorder_tab(),
        "file": img_file_tab(),
        "panel_closed": img_panel("03_panel_closed.png", dict(
            sec=0, mb="0.000", files=7, progress=0.0, label="Pause", label_color=TEXT, db=-45, gate="closed")),
        "panel_open": img_panel("04_panel_open.png", dict(
            sec=6, mb="0.094", files=8, progress=0.35, label="Record", label_color=RED, db=-17, gate="open"),
            callouts=False),
        "good": img_chart("05_chart_good.png", "Правильно: open −32 / close −38, шум ≈ −45",
                          -45, 3, -32, -38, ok=True),
        "bad": img_chart("06_chart_bad.png", "Неправильно: open −30 / close −40, шум ≈ −28 (швидкий AGC)",
                         -28, 3, -30, -40, ok=False),
    }
    build_pdf(imgs)
    print(OUT)


if __name__ == "__main__":
    main()
