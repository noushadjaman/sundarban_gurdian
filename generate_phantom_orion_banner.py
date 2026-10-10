import os
import numpy as np
from PIL import Image, ImageFilter, ImageDraw, ImageFont, ImageEnhance

TARGET_W = 3840
TARGET_H = 2160

def get_font(font_name, size):
    try:
        return ImageFont.truetype(f"C:/Windows/Fonts/{font_name}", size)
    except Exception:
        try:
            return ImageFont.truetype("arial.ttf", size)
        except Exception:
            return ImageFont.load_default()

font_bahn_bold = lambda s: get_font("bahnschrift.ttf", s)
font_segoe_bold = lambda s: get_font("segoeuib.ttf", s)

def create_phantom_orion_banner():
    print("Generating Phantom Orion 4K Cinematic Banner...")
    base_src = r"C:\Users\RAJ\.gemini\antigravity-ide\brain\eb7e8551-db12-437a-9a17-f5e56446f76c\space_apps_team_1791230553955.jpg"
    bg = Image.open(base_src).convert("RGBA")
    
    # 1. Clean old logo on the right side using JWST deep space
    jwst_path = "nasa_jwst_deepfield.jpg"
    if os.path.exists(jwst_path):
        jwst = Image.open(jwst_path).convert("RGBA")
        jwst_patch = jwst.crop((600, 600, 2200, 2200)).resize((680, 620))
        
        mask = Image.new("L", (680, 620), 0)
        draw_mask = ImageDraw.Draw(mask)
        cx, cy = 340, 300
        for r in range(300, 0, -3):
            val = int(255 * (1 - (r / 300) ** 1.8))
            draw_mask.ellipse([cx - r, cy - r, cx + r, cy + r], fill=min(255, val * 2))
        mask = mask.filter(ImageFilter.GaussianBlur(25))
        bg.paste(jwst_patch, (710, 10), mask)

    # Upscale base composite to 4K
    bg_4k = bg.resize((TARGET_W, TARGET_H), Image.Resampling.LANCZOS)

    # 2. Load the Phantom Orion badge
    badge_path = r"F:\SundarbanGuardian\team_phantom_orion_transparent_badge.png"
    badge = Image.open(badge_path).convert("RGBA")

    # Resize badge to fit right side harmoniously with NASA logo on left
    badge_w = 1250
    badge_h = int(badge.height * (badge_w / badge.width))
    badge_resized = badge.resize((badge_w, badge_h), Image.Resampling.LANCZOS)

    target_cx = 2860
    target_cy = 930
    pos_x = target_cx - badge_w // 2
    pos_y = target_cy - badge_h // 2

    # 3. Create cosmic spectral purple & cyan nebula aura behind badge
    aura = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    d_aura = ImageDraw.Draw(aura)

    for r_out in range(700, 0, -10):
        al = int(55 * (1 - (r_out / 700)**1.4))
        d_aura.ellipse([target_cx - r_out, target_cy - r_out, target_cx + r_out, target_cy + r_out], fill=(120, 40, 240, al))

    for r_in in range(480, 0, -8):
        al = int(65 * (1 - (r_in / 480)**1.2))
        d_aura.ellipse([target_cx - r_in, target_cy - r_in, target_cx + r_in, target_cy + r_in], fill=(0, 210, 255, al))

    aura = aura.filter(ImageFilter.GaussianBlur(40))
    bg_4k = Image.alpha_composite(bg_4k, aura)

    # Paste Phantom Orion badge
    bg_4k.paste(badge_resized, (pos_x, pos_y), badge_resized)

    # 4. Sci-Fi HUD & Location Badge
    overlay = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)

    # Top Team Header Tag
    team_header = "TEAM PHANTOM ORION"
    f_header = font_bahn_bold(68)
    h_bbox = draw.textbbox((0, 0), team_header, font=f_header)
    hw = h_bbox[2] - h_bbox[0]
    draw.text((target_cx - hw // 2, pos_y - 95), team_header, font=f_header, fill=(255, 255, 255, 245))

    # Location pill badge
    loc_text = "RAJSHAHI, BANGLADESH"
    f_loc = font_bahn_bold(52)
    l_bbox = draw.textbbox((0, 0), loc_text, font=f_loc)
    lw = l_bbox[2] - l_bbox[0]
    lh = l_bbox[3] - l_bbox[1]

    loc_x = target_cx - lw // 2
    loc_y = pos_y + badge_h + 35

    pill_rect = [
        loc_x - 70,
        loc_y - 18,
        loc_x + lw + 45,
        loc_y + lh + 22
    ]
    draw.rounded_rectangle(pill_rect, radius=25, fill=(10, 16, 38, 190), outline=(0, 230, 255, 200), width=3)

    # Glowing red/white location beacon pin
    pin_cx = loc_x - 35
    pin_cy = loc_y + lh // 2
    draw.ellipse([pin_cx - 9, pin_cy - 9, pin_cx + 9, pin_cy + 9], fill=(255, 75, 75, 255), outline=(255, 255, 255, 220), width=2)

    draw.text((loc_x, loc_y), loc_text, font=f_loc, fill=(240, 250, 255, 255))

    final_comp = Image.alpha_composite(bg_4k, overlay).convert("RGB")
    final_sharp = final_comp.filter(ImageFilter.UnsharpMask(radius=1.5, percent=120, threshold=3))
    enhancer = ImageEnhance.Contrast(final_sharp)
    res = enhancer.enhance(1.03)

    out_file = r"F:\SundarbanGuardian\nasa_space_apps_2026_phantom_orion_cinematic_4k.png"
    out_artifact = r"C:\Users\RAJ\.gemini\antigravity-ide\brain\eb7e8551-db12-437a-9a17-f5e56446f76c\nasa_space_apps_2026_phantom_orion_cinematic_4k.png"
    res.save(out_file, format="PNG", compress_level=6)
    res.save(out_artifact, format="PNG", compress_level=6)
    print("Phantom Orion 4K banner saved successfully to:", out_file)

if __name__ == "__main__":
    create_phantom_orion_banner()
