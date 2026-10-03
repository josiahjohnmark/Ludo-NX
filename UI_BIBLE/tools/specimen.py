import os, math
from PIL import Image, ImageDraw, ImageFilter, ImageChops
import bk
from bk import px, hx, T, LILITA, NUNITO, over, blank, rr, text, paste_center

ART = r"C:/Dev/LudoNX/Assets/Art/UI/"
WALL = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "source", "bg_menu_emboss.png")
OUT = r"C:/Dev/LudoNX/UI_BIBLE/specimens/bible_specimen_v1.png"
K = bk.K
SW, SH = 1180, 1190  # sheet in points


def put(sheet, comp, pad, x_pt, y_pt):
    """Downsample an SS-scale component and place its box top-left at (x_pt, y_pt)."""
    small = bk.down(comp)
    p = pad // bk.SS
    lay = Image.new("RGBA", sheet.size, (0, 0, 0, 0))
    lay.paste(small, (int(x_pt * K) - p, int(y_pt * K) - p), small)
    return Image.alpha_composite(sheet, lay)


def at(base, comp, pad, x, y):
    """Place an SS-scale component inside another SS-scale image (x, y in SS px)."""
    lay = blank(*base.size)
    lay.paste(comp, (int(x - pad), int(y - pad)), comp)
    return Image.alpha_composite(base, lay)


def small_text(sheet, x, y, s, pt=13, col="#E9DCC4", path=None, anchor="la"):
    # sheet is final scale: draw with a temporary SS image for consistent font metrics
    tmp = blank(px(900), px(pt * 2.2))
    text(tmp, (0, px(pt * 1.1)), s, path or bk.NUNITO, pt, hx(col), anchor="lm")
    small = bk.down(tmp)
    lay = Image.new("RGBA", sheet.size, (0, 0, 0, 0))
    lay.paste(small, (int(x * K), int(y * K - pt * 1.1 * K)), small)
    return Image.alpha_composite(sheet, lay)


def section_label(img, cx, y, label, span, col=T["inkMuted"]):
    d = ImageDraw.Draw(img)
    tw = bk.text_w(label, LILITA, 14)
    gap = px(10)
    d.line((cx - span / 2, y, cx - tw / 2 - gap, y), fill=hx(T["tan400"]), width=px(1.4))
    d.line((cx + tw / 2 + gap, y, cx + span / 2, y), fill=hx(T["tan400"]), width=px(1.4))
    text(img, (cx, y + px(0.5)), label, LILITA, 14, hx(col))


