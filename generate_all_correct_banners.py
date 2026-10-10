import os
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageEnhance

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
font_segoe_reg = lambda s: get_font("segoeui.ttf", s)

# ==============================================================================
# OFFICIAL NASA SPACE APPS LOGO (LIGHT THEME)
# ==============================================================================
def draw_light_nasa_logo(size=800):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    cx, cy = size // 2, size // 2
    r_main = int(size * 0.40)
    stroke_w = int(size * 0.018)
    
    angle_sat = math.radians(-55)
    sat_cx = cx + int(r_main * math.cos(angle_sat))
    sat_cy = cy + int(r_main * math.sin(angle_sat))
    sat_r = int(size * 0.056)
    
    circle_col = (15, 23, 42, 255)
    draw.arc([cx - r_main, cy - r_main, cx + r_main, cy + r_main], start=328, end=278, fill=circle_col, width=stroke_w)
    draw.ellipse([sat_cx - sat_r, sat_cy - sat_r, sat_cx + sat_r, sat_cy + sat_r], outline=circle_col, width=stroke_w)
    for ray_deg in [-80, -60, -40, -20]:
        rad = math.radians(ray_deg)
        p1 = (sat_cx + int(sat_r * 0.95 * math.cos(rad)), sat_cy + int(sat_r * 0.95 * math.sin(rad)))
        p2 = (sat_cx + int(sat_r * 2.2 * math.cos(rad)), sat_cy + int(sat_r * 2.2 * math.sin(rad)))
        draw.line([p1, p2], fill=circle_col, width=stroke_w)

    f_nasa = font_bahn_bold(int(size * 0.075))
    f_sa = font_bahn_bold(int(size * 0.155))
    f_ch = font_bahn_bold(int(size * 0.082))
    
    n_txt = "N A S A"
    nb = draw.textbbox((0, 0), n_txt, font=f_nasa)
    draw.text((cx - (nb[2] - nb[0]) // 2, cy - int(size * 0.22)), n_txt, font=f_nasa, fill=(225, 45, 35, 255))
    
    s_txt = "SPACE APPS"
    sb = draw.textbbox((0, 0), s_txt, font=f_sa)
    draw.text((cx - (sb[2] - sb[0]) // 2, cy - int(size * 0.10)), s_txt, font=f_sa, fill=(15, 23, 42, 255))
    
    c_txt = "C H A L L E N G E"
    cb = draw.textbbox((0, 0), c_txt, font=f_ch)
    draw.text((cx - (cb[2] - cb[0]) // 2, cy + int(size * 0.10)), c_txt, font=f_ch, fill=(15, 23, 42, 255))
    
    return img

# ==============================================================================
# RENDER 4K LIGHT UI MEMBER BANNER
# ==============================================================================
def render_member_banner(photo_path, name, role_badge, role_title, skill_tags, crop_y_ratio, out_filename):
    print(f"Building 4K banner for: {name} using photo: {os.path.basename(photo_path)}...")
    
    # Base pearl canvas
    base = Image.new("RGBA", (TARGET_W, TARGET_H), (248, 250, 252, 255))
    draw_base = ImageDraw.Draw(base)
    for y in range(TARGET_H):
        val = int(248 - (y / TARGET_H) * 8)
        draw_base.line([(0, y), (TARGET_W, y)], fill=(val, val + 2, val + 6, 255))
        
    # Ambient azure / cyan blooms
    ambient_glow = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    d_amb = ImageDraw.Draw(ambient_glow)
    for r in range(1200, 0, -20):
        al = int(35 * (1 - (r / 1200) ** 1.5))
        d_amb.ellipse([TARGET_W - 800 - r, 200 - r, TARGET_W - 800 + r, 200 + r], fill=(215, 235, 255, al))
    for r in range(1000, 0, -20):
        al = int(30 * (1 - (r / 1000) ** 1.5))
        d_amb.ellipse([700 - r, 1100 - r, 700 + r, 1100 + r], fill=(225, 240, 255, al))
    ambient_glow = ambient_glow.filter(ImageFilter.GaussianBlur(50))
    base = Image.alpha_composite(base, ambient_glow)

    # Grid
    grid_layer = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    d_grid = ImageDraw.Draw(grid_layer)
    for x in range(0, TARGET_W, 160):
        d_grid.line([(x, 0), (x, TARGET_H)], fill=(226, 232, 240, 60), width=1)
    for y in range(0, TARGET_H, 160):
        d_grid.line([(0, y), (TARGET_W, y)], fill=(226, 232, 240, 60), width=1)
    base = Image.alpha_composite(base, grid_layer)

    # Photo card dimensions
    card_w, card_h = 1140, 1600
    card_x = 220
    card_y = (TARGET_H - card_h) // 2 - 20
    
    # Shadow
    shadow = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    d_sh = ImageDraw.Draw(shadow)
    d_sh.rounded_rectangle([card_x + 15, card_y + 25, card_x + card_w + 15, card_y + card_h + 25], radius=44, fill=(15, 23, 42, 60))
    shadow = shadow.filter(ImageFilter.GaussianBlur(32))
    base = Image.alpha_composite(base, shadow)

    # Load and scale photo
    photo_raw = Image.open(photo_path).convert("RGBA")
    pw, ph = photo_raw.size
    target_aspect = card_w / card_h
    p_aspect = pw / ph
    if p_aspect > target_aspect:
        new_h = card_h
        new_w = int(pw * (card_h / ph))
    else:
        new_w = card_w
        new_h = int(ph * (card_w / pw))
    photo_scaled = photo_raw.resize((new_w, new_h), Image.Resampling.LANCZOS)
    
    crop_x = (new_w - card_w) // 2
    crop_y = int((new_h - card_h) * crop_y_ratio)
    photo_cropped = photo_scaled.crop((crop_x, crop_y, crop_x + card_w, crop_y + card_h))
    
    corner_mask = Image.new("L", (card_w, card_h), 0)
    ImageDraw.Draw(corner_mask).rounded_rectangle([0, 0, card_w, card_h], radius=44, fill=255)
    
    photo_card = Image.new("RGBA", (card_w, card_h), (255, 255, 255, 255))
    photo_card.paste(photo_cropped, (0, 0), corner_mask)
    
    d_pc = ImageDraw.Draw(photo_card)
    d_pc.rounded_rectangle([0, 0, card_w, card_h], radius=44, outline=(2, 132, 199, 255), width=8)
    d_pc.rounded_rectangle([6, 6, card_w - 6, card_h - 6], radius=38, outline=(255, 255, 255, 230), width=4)

    # Bottom gradient overlay for tag
    grad_tag = Image.new("RGBA", (card_w, 220), (0, 0, 0, 0))
    d_gt = ImageDraw.Draw(grad_tag)
    for y in range(220):
        al = int(220 * (y / 220) ** 1.3)
        d_gt.line([(0, y), (card_w, y)], fill=(15, 23, 42, al))
    photo_card.paste(grad_tag, (0, card_h - 220), grad_tag)
    
    f_tag = font_bahn_bold(34)
    tag_txt = "REGIONAL ROUND PARTICIPANT  •  NASA SPACE APPS 2026"
    tb = d_pc.textbbox((0, 0), tag_txt, font=f_tag)
    tw = tb[2] - tb[0]
    d_pc.text((card_w // 2 - tw // 2, card_h - 90), tag_txt, font=f_tag, fill=(240, 249, 255, 255))

    base.paste(photo_card, (card_x, card_y), corner_mask)

    # Right Content Area
    content_x = card_x + card_w + 140
    content_w = TARGET_W - content_x - 180
    
    ui_layer = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    draw_ui = ImageDraw.Draw(ui_layer)

    # Top Logos Header
    top_y = card_y + 10
    nasa_logo = draw_light_nasa_logo(480)
    nw = 460
    nh = int(nasa_logo.height * (nw / nasa_logo.width))
    nasa_resized = nasa_logo.resize((nw, nh), Image.Resampling.LANCZOS)
    ui_layer.paste(nasa_resized, (content_x, top_y), nasa_resized)

    div_x = content_x + nw + 60
    draw_ui.line([(div_x, top_y + 30), (div_x, top_y + nh - 30)], fill=(203, 213, 225, 255), width=3)

    badge_path = r"F:\SundarbanGuardian\team_phantom_orion_uploaded_transparent.png"
    badge_img = Image.open(badge_path).convert("RGBA")
    bw = 360
    bh = int(badge_img.height * (bw / badge_img.width))
    badge_resized = badge_img.resize((bw, bh), Image.Resampling.LANCZOS)
    
    badge_x = div_x + 60
    badge_y = top_y + (nh - bh) // 2
    ui_layer.paste(badge_resized, (badge_x, badge_y), badge_resized)

    f_t_mini = font_bahn_bold(32)
    f_t_team = font_bahn_bold(52)
    t_text_x = badge_x + bw + 45
    draw_ui.text((t_text_x, badge_y + 40), "TEAM", font=f_t_mini, fill=(100, 116, 139, 255))
    draw_ui.text((t_text_x, badge_y + 85), "PHANTOM ORION", font=f_t_team, fill=(15, 23, 42, 255))

    # Main Card
    bio_y = top_y + nh + 70
    bio_w = content_w
    bio_h = card_h - (nh + 70)
    card_rect = [content_x, bio_y, content_x + bio_w, bio_y + bio_h]
    
    c_shadow = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    d_cs = ImageDraw.Draw(c_shadow)
    d_cs.rounded_rectangle([content_x + 8, bio_y + 16, content_x + bio_w + 8, bio_y + bio_h + 16], radius=36, fill=(15, 23, 42, 35))
    c_shadow = c_shadow.filter(ImageFilter.GaussianBlur(24))
    base = Image.alpha_composite(base, c_shadow)

    draw_ui.rounded_rectangle(card_rect, radius=36, fill=(255, 255, 255, 245), outline=(226, 232, 240, 255), width=3)
    draw_ui.rounded_rectangle([content_x, bio_y, content_x + 12, bio_y + bio_h], radius=6, fill=(2, 132, 199, 255))

    pad_x = content_x + 75
    cur_y = bio_y + 70

    # Badges
    f_badge = font_bahn_bold(34)
    bb = draw_ui.textbbox((0, 0), role_badge, font=f_badge)
    btw = bb[2] - bb[0]
    bth = bb[3] - bb[1]
    
    badge_rect = [pad_x, cur_y, pad_x + btw + 50, cur_y + bth + 24]
    draw_ui.rounded_rectangle(badge_rect, radius=18, fill=(238, 242, 255, 255), outline=(99, 102, 241, 200), width=2)
    draw_ui.text((pad_x + 25, cur_y + 10), role_badge, font=f_badge, fill=(79, 70, 229, 255))

    tag_ns = "REGIONAL ROUND"
    ntb = draw_ui.textbbox((0, 0), tag_ns, font=f_badge)
    tag_rect = [pad_x + btw + 75, cur_y, pad_x + btw + 75 + (ntb[2]-ntb[0]) + 50, cur_y + bth + 24]
    draw_ui.rounded_rectangle(tag_rect, radius=18, fill=(240, 253, 250, 255), outline=(20, 184, 166, 200), width=2)
    draw_ui.text((pad_x + btw + 100, cur_y + 10), tag_ns, font=f_badge, fill=(13, 148, 136, 255))

    cur_y += bth + 55

    # Member Name
    f_name = font_bahn_bold(102)
    draw_ui.text((pad_x, cur_y), name, font=f_name, fill=(15, 23, 42, 255))
    cur_y += 125

    # Title
    f_title = font_bahn_bold(54)
    draw_ui.text((pad_x, cur_y), role_title, font=f_title, fill=(2, 132, 199, 255))
    cur_y += 75

    # Skill Tags
    f_tag_sm = font_segoe_bold(30)
    tx = pad_x
    for t in skill_tags:
        tb = draw_ui.textbbox((0, 0), t, font=f_tag_sm)
        t_w = tb[2] - tb[0]
        t_h = tb[3] - tb[1]
        draw_ui.rounded_rectangle([tx, cur_y, tx + t_w + 36, cur_y + t_h + 16], radius=14, fill=(241, 245, 249, 255), outline=(203, 213, 225, 200), width=1)
        draw_ui.text((tx + 18, cur_y + 7), t, font=f_tag_sm, fill=(71, 85, 105, 255))
        tx += t_w + 50

    cur_y += 85
    draw_ui.line([(pad_x, cur_y), (content_x + bio_w - 75, cur_y)], fill=(241, 245, 249, 255), width=2)
    cur_y += 50

    # Department & Institution
    f_inst_label = font_bahn_bold(30)
    f_inst_main = font_segoe_bold(44)
    f_inst_sub = font_segoe_reg(36)
    
    draw_ui.text((pad_x, cur_y), "DEPARTMENT & INSTITUTION", font=f_inst_label, fill=(148, 163, 184, 255))
    cur_y += 45
    draw_ui.text((pad_x, cur_y), "Computer Science & Engineering (CSE)", font=f_inst_main, fill=(30, 41, 59, 255))
    cur_y += 55
    draw_ui.text((pad_x, cur_y), "Rajshahi University of Engineering & Technology (RUET)", font=f_inst_sub, fill=(71, 85, 105, 255))
    
    cur_y += 90
    draw_ui.line([(pad_x, cur_y), (content_x + bio_w - 75, cur_y)], fill=(241, 245, 249, 255), width=2)
    cur_y += 45

    # Location & Event Footer
    f_loc_lbl = font_bahn_bold(30)
    draw_ui.text((pad_x, cur_y), "LOCATION & REGION", font=f_loc_lbl, fill=(148, 163, 184, 255))
    cur_y += 45
    
    loc_full = "Rajshahi, Bangladesh"
    f_loc_txt = font_segoe_bold(42)
    pin_cx = pad_x + 14
    pin_cy = cur_y + 22
    draw_ui.ellipse([pin_cx - 10, pin_cy - 10, pin_cx + 10, pin_cy + 10], fill=(239, 68, 68, 255), outline=(254, 202, 202, 255), width=3)
    draw_ui.text((pad_x + 40, cur_y), loc_full, font=f_loc_txt, fill=(15, 23, 42, 255))
    
    event_tag = "NASA SPACE APPS CHALLENGE 2026 • REGIONAL ROUND"
    f_ev = font_bahn_bold(32)
    ev_b = draw_ui.textbbox((0, 0), event_tag, font=f_ev)
    ev_w = ev_b[2] - ev_b[0]
    draw_ui.text((content_x + bio_w - ev_w - 75, cur_y + 6), event_tag, font=f_ev, fill=(100, 116, 139, 255))

    final = Image.alpha_composite(base, ui_layer).convert("RGB")
    final_sharp = final.filter(ImageFilter.UnsharpMask(radius=1.2, percent=110, threshold=2))
    
    out_file = os.path.join(r"F:\SundarbanGuardian", out_filename)
    final_sharp.save(out_file, format="PNG", compress_level=6)
    print("Saved:", out_file)

if __name__ == "__main__":
    # 1. TANJID HOSSEN RIFAT (Photo: media_1791233931335.jpg - carved stone wall background, black jacket)
    render_member_banner(
        photo_path=r"C:\Users\RAJ\.gemini\antigravity-ide\brain\eb7e8551-db12-437a-9a17-f5e56446f76c\.user_uploaded\media_1791233931335.jpg",
        name="Tanjid Hossen Rifat",
        role_badge="VIDEO EDITOR & CONTENT CREATOR",
        role_title="Lead Media Architect & Creative Content Director",
        skill_tags=["Cinematic Video Editing", "Visual Storytelling", "Motion Graphics", "Content Strategy"],
        crop_y_ratio=0.10,
        out_filename="member_banner_tanjid_hossen_rifat_4k.png"
    )

    # 2. RAF RAFIN MAHMUD (Photo: media_1791233391218.png - night photo with crossed arms, RUET shirt)
    render_member_banner(
        photo_path=r"C:\Users\RAJ\.gemini\antigravity-ide\brain\eb7e8551-db12-437a-9a17-f5e56446f76c\.user_uploaded\media_1791233391218.png",
        name="Raf Rafin Mahmud",
        role_badge="DATA ANALYST",
        role_title="Lead Data Analyst & Mission Intelligence Specialist",
        skill_tags=["Data Analytics", "Planetary Modeling", "Statistical Insights", "Predictive Analytics"],
        crop_y_ratio=0.12,
        out_filename="member_banner_raf_rafin_mahmud_4k.png"
    )

    # 3. MAHIR HAMI ABRAR (Photo: media_1791233561825.jpg - suit with glasses)
    render_member_banner(
        photo_path=r"C:\Users\RAJ\.gemini\antigravity-ide\brain\eb7e8551-db12-437a-9a17-f5e56446f76c\.user_uploaded\media_1791233561825.jpg",
        name="Mahir Hami Abrar",
        role_badge="GAME DEVELOPER",
        role_title="Lead Gameplay Systems & Simulation Architect",
        skill_tags=["Game Mechanics", "Engine Architecture", "Real-Time Physics", "Interactive Simulation"],
        crop_y_ratio=0.08,
        out_filename="member_banner_mahir_hami_abrar_4k.png"
    )

    # 4. MD NUSHAD JAMAN RAJ (Photo: media_1791233324336.jpg - blue striped shirt, backpack)
    render_member_banner(
        photo_path=r"C:\Users\RAJ\.gemini\antigravity-ide\brain\eb7e8551-db12-437a-9a17-f5e56446f76c\.user_uploaded\media_1791233324336.jpg",
        name="Md. Nushad Jaman Raj",
        role_badge="TEAM LEADER",
        role_title="Lead Game Architect & Creative Visionary",
        skill_tags=["Game Architecture", "Creative Ideation", "System Design", "Interactive Worlds"],
        crop_y_ratio=0.15,
        out_filename="member_banner_nushad_jaman_raj_4k.png"
    )
