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
font_segoe = lambda s: get_font("segoeui.ttf", s)

# ==============================================================================
# VARIATION 1: Cinematic Deep Horizon (The 1st-image style, refined for NovaKnight)
# ==============================================================================
def create_variation_1():
    print("Generating Variation 1 (Cinematic Deep Horizon)...")
    base_src = r"C:\Users\RAJ\.gemini\antigravity-ide\brain\eb7e8551-db12-437a-9a17-f5e56446f76c\space_apps_team_1791230553955.jpg"
    bg = Image.open(base_src).convert("RGBA")
    bw, bh = bg.size # 1376 x 768

    # Clean the old logo on the right side using deep space starfield
    jwst_path = "nasa_jwst_deepfield.jpg"
    if os.path.exists(jwst_path):
        jwst = Image.open(jwst_path).convert("RGBA")
        jwst_patch = jwst.crop((800, 800, 2000, 2000)).resize((650, 600))
        
        # Soft radial alpha mask
        mask = Image.new("L", (650, 600), 0)
        draw_mask = ImageDraw.Draw(mask)
        cx, cy = 325, 290
        for r in range(290, 0, -3):
            val = int(255 * (1 - (r / 290) ** 1.8))
            draw_mask.ellipse([cx - r, cy - r, cx + r, cy + r], fill=min(255, val * 2))
        mask = mask.filter(ImageFilter.GaussianBlur(25))
        bg.paste(jwst_patch, (730, 20), mask)

    # Upscale base composite to 4K first for crisp detail
    bg_4k = bg.resize((TARGET_W, TARGET_H), Image.Resampling.LANCZOS)
    
    # Load authentic NovaKnight logo
    raw_logo_path = r"C:\Users\RAJ\.gemini\antigravity-ide\brain\eb7e8551-db12-437a-9a17-f5e56446f76c\.user_uploaded\media_1791230780320.png"
    logo = Image.open(raw_logo_path).convert("RGBA")
    
    # Clean transparent background outside logo
    arr = np.array(logo)
    r, g, b, a = arr[:,:,0], arr[:,:,1], arr[:,:,2], arr[:,:,3]
    black_mask = (r < 22) & (g < 22) & (b < 22)
    from collections import deque
    h, w = black_mask.shape
    visited = np.zeros((h, w), dtype=bool)
    q = deque()
    for x in range(w):
        if black_mask[0, x]: q.append((0, x)); visited[0, x] = True
        if black_mask[h-1, x]: q.append((h-1, x)); visited[h-1, x] = True
    for y in range(h):
        if black_mask[y, 0]: q.append((y, 0)); visited[y, 0] = True
        if black_mask[y, w-1]: q.append((y, w-1)); visited[y, w-1] = True
    while q:
        cy, cx = q.popleft()
        for dy, dx in [(-1,0), (1,0), (0,-1), (0,1)]:
            ny, nx = cy + dy, cx + dx
            if 0 <= ny < h and 0 <= nx < w and not visited[ny, nx] and black_mask[ny, nx]:
                visited[ny, nx] = True
                q.append((ny, nx))
    arr[visited, 3] = 0
    clean_logo = Image.fromarray(arr)

    # Size and place NovaKnight logo on the right side
    logo_w = 1150
    logo_h = int(clean_logo.height * (logo_w / clean_logo.width))
    logo_resized = clean_logo.resize((logo_w, logo_h), Image.Resampling.LANCZOS)
    
    # Center coordinates for logo on right side
    target_cx = 2850
    target_cy = 920
    pos_x = target_cx - logo_w // 2
    pos_y = target_cy - logo_h // 2

    # Create glowing aura behind NovaKnight logo
    glow_canvas = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(glow_canvas)
    
    # Subtle cosmic nebula halo
    for r_outer in range(650, 0, -10):
        alpha = int(45 * (1 - (r_outer / 650) ** 1.5))
        glow_draw.ellipse(
            [target_cx - r_outer, target_cy - r_outer, target_cx + r_outer, target_cy + r_outer],
            fill=(255, 170, 40, alpha) # Warm amber/gold glow matching orbit ring
        )
    for r_inner in range(450, 0, -8):
        alpha = int(60 * (1 - (r_inner / 450) ** 1.3))
        glow_draw.ellipse(
            [target_cx - r_inner, target_cy - r_inner, target_cx + r_inner, target_cy + r_inner],
            fill=(0, 180, 255, alpha) # Cyan accent matching Earth limb
        )
    glow_canvas = glow_canvas.filter(ImageFilter.GaussianBlur(40))
    bg_4k = Image.alpha_composite(bg_4k, glow_canvas)

    # Overlay NovaKnight logo
    bg_4k.paste(logo_resized, (pos_x, pos_y), logo_resized)

    # Draw refined, glowing "Rajshahi, Bangladesh" sci-fi badge below logo
    overlay = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)

    badge_text = "RAJSHAHI, BANGLADESH"
    f_badge = font_bahn_bold(52)
    bbox = draw.textbbox((0, 0), badge_text, font=f_badge)
    tw = bbox[2] - bbox[0]
    th = bbox[3] - bbox[1]
    
    badge_x = target_cx - tw // 2
    badge_y = target_cy + logo_h // 2 + 50
    
    # Pill background for location
    pill_pad_x = 45
    pill_pad_y = 18
    pill_rect = [
        badge_x - pill_pad_x - 30, # extra space for pin dot
        badge_y - pill_pad_y,
        badge_x + tw + pill_pad_x,
        badge_y + th + pill_pad_y + 8
    ]
    
    # Glassmorphic pill
    draw.rounded_rectangle(pill_rect, radius=25, fill=(10, 25, 55, 170), outline=(0, 210, 255, 180), width=3)
    
    # Glowing pin dot
    pin_x = badge_x - 15
    pin_y = badge_y + th // 2
    draw.ellipse([pin_x - 8, pin_y - 8, pin_x + 8, pin_y + 8], fill=(255, 80, 70, 255), outline=(255, 255, 255, 220), width=2)
    
    # Text
    draw.text((badge_x + 12, badge_y), badge_text, font=f_badge, fill=(240, 250, 255, 255))
    
    # Team title tag above
    team_tag = "TEAM NOVAKNIGHT"
    f_team = font_bahn_bold(68)
    t_bbox = draw.textbbox((0, 0), team_tag, font=f_team)
    tt_w = t_bbox[2] - t_bbox[0]
    draw.text((target_cx - tt_w // 2, target_cy - logo_h // 2 - 95), team_tag, font=f_team, fill=(255, 255, 255, 240))

    final_comp = Image.alpha_composite(bg_4k, overlay)
    
    # Final sharpening & contrast tuning for 4K video display
    final_rgb = final_comp.convert("RGB")
    final_sharp = final_rgb.filter(ImageFilter.UnsharpMask(radius=1.5, percent=115, threshold=3))
    enhancer = ImageEnhance.Contrast(final_sharp)
    res = enhancer.enhance(1.03)

    out_path = r"F:\SundarbanGuardian\nasa_space_apps_2026_novaknight_cinematic_4k.png"
    out_artifact = r"C:\Users\RAJ\.gemini\antigravity-ide\brain\eb7e8551-db12-437a-9a17-f5e56446f76c\nasa_space_apps_2026_novaknight_cinematic_4k.png"
    res.save(out_path, format="PNG", compress_level=6)
    res.save(out_artifact, format="PNG", compress_level=6)
    print("Variation 1 saved successfully to:", out_path)

# ==============================================================================
# VARIATION 2: Orbital Sunrise (Photorealistic NASA ISS Earth + Dual Emblems)
# ==============================================================================
def create_variation_2():
    print("Generating Variation 2 (Orbital Sunrise)...")
    iss_path = "nasa_iss_earth.jpg"
    jwst_path = "nasa_jwst_deepfield.jpg"
    
    # Load ISS Earth sunrise
    iss = Image.open(iss_path).convert("RGBA")
    iss_4k = iss.resize((TARGET_W, TARGET_H), Image.Resampling.LANCZOS)
    
    # Blend JWST stars into the upper space section
    if os.path.exists(jwst_path):
        jwst = Image.open(jwst_path).convert("RGBA")
        jwst_4k = jwst.crop((0, 0, jwst.width, int(jwst.height * 0.6))).resize((TARGET_W, TARGET_H), Image.Resampling.LANCZOS)
        
        # Gradient mask so stars appear in upper space, fade down to Earth sunrise
        grad = Image.new("L", (TARGET_W, TARGET_H), 0)
        draw_g = ImageDraw.Draw(grad)
        for y in range(TARGET_H):
            if y < 900:
                alpha = int(140 * (1 - (y / 900)))
            else:
                alpha = 0
            draw_g.line([(0, y), (TARGET_W, y)], fill=alpha)
        iss_4k = Image.composite(jwst_4k, iss_4k, grad)

    # Add dark vignette across center-top to make logos pop with supreme contrast
    vignette = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    draw_v = ImageDraw.Draw(vignette)
    for y in range(TARGET_H):
        if y < 1400:
            val = int(180 * (1 - (y / 1400)**1.2))
            draw_v.line([(0, y), (TARGET_W, y)], fill=(5, 8, 20, val))
    iss_4k = Image.alpha_composite(iss_4k, vignette)

    # 1. Official NASA Space Apps Challenge Circular Logo on Left
    nasa_banner_path = r"C:\Users\RAJ\.gemini\antigravity-ide\brain\eb7e8551-db12-437a-9a17-f5e56446f76c\.user_uploaded\media_1791230380844.png"
    nasa_img = Image.open(nasa_banner_path).convert("RGBA")
    
    # Crop NASA circle: x: 190 to 415, y: 15 to 220
    nasa_crop = nasa_img.crop((190, 15, 415, 220))
    # Make dark blue background transparent
    arr_n = np.array(nasa_crop)
    # Background is roughly rgb (23, 21, 64)
    r, g, b, a = arr_n[:,:,0], arr_n[:,:,1], arr_n[:,:,2], arr_n[:,:,3]
    bg_n_mask = (r < 35) & (g < 35) & (b < 75)
    arr_n[bg_n_mask, 3] = 0
    nasa_clean = Image.fromarray(arr_n)

    nasa_w = 950
    nasa_h = int(nasa_clean.height * (nasa_w / nasa_clean.width))
    nasa_resized = nasa_clean.resize((nasa_w, nasa_h), Image.Resampling.LANCZOS)
    
    # Position NASA logo on left
    nasa_cx = 950
    nasa_cy = 780
    
    # Soft backlight for NASA logo
    nasa_glow = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    d_ng = ImageDraw.Draw(nasa_glow)
    for r_g in range(500, 0, -8):
        al = int(35 * (1 - (r_g / 500)**1.2))
        d_ng.ellipse([nasa_cx - r_g, nasa_cy - r_g, nasa_cx + r_g, nasa_cy + r_g], fill=(0, 160, 255, al))
    nasa_glow = nasa_glow.filter(ImageFilter.GaussianBlur(30))
    iss_4k = Image.alpha_composite(iss_4k, nasa_glow)
    
    iss_4k.paste(nasa_resized, (nasa_cx - nasa_w // 2, nasa_cy - nasa_h // 2), nasa_resized)

    # 2. NovaKnight Logo on Right
    badge_path = r"F:\SundarbanGuardian\team_novaknight_transparent_badge.png"
    badge = Image.open(badge_path).convert("RGBA")
    nk_w = 1100
    nk_h = int(badge.height * (nk_w / badge.width))
    nk_resized = badge.resize((nk_w, nk_h), Image.Resampling.LANCZOS)
    
    nk_cx = 2890
    nk_cy = 780
    
    nk_glow = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    d_nkg = ImageDraw.Draw(nk_glow)
    for r_g in range(550, 0, -8):
        al = int(40 * (1 - (r_g / 550)**1.2))
        d_nkg.ellipse([nk_cx - r_g, nk_cy - r_g, nk_cx + r_g, nk_cy + r_g], fill=(255, 175, 45, al))
    nk_glow = nk_glow.filter(ImageFilter.GaussianBlur(35))
    iss_4k = Image.alpha_composite(iss_4k, nk_glow)

    iss_4k.paste(nk_resized, (nk_cx - nk_w // 2, nk_cy - nk_h // 2), nk_resized)

    # 3. Bottom Aerospace Banner: Team & Location
    banner_layer = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    d_b = ImageDraw.Draw(banner_layer)
    
    # Sleek bottom glass panel
    panel_y1 = 1720
    panel_y2 = 1960
    d_b.rounded_rectangle([250, panel_y1, TARGET_W - 250, panel_y2], radius=30, fill=(8, 14, 30, 205), outline=(0, 200, 255, 160), width=4)
    
    # Title & Location in banner
    f_main = font_bahn_bold(66)
    f_sub = font_bahn_bold(44)
    
    main_txt = "NASA SPACE APPS CHALLENGE 2026"
    sub_txt = "TEAM NOVAKNIGHT   •   RAJSHAHI DIVISION, BANGLADESH"
    
    b_main = d_b.textbbox((0, 0), main_txt, font=f_main)
    mw = b_main[2] - b_main[0]
    d_b.text((TARGET_W // 2 - mw // 2, panel_y1 + 35), main_txt, font=f_main, fill=(255, 255, 255, 255))
    
    b_sub = d_b.textbbox((0, 0), sub_txt, font=f_sub)
    sw = b_sub[2] - b_sub[0]
    d_b.text((TARGET_W // 2 - sw // 2, panel_y1 + 125), sub_txt, font=f_sub, fill=(0, 220, 255, 240))

    final_v2 = Image.alpha_composite(iss_4k, banner_layer).convert("RGB")
    final_v2_sharp = final_v2.filter(ImageFilter.UnsharpMask(radius=1.5, percent=115, threshold=3))
    
    out_path_v2 = r"F:\SundarbanGuardian\nasa_space_apps_2026_novaknight_orbital_hero_4k.png"
    out_art_v2 = r"C:\Users\RAJ\.gemini\antigravity-ide\brain\eb7e8551-db12-437a-9a17-f5e56446f76c\nasa_space_apps_2026_novaknight_orbital_hero_4k.png"
    final_v2_sharp.save(out_path_v2, format="PNG", compress_level=6)
    final_v2_sharp.save(out_art_v2, format="PNG", compress_level=6)
    print("Variation 2 saved successfully to:", out_path_v2)

if __name__ == "__main__":
    create_variation_1()
    create_variation_2()
