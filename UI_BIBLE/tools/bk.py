"""Ludo NX bible kit: draws UI components in PIL at supersampled resolution.

All public sizes are in design points (440 x 956 frame). K = output pixels per point,
SS = supersampling factor. Components are built at K*SS and downsampled once.
"""
import math
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageChops

SS = 3
K = 2.0
FONTS = r"C:/Dev/LudoNX/Assets/Fonts/"
LILITA = FONTS + "LilitaOne-Regular.ttf"
NUNITO = FONTS + "Nunito-Bold.ttf"


def hx(h, a=255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def px(pt):
    return int(round(pt * K * SS))


# ---------------------------------------------------------------- tokens
T = {
    "cream50": "#FBF4E6", "cream100": "#F3E4C9", "tan200": "#CFAE84", "tan300": "#C09666",
    "tan400": "#A27E57", "brown600": "#7A5E40", "brown700": "#5A4227", "brown800": "#3E2C19",
    "ink": "#2A1E14", "inkMuted": "#6B5234",
    "houseRed": "#E73735", "houseGreen": "#4DAE51", "houseYellow": "#FBC02B", "houseBlue": "#1B89E7",
    "rarCommon": "#9A7148", "rarRare": "#1B89E7", "rarEpic": "#8E44D6", "rarLegend": "#F2B51F",
    "rankGold": "#E3A632", "rankSilver": "#A9B1BB", "rankBronze": "#D0743C",
}
BTN = {  # face, lip, highlight, label
    "play":    ("#FF3930", "#B7271F", "#FF7A6E", "#FFFFFF"),
    "go":      ("#2DA830", "#1B6E1E", "#6ED36F", "#FFFFFF"),
    "info":    ("#0051D5", "#00307F", "#4D8CF0", "#FFFFFF"),
    "option":  ("#FCAB1E", "#B8720A", "#FFD07A", "#FFFFFF"),
    "neutral": ("#C09666", "#8A6A45", "#DDBB8E", "#FFF3E1"),
    "disabled": ("#BDB3A3", "#8F8676", "#D9D1C4", "#F4EFE7"),
}


# ---------------------------------------------------------------- primitives
def blank(w, h):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))


def rr(size, box, r):
    m = Image.new("L", size, 0)
    ImageDraw.Draw(m).rounded_rectangle([int(v) for v in box], radius=int(r), fill=255)
    return m


def shifted(mask, dx, dy):
    out = Image.new("L", mask.size, 0)
    out.paste(mask, (int(dx), int(dy)))
    return out


