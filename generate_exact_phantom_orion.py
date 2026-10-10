import os
import numpy as np
from PIL import Image, ImageFilter, ImageDraw, ImageFont, ImageEnhance

# ----------------------------------------------------------------------
# 1. GENERATE EXACT PHANTOM ORION LOGO BADGE (from uploaded asset)
# ----------------------------------------------------------------------
def generate_exact_phantom_orion_logo():
    print("Generating exact Phantom Orion logo badge...")
    src = r"C:\Users\RAJ\.gemini\antigravity-ide\brain\eb7e8551-db12-437a-9a17-f5e56446f76c\.user_uploaded\media_1791230380856.jpg"
    img = Image.open(src).convert("RGBA")
    arr = np.array(img)

    # 1. Clean the BrainNotFound text inside the banner
    # Banner text bounding box: x: 75 to 935, y: 695 to 828
    mask = (arr[695:828, 75:935, 0] > 15) | (arr[695:828, 75:935, 1] > 15) | (arr[695:828, 75:935, 2] > 15)
    arr[695:828, 75:935][mask] = [0, 2, 7, 255]
    cleaned = Image.fromarray(arr)

    # 2. Render slanted text with gradient and stroke
    def make_gradient_text(text, font, top_col, bot_col, stroke_col=(0, 2, 8, 255), stroke_w=6, shear=0.10):
        dummy = Image.new("RGBA", (10, 10))
        d = ImageDraw.Draw(dummy)
        bbox = d.textbbox((0, 0), text, font=font, stroke_width=stroke_w)
        tw = bbox[2] - bbox[0] + 60
        th = bbox[3] - bbox[1] + 60
        
        mask_img = Image.new("L", (tw, th), 0)
        md = ImageDraw.Draw(mask_img)
        md.text((30, 30), text, font=font, fill=255)
        
        grad_img = Image.new("RGBA", (tw, th), (0, 0, 0, 0))
        gd = ImageDraw.Draw(grad_img)
        for y in range(th):
            factor = y / th
            r = int(top_col[0] * (1 - factor) + bot_col[0] * factor)
            g = int(top_col[1] * (1 - factor) + bot_col[1] * factor)
            b = int(top_col[2] * (1 - factor) + bot_col[2] * factor)
            gd.line([(0, y), (tw, y)], fill=(r, g, b, 255))
            
        res = Image.new("RGBA", (tw, th), (0, 0, 0, 0))
        res.paste(grad_img, (0, 0), mask_img)
        
        stroke_layer = Image.new("RGBA", (tw, th), (0, 0, 0, 0))
        sd = ImageDraw.Draw(stroke_layer)
        sd.text((30, 30), text, font=font, fill=stroke_col, stroke_fill=stroke_col, stroke_width=stroke_w)
        stroke_layer.paste(res, (0, 0), mask_img)
        
        m = (1, shear, -shear * th, 0, 1, 0)
        out = stroke_layer.transform((tw + int(abs(shear)*th), th), Image.Transform.AFFINE, m, Image.Resampling.BICUBIC)
        return out

    font = ImageFont.truetype("C:/Windows/Fonts/ariblk.ttf", 104)

    # "Phantom" in clean white with light-gray gradient
    t_phantom = make_gradient_text("Phantom", font, (255, 255, 255), (220, 226, 238), stroke_w=6, shear=0.10)
    # "Orion" in electric sky blue matching original NotFound
    t_orion = make_gradient_text("Orion", font, (140, 210, 255), (55, 138, 235), stroke_w=6, shear=0.10)

    tot_w = t_phantom.width + t_orion.width - 45
    sx = (1004 - tot_w) // 2
    sy = 698

    cleaned.paste(t_phantom, (sx, sy), t_phantom)
    cleaned.paste(t_orion, (sx + t_phantom.width - 45, sy), t_orion)

    # Save solid logo badge
    solid_path = r"F:\SundarbanGuardian\team_phantom_orion_exact_logo.png"
    cleaned.save(solid_path)
    print("Saved solid logo badge to:", solid_path)

    # 3. Create transparent badge (isolate from black background)
    c_arr = np.array(cleaned)
    r, g, b, a = c_arr[:,:,0], c_arr[:,:,1], c_arr[:,:,2], c_arr[:,:,3]
    black_mask = (r < 18) & (g < 18) & (b < 18)
    h, w = black_mask.shape
    visited = np.zeros((h, w), dtype=bool)
    from collections import deque
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
    c_arr[visited, 3] = 0
    transparent_badge = Image.fromarray(c_arr)
    trans_path = r"F:\SundarbanGuardian\team_phantom_orion_exact_transparent.png"
    transparent_badge.save(trans_path)
    print("Saved transparent logo badge to:", trans_path)

    return cleaned, transparent_badge

