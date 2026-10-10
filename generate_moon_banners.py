import os
import numpy as np
from PIL import Image, ImageFilter, ImageDraw, ImageFont, ImageEnhance

TARGET_W = 3840
TARGET_H = 2160

def get_font(name, sz):
    try:
        return ImageFont.truetype(f"C:/Windows/Fonts/{name}", sz)
    except Exception:
        try:
            return ImageFont.truetype("arial.ttf", sz)
        except Exception:
            return ImageFont.load_default()

font_bahn_bold = lambda s: get_font("bahnschrift.ttf", s)
font_segoe_bold = lambda s: get_font("segoeuib.ttf", s)

# ==============================================================================
# PREPARE CLEAN MOON 4K BACKGROUND
# ==============================================================================
def get_clean_moon_4k():
    moon_src = r"C:\Users\RAJ\.gemini\antigravity-ide\brain\eb7e8551-db12-437a-9a17-f5e56446f76c\.user_uploaded\media_1791232059772.png"
    moon_img = Image.open(moon_src).convert("RGBA")
    w, h = moon_img.size
    arr = np.array(moon_img)
    # Remove window controls in top right (x > 900, y < 35)
    arr[0:35, 900:w] = [0, 0, 0, 255]
    
    cleaned = Image.fromarray(arr)
    # High-quality Lanczos upscale to 4K
    moon_4k = cleaned.resize((TARGET_W, TARGET_H), Image.Resampling.LANCZOS)
    # Sharpen to emphasize lunar craters and stars
    moon_4k = moon_4k.filter(ImageFilter.UnsharpMask(radius=1.2, percent=115, threshold=2))
    return moon_4k

# ==============================================================================
# PREPARE CLEAN NASA SPACE APPS CHALLENGE LOGO
# ==============================================================================
def get_clean_nasa_logo():
    nasa_path = "clean_nasa_logo.png"
    if not os.path.exists(nasa_path):
        nasa_banner = r"C:\Users\RAJ\.gemini\antigravity-ide\brain\eb7e8551-db12-437a-9a17-f5e56446f76c\.user_uploaded\media_1791230380844.png"
        nasa_img = Image.open(nasa_banner).convert("RGBA")
        crop = nasa_img.crop((185, 10, 420, 225))
        arr_c = np.array(crop)
        mask_bg = (arr_c[:,:,0] < 40) & (arr_c[:,:,1] < 40) & (arr_c[:,:,2] < 75)
        arr_c[mask_bg, 3] = 0
        clean_nasa = Image.fromarray(arr_c)
        clean_nasa.save(nasa_path)
    return Image.open(nasa_path).convert("RGBA")

