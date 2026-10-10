import math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageEnhance
import os

def create_phantom_orion_logo(size=2048):
    print("Designing Phantom Orion logo badge (size: %dx%d)..." % (size, size))
    # High-resolution canvas with RGBA
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    cx, cy = size // 2, int(size * 0.44)
    scale = size / 1000.0

    # Helper function to get font
    def get_font(name, sz):
        try:
            return ImageFont.truetype(f"C:/Windows/Fonts/{name}", int(sz * scale))
        except Exception:
            return ImageFont.load_default()

    font_title = get_font("bahnschrift.ttf", 62)
    font_sub = get_font("bahnschrift.ttf", 26)

    # -------------------------------------------------------------
    # 1. Outer Crest Glow
    # -------------------------------------------------------------
    glow_layer = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(glow_layer)

    # Shield polygon coordinates (relative to 1000x1000)
    shield_pts_1000 = [
        (500, 70),   # Top center peak
        (820, 150),  # Top right shoulder
        (880, 520),  # Right upper curve
        (760, 720),  # Right lower taper
        (500, 860),  # Bottom point
        (240, 720),  # Left lower taper
        (120, 520),  # Left upper curve
        (180, 150),  # Top left shoulder
    ]
    shield_pts = [(int(x * scale), int(y * scale)) for x, y in shield_pts_1000]

    # Draw expanding glow behind shield
    for r in range(40, 0, -2):
        alpha = int(70 * (1 - (r / 40) ** 1.3))
        # Draw slightly expanded polygon
        exp_pts = []
        for x, y in shield_pts:
            dx = x - cx
            dy = y - cy
            dist = math.sqrt(dx*dx + dy*dy)
            exp_pts.append((int(x + (dx/dist)*r*scale), int(y + (dy/dist)*r*scale)))
        glow_draw.polygon(exp_pts, fill=(110, 30, 240, alpha)) # Deep nebula violet
    
    for r in range(20, 0, -2):
        alpha = int(90 * (1 - (r / 20)))
        exp_pts = []
        for x, y in shield_pts:
            dx = x - cx
            dy = y - cy
            dist = math.sqrt(dx*dx + dy*dy)
            exp_pts.append((int(x + (dx/dist)*r*scale), int(y + (dy/dist)*r*scale)))
        glow_draw.polygon(exp_pts, fill=(0, 230, 255, alpha)) # Electric cyan rim

    glow_layer = glow_layer.filter(ImageFilter.GaussianBlur(int(22 * scale)))
    img = Image.alpha_composite(img, glow_layer)

    # -------------------------------------------------------------
    # 2. Main Shield Body (Multi-layer bevel & gradient)
    # -------------------------------------------------------------
    shield_layer = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    s_draw = ImageDraw.Draw(shield_layer)

    # Outer border (Dark titanium with cyan highlight)
    s_draw.polygon(shield_pts, fill=(14, 18, 38, 255), outline=(0, 220, 255, 255), width=int(12 * scale))

    # Inner shield background (Cosmic deep indigo)
    inner_pts_1000 = [
        (500, 92),
        (800, 168),
        (855, 510),
        (742, 698),
        (500, 830),
        (258, 698),
        (145, 510),
        (200, 168),
    ]
    inner_pts = [(int(x * scale), int(y * scale)) for x, y in inner_pts_1000]
    s_draw.polygon(inner_pts, fill=(10, 12, 28, 255), outline=(130, 60, 255, 255), width=int(6 * scale))

    # Add gradient nebula inside shield
    nebula = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    n_draw = ImageDraw.Draw(nebula)
    for r in range(int(360 * scale), 0, -6):
        al = int(140 * (1 - (r / (360 * scale)) ** 1.5))
        n_draw.ellipse([cx - r, cy - int(r*0.8), cx + r, cy + int(r*0.8)], fill=(80, 20, 160, al))
    for r in range(int(180 * scale), 0, -4):
        al = int(120 * (1 - (r / (180 * scale))))
        n_draw.ellipse([cx - r, cy - int(r*0.8), cx + r, cy + int(r*0.8)], fill=(0, 180, 240, al))
    nebula = nebula.filter(ImageFilter.GaussianBlur(int(25 * scale)))

    # Mask nebula to inner shield
    inner_mask = Image.new("L", (size, size), 0)
    ImageDraw.Draw(inner_mask).polygon(inner_pts, fill=255)
    shield_layer.paste(nebula, (0, 0), inner_mask)

    # -------------------------------------------------------------
    # 3. The Orion Constellation Background
    # -------------------------------------------------------------
    const_layer = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    c_draw = ImageDraw.Draw(const_layer)

    # Accurate Orion constellation star coordinates mapped to crest
    # Betelgeuse (alpha), Bellatrix (gamma), Belt (Alnitak, Alnilam, Mintaka), Saiph (kappa), Rigel (beta)
    orion_stars = {
        "Betelgeuse": ((340, 240), (255, 140, 30), 12),  # Red supergiant
        "Bellatrix":  ((660, 230), (200, 240, 255), 10), # Blue-white giant
        "Mintaka":    ((570, 390), (240, 250, 255), 8),  # Belt 1
        "Alnilam":    ((500, 420), (255, 255, 255), 9),  # Belt 2 (Center)
        "Alnitak":    ((430, 450), (220, 245, 255), 8),  # Belt 3
        "Saiph":      ((360, 610), (180, 220, 255), 10), # Bottom-left
        "Rigel":      ((640, 590), (140, 210, 255), 13), # Brilliant blue supergiant
    }

    # Constellation lines
    lines = [
        ("Betelgeuse", "Bellatrix"),
        ("Betelgeuse", "Alnitak"),
        ("Bellatrix", "Mintaka"),
        ("Mintaka", "Alnilam"),
        ("Alnilam", "Alnitak"),
        ("Alnitak", "Saiph"),
        ("Mintaka", "Rigel"),
        ("Saiph", "Rigel")
    ]

    for s1, s2 in lines:
        p1 = (int(orion_stars[s1][0][0] * scale), int(orion_stars[s1][0][1] * scale))
        p2 = (int(orion_stars[s2][0][0] * scale), int(orion_stars[s2][0][1] * scale))
        c_draw.line([p1, p2], fill=(0, 220, 255, 110), width=int(3 * scale))

    # Draw stars with 4-point flare
    for name, (pos, col, sz) in orion_stars.items():
        sx, sy = int(pos[0] * scale), int(pos[1] * scale)
        s_rad = int(sz * scale)
        
        # Soft outer glow
        for gr in range(s_rad * 3, s_rad, -2):
            ga = int(120 * (1 - (gr / (s_rad * 3))))
            c_draw.ellipse([sx - gr, sy - gr, sx + gr, sy + gr], fill=(*col, ga))

        # 4-point star spike flare
        spike_len = s_rad * 4
        c_draw.line([(sx - spike_len, sy), (sx + spike_len, sy)], fill=(*col, 220), width=int(2 * scale))
        c_draw.line([(sx, sy - spike_len), (sx, sy + spike_len)], fill=(*col, 220), width=int(2 * scale))

        # Core star
        c_draw.ellipse([sx - s_rad, sy - s_rad, sx + s_rad, sy + s_rad], fill=(255, 255, 255, 255))

    shield_layer.paste(const_layer, (0, 0), inner_mask)

    # -------------------------------------------------------------
    # 4. The "Phantom" Cyber-Hunter Emblem
    # -------------------------------------------------------------
    phantom = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    p_draw = ImageDraw.Draw(phantom)

    # Hood / Cowl outer silhouette (Angular faceted geometric shape)
    cowl_pts_1000 = [
        (500, 180),  # Head crown
        (580, 240),  # Upper right temple
        (650, 350),  # Right cheek flare
        (610, 480),  # Right jaw taper
        (500, 560),  # Chin point
        (390, 480),  # Left jaw taper
        (350, 350),  # Left cheek flare
        (420, 240),  # Upper left temple
    ]
    cowl_pts = [(int(x * scale), int(y * scale)) for x, y in cowl_pts_1000]

    # Metallic cowl with cyan & purple edge
    p_draw.polygon(cowl_pts, fill=(18, 22, 44, 255), outline=(0, 240, 255, 255), width=int(7 * scale))

    # Inner face shadow
    face_pts_1000 = [
        (500, 230),
        (560, 280),
        (590, 370),
        (560, 470),
        (500, 525),
        (440, 470),
        (410, 370),
        (440, 280),
    ]
    face_pts = [(int(x * scale), int(y * scale)) for x, y in face_pts_1000]
    p_draw.polygon(face_pts, fill=(6, 8, 18, 255), outline=(140, 60, 255, 255), width=int(4 * scale))

    # Glowing Cyan Visor (The signature Phantom look)
    # Dual angled aggressive visor slits
    v_left_1000 = [(430, 375), (485, 395), (475, 410), (420, 390)]
    v_right_1000 = [(570, 375), (515, 395), (525, 410), (580, 390)]
    v_left = [(int(x * scale), int(y * scale)) for x, y in v_left_1000]
    v_right = [(int(x * scale), int(y * scale)) for x, y in v_right_1000]

    # Visor Glow
    v_glow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    vg_draw = ImageDraw.Draw(v_glow)
    vg_draw.polygon(v_left, fill=(0, 245, 255, 255))
    vg_draw.polygon(v_right, fill=(0, 245, 255, 255))
    v_glow_blurred = v_glow.filter(ImageFilter.GaussianBlur(int(14 * scale)))
    phantom = Image.alpha_composite(phantom, v_glow_blurred)

    # Visor Core (Brilliant cyan & white core)
    p_draw.polygon(v_left, fill=(220, 255, 255, 255), outline=(0, 220, 255, 255), width=int(2 * scale))
    p_draw.polygon(v_right, fill=(220, 255, 255, 255), outline=(0, 220, 255, 255), width=int(2 * scale))

    # Forehead cybernetic crest / jewel
    crest_gem = [
        (int(500 * scale), int(260 * scale)),
        (int(515 * scale), int(290 * scale)),
        (int(500 * scale), int(320 * scale)),
        (int(485 * scale), int(290 * scale)),
    ]
    p_draw.polygon(crest_gem, fill=(0, 240, 255, 255), outline=(255, 255, 255, 255), width=int(2 * scale))

    # Shoulder armor pads
    left_shoulder = [
        (int(350 * scale), int(420 * scale)),
        (int(260 * scale), int(470 * scale)),
        (int(290 * scale), int(580 * scale)),
        (int(400 * scale), int(530 * scale)),
    ]
    right_shoulder = [
        (int(650 * scale), int(420 * scale)),
        (int(740 * scale), int(470 * scale)),
        (int(710 * scale), int(580 * scale)),
        (int(600 * scale), int(530 * scale)),
    ]
    p_draw.polygon(left_shoulder, fill=(16, 20, 42, 255), outline=(0, 200, 255, 255), width=int(4 * scale))
    p_draw.polygon(right_shoulder, fill=(16, 20, 42, 255), outline=(0, 200, 255, 255), width=int(4 * scale))

    # Merge phantom into shield
    shield_layer = Image.alpha_composite(shield_layer, phantom)

    # -------------------------------------------------------------
    # 5. Cosmic Orbit Ring circling the badge
    # -------------------------------------------------------------
    orbit_canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    o_draw = ImageDraw.Draw(orbit_canvas)

    # Tilted orbital ellipse
    ring_cx, ring_cy = cx, int(520 * scale)
    rx, ry = int(450 * scale), int(160 * scale)
    
    # Outer ring glow
    for rw in range(int(18 * scale), int(4 * scale), -2):
        al = int(70 * (1 - (rw / (18 * scale))))
        o_draw.ellipse([ring_cx - rx - rw, ring_cy - ry - rw, ring_cx + rx + rw, ring_cy + ry + rw], outline=(0, 240, 255, al), width=int(3 * scale))
    
    # Sharp ring
    o_draw.ellipse([ring_cx - rx, ring_cy - ry, ring_cx + rx, ring_cy + ry], outline=(0, 230, 255, 240), width=int(6 * scale))
    
    # Orbiting satellite / mini space probe
    probe_x = ring_cx + int(rx * 0.88)
    probe_y = ring_cy - int(ry * 0.45)
    o_draw.ellipse([probe_x - int(12 * scale), probe_y - int(12 * scale), probe_x + int(12 * scale), probe_y + int(12 * scale)], fill=(255, 255, 255, 255), outline=(255, 180, 40, 255), width=int(3 * scale))

    # Mask orbit ring so it goes behind head and in front of chest
    shield_layer = Image.alpha_composite(shield_layer, orbit_canvas)
    img = Image.alpha_composite(img, shield_layer)

    # -------------------------------------------------------------
    # 6. Futuristic Esports Ribbon Banner (PHANTOM ORION)
    # -------------------------------------------------------------
    banner_layer = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    b_draw = ImageDraw.Draw(banner_layer)

    # Angular ribbon banner coordinates
    by1 = int(760 * scale)
    by2 = int(900 * scale)
    bx_left = int(120 * scale)
    bx_right = int(880 * scale)
    notch = int(35 * scale)

    banner_pts = [
        (bx_left + notch, by1),
        (cx, by1 - int(15 * scale)),
        (bx_right - notch, by1),
        (bx_right, by1 + int(45 * scale)),
        (bx_right - notch, by2),
        (cx, by2 + int(15 * scale)),
        (bx_left + notch, by2),
        (bx_left, by1 + int(45 * scale)),
    ]

    # Banner glow
    b_glow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    bg_draw = ImageDraw.Draw(b_glow)
    bg_draw.polygon(banner_pts, fill=(0, 230, 255, 120))
    b_glow = b_glow.filter(ImageFilter.GaussianBlur(int(20 * scale)))
    img = Image.alpha_composite(img, b_glow)

    # Banner body (Deep Midnight Navy with vibrant Cyan and Purple bevel)
    b_draw.polygon(banner_pts, fill=(10, 14, 32, 255), outline=(0, 240, 255, 255), width=int(8 * scale))

    # Inner bevel line
    inner_banner = [
        (bx_left + notch + int(10 * scale), by1 + int(8 * scale)),
        (cx, by1 - int(5 * scale)),
        (bx_right - notch - int(10 * scale), by1 + int(8 * scale)),
        (bx_right - int(10 * scale), by1 + int(45 * scale)),
        (bx_right - notch - int(10 * scale), by2 - int(8 * scale)),
        (cx, by2 + int(5 * scale)),
        (bx_left + notch + int(10 * scale), by2 - int(8 * scale)),
        (bx_left + int(10 * scale), by1 + int(45 * scale)),
    ]
    b_draw.polygon(inner_banner, outline=(140, 60, 255, 220), width=int(3 * scale))

    # Typography: PHANTOM ORION
    title_text = "PHANTOM ORION"
    bbox = b_draw.textbbox((0, 0), title_text, font=font_title)
    tw = bbox[2] - bbox[0]
    th = bbox[3] - bbox[1]
    tx = cx - tw // 2
    ty = by1 + (by2 - by1) // 2 - th // 2 - int(5 * scale)

    # Drop shadow & text outline for high impact
    for dx, dy in [(-3,0),(3,0),(0,-3),(0,3),(-2,-2),(2,2),(-2,2),(2,-2)]:
        b_draw.text((tx + dx, ty + dy), title_text, font=font_title, fill=(0, 30, 70, 255))
    b_draw.text((tx, ty), title_text, font=font_title, fill=(255, 255, 255, 255))

    # Sub-ribbon tag below banner: RAJSHAHI • BANGLADESH
    sub_text = "RAJSHAHI  •  BANGLADESH"
    s_bbox = b_draw.textbbox((0, 0), sub_text, font=font_sub)
    stw = s_bbox[2] - s_bbox[0]
    sth = s_bbox[3] - s_bbox[1]
    
    sub_pill = [
        cx - stw // 2 - int(30 * scale),
        by2 + int(24 * scale),
        cx + stw // 2 + int(30 * scale),
        by2 + int(24 * scale) + sth + int(16 * scale)
    ]
    b_draw.rounded_rectangle(sub_pill, radius=int(12 * scale), fill=(8, 12, 28, 240), outline=(0, 220, 255, 200), width=int(2 * scale))
    b_draw.text((cx - stw // 2, by2 + int(30 * scale)), sub_text, font=font_sub, fill=(0, 230, 255, 255))

    img = Image.alpha_composite(img, banner_layer)

    # Save transparent badge
    transparent_path = r"F:\SundarbanGuardian\team_phantom_orion_transparent_badge.png"
    img.save(transparent_path, format="PNG")
    print("Saved transparent badge to:", transparent_path)

    # Also save dark solid background edition
    solid_bg = Image.new("RGBA", (size, size), (5, 8, 18, 255))
    # Subtle space starfield on solid edition
    s_draw = ImageDraw.Draw(solid_bg)
    import random
    random.seed(42)
    for _ in range(250):
        sx = random.randint(0, size)
        sy = random.randint(0, size)
        br = random.randint(120, 255)
        rad = random.choice([1, 2, 2, 3])
        s_draw.ellipse([sx, sy, sx + rad, sy + rad], fill=(br, br, 255, br))

    solid_final = Image.alpha_composite(solid_bg, img).convert("RGB")
    solid_path = r"F:\SundarbanGuardian\team_phantom_orion_logo_badge.png"
    solid_final.save(solid_path, format="PNG")
    print("Saved solid logo badge to:", solid_path)

    return img

if __name__ == "__main__":
    create_phantom_orion_logo(2048)