def over(base, src, mask=None):
    """Porter-Duff 'over' of a colour or RGBA image through an optional mask."""
    if isinstance(src, tuple):
        lay = Image.new("RGBA", base.size, src[:3] + (0,))
        a = mask if mask is not None else Image.new("L", base.size, 255)
        if src[3] != 255:
            a = a.point(lambda v, k=src[3]: v * k // 255)
        lay.putalpha(a)
    else:
        lay = src.copy()
        if mask is not None:
            lay.putalpha(ImageChops.multiply(lay.getchannel("A"), mask))
    return Image.alpha_composite(base, lay)


def vgrad(size, stops):
    w, h = size
    col = Image.new("RGBA", (1, h))
    for y in range(h):
        t = y / max(1, h - 1)
        for i in range(len(stops) - 1):
            t0, c0 = stops[i]
            t1, c1 = stops[i + 1]
            if t0 <= t <= t1:
                u = 0 if t1 == t0 else (t - t0) / (t1 - t0)
                col.putpixel((0, y), tuple(int(c0[j] + (c1[j] - c0[j]) * u) for j in range(4)))
                break
        else:
            col.putpixel((0, y), stops[-1][1] if t > stops[-1][0] else stops[0][1])
    return col.resize((w, h), Image.NEAREST)


def vgrad_box(size, box, stops):
    """Gradient that runs over the box's height, not the whole image."""
    x0, y0, x1, y1 = [int(v) for v in box]
    g = vgrad((max(1, x1 - x0), max(1, y1 - y0)), stops)
    out = blank(*size)
    out.paste(g, (x0, y0))
    return out


def drop_shadow(mask, dx, dy, blur, color):
    m = shifted(mask, dx, dy).filter(ImageFilter.GaussianBlur(blur))
    lay = Image.new("RGBA", mask.size, color[:3] + (0,))
    lay.putalpha(m.point(lambda v, k=color[3]: v * k // 255))
    return lay


def inner_shadow(mask, dy, blur, color):
    inv = ImageChops.invert(mask)
    sh = ImageChops.offset(inv, 0, int(dy)).filter(ImageFilter.GaussianBlur(blur))
    sh = ImageChops.multiply(sh, mask)
    lay = Image.new("RGBA", mask.size, color[:3] + (0,))
    lay.putalpha(sh.point(lambda v, k=color[3]: v * k // 255))
    return lay


def erode(mask, pts):
    n = max(3, int(px(pts)) * 2 + 1)
    return mask.filter(ImageFilter.MinFilter(n if n % 2 else n + 1))


def font(path, pt):
    return ImageFont.truetype(path, px(pt))


def text(img, xy, s, path, pt, fill, stroke_pt=0, stroke=None, shadow=None, anchor="mm"):
    d = ImageDraw.Draw(img)
    f = font(path, pt)
    sw = px(stroke_pt) if stroke_pt else 0
    if shadow:
        dx, dy, col = shadow
        d.text((xy[0] + px(dx), xy[1] + px(dy)), s, font=f, fill=col, anchor=anchor,
               stroke_width=sw, stroke_fill=col)
    d.text(xy, s, font=f, fill=fill, anchor=anchor, stroke_width=sw, stroke_fill=stroke or fill)
    return img


def text_w(s, path, pt):
    f = font(path, pt)
    b = f.getbbox(s)
    return b[2] - b[0]


def down(img):
    return img.resize((img.width // SS, img.height // SS), Image.LANCZOS)


def load_art(path, h_pt):
    im = Image.open(path).convert("RGBA")
    bb = im.getchannel("A").getbbox()
    if bb:
        im = im.crop(bb)
    s = px(h_pt) / im.height
    return im.resize((max(1, int(im.width * s)), px(h_pt)), Image.LANCZOS)


def paste_center(base, art, cx, cy):
    lay = blank(*base.size)
    lay.paste(art, (int(cx - art.width / 2), int(cy - art.height / 2)), art)
    return Image.alpha_composite(base, lay)


# ---------------------------------------------------------------- components
def die(size_pt, body, pip, rot=-16, bevel=None):
    s = px(size_pt)
    pad = s // 2
    n = s + 2 * pad
    im = blank(n, n)
    m = rr((n, n), (pad, pad, pad + s, pad + s), s * 0.24)
    if bevel:
        im = over(im, hx(bevel), shifted(m, 0, s * 0.09))
    im = over(im, hx(body), m)
    if bevel:  # soft top light on the coloured hero dice
        im = over(im, (255, 255, 255, 60), ImageChops.multiply(m, rr((n, n), (pad, pad, pad + s, pad + s * 0.45), s * 0.24)))
    pm = Image.new("L", (n, n), 0)
    pd = ImageDraw.Draw(pm)
    r = s * 0.093
    for cx in (0.30, 0.70):
        for cy in (0.24, 0.50, 0.76):
            x, y = pad + cx * s, pad + cy * s
            pd.ellipse((x - r, y - r, x + r, y + r), fill=255)
    im = over(im, hx(pip), pm)
    return im.rotate(rot, resample=Image.BICUBIC)


def panel(w, h, hero=False, rim=10, ro=22, ri=12, notch=24, die_pt=17):
    """The NX Card: cream rim, tan field, four notched corners each holding a six-pip die."""
    pad = px(34)
    W, H = px(w) + 2 * pad, px(h) + 2 * pad
    img = blank(W, H)
    x0, y0, x1, y1 = pad, pad, pad + px(w), pad + px(h)
    outer = rr((W, H), (x0, y0, x1, y1), px(ro))
    img = Image.alpha_composite(img, drop_shadow(outer, 0, px(10), px(16), (0, 0, 0, 125)))
    img = over(img, vgrad_box((W, H), (x0, y0, x1, y1), [(0, hx("#FDF5E4")), (1, hx("#EBD9B8"))]), outer)
    # lit top edge of the rim
    edge = ImageChops.subtract(outer, shifted(outer, 0, px(1.4)))
    img = over(img, (255, 255, 255, 170), edge)
    t = px(rim)
    fx0, fy0, fx1, fy1 = x0 + t, y0 + t, x1 - t, y1 - t
    field = rr((W, H), (fx0, fy0, fx1, fy1), px(ri))
    d = ImageDraw.Draw(field)
    rn = px(notch)
    corners = [(fx0, fy0), (fx1, fy0), (fx0, fy1), (fx1, fy1)]
    for cx, cy in corners:
        d.ellipse((cx - rn, cy - rn, cx + rn, cy + rn), fill=0)
    field = field.filter(ImageFilter.GaussianBlur(px(1.8))).point(lambda v: 255 if v > 127 else 0)
    img = over(img, hx(T["tan200"]), field)
    img = Image.alpha_composite(img, inner_shadow(field, px(3.5), px(3.5), (70, 45, 20, 95)))
    ring = ImageChops.subtract(field, erode(field, 0.9))
    img = over(img, hx(T["tan400"]), ring)
    house = [T["houseRed"], T["houseGreen"], T["houseBlue"], T["houseYellow"]]
    rots = [-16, 16, 16, -16]
    for i, (cx, cy) in enumerate(corners):
        sx = 1 if cx == fx0 else -1
        sy = 1 if cy == fy0 else -1
        dcx = cx + sx * (-px(rim) * 0.05 + rn * 0.20)
        dcy = cy + sy * (-px(rim) * 0.05 + rn * 0.20)
        g = die(die_pt, house[i], "#FFFFFF", rots[i], bevel=None) if hero else die(die_pt, T["brown600"], T["cream100"], rots[i])
        if hero:
            dark = {0: "#9E1F1D", 1: "#2E7A32", 2: "#0E5CA3", 3: "#C08F12"}[i]
            g = die(die_pt, house[i], "#FFFFFF", rots[i], bevel=dark)
        img = paste_center(img, g, dcx, dcy)
    return img, pad, (fx0, fy0, fx1, fy1)


def plaque(label, h=38, fpt=22, min_w=0):
    tw = text_w(label, LILITA, fpt)
    w_px = max(px(min_w), tw + px(76))
    pad = px(10)
    W, H = w_px + 2 * pad, px(h) + 2 * pad
    img = blank(W, H)
    box = (pad, pad, pad + w_px, pad + px(h))
    outer = rr((W, H), box, px(h * 0.32))
    img = Image.alpha_composite(img, drop_shadow(outer, 0, px(3), px(3), (0, 0, 0, 110)))
    img = over(img, vgrad_box((W, H), box, [(0, hx("#E9C99A")), (1, hx("#B88E5C"))]), outer)
    ib = (box[0] + px(3), box[1] + px(3), box[2] - px(3), box[3] - px(3))
    inner = rr((W, H), ib, px(h * 0.26))
    img = over(img, vgrad_box((W, H), ib, [(0, hx("#735638")), (0.55, hx(T["brown700"])), (1, hx("#44311C"))]), inner)
    img = Image.alpha_composite(img, inner_shadow(inner, px(2), px(1.5), (0, 0, 0, 90)))
    hl = ImageChops.subtract(inner, shifted(inner, 0, -px(1.2)))
    img = over(img, (255, 230, 190, 70), ImageChops.subtract(inner, shifted(inner, 0, px(1.2))))
    cy = pad + px(h) / 2
    for cx in (box[0] + px(20), box[2] - px(20)):
        s = px(4.2)
        dm = Image.new("L", (W, H), 0)
        ImageDraw.Draw(dm).polygon([(cx, cy - s), (cx + s, cy), (cx, cy + s), (cx - s, cy)], fill=255)
        img = over(img, hx("#E3C394"), dm)
    text(img, (W / 2, cy + px(1)), label, LILITA, fpt, hx("#F0D3A6"), shadow=(0, 1.6, hx("#2A1D10")))
    return img, pad


def button(label, kind, w, h=50, lip=5, pressed=False, slab=False, fpt=20, collar=0, icon=None):
    face, lipc, hi, lab = [hx(c) for c in BTN[kind]]
    pad = px(12 + collar)
    W, H = px(w) + 2 * pad, px(h) + 2 * pad
    img = blank(W, H)
    fh = h - lip
    r = px(16) if slab else px(fh / 2)
    x0, x1 = pad, pad + px(w)
    if collar:
        cb = (x0 - px(collar), pad - px(collar), x1 + px(collar), pad + px(h) + px(collar))
        cm = rr((W, H), cb, r + px(collar))
        img = Image.alpha_composite(img, drop_shadow(cm, 0, px(4), px(5), (0, 0, 0, 90)))
        img = over(img, vgrad_box((W, H), cb, [(0, hx("#FFF8EA")), (1, hx("#EAD6B2"))]), cm)
    lip_now = 1 if pressed else lip
    fy0 = pad + px(lip - lip_now)
    lm = rr((W, H), (x0, pad + px(lip), x1, pad + px(lip) + px(fh)), r)
    if not collar:
        img = Image.alpha_composite(img, drop_shadow(lm, 0, px(2), px(2.5), (0, 0, 0, 80)))
    img = over(img, lipc, lm)
    fm = rr((W, H), (x0, fy0, x1, fy0 + px(fh)), r)
    fb = (x0, fy0, x1, fy0 + px(fh))
    img = over(img, vgrad_box((W, H), fb, [(0, hi), (0.5, face), (1, face)]), fm)
    gl = rr((W, H), (x0 + px(4), fy0 + px(2.5), x1 - px(4), fy0 + px(fh * 0.46)), r)
    img = over(img, vgrad_box((W, H), (x0, fy0, x1, fy0 + px(fh * 0.46)), [(0, (255, 255, 255, 95)), (1, (255, 255, 255, 12))]), gl)
    img = over(img, (255, 255, 255, 120), ImageChops.subtract(fm, shifted(fm, 0, px(1.2))))
    if pressed:
        img = over(img, (0, 0, 0, 28), fm)
    cx, cy = W / 2, fy0 + px(fh) / 2 + px(0.5)
    if icon is not None:
        tw = text_w(label, LILITA, fpt)
        ic = icon
        total = ic.width + px(6) + tw
        img = paste_center(img, ic, cx - total / 2 + ic.width / 2, cy)
        cx = cx - total / 2 + ic.width + px(6) + tw / 2
    text(img, (cx, cy), label, LILITA, fpt, lab, stroke_pt=1.3, stroke=lipc, shadow=(0, 1.4, lipc))
    return img, pad


def close_x(d=44):
    pad = px(10)
    W = px(d) + 2 * pad
    img = blank(W, W)
    m = Image.new("L", (W, W), 0)
    ImageDraw.Draw(m).ellipse((pad, pad, pad + px(d), pad + px(d)), fill=255)
    img = Image.alpha_composite(img, drop_shadow(m, 0, px(3), px(3), (0, 0, 0, 120)))
    img = over(img, vgrad_box((W, W), (pad, pad, pad + px(d), pad + px(d)), [(0, hx("#FFF8EA")), (1, hx("#E4CFA8"))]), m)
    ring = ImageChops.subtract(m, erode(m, 2.2))
    img = over(img, hx(T["tan300"]), ring)
    xm = Image.new("L", (W, W), 0)
    dx = ImageDraw.Draw(xm)
    c, s = W / 2, px(d * 0.2)
    dx.line((c - s, c - s, c + s, c + s), fill=255, width=px(4.2))
    dx.line((c - s, c + s, c + s, c - s), fill=255, width=px(4.2))
    img = over(img, hx(T["brown700"]), xm)
    return img, pad


def row_plate(w, h, fill=None, r=14):
    pad = px(8)
    W, H = px(w) + 2 * pad, px(h) + 2 * pad
    img = blank(W, H)
    box = (pad, pad, pad + px(w), pad + px(h))
    m = rr((W, H), box, px(r))
    img = Image.alpha_composite(img, drop_shadow(m, 0, px(2), px(2), (60, 40, 20, 70)))
    lip = rr((W, H), (box[0], box[1] + px(2), box[2], box[3]), px(r))
    img = over(img, hx(T["tan300"]), lip)
    top = rr((W, H), (box[0], box[1], box[2], box[3] - px(2)), px(r))
    if fill is None:
        img = over(img, vgrad_box((W, H), box, [(0, hx("#FBF1DE")), (1, hx(T["cream100"]))]), top)
    else:
        img = over(img, fill, top)
    return img, pad, box


def avatar(path, d, ring=("#3C8BFF", "#6A3BD8"), ring_pt=3.5):
    s = px(d)
    pad = px(4)
    W = s + 2 * pad
    img = blank(W, W)
    m = Image.new("L", (W, W), 0)
    ImageDraw.Draw(m).ellipse((pad, pad, pad + s, pad + s), fill=255)
    img = Image.alpha_composite(img, drop_shadow(m, 0, px(1.5), px(1.5), (0, 0, 0, 90)))
    img = over(img, vgrad_box((W, W), (pad, pad, pad + s, pad + s), [(0, hx(ring[0])), (1, hx(ring[1]))]), m)
    inner = erode(m, ring_pt)
    pic = Image.open(path).convert("RGBA")
    side = min(pic.size)
    pic = pic.crop(((pic.width - side) // 2, 0, (pic.width - side) // 2 + side, side))
    ib = inner.getbbox()
    pic = pic.resize((ib[2] - ib[0], ib[3] - ib[1]), Image.LANCZOS)
    lay = blank(W, W)
    lay.paste(pic, (ib[0], ib[1]))
    img = over(img, lay, inner)
    return img, pad


def progress(w, h, frac, fill=("#FFD07A", "#FCAB1E", "#D9870C")):
    pad = px(3)
    W, H = px(w) + 2 * pad, px(h) + 2 * pad
    img = blank(W, H)
    box = (pad, pad, pad + px(w), pad + px(h))
    m = rr((W, H), box, px(h / 2))
    img = over(img, hx(T["tan300"]), m)
    img = Image.alpha_composite(img, inner_shadow(m, px(1.5), px(1.2), (60, 35, 10, 120)))
    if frac > 0:
        fb = (box[0] + px(1.5), box[1] + px(1.5), box[0] + px(1.5) + (px(w) - px(3)) * frac, box[3] - px(1.5))
        fm = rr((W, H), fb, px((h - 3) / 2))
        img = over(img, vgrad_box((W, H), fb, [(0, hx(fill[0])), (0.5, hx(fill[1])), (1, hx(fill[2]))]), fm)
        img = over(img, (255, 255, 255, 90), rr((W, H), (fb[0] + px(2), fb[1] + px(1), fb[2] - px(2), fb[1] + px(h * 0.3)), px(2)))
    return img, pad