# ----------------------------------------------------------------------
# 2. GENERATE 4K BANNER (From the 1st generated image that the user loved)
# ----------------------------------------------------------------------
def generate_exact_phantom_orion_banner():
    print("Generating exact Phantom Orion 4K banner from 1st generated image...")
    src1 = r"C:\Users\RAJ\.gemini\antigravity-ide\brain\eb7e8551-db12-437a-9a17-f5e56446f76c\space_apps_team_1791230553955.jpg"
    im1 = Image.open(src1).convert("RGBA")
    arr = np.array(im1)

    # Text bounding box in 1st image: x: 750 to 1275, y: 550 to 645
    # Clean only text pixels inside the banner:
    banner_crop = arr[550:645, 750:1275]
    # In this region, text is bright (white/light blue)
    mask = (banner_crop[:,:,0] > 70) | (banner_crop[:,:,1] > 70) | (banner_crop[:,:,2] > 70)
    # The banner inside background is deep dark navy/black
    banner_crop[mask] = [8, 12, 28, 255]
    cleaned_bg = Image.fromarray(arr)

    # Render "Phantom" and "Orion" for the banner
    def make_gradient_text(text, font, top_col, bot_col, stroke_col=(0, 2, 8, 255), stroke_w=4, shear=0.10):
        dummy = Image.new("RGBA", (10, 10))
        d = ImageDraw.Draw(dummy)
        bbox = d.textbbox((0, 0), text, font=font, stroke_width=stroke_w)
        tw = bbox[2] - bbox[0] + 40
        th = bbox[3] - bbox[1] + 40
        mask_img = Image.new("L", (tw, th), 0)
        md = ImageDraw.Draw(mask_img)
        md.text((20, 20), text, font=font, fill=255)
        grad_img = Image.new("RGBA", (tw, th), (0, 0, 0, 0))
        gd = ImageDraw.Draw(grad_img)
        for y in range(th):
            factor = y / th
            r = int(top_col[0] * (1 - factor) + bot_col[0] * factor)
            g = int(top_col[1] * (1 - factor) + bot_col[1] * factor)
            b = int(top_col[2] * (1 - factor) + bot_col[2] * factor)
            gd.line([(0, y), (tw, y)], fill=(r, g, b, 255))
        res = Image.new("RGBA", (tw, th), (0, 0, 0, 0))
        res.paste(grad_img, (0, 0), mask_img)
        stroke_layer = Image.new("RGBA", (tw, th), (0, 0, 0, 0))
        sd = ImageDraw.Draw(stroke_layer)
        sd.text((20, 20), text, font=font, fill=stroke_col, stroke_fill=stroke_col, stroke_width=stroke_w)
        stroke_layer.paste(res, (0, 0), mask_img)
        m = (1, shear, -shear * th, 0, 1, 0)
        return stroke_layer.transform((tw + int(abs(shear)*th), th), Image.Transform.AFFINE, m, Image.Resampling.BICUBIC)

    font_banner = ImageFont.truetype("C:/Windows/Fonts/ariblk.ttf", 66)
    b_phantom = make_gradient_text("Phantom", font_banner, (255, 255, 255), (220, 226, 238), stroke_w=4, shear=0.10)
    b_orion = make_gradient_text("Orion", font_banner, (140, 210, 255), (55, 138, 235), stroke_w=4, shear=0.10)

    tot_w = b_phantom.width + b_orion.width - 25
    banner_cx = (750 + 1275) // 2
    sx = banner_cx - tot_w // 2
    sy = 556

    cleaned_bg.paste(b_phantom, (sx, sy), b_phantom)
    cleaned_bg.paste(b_orion, (sx + b_phantom.width - 25, sy), b_orion)

    # 4K UHD Upscale (3840 x 2160)
    target_w, target_h = 3840, 2160
    im_4k = cleaned_bg.resize((target_w, target_h), Image.Resampling.LANCZOS)
    im_sharp = im_4k.filter(ImageFilter.UnsharpMask(radius=1.5, percent=120, threshold=3))
    enhancer = ImageEnhance.Contrast(im_sharp)
    final_4k = enhancer.enhance(1.04)

    out_4k = r"F:\SundarbanGuardian\nasa_space_apps_2026_phantom_orion_cinematic_4k.png"
    out_art = r"C:\Users\RAJ\.gemini\antigravity-ide\brain\eb7e8551-db12-437a-9a17-f5e56446f76c\nasa_space_apps_2026_phantom_orion_cinematic_4k.png"
    final_4k.save(out_4k, format="PNG", compress_level=6)
    final_4k.save(out_art, format="PNG", compress_level=6)
    print("Exact Phantom Orion 4K banner saved successfully to:", out_4k)

if __name__ == "__main__":
    generate_exact_phantom_orion_logo()
    generate_exact_phantom_orion_banner()
