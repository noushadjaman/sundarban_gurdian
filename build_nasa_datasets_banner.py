import os
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

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
font_segoe_semi = lambda s: get_font("segoeui.ttf", s)
font_segoe_reg = lambda s: get_font("segoeui.ttf", s)

def draw_light_nasa_logo(size=600):
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

def wrap_text(text, font, max_width, draw):
    words = text.split(" ")
    lines = []
    cur = ""
    for w in words:
        test = (cur + " " + w).strip()
        bb = draw.textbbox((0, 0), test, font=font)
        if bb[2] - bb[0] <= max_width:
            cur = test
        else:
            if cur:
                lines.append(cur)
            cur = w
    if cur:
        lines.append(cur)
    return lines

def render_dataset_banner():
    print("Rendering 4K NASA Dataset Banner...")
    # Base background: ultra clean high-tech light canvas with subtle radial depth
    base = Image.new("RGBA", (TARGET_W, TARGET_H), (247, 249, 252, 255))
    draw_base = ImageDraw.Draw(base)
    
    # Subtle vertical gradient
    for y in range(TARGET_H):
        ratio = y / TARGET_H
        val = int(248 - ratio * 9)
        draw_base.line([(0, y), (TARGET_W, y)], fill=(val, val + 2, val + 6, 255))
        
    # Ambient glows (cool cyan/blue top left and top right)
    ambient = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    d_amb = ImageDraw.Draw(ambient)
    for r in range(1400, 0, -25):
        al = int(32 * (1 - (r / 1400) ** 1.4))
        d_amb.ellipse([600 - r, 300 - r, 600 + r, 300 + r], fill=(210, 235, 255, al))
        d_amb.ellipse([TARGET_W - 600 - r, 350 - r, TARGET_W - 600 + r, 350 + r], fill=(220, 245, 255, al))
        d_amb.ellipse([TARGET_W // 2 - r, TARGET_H - 200 - r, TARGET_W // 2 + r, TARGET_H - 200 + r], fill=(225, 240, 255, al))
    ambient = ambient.filter(ImageFilter.GaussianBlur(60))
    base = Image.alpha_composite(base, ambient)
    
    # Subtle aerospace technical grid
    grid = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    d_grid = ImageDraw.Draw(grid)
    for x in range(0, TARGET_W, 160):
        d_grid.line([(x, 0), (x, TARGET_H)], fill=(226, 232, 240, 50), width=1)
    for y in range(0, TARGET_H, 160):
        d_grid.line([(0, y), (TARGET_W, y)], fill=(226, 232, 240, 50), width=1)
        
    # Accent crosshairs
    for cx in [320, TARGET_W - 320]:
        for cy in [260, 1150, TARGET_H - 220]:
            d_grid.line([(cx - 20, cy), (cx + 20, cy)], fill=(148, 163, 184, 90), width=2)
            d_grid.line([(cx, cy - 20), (cx, cy + 20)], fill=(148, 163, 184, 90), width=2)
            d_grid.ellipse([cx - 6, cy - 6, cx + 6, cy + 6], outline=(2, 132, 199, 120), width=1)
            
    base = Image.alpha_composite(base, grid)
    
    # UI layer for header
    ui = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    draw_ui = ImageDraw.Draw(ui)
    
    # ==========================================
    # HEADER SECTION
    # ==========================================
    header_y = 80
    pad_h = 160
    
    # Left: NASA Space Apps Logo
    nasa_logo = draw_light_nasa_logo(380)
    nw = 340
    nh = int(nasa_logo.height * (nw / nasa_logo.width))
    nasa_resized = nasa_logo.resize((nw, nh), Image.Resampling.LANCZOS)
    ui.paste(nasa_resized, (pad_h, header_y - 15), nasa_resized)
    
    # Vertical divider
    v_div1_x = pad_h + nw + 50
    draw_ui.line([(v_div1_x, header_y + 15), (v_div1_x, header_y + 245)], fill=(203, 213, 225, 255), width=3)
    
    # Middle: Title and Subtitles
    title_x = v_div1_x + 55
    
    # Top Badges
    f_badge = font_bahn_bold(26)
    
    b1_text = "NASA SPACE APPS CHALLENGE 2026"
    bb1 = draw_ui.textbbox((0, 0), b1_text, font=f_badge)
    b1_w = bb1[2] - bb1[0]
    draw_ui.rounded_rectangle([title_x, header_y, title_x + b1_w + 36, header_y + 42], radius=14, fill=(238, 242, 255, 255), outline=(99, 102, 241, 180), width=2)
    draw_ui.text((title_x + 18, header_y + 6), b1_text, font=f_badge, fill=(79, 70, 229, 255))
    
    b2_x = title_x + b1_w + 55
    b2_text = "REGIONAL ROUND"
    bb2 = draw_ui.textbbox((0, 0), b2_text, font=f_badge)
    b2_w = bb2[2] - bb2[0]
    draw_ui.rounded_rectangle([b2_x, header_y, b2_x + b2_w + 36, header_y + 42], radius=14, fill=(240, 253, 250, 255), outline=(20, 184, 166, 180), width=2)
    draw_ui.text((b2_x + 18, header_y + 6), b2_text, font=f_badge, fill=(13, 148, 136, 255))

    b3_x = b2_x + b2_w + 55
    b3_text = "RAJSHAHI, BANGLADESH • RUET"
    bb3 = draw_ui.textbbox((0, 0), b3_text, font=f_badge)
    b3_w = bb3[2] - bb3[0]
    draw_ui.rounded_rectangle([b3_x, header_y, b3_x + b3_w + 36, header_y + 42], radius=14, fill=(254, 242, 242, 255), outline=(239, 68, 68, 180), width=2)
    draw_ui.text((b3_x + 18, header_y + 6), b3_text, font=f_badge, fill=(220, 38, 38, 255))
    
    # Title
    f_main_title = font_bahn_bold(68)
    draw_ui.text((title_x, header_y + 56), "NASA EARTH OBSERVATION & SATELLITE DATASETS", font=f_main_title, fill=(15, 23, 42, 255))
    
    # Subtitle
    f_sub = font_segoe_bold(32)
    draw_ui.text((title_x, header_y + 144), "Real-Time & Archival Planetary Data Powering the 'Sundarban Guardian' Wildlife Simulation", font=f_sub, fill=(2, 132, 199, 255))
    
    f_desc = font_segoe_reg(24)
    draw_ui.text((title_x, header_y + 192), "Direct telemetry integration from NASA Earthdata, LP DAAC, and PO.DAAC calibrated with ground-truth environmental surveys.", font=f_desc, fill=(100, 116, 139, 255))

    # Right: Team Phantom Orion Badge
    badge_path = r"F:\SundarbanGuardian\team_phantom_orion_uploaded_transparent.png"
    if os.path.exists(badge_path):
        badge_img = Image.open(badge_path).convert("RGBA")
        bw = 250
        bh = int(badge_img.height * (bw / badge_img.width))
        badge_resized = badge_img.resize((bw, bh), Image.Resampling.LANCZOS)
        badge_x = TARGET_W - pad_h - bw - 210
        badge_y = header_y + 10
        ui.paste(badge_resized, (badge_x, badge_y), badge_resized)
        
        f_tm = font_bahn_bold(26)
        f_tb = font_bahn_bold(40)
        draw_ui.text((badge_x + bw + 25, badge_y + 40), "TEAM", font=f_tm, fill=(100, 116, 139, 255))
        draw_ui.text((badge_x + bw + 25, badge_y + 75), "PHANTOM", font=f_tb, fill=(15, 23, 42, 255))
        draw_ui.text((badge_x + bw + 25, badge_y + 122), "ORION", font=f_tb, fill=(2, 132, 199, 255))

    # Composite header into base
    base = Image.alpha_composite(base, ui)

    # ==========================================
    # DATASET CARDS GRID (3 Cards across)
    # ==========================================
    card_y = 390
    card_h = 1130
    card_gap = 55
    card_w = (TARGET_W - (pad_h * 2) - (card_gap * 2)) // 3  # ~1125px each
    
    cards_data = [
        {
            "tag": "VEGETATION & HABITAT CANOPY",
            "tag_bg": (236, 253, 245, 255),
            "tag_border": (16, 185, 129, 200),
            "tag_color": (5, 150, 105, 255),
            "accent_color": (16, 185, 129, 255),
            "title": "NASA MODIS MOD13Q1",
            "full_name": "Terra & Aqua 250m 16-Day Vegetation Indices",
            "archive": "NASA LP DAAC (Land Processes DAAC)",
            "metrics": [
                ("PARAMETER", "NDVI & EVI (Normalized Difference Veg. Index)"),
                ("SPATIAL RESOLUTION", "250-meter Gridded Sinusoidal Swath"),
                ("TEMPORAL CADENCE", "16-Day Cloud-Free Composites"),
                ("ACCURACY / RANGE", "0.00 – 1.00 (Dense Mangroves: 0.72 – 0.88)"),
                ("LOCAL COORDINATES", "Kotka Range: 21.87° N, 89.75° E (Sundarbans)")
            ],
            "in_game_usage": [
                "Forest Canopy Density Mapping: Generates vegetation density matrices for Tiger & Deer movement paths.",
                "Illegal Deforestation & Logging Hazards: Identifies poacher clearings & timber-felling zones (NDVI < 0.18) triggering active danger zones.",
                "Cyclone Impact & Dieback Tracking: Dynamically calculates mangrove degradation and saline wasteland progression."
            ],
            "highlight": "Directly controls dynamic stealth multipliers and food availability in Royal Bengal Tiger & Spotted Deer missions."
        },
        {
            "tag": "ESTUARINE SALINITY & WATER CHEMISTRY",
            "tag_bg": (239, 246, 255, 255),
            "tag_border": (59, 130, 246, 200),
            "tag_color": (37, 99, 235, 255),
            "accent_color": (37, 99, 235, 255),
            "title": "NASA SMAP JPL L3 SSS",
            "full_name": "Soil Moisture Active Passive Sea Surface Salinity",
            "archive": "NASA PO.DAAC (Physical Oceanography DAAC)",
            "metrics": [
                ("PARAMETER", "Sea Surface Salinity (CAP v5.0 Algorithm)"),
                ("SPATIAL RESOLUTION", "0.25° × 0.25° (~25 km) Grid Composite"),
                ("TEMPORAL CADENCE", "8-Day Running Mean & Monthly Integrations"),
                ("ACCURACY / RANGE", "0.1 to 35.0 PSU (Practical Salinity Units)"),
                ("LOCAL COORDINATES", "Passur-Shibsa & Meghna Mouth: 21.60° N, 91.05° E")
            ],
            "in_game_usage": [
                "Hilsha Migration Corridor: Real salinity transect from Bay of Bengal (28.2 PSU) upstream to Chandpur (1.4 PSU).",
                "Saltwater Intrusion Barriers: Computes osmotic pressure boundaries forcing migratory fish deeper into freshwater zones.",
                "Gangetic Dolphin Habitat Safety: Regulates brackish-to-freshwater transition zones across the Sundarbans rivers."
            ],
            "highlight": "Powers the core survival gauge of the Hilsha Migration controller—wrong salinity levels inflict physiological osmotic stress."
        },
        {
            "tag": "THERMAL GRADIENT & SEA SURFACE TEMPERATURE",
            "tag_bg": (254, 243, 199, 255),
            "tag_border": (245, 158, 11, 200),
            "tag_color": (217, 119, 6, 255),
            "accent_color": (245, 158, 11, 255),
            "title": "NASA MODIS MOD11A2",
            "full_name": "Terra / Aqua 1km 8-Day Land & Sea Surface Temp",
            "archive": "NASA Earthdata / OB.DAAC & LP DAAC",
            "metrics": [
                ("PARAMETER", "Day/Night Water & Land Surface Temp (LST/SST)"),
                ("SPATIAL RESOLUTION", "1000-meter (1 km) Sinusoidal Grid"),
                ("TEMPORAL CADENCE", "8-Day Average Granule Composite"),
                ("ACCURACY / RANGE", "19.5°C to 32.0°C (Accuracy within ±0.5°C)"),
                ("LOCAL COORDINATES", "Meghna-Padma Confluence: 23.23° N, 90.66° E")
            ],
            "in_game_usage": [
                "Thermal Comfort Gradient: Simulates water temperature drop from ocean (23.5°C) to upstream Padma river (19.7°C).",
                "Spawning Season Trigger: Models optimal reproductive windows for Mother Hilsa based on real satellite thermal shifts.",
                "Microclimate & Heat Stress: Affects land animal hydration depletion rates during midday heat spikes in the forest."
            ],
            "highlight": "Calibrates spawning viability scores along the 130 km migration route in the Mother Hilsa Sanctuary conservation level."
        }
    ]
    
    # Draw shadows for cards
    shadow_layer = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    d_sh = ImageDraw.Draw(shadow_layer)
    for i in range(3):
        cx = pad_h + i * (card_w + card_gap)
        d_sh.rounded_rectangle([cx + 12, card_y + 18, cx + card_w + 12, card_y + card_h + 18], radius=32, fill=(15, 23, 42, 35))
    shadow_layer = shadow_layer.filter(ImageFilter.GaussianBlur(24))
    base = Image.alpha_composite(base, shadow_layer)
    
    card_ui = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
    draw_card = ImageDraw.Draw(card_ui)
    
    for i, c in enumerate(cards_data):
        cx = pad_h + i * (card_w + card_gap)
        rect = [cx, card_y, cx + card_w, card_y + card_h]
        
        # White card body with crisp border
        draw_card.rounded_rectangle(rect, radius=32, fill=(255, 255, 255, 250), outline=(226, 232, 240, 255), width=3)
        # Top color accent bar
        draw_card.rounded_rectangle([cx, card_y, cx + card_w, card_y + 12], radius=6, fill=c["accent_color"])
        
        pad_in = cx + 45
        inner_w = card_w - 90
        cy = card_y + 42
        
        # Tag
        f_tag = font_bahn_bold(24)
        tb = draw_card.textbbox((0, 0), c["tag"], font=f_tag)
        tw = tb[2] - tb[0]
        draw_card.rounded_rectangle([pad_in, cy, pad_in + tw + 32, cy + 42], radius=12, fill=c["tag_bg"], outline=c["tag_border"], width=2)
        draw_card.text((pad_in + 16, cy + 7), c["tag"], font=f_tag, fill=c["tag_color"])
        cy += 65
        
        # Title
        f_ctitle = font_bahn_bold(50)
        draw_card.text((pad_in, cy), c["title"], font=f_ctitle, fill=(15, 23, 42, 255))
        cy += 65
        
        # Full Name
        f_cname = font_segoe_bold(28)
        draw_card.text((pad_in, cy), c["full_name"], font=f_cname, fill=c["accent_color"])
        cy += 40
        
        # Archive
        f_arch = font_segoe_reg(23)
        draw_card.text((pad_in, cy), f"Archive: {c['archive']}", font=f_arch, fill=(100, 116, 139, 255))
        cy += 42
        
        # Divider
        draw_card.line([(pad_in, cy), (cx + card_w - 45, cy)], fill=(241, 245, 249, 255), width=2)
        cy += 25
        
        # Telemetry Specifications Table
        f_th = font_bahn_bold(22)
        draw_card.text((pad_in, cy), "TECHNICAL SPECIFICATIONS", font=f_th, fill=(148, 163, 184, 255))
        cy += 35
        
        f_lbl = font_segoe_bold(21)
        f_val = font_segoe_reg(23)
        
        col_split_x = pad_in + 315
        
        for k, v in c["metrics"]:
            draw_card.rounded_rectangle([pad_in, cy, cx + card_w - 45, cy + 48], radius=10, fill=(248, 250, 252, 255), outline=(238, 242, 246, 255), width=1)
            draw_card.text((pad_in + 16, cy + 12), k, font=f_lbl, fill=(71, 85, 105, 255))
            draw_card.text((col_split_x, cy + 11), v, font=f_val, fill=(15, 23, 42, 255))
            cy += 56
            
        cy += 10
        draw_card.line([(pad_in, cy), (cx + card_w - 45, cy)], fill=(241, 245, 249, 255), width=2)
        cy += 25
        
        # In-Game Usage
        f_uh = font_bahn_bold(22)
        draw_card.text((pad_in, cy), "GAMEPLAY INTEGRATION & MECHANICS", font=f_uh, fill=(148, 163, 184, 255))
        cy += 35
        
        f_bullet = font_segoe_reg(23)
        for b_item in c["in_game_usage"]:
            draw_card.ellipse([pad_in + 4, cy + 8, pad_in + 14, cy + 18], fill=c["accent_color"])
            
            lines = wrap_text(b_item, f_bullet, inner_w - 32, draw_card)
            for l in lines:
                draw_card.text((pad_in + 26, cy), l, font=f_bullet, fill=(30, 41, 59, 255))
                cy += 30
            cy += 6
            
        cy += 8
        # Bottom highlight badge
        draw_card.rounded_rectangle([pad_in, cy, cx + card_w - 45, cy + 90], radius=14, fill=c["tag_bg"], outline=c["tag_border"], width=1)
        f_hl = font_segoe_bold(22)
        
        hl_lines = wrap_text(c["highlight"], f_hl, inner_w - 36, draw_card)
        for l_idx, l in enumerate(hl_lines):
            draw_card.text((pad_in + 18, cy + 14 + (l_idx * 32)), l, font=f_hl, fill=c["tag_color"])

    # ==========================================
    # BOTTOM FULL-WIDTH PIPELINE & VALIDATION CARD
    # ==========================================
    bot_y = 1575
    bot_h = 475
    bot_w = TARGET_W - (pad_h * 2)
    
    # Shadow
    d_sh.rounded_rectangle([pad_h + 12, bot_y + 18, pad_h + bot_w + 12, bot_y + bot_h + 18], radius=32, fill=(15, 23, 42, 35))
    
    # Body
    draw_card.rounded_rectangle([pad_h, bot_y, pad_h + bot_w, bot_y + bot_h], radius=32, fill=(255, 255, 255, 250), outline=(226, 232, 240, 255), width=3)
    draw_card.rounded_rectangle([pad_h, bot_y, pad_h + 12, bot_y + bot_h], radius=6, fill=(2, 132, 199, 255))
    
    bpad_x = pad_h + 55
    bcur_y = bot_y + 40
    
    # Header of bottom section
    f_b_tag = font_bahn_bold(24)
    b_tag_txt = "END-TO-END TELEMETRY PIPELINE & GROUND-TRUTH CROSS-VALIDATION"
    btb = draw_card.textbbox((0, 0), b_tag_txt, font=f_b_tag)
    bt_w = btb[2] - btb[0]
    draw_card.rounded_rectangle([bpad_x, bcur_y, bpad_x + bt_w + 32, bcur_y + 42], radius=12, fill=(240, 249, 255, 255), outline=(2, 132, 199, 180), width=2)
    draw_card.text((bpad_x + 16, bcur_y + 7), b_tag_txt, font=f_b_tag, fill=(2, 132, 199, 255))
    
    f_b_title = font_bahn_bold(44)
    draw_card.text((bpad_x + bt_w + 65, bcur_y - 2), "How NASA Satellite Data Streams Directly Into Unity Engine Mechanics", font=f_b_title, fill=(15, 23, 42, 255))
    
    bcur_y += 75
    draw_card.line([(bpad_x, bcur_y), (pad_h + bot_w - 55, bcur_y)], fill=(241, 245, 249, 255), width=2)
    bcur_y += 35
    
    # 4 Architecture Columns
    col_w = (bot_w - 110 - (3 * 45)) // 4
    
    steps = [
        {
            "num": "01",
            "name": "NASA Earthdata CMR API",
            "tech": "CMR Search & earthaccess",
            "desc": "Automated queries against cmr.earthdata.nasa.gov for latest granules matching geographic coordinates of Sundarbans & Meghna Estuary."
        },
        {
            "num": "02",
            "name": "Array Processing & Resampling",
            "tech": "Python, xarray, rasterio",
            "desc": "Multi-temporal aggregation of 250m MODIS NDVI, 1km LST, and 0.25° SMAP salinity grids; normalized into uniform hazard indices."
        },
        {
            "num": "03",
            "name": "In-Situ Ground Truth Check",
            "tech": "BFD, BFRI & BWDB Surveys",
            "desc": "Cross-calibrated with field data from Bangladesh Forest Dept, Riverine Station Chandpur salinity sensors, and Water Development Board."
        },
        {
            "num": "04",
            "name": "Unity Real-Time Execution",
            "tech": "LevelData.cs & StreamingAssets",
            "desc": "Game dynamically injects real satellite metrics into HazardZoneTrigger.cs and HilshaMigrationController.cs for live environmental response."
        }
    ]
    
    for idx, st in enumerate(steps):
        col_x = bpad_x + idx * (col_w + 45)
        
        # Step box
        draw_card.rounded_rectangle([col_x, bcur_y, col_x + col_w, bcur_y + 250], radius=20, fill=(248, 250, 252, 255), outline=(226, 232, 240, 200), width=1)
        
        # Arrow connector between steps
        if idx < 3:
            arrow_x = col_x + col_w + 14
            arrow_y = bcur_y + 125
            draw_card.line([(arrow_x, arrow_y), (arrow_x + 18, arrow_y)], fill=(2, 132, 199, 180), width=3)
            draw_card.polygon([(arrow_x + 18, arrow_y - 6), (arrow_x + 26, arrow_y), (arrow_x + 18, arrow_y + 6)], fill=(2, 132, 199, 200))
        
        # Number pill
        f_pnum = font_bahn_bold(22)
        draw_card.rounded_rectangle([col_x + 18, bcur_y + 18, col_x + 65, bcur_y + 54], radius=10, fill=(15, 23, 42, 255))
        draw_card.text((col_x + 28, bcur_y + 22), st["num"], font=f_pnum, fill=(255, 255, 255, 255))
        
        # Tech badge
        f_ptech = font_segoe_bold(20)
        draw_card.text((col_x + 78, bcur_y + 24), st["tech"], font=f_ptech, fill=(2, 132, 199, 255))
        
        # Name
        f_pname = font_bahn_bold(26)
        draw_card.text((col_x + 18, bcur_y + 70), st["name"], font=f_pname, fill=(15, 23, 42, 255))
        
        # Desc
        f_pdesc = font_segoe_reg(21)
        desc_lines = wrap_text(st["desc"], f_pdesc, col_w - 36, draw_card)
        for d_idx, dl in enumerate(desc_lines):
            draw_card.text((col_x + 18, bcur_y + 115 + (d_idx * 32)), dl, font=f_pdesc, fill=(71, 85, 105, 255))

    # Composite card_ui into base
    base = Image.alpha_composite(base, card_ui)

    final = base.convert("RGB")
    final_sharp = final.filter(ImageFilter.UnsharpMask(radius=1.2, percent=110, threshold=2))
    
    out_file = os.path.join(r"F:\SundarbanGuardian", "nasa_space_apps_2026_detailed_datasets_banner_4k.png")
    final_sharp.save(out_file, format="PNG", compress_level=6)
    print("Saved 4K Dataset Banner to:", out_file)

if __name__ == "__main__":
    render_dataset_banner()
