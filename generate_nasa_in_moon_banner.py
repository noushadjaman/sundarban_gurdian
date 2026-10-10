import os
import numpy as np
import math
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

def create_nasa_in_moon_banner():
    print("Creating 4K banner with NASA logo inside the Moon and Phantom Orion badge...")
    
    # 1. Load Moon background and clean window controls in top right
    moon_src = r"C:\Users\RAJ\.gemini\antigravity-ide\brain\eb7e8551-db12-437a-9a17-f5e56446f76c\.user_uploaded\media_1791232059772.png"
    im_moon = Image.open(moon_src).convert("RGBA")
    w, h = im_moon.size
    arr_m = np.array(im_moon)
    arr_m[0:35, 920:w] = [0, 0, 0, 255]
    
    moon_4k = Image.fromarray(arr_m).resize((TARGET_W, TARGET_H), Image.Resampling.LANCZOS)
    moon_4k = moon_4k.filter(ImageFilter.UnsharpMask(radius=1.2, percent=115, threshold=2))

    # Center coordinates
    moon_cx = 980
    moon_cy = 1040
    
    badge_cx = 2850
    badge_cy = 1020

    # -------------------------------------------------------------------------
    # 2. PLACE NASA SPACE APPS CHALLENGE LOGO INSIDE THE MOON (Left side)
    # -------------------------------------------------------------------------
    nasa_target_w = 980
    nasa_target_h = int(nasa_target_w * 0.92) # aspect ratio of circular logo
    
    # To make the NASA logo 100% visible against lunar craters and texture:
    # Create an elegant circular glassmorphic / dark lunar nebula backing
    backing = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    d_back = ImageDraw.Draw(backing)
    
    # Soft radial dark contrast halo
    r_dark = int(nasa_target_w * 0.62)
    for r in range(r_dark, 0, -8):
        alpha = int(220 * (1 - (r / r_dark) ** 2.0))
        d_back.ellipse([moon_cx - r, moon_cy - r, moon_cx + r, moon_cy + r], fill=(6, 10, 24, alpha))
        
    # Subtle cyan & blue cosmic rim glow
    r_rim = int(nasa_target_w * 0.52)
    for r in range(r_rim + 30, r_rim - 10, -4):
        alpha = int(65 * (1 - abs(r - r_rim) / 30))
        d_back.ellipse([moon_cx - r, moon_cy - r, moon_cx + r, moon_cy + r], fill=(0, 180, 255, alpha))
        
    backing = backing.filter(ImageFilter.GaussianBlur(25))
    moon_4k = Image.alpha_composite(moon_4k, backing)

    # Render high-resolution NASA logo elements
    nasa_layer = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    d_nasa = ImageDraw.Draw(nasa_layer)
    
    # Draw circular emblem outline
    circle_r = int(nasa_target_w * 0.44)
    stroke_w = 14
    
    # Satellite at angle -55 deg
    angle_sat = math.radians(-55)
    sat_cx = moon_cx + int(circle_r * math.cos(angle_sat))
    sat_cy = moon_cy + int(circle_r * math.sin(angle_sat))
    sat_r = 55
    
    # Draw main circle with satellite gap
    d_nasa.arc(
        [moon_cx - circle_r, moon_cy - circle_r, moon_cx + circle_r, moon_cy + circle_r],
        start=326, end=278, fill=(255, 255, 255, 255), width=stroke_w
    )
    # Satellite circle
    d_nasa.ellipse([sat_cx - sat_r, sat_cy - sat_r, sat_cx + sat_r, sat_cy + sat_r], outline=(255, 255, 255, 255), width=stroke_w)
    # Satellite radiating rays
    for ray_deg in [-80, -60, -40, -20]:
        rad = math.radians(ray_deg)
        p1 = (sat_cx + int(sat_r * 0.95 * math.cos(rad)), sat_cy + int(sat_r * 0.95 * math.sin(rad)))
        p2 = (sat_cx + int(sat_r * 2.2 * math.cos(rad)), sat_cy + int(sat_r * 2.2 * math.sin(rad)))
        d_nasa.line([p1, p2], fill=(255, 255, 255, 255), width=stroke_w)

    # NASA Typography
    f_nasa = font_bahn_bold(85)
    f_space_apps = font_bahn_bold(172)
    f_challenge = font_bahn_bold(92)
    f_year = font_bahn_bold(52)

    # "NASA" in red/orange with subtle drop shadow
    n_txt = "N A S A"
    nb = d_nasa.textbbox((0, 0), n_txt, font=f_nasa)
    nx = moon_cx - (nb[2] - nb[0]) // 2
    ny = moon_cy - 240
    # Shadow
    for dx, dy in [(-2,0),(2,0),(0,-2),(0,2)]:
        d_nasa.text((nx + dx, ny + dy), n_txt, font=f_nasa, fill=(0, 0, 0, 200))
    d_nasa.text((nx, ny), n_txt, font=f_nasa, fill=(235, 55, 40, 255))

    # "SPACE APPS" in bold white
    sa_txt = "SPACE APPS"
    sab = d_nasa.textbbox((0, 0), sa_txt, font=f_space_apps)
    sax = moon_cx - (sab[2] - sab[0]) // 2
    say = moon_cy - 120
    for dx, dy in [(-3,0),(3,0),(0,-3),(0,3)]:
        d_nasa.text((sax + dx, say + dy), sa_txt, font=f_space_apps, fill=(0, 0, 0, 220))
    d_nasa.text((sax, say), sa_txt, font=f_space_apps, fill=(255, 255, 255, 255))

    # "CHALLENGE" in spaced white
    ch_txt = "C H A L L E N G E"
    chb = d_nasa.textbbox((0, 0), ch_txt, font=f_challenge)
    chx = moon_cx - (chb[2] - chb[0]) // 2
    chy = moon_cy + 95
    for dx, dy in [(-2,0),(2,0),(0,-2),(0,2)]:
        d_nasa.text((chx + dx, chy + dy), ch_txt, font=f_challenge, fill=(0, 0, 0, 220))
    d_nasa.text((chx, chy), ch_txt, font=f_challenge, fill=(255, 255, 255, 255))

    # "GLOBAL CHALLENGE 2026" under the circle
    gc_txt = "GLOBAL CHALLENGE 2026"
    gcb = d_nasa.textbbox((0, 0), gc_txt, font=f_year)
    gcx = moon_cx - (gcb[2] - gcb[0]) // 2
    gcy = moon_cy + circle_r + 35
    
    # Small glowing pill badge for 2026
    pill_w = (gcb[2] - gcb[0]) + 80
    pill_h = (gcb[3] - gcb[1]) + 24
    d_nasa.rounded_rectangle(
        [gcx - 40, gcy - 12, gcx + (gcb[2] - gcb[0]) + 40, gcy + (gcb[3] - gcb[1]) + 12],
        radius=20, fill=(8, 18, 40, 220), outline=(0, 210, 255, 220), width=3
    )
    d_nasa.text((gcx, gcy), gc_txt, font=f_year, fill=(0, 230, 255, 255))

    moon_4k = Image.alpha_composite(moon_4k, nasa_layer)

    # -------------------------------------------------------------------------
    # 3. PLACE TEAM PHANTOM ORION BADGE (Right side in deep space)
    # -------------------------------------------------------------------------
    badge_path = r"F:\SundarbanGuardian\team_phantom_orion_uploaded_transparent.png"
    badge_img = Image.open(badge_path).convert("RGBA")
    
    # Scale badge generously so all details are vivid
    badge_w = 1350
    badge_h = int(badge_img.height * (badge_w / badge_img.width))
    badge_resized = badge_img.resize((badge_w, badge_h), Image.Resampling.LANCZOS)
    
    bx = badge_cx - badge_w // 2
    by = badge_cy - badge_h // 2 - 30

    # Glowing cosmic backlight aura behind badge
    aura = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    d_aura = ImageDraw.Draw(aura)
    for r in range(700, 0, -10):
        al = int(55 * (1 - (r / 700) ** 1.3))
        d_aura.ellipse([badge_cx - r, (by + badge_h//2) - r, badge_cx + r, (by + badge_h//2) + r], fill=(20, 80, 220, al))
    for r in range(480, 0, -8):
        al = int(65 * (1 - (r / 480) ** 1.2))
        d_aura.ellipse([badge_cx - r, (by + badge_h//2) - r, badge_cx + r, (by + badge_h//2) + r], fill=(0, 220, 255, al))
    aura = aura.filter(ImageFilter.GaussianBlur(35))
    moon_4k = Image.alpha_composite(moon_4k, aura)

    # Paste Phantom Orion badge
    moon_4k.paste(badge_resized, (bx, by), badge_resized)

    # -------------------------------------------------------------------------
    # 4. LOCATION BADGE: RAJSHAHI, BANGLADESH (Under Phantom Orion Badge)
    # -------------------------------------------------------------------------
    loc_layer = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    d_loc = ImageDraw.Draw(loc_layer)

    loc_text = "RAJSHAHI, BANGLADESH"
    f_loc = font_bahn_bold(54)
    l_bbox = d_loc.textbbox((0, 0), loc_text, font=f_loc)
    lw = l_bbox[2] - l_bbox[0]
    lh = l_bbox[3] - l_bbox[1]

    lx = badge_cx - lw // 2
    ly = by + badge_h + 30

    pill_rect = [
        lx - 75,
        ly - 20,
        lx + lw + 50,
        ly + lh + 24
    ]
    d_loc.rounded_rectangle(pill_rect, radius=28, fill=(8, 16, 38, 220), outline=(0, 220, 255, 220), width=4)

    # Glowing red/white location beacon pin
    pin_x = lx - 40
    pin_y = ly + lh // 2
    d_loc.ellipse([pin_x - 10, pin_y - 10, pin_x + 10, pin_y + 10], fill=(255, 75, 75, 255), outline=(255, 255, 255, 220), width=2)
    d_loc.text((lx, ly), loc_text, font=f_loc, fill=(240, 250, 255, 255))

    final_comp = Image.alpha_composite(moon_4k, loc_layer).convert("RGB")
    final_sharp = final_comp.filter(ImageFilter.UnsharpMask(radius=1.5, percent=115, threshold=3))
    enhancer = ImageEnhance.Contrast(final_sharp)
    res = enhancer.enhance(1.03)

    out_file = r"F:\SundarbanGuardian\nasa_space_apps_2026_phantom_orion_moon_banner_4k.png"
    out_artifact = r"C:\Users\RAJ\.gemini\antigravity-ide\brain\eb7e8551-db12-437a-9a17-f5e56446f76c\nasa_space_apps_2026_phantom_orion_moon_banner_4k.png"
    res.save(out_file, format="PNG", compress_level=6)
    res.save(out_artifact, format="PNG", compress_level=6)
    print("4K Moon banner saved successfully to:", out_file)

if __name__ == "__main__":
    create_nasa_in_moon_banner()