# ------------------------------------------------------------------ ground
sheet = Image.new("RGBA", (int(SW * K), int(SH * K)), hx("#1E2229"))
wall = Image.open(WALL).convert("RGBA")
s = max(sheet.width / wall.width, sheet.height / wall.height)
wall = wall.resize((int(wall.width * s), int(wall.height * s)), Image.LANCZOS)
sheet.paste(wall, ((sheet.width - wall.width) // 2, (sheet.height - wall.height) // 2))
sheet = over(sheet, (10, 12, 16, 60))

sheet = small_text(sheet, 40, 40, "LUDO NX  \u00b7  UI BIBLE v1  \u2014  SPECIMEN (proposal)", 24, "#F3E4C9", LILITA)
sheet = small_text(sheet, 40, 64, "Drawn in code from the bible tokens on your own navy wallpaper. Each piece can be exported as a @3x, 9-slice-ready sprite.", 13, "#B9B2A4")

# ------------------------------------------------------------------ A: standard panel (Daily Missions)
PW, PH = 380, 470
img, pad, field = bk.panel(PW, PH, hero=False)
x0 = y0 = pad
cx = x0 + px(PW) / 2
pl, ppad = bk.plaque("DAILY MISSIONS", h=40, fpt=22)
img = at(img, pl, ppad, cx - (pl.width - 2 * ppad) / 2, y0 - px(20))
text(img, (cx, y0 + px(44)), "New missions in 9h 24m", NUNITO, 13, hx(T["inkMuted"]))
section_label(img, cx, y0 + px(70), "TODAY", px(300))
rows = [("Capture 5 tokens", 3, 5, "+120 NX", "icon_dice.png", False),
        ("Win 2 matches in a row", 2, 2, "+200 NX", "icon_stat.png", True),
        ("Play 5 matches", 1, 5, "+140 NX", "icon_badge.png", False)]
ry = y0 + px(88)
for title, v, mx, rew, icon, done in rows:
    rp, rpad, rbox = bk.row_plate(336, 74)
    rx = cx - px(336) / 2
    # icon well
    wm = Image.new("L", rp.size, 0)
    wcx, wcy = rpad + px(34), rpad + px(35)
    ImageDraw.Draw(wm).ellipse((wcx - px(24), wcy - px(24), wcx + px(24), wcy + px(24)), fill=255)
    rp = over(rp, hx(T["tan300"]), wm)
    rp = Image.alpha_composite(rp, bk.inner_shadow(wm, px(2), px(2), (60, 35, 10, 110)))
    rp = paste_center(rp, bk.load_art(ART + "Missions/" + icon, 34), wcx, wcy)
    text(rp, (rpad + px(68), rpad + px(20)), title, LILITA, 16, hx(T["ink"]), anchor="lm")
    pg, ppad2 = bk.progress(130, 12, v / mx)
    rp = at(rp, pg, ppad2, rpad + px(68), rpad + px(34))
    text(rp, (rpad + px(206), rpad + px(40)), f"{v}/{mx}", LILITA, 14, hx(T["ink"]), anchor="lm")
    text(rp, (rpad + px(68), rpad + px(58)), rew, NUNITO, 12, hx(T["inkMuted"]), anchor="lm")
    b, bpad = bk.button("CLAIM", "go" if done else "disabled", 78, 34, lip=4, fpt=15)
    rp = at(rp, b, bpad, rpad + px(248), rpad + px(20))
    img = at(img, rp, rpad, rx, ry)
    ry += px(84)
section_label(img, cx, ry + px(8), "BONUS", px(300))
bw = rr(img.size, (cx - px(168), ry + px(22), cx + px(168), ry + px(78)), px(14))
img = over(img, hx(T["tan300"]), bw)
img = Image.alpha_composite(img, bk.inner_shadow(bw, px(2.5), px(2.5), (60, 35, 10, 120)))
img = paste_center(img, bk.load_art(ART + "Missions/chest.png", 44), cx - px(136), ry + px(50))
text(img, (cx - px(104), ry + px(42)), "Finish all 3 missions", LILITA, 16, hx(T["ink"]), anchor="lm")
text(img, (cx - px(104), ry + px(61)), "Bonus chest: +250 NX, +40 energy", NUNITO, 12, hx(T["inkMuted"]), anchor="lm")
pg, ppad2 = bk.progress(70, 12, 1 / 3)
img = at(img, pg, ppad2, cx + px(80), ry + px(44))
text(img, (cx + px(115), ry + px(66)), "1/3", LILITA, 13, hx(T["ink"]), anchor="mm")
b, bpad = bk.button("CLAIM ALL", "go", 200, 52, lip=6, fpt=21, collar=6)
img = at(img, b, bpad, cx - px(100), y0 + px(PH) - px(28))
sheet = put(sheet, img, pad, 40, 110)
cx_, cpad = bk.close_x(44)
sheet = put(sheet, cx_, cpad, 40 + PW / 2 - 22, 110 + PH + 48)
sheet = small_text(sheet, 40, 700, "STANDARD PANEL \u2014 every popup and screen body", 13, "#F3E4C9", LILITA)
sheet = small_text(sheet, 40, 718, "Cream rim, tan field, four notched corners with the six-pip die.", 11, "#B9B2A4")
sheet = small_text(sheet, 40, 733, "Nameplate on the top edge, main action on the bottom edge, close X below.", 11, "#B9B2A4")

# ------------------------------------------------------------------ B: hero panel (Quick Match)
HX = 460
img, pad, field = bk.panel(PW, PH, hero=True)
x0 = y0 = pad
cx = x0 + px(PW) / 2
av, apad = bk.avatar(ART + "MatchSetup/Cards/mascot_avatar.png", 84, ring=("#FFF6E3", "#E2C99F"), ring_pt=5)
img = at(img, av, apad, cx - px(42), y0 - px(46))
pl, ppad = bk.plaque("QUICK MATCH", h=38, fpt=21)
img = at(img, pl, ppad, cx - (pl.width - 2 * ppad) / 2, y0 + px(50))
section_label(img, cx, y0 + px(112), "YOUR SET", px(300))
for i, (lab, art) in enumerate([("DICE", "Shop/Dice/dice_05_gold.png"), ("TOKEN", "MatchSetup/pawn_red.png")]):
    wx = cx + (i * 2 - 1) * px(52) - px(38)
    wy = y0 + px(126)
    wm = rr(img.size, (wx, wy, wx + px(76), wy + px(66)), px(12))
    img = over(img, hx(T["tan300"]), wm)
    img = Image.alpha_composite(img, bk.inner_shadow(wm, px(2.5), px(2.5), (60, 35, 10, 120)))
    img = paste_center(img, bk.load_art(ART + art, 46), wx + px(38), wy + px(33))
    text(img, (wx + px(38), wy + px(78)), lab, NUNITO, 12, hx(T["inkMuted"]))
section_label(img, cx, y0 + px(226), "STAKE", px(300))
chips = [("X1", "neutral"), ("X2", "neutral"), ("X5", "option"), ("X10", "neutral"), ("X20", "disabled")]
chx = cx - px(5 * 56 + 4 * 6) / 2
for lab, kind in chips:
    b, bpad = bk.button(lab, kind, 56, 40, lip=4, fpt=17)
    img = at(img, b, bpad, chx, y0 + px(240))
    if kind == "disabled":
        img = paste_center(img, bk.load_art(ART + "MatchSetup/icon_lock.png", 16), chx + px(49), y0 + px(242))
    chx += px(62)
parts = [("WIN ", NUNITO, 13, T["inkMuted"]), ("+250 NX", LILITA, 17, "#1F7A22"), ("     LOSE ", NUNITO, 13, T["inkMuted"]), ("−125 NX", LILITA, 17, "#B7271F")]
tot = sum(bk.text_w(t_, f_, z_) for t_, f_, z_, c_ in parts)
xx = cx - tot / 2
for t_, f_, z_, c_ in parts:
    text(img, (xx, y0 + px(300)), t_, f_, z_, hx(c_), anchor="lm")
    xx += bk.text_w(t_, f_, z_)
b, bpad = bk.button("PLAY", "play", 230, 66, lip=7, fpt=34, slab=True, collar=6)
img = at(img, b, bpad, cx - px(115), y0 + px(330))
# multiplier coin
mc = Image.new("L", img.size, 0)
mcx, mcy, mr = cx + px(112), y0 + px(334), px(22)
ImageDraw.Draw(mc).ellipse((mcx - mr - px(3), mcy - mr - px(3), mcx + mr + px(3), mcy + mr + px(3)), fill=255)
img = over(img, hx("#FFF6E3"), mc)
mc2 = Image.new("L", img.size, 0)
ImageDraw.Draw(mc2).ellipse((mcx - mr, mcy - mr, mcx + mr, mcy + mr), fill=255)
img = over(img, bk.vgrad_box(img.size, (mcx - mr, mcy - mr, mcx + mr, mcy + mr), [(0, hx("#4D8CF0")), (0.55, hx("#0051D5")), (1, hx("#00307F"))]), mc2)
text(img, (mcx, mcy + px(1)), "X5", LILITA, 17, hx("#FFFFFF"), stroke_pt=1.2, stroke=hx("#00307F"))
en = bk.load_art(r"C:/Dev/LudoNX/Assets/Art/UI/HomeScreen/energy_icon.png", 18)
img = paste_center(img, en, cx - px(64), y0 + px(420))
text(img, (cx - px(52), y0 + px(420)), "1 energy  \u00b7  you have 18", NUNITO, 13, hx(T["inkMuted"]), anchor="lm")
sheet = put(sheet, img, pad, HX, 110)
sheet = put(sheet, cx_, cpad, HX + PW / 2 - 22, 110 + PH + 48)
sheet = small_text(sheet, HX, 700, "HERO PANEL \u2014 the match flow only (pre-match, found, result)", 13, "#F3E4C9", LILITA)
sheet = small_text(sheet, HX, 718, "Same chassis. The four corner dice take the house colours,", 11, "#B9B2A4")
sheet = small_text(sheet, HX, 733, "placed like the board: red top-left, green top-right, blue and yellow below.", 11, "#B9B2A4")

# ------------------------------------------------------------------ C: buttons
BX = 880
sheet = small_text(sheet, BX, 104, "BUTTONS \u2014 one colour, one meaning", 13, "#F3E4C9", LILITA)
kinds = [("play", "PLAY", "Red: start a match. Max one per screen."),
         ("go", "CLAIM", "Green: buy, claim, confirm, save, OK."),
         ("info", "SHARE", "Blue: social, info, invite, connect."),
         ("option", "LANGUAGE", "Amber: open an option or sub-page."),
         ("neutral", "HOME", "Tan: back, cancel, later, home."),
         ("disabled", "LOCKED", "Grey: not available yet.")]
by = 126
for kind, lab, cap in kinds:
    sheet = small_text(sheet, BX, by, cap, 11, "#CFC6B6")
    b, bpad = bk.button(lab, kind, 128, 46, lip=5, fpt=18, slab=(kind == "play"))
    sheet = put(sheet, b, bpad, BX, by + 12)
    b, bpad = bk.button(lab, kind, 128, 46, lip=5, fpt=18, pressed=True, slab=(kind == "play"))
    sheet = put(sheet, b, bpad, BX + 142, by + 12)
    by += 84
sheet = small_text(sheet, BX, by + 4, "left: resting (5pt lip)   right: pressed (face drops, lip 1pt)", 11, "#8F897D")

# ------------------------------------------------------------------ D: shop cards
SY = 800
sheet = small_text(sheet, 40, SY - 14, "SHOP CARDS \u2014 rarity sets the rim line, glow and ribbon", 13, "#F3E4C9", LILITA)
cards = [("CLASSIC", "dice_01_classic.png", "rarCommon", "coin", "1,200", None),
         ("SAPPHIRE", "dice_03_sapphire.png", "rarRare", "coin", "2,500", "NEW"),
         ("AMETHYST", "dice_06_amethyst.png", "rarEpic", "iap", "$1.99", None),
         ("GOLDEN NX", "dice_05_gold.png", "rarLegend", "iap", "$4.99", "-40%")]
CW, CH = 150, 210
coin = bk.load_art(r"C:/Dev/LudoNX/Assets/Art/UI/HomeScreen/coin_icon_gold.png", 20)
for i, (name, dice, rar, kind, price, tag) in enumerate(cards):
    pad = px(14)
    W, H = px(CW) + 2 * pad, px(CH) + 2 * pad
    c = blank(W, H)
    box = (pad, pad, pad + px(CW), pad + px(CH))
    outer = rr((W, H), box, px(16))
    c = Image.alpha_composite(c, bk.drop_shadow(outer, 0, px(6), px(8), (0, 0, 0, 120)))
    rimstops = [(0, hx("#FFF3D6")), (1, hx("#E9D3AA"))] if rar != "rarLegend" else [(0, hx("#FFE9A6")), (0.5, hx("#F2B51F")), (1, hx("#B97F0B"))]
    c = over(c, bk.vgrad_box((W, H), box, rimstops), outer)
    ib = (box[0] + px(6), box[1] + px(6), box[2] - px(6), box[3] - px(6))
    inner = rr((W, H), ib, px(11))
    c = over(c, hx(T["tan200"]), inner)
    if rar != "rarCommon":
        line = ImageChops.subtract(inner, bk.erode(inner, 1.6))
        c = over(c, hx(T[rar]), line)
    # glow / rays behind the item
    gcx, gcy = W / 2, box[1] + px(78)
    if rar in ("rarRare", "rarEpic", "rarLegend"):
        g = Image.radial_gradient("L").resize((px(150), px(150)))
        g = ImageChops.invert(g).point(lambda v: max(0, v - 60) * 255 // 195)
        gl = blank(W, H)
        col = Image.new("RGBA", g.size, hx(T[rar])[:3] + (0,))
        col.putalpha(g.point(lambda v: v * 150 // 255))
        gl.paste(col, (int(gcx - g.width / 2), int(gcy - g.height / 2)), col)
        c = over(c, gl, inner)
    if rar == "rarLegend":
        rays = Image.new("L", (W, H), 0)
        dr = ImageDraw.Draw(rays)
        for k in range(12):
            a0 = math.radians(k * 30)
            a1 = math.radians(k * 30 + 12)
            R = px(120)
            dr.polygon([(gcx, gcy), (gcx + R * math.cos(a0), gcy + R * math.sin(a0)), (gcx + R * math.cos(a1), gcy + R * math.sin(a1))], fill=255)
        fade = Image.radial_gradient("L").resize((px(170), px(170)))
        fl = Image.new("L", (W, H), 255)
        fl.paste(fade, (int(gcx - px(85)), int(gcy - px(85))))
        rays = ImageChops.multiply(rays, ImageChops.invert(fl))
        rays = ImageChops.multiply(rays, inner)
        c = over(c, (255, 236, 160, 120), rays)
    for (rx_, ry_) in [(ib[0] + px(9), ib[1] + px(9)), (ib[2] - px(9), ib[1] + px(9)), (ib[0] + px(9), ib[3] - px(9)), (ib[2] - px(9), ib[3] - px(9))]:
        rm = Image.new("L", (W, H), 0)
        ImageDraw.Draw(rm).ellipse((rx_ - px(3.2), ry_ - px(3.2), rx_ + px(3.2), ry_ + px(3.2)), fill=255)
        c = over(c, hx(T["brown600"]), rm)
        hm = Image.new("L", (W, H), 0)
        ImageDraw.Draw(hm).ellipse((rx_ - px(1.6), ry_ - px(2.2), rx_ + px(0.6), ry_ - px(0.4)), fill=255)
        c = over(c, (255, 240, 210, 150), hm)
    c = paste_center(c, bk.load_art(ART + "Shop/Dice/" + dice, 84), gcx, gcy)
    # ribbon
    ry0 = box[1] + px(128)
    rbx = (box[0] - px(4), ry0, box[2] + px(4), ry0 + px(30))
    rmask = rr((W, H), rbx, px(5))
    base = hx(T[rar])
    dark = tuple(int(v * 0.72) for v in base[:3]) + (255,)
    c = Image.alpha_composite(c, bk.drop_shadow(rmask, 0, px(2), px(2), (0, 0, 0, 90)))
    c = over(c, base, rmask)
    st = Image.new("L", (W, H), 0)
    ds = ImageDraw.Draw(st)
    for k in range(-2, 9):
        x = rbx[0] + k * px(26)
        ds.polygon([(x, rbx[3]), (x + px(10), rbx[3]), (x + px(24), rbx[1]), (x + px(14), rbx[1])], fill=255)
    c = over(c, dark[:3] + (120,), ImageChops.multiply(st, rmask))
    text(c, (W / 2, ry0 + px(15.5)), name, LILITA, 15, hx("#FFFFFF"), stroke_pt=1.4, stroke=dark, shadow=(0, 1.3, dark))
    # price button straddling the bottom edge
    if kind == "coin":
        b, bpad = bk.button(price, "go", 112, 38, lip=4, fpt=16, icon=coin)
    else:
        b, bpad = bk.button(price, "go", 112, 38, lip=4, fpt=17)
    c = at(c, b, bpad, W / 2 - px(56), box[3] - px(26))
    if tag:
        tb = (box[0] - px(6), box[1] - px(6), box[0] + px(46), box[1] + px(20))
        tm = rr((W, H), tb, px(9))
        tc = hx("#0051D5") if tag == "NEW" else hx("#E7302A")
        c = Image.alpha_composite(c, bk.drop_shadow(tm, 0, px(2), px(2), (0, 0, 0, 100)))
        c = over(c, tc, tm)
        text(c, ((tb[0] + tb[2]) / 2, (tb[1] + tb[3]) / 2 + px(0.5)), tag, LILITA, 13, hx("#FFFFFF"))
    sheet = put(sheet, c, pad, 40 + i * (CW + 16), SY + 10)
labels = ["COMMON \u00b7 coins", "RARE \u00b7 coins", "EPIC \u00b7 real money", "LEGENDARY \u00b7 real money"]
for i, l in enumerate(labels):
    sheet = small_text(sheet, 40 + i * (CW + 16) + 4, SY + CH + 50, l, 11, "#CFC6B6")

# ------------------------------------------------------------------ E: leaderboard rows + pills
LX, LY = 720, 800
sheet = small_text(sheet, LX, LY - 14, "RANK ROWS \u2014 flag carries the rank colour, self row pinned in blue", 13, "#F3E4C9", LILITA)
people = [(1, "JOSIAH", "12,480", "rankGold", "avatar_01.jpg"), (2, "VICTOR", "11,930", "rankSilver", "avatar_02.jpg"),
          (3, "SELYA", "10,210", "rankBronze", "avatar_03.jpg"), (4, "RHODA", "9,540", None, "avatar_04.jpg"),
          (35, "YOU", "850", "self", "avatar_05.jpg")]
ry = LY + 10
for rank, name, score, flag, av in people:
    RW, RH = 420, 50
    if flag == "self":
        rp, rpad, rbox = bk.row_plate(RW, RH, fill=None)
        top = rr(rp.size, (rbox[0], rbox[1], rbox[2], rbox[3] - px(2)), px(14))
        rp = over(rp, bk.vgrad_box(rp.size, rbox, [(0, hx("#8DB8E6")), (1, hx("#5B82AD"))]), top)
    else:
        rp, rpad, rbox = bk.row_plate(RW, RH)
    fb = (rbox[0], rbox[1], rbox[0] + px(46), rbox[3] - px(2))
    fm = rr(rp.size, fb, px(14))
    fm = ImageChops.multiply(fm, rr(rp.size, (rbox[0], rbox[1], rbox[0] + px(46), rbox[3] - px(2)), px(14)))
    cut = Image.new("L", rp.size, 0)
    ImageDraw.Draw(cut).rectangle((fb[0] + px(24), fb[1], fb[2], fb[3]), fill=255)
    fm2 = ImageChops.lighter(rr(rp.size, fb, px(14)), ImageChops.multiply(cut, Image.new("L", rp.size, 255)))
    fm2 = ImageChops.multiply(fm2, rr(rp.size, (rbox[0], rbox[1], rbox[2], rbox[3] - px(2)), px(14)))
    fm2 = ImageChops.multiply(fm2, rr(rp.size, (rbox[0] - px(20), rbox[1], rbox[0] + px(46), rbox[3]), px(2)))
    if flag and flag != "self":
        c0 = hx(T[flag])
        rp = over(rp, bk.vgrad_box(rp.size, fb, [(0, tuple(min(255, int(v * 1.18)) for v in c0[:3]) + (255,)), (1, c0)]), fm2)
        text(rp, ((fb[0] + fb[2]) / 2, (fb[1] + fb[3]) / 2 + px(1)), str(rank), LILITA, 20, hx("#FFFFFF"), stroke_pt=1.4, stroke=tuple(int(v * 0.6) for v in c0[:3]) + (255,))
    elif flag == "self":
        rp = over(rp, hx("#2F5A86"), fm2)
        text(rp, ((fb[0] + fb[2]) / 2, (fb[1] + fb[3]) / 2 + px(1)), str(rank), LILITA, 18, hx("#FFFFFF"))
    else:
        rp = over(rp, hx(T["tan300"]), fm2)
        text(rp, ((fb[0] + fb[2]) / 2, (fb[1] + fb[3]) / 2 + px(1)), str(rank), LILITA, 18, hx(T["ink"]))
    a, apad = bk.avatar(ART + "Avatars/" + av, 36, ring_pt=3)
    rp = at(rp, a, apad, rbox[0] + px(56), rbox[1] + px(5))
    ink = "#FFFFFF" if flag == "self" else T["ink"]
    text(rp, (rbox[0] + px(102), rbox[1] + px(24)), name, LILITA, 17, hx(ink), anchor="lm")
    text(rp, (rbox[2] - px(40), rbox[1] + px(24)), score, LILITA, 18, hx(ink), anchor="rm")
    nb = (rbox[2] - px(34), rbox[1] + px(15), rbox[2] - px(10), rbox[1] + px(33))
    nm = rr(rp.size, nb, px(5))
    rp = over(rp, hx("#9C0C33"), nm)
    text(rp, ((nb[0] + nb[2]) / 2, (nb[1] + nb[3]) / 2 + px(0.5)), "NX", LILITA, 11, hx("#FFE3A3"))
    sheet = put(sheet, rp, rpad, LX, ry)
    ry += 58 if rank != 4 else 70
sheet = small_text(sheet, LX, ry + 8, "CURRENCY PILLS \u2014 icon overlaps the left end, green + buys more", 13, "#F3E4C9", LILITA)
pills = [("HomeScreen/coin_icon_gold.png", "12,000", True), ("HomeScreen/energy_icon.png", "18/20", True)]
pxx = LX
for icon, val, plus in pills:
    pad = px(10)
    PWp, PHp = 150, 36
    W, H = px(PWp) + 2 * pad, px(PHp) + 2 * pad
    p = blank(W, H)
    box = (pad + px(12), pad, pad + px(PWp), pad + px(PHp))
    m = rr((W, H), box, px(PHp / 2))
    p = over(p, (14, 17, 23, 215), m)
    p = Image.alpha_composite(p, bk.inner_shadow(m, px(2), px(2), (0, 0, 0, 160)))
    p = over(p, (255, 255, 255, 40), ImageChops.subtract(m, bk.erode(m, 0.8)))
    p = paste_center(p, bk.load_art(ART + icon, 40), pad + px(16), pad + px(PHp / 2))
    text(p, (pad + px(80), pad + px(PHp / 2) + px(1)), val, LILITA, 19, hx("#FFFFFF"), anchor="mm")
    if plus:
        b, bpad = bk.button("+", "go", 28, 30, lip=3, fpt=20)
        p = at(p, b, bpad, box[2] - px(32), pad + px(3))
    sheet = put(sheet, p, pad, pxx, ry + 26)
    pxx += 176

os.makedirs(os.path.dirname(OUT), exist_ok=True)
sheet.convert("RGB").save(OUT, optimize=True)
print(OUT, sheet.size)