# ==============================================================================
# VARIATION 1: Moon Cinematic Video Intro Card (Left Moon, Hero Right Layout)
# ==============================================================================
def create_moon_variation_1():
    print("Generating Variation 1 (Moon Cinematic Video Intro)...")
    bg = get_clean_moon_4k()
    
    # Target center for the right side elements: x = 2750
    right_cx = 2780
    
    # 1. NASA Space Apps Challenge circular logo at upper right
    nasa_logo = get_clean_nasa_logo()
    nw = 680
    nh = int(nasa_logo.height * (nw / nasa_logo.width))
    nasa_resized = nasa_logo.resize((nw, nh), Image.Resampling.LANCZOS)
    
    nasa_y = 120
    nasa_x = right_cx - nw // 2
    
    # NASA logo ambient blue glow
    glow_nasa = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    d_ng = ImageDraw.Draw(glow_nasa)
    for r in range(320, 0, -6):
        al = int(45 * (1 - (r / 320) ** 1.3))
        d_ng.ellipse([right_cx - r, (nasa_y + nh//2) - r, right_cx + r, (nasa_y + nh//2) + r], fill=(0, 160, 255, al))
    glow_nasa = glow_nasa.filter(ImageFilter.GaussianBlur(25))
    bg = Image.alpha_composite(bg, glow_nasa)
    bg.paste(nasa_resized, (nasa_x, nasa_y), nasa_resized)
    
    # "GLOBAL CHALLENGE 2026" under NASA logo
    draw_nasa_txt = ImageDraw.Draw(bg)
    f_gc = font_bahn_bold(38)
    gc_text = "GLOBAL CHALLENGE 2026"
    gc_bbox = draw_nasa_txt.textbbox((0, 0), gc_text, font=f_gc)
    gc_w = gc_bbox[2] - gc_bbox[0]
    draw_nasa_txt.text((right_cx - gc_w // 2, nasa_y + nh + 12), gc_text, font=f_gc, fill=(0, 220, 255, 240))

    # 2. Team Phantom Orion Astronaut Badge in Center-Right
    badge_path = r"F:\SundarbanGuardian\team_phantom_orion_uploaded_transparent.png"
    badge = Image.open(badge_path).convert("RGBA")
    
    bw = 1180
    bh = int(badge.height * (bw / badge.width))
    badge_resized = badge.resize((bw, bh), Image.Resampling.LANCZOS)
    
    badge_cx = right_cx
    badge_cy = 1220
    pos_bx = badge_cx - bw // 2
    pos_by = badge_cy - bh // 2
    
    # Soft cosmic backlight aura behind badge
    aura = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    d_aura = ImageDraw.Draw(aura)
    for r in range(650, 0, -10):
        al = int(55 * (1 - (r / 650) ** 1.4))
        d_aura.ellipse([badge_cx - r, badge_cy - r, badge_cx + r, badge_cy + r], fill=(20, 90, 220, al))
    for r in range(450, 0, -8):
        al = int(65 * (1 - (r / 450) ** 1.2))
        d_aura.ellipse([badge_cx - r, badge_cy - r, badge_cx + r, badge_cy + r], fill=(0, 220, 255, al))
    aura = aura.filter(ImageFilter.GaussianBlur(35))
    bg = Image.alpha_composite(bg, aura)
    
    bg.paste(badge_resized, (pos_bx, pos_by), badge_resized)
    
    # 3. Location Badge: Rajshahi, Bangladesh
    overlay = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)
    
    loc_text = "RAJSHAHI, BANGLADESH"
    f_loc = font_bahn_bold(52)
    l_bbox = draw.textbbox((0, 0), loc_text, font=f_loc)
    lw = l_bbox[2] - l_bbox[0]
    lh = l_bbox[3] - l_bbox[1]
    
    loc_x = badge_cx - lw // 2
    loc_y = pos_by + bh + 45
    
    # Glassmorphic pill
    pill_rect = [
        loc_x - 70,
        loc_y - 20,
        loc_x + lw + 45,
        loc_y + lh + 24
    ]
    draw.rounded_rectangle(pill_rect, radius=28, fill=(8, 16, 36, 210), outline=(0, 220, 255, 200), width=4)
    
    # Glowing beacon pin dot
    pin_cx = loc_x - 35
    pin_cy = loc_y + lh // 2
    draw.ellipse([pin_cx - 10, pin_cy - 10, pin_cx + 10, pin_cy + 10], fill=(255, 75, 75, 255), outline=(255, 255, 255, 220), width=2)
    draw.text((loc_x, loc_y), loc_text, font=f_loc, fill=(240, 250, 255, 255))

    final = Image.alpha_composite(bg, overlay).convert("RGB")
    final_sharp = final.filter(ImageFilter.UnsharpMask(radius=1.5, percent=115, threshold=3))
    
    out_path = r"F:\SundarbanGuardian\nasa_space_apps_2026_phantom_orion_moon_cinematic_4k.png"
    out_art = r"C:\Users\RAJ\.gemini\antigravity-ide\brain\eb7e8551-db12-437a-9a17-f5e56446f76c\nasa_space_apps_2026_phantom_orion_moon_cinematic_4k.png"
    final_sharp.save(out_path, format="PNG", compress_level=6)
    final_sharp.save(out_art, format="PNG", compress_level=6)
    print("Variation 1 saved to:", out_path)

# ==============================================================================
# VARIATION 2: Moon Aerospace Mission Presentation (Dual Hero & Lower Telemetry)
# ==============================================================================
def create_moon_variation_2():
    print("Generating Variation 2 (Moon Aerospace Mission Presentation)...")
    bg = get_clean_moon_4k()
    
    # Symmetrical Dual Emblems on the right space section
    right_cx = 2780
    
    # 1. NASA circular logo (left of the pair)
    nasa_logo = get_clean_nasa_logo()
    nw = 850
    nh = int(nasa_logo.height * (nw / nasa_logo.width))
    nasa_resized = nasa_logo.resize((nw, nh), Image.Resampling.LANCZOS)
    
    # 2. Phantom Orion logo (right of the pair)
    badge_path = r"F:\SundarbanGuardian\team_phantom_orion_uploaded_transparent.png"
    badge = Image.open(badge_path).convert("RGBA")
    bw = 1000
    bh = int(badge.height * (bw / badge.width))
    badge_resized = badge.resize((bw, bh), Image.Resampling.LANCZOS)
    
    # Positions
    nasa_cx = 2220
    badge_cx = 3220
    center_y = 880
    
    # Auras
    aura = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    d_aura = ImageDraw.Draw(aura)
    # NASA aura
    for r in range(450, 0, -8):
        d_aura.ellipse([nasa_cx - r, center_y - r, nasa_cx + r, center_y + r], fill=(0, 150, 255, int(35 * (1 - r/450))))
    # Badge aura
    for r in range(500, 0, -8):
        d_aura.ellipse([badge_cx - r, center_y - r, badge_cx + r, center_y + r], fill=(30, 100, 240, int(45 * (1 - r/500))))
    aura = aura.filter(ImageFilter.GaussianBlur(30))
    bg = Image.alpha_composite(bg, aura)
    
    bg.paste(nasa_resized, (nasa_cx - nw // 2, center_y - nh // 2), nasa_resized)
    bg.paste(badge_resized, (badge_cx - bw // 2, center_y - bh // 2), badge_resized)
    
    # 3. Bottom Aerospace Telemetry Banner
    banner = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    d_b = ImageDraw.Draw(banner)
    
    by1, by2 = 1680, 1960
    bx1, bx2 = 1650, TARGET_W - 120
    d_b.rounded_rectangle([bx1, by1, bx2, by2], radius=32, fill=(8, 14, 32, 215), outline=(0, 210, 255, 180), width=4)
    
    f_main = font_bahn_bold(64)
    f_sub = font_bahn_bold(44)
    
    m_txt = "NASA SPACE APPS CHALLENGE 2026"
    s_txt = "TEAM PHANTOM ORION   •   RAJSHAHI, BANGLADESH"
    
    bm = d_b.textbbox((0, 0), m_txt, font=f_main)
    bs = d_b.textbbox((0, 0), s_txt, font=f_sub)
    
    panel_cx = (bx1 + bx2) // 2
    d_b.text((panel_cx - (bm[2] - bm[0]) // 2, by1 + 45), m_txt, font=f_main, fill=(255, 255, 255, 255))
    d_b.text((panel_cx - (bs[2] - bs[0]) // 2, by1 + 145), s_txt, font=f_sub, fill=(0, 220, 255, 240))
    
    final_v2 = Image.alpha_composite(bg, banner).convert("RGB")
    final_v2_sharp = final_v2.filter(ImageFilter.UnsharpMask(radius=1.5, percent=115, threshold=3))
    
    out_path_v2 = r"F:\SundarbanGuardian\nasa_space_apps_2026_phantom_orion_moon_hero_4k.png"
    out_art_v2 = r"C:\Users\RAJ\.gemini\antigravity-ide\brain\eb7e8551-db12-437a-9a17-f5e56446f76c\nasa_space_apps_2026_phantom_orion_moon_hero_4k.png"
    final_v2_sharp.save(out_path_v2, format="PNG", compress_level=6)
    final_v2_sharp.save(out_art_v2, format="PNG", compress_level=6)
    print("Variation 2 saved to:", out_path_v2)

if __name__ == "__main__":
    create_moon_variation_1()
    create_moon_variation_2()
