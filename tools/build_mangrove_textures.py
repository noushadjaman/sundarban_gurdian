import os
import numpy as np
from PIL import Image

src_path = r"C:\Users\RAJ\.gemini\antigravity-ide\brain\953dbde4-1362-4979-80d9-01f42c5040ac\sundarban_mangrove_mud_surface_1789952951037.jpg"
if not os.path.exists(src_path):
    print("Source image not found:", src_path)
    exit(1)

img = Image.open(src_path).convert("RGB")
w, h = img.size
print(f"Loaded source image: {w}x{h}")

arr = np.array(img, dtype=np.float32)

# Make seamless using overlapping boundary feathering (blend 15% edges)
border = int(min(w, h) * 0.15)
seamless = arr.copy()
# Horizontal blend
for x in range(border):
    alpha = 0.5 * (1.0 - np.cos(np.pi * x / border))
    seamless[:, x] = alpha * arr[:, x] + (1.0 - alpha) * arr[:, w - border + x]
    seamless[:, w - border + x] = (1.0 - alpha) * arr[:, x] + alpha * arr[:, w - border + x]

# Vertical blend
for y in range(border):
    alpha = 0.5 * (1.0 - np.cos(np.pi * y / border))
    seamless[y, :] = alpha * seamless[y, :] + (1.0 - alpha) * seamless[h - border + y, :]
    seamless[h - border + y, :] = (1.0 - alpha) * seamless[y, :] + alpha * seamless[h - border + y, :]

seamless_img = Image.fromarray(np.clip(seamless, 0, 255).astype(np.uint8))
os.makedirs("Assets/Textures", exist_ok=True)
seamless_img.save("Assets/Textures/Sundarban_Ground_Rich.jpg", quality=95)
seamless_img.save("Assets/Textures/Mangrove_Mud_Surface_Albedo.png", format="PNG")
print("Saved seamless albedo maps!")

# Compute high quality Normal Map using Sobel filter
gray = np.dot(seamless[..., :3], [0.2989, 0.5870, 0.1140]) / 255.0

# Vectorized Sobel with periodic boundary conditions (since seamless)
gx = (
    -1.0 * np.roll(np.roll(gray, 1, axis=0), 1, axis=1) + 1.0 * np.roll(np.roll(gray, 1, axis=0), -1, axis=1) +
    -2.0 * np.roll(gray, 1, axis=1)                     + 2.0 * np.roll(gray, -1, axis=1) +
    -1.0 * np.roll(np.roll(gray, -1, axis=0), 1, axis=1) + 1.0 * np.roll(np.roll(gray, -1, axis=0), -1, axis=1)
)

gy = (
    -1.0 * np.roll(np.roll(gray, 1, axis=0), 1, axis=1) - 2.0 * np.roll(gray, 1, axis=0) - 1.0 * np.roll(np.roll(gray, 1, axis=0), -1, axis=1) +
     1.0 * np.roll(np.roll(gray, -1, axis=0), 1, axis=1) + 2.0 * np.roll(gray, -1, axis=0) + 1.0 * np.roll(np.roll(gray, -1, axis=0), -1, axis=1)
)

# Bump strength factor for rich surface depth
strength = 3.5
nz = np.ones_like(gray)
nx = -gx * strength
ny = -gy * strength

norm = np.sqrt(nx**2 + ny**2 + nz**2)
nx /= norm
ny /= norm
nz /= norm

# Map [-1, 1] to [0, 255]
normal_map = np.stack([
    (nx * 0.5 + 0.5) * 255.0,
    (ny * 0.5 + 0.5) * 255.0,
    (nz * 0.5 + 0.5) * 255.0
], axis=-1)

normal_img = Image.fromarray(np.clip(normal_map, 0, 255).astype(np.uint8))
normal_img.save("Assets/Textures/Mangrove_Mud_Surface_Normal.png", format="PNG")
normal_img.save("Assets/Textures/PBR_Mud/brown_mud_nor_1k.jpg", quality=95)
print("Saved normal maps!")

# Compute Smoothness map: Dark wet silt & puddles are glossy (smooth), roots are matte
smoothness = 0.75 - 0.45 * (gray - np.min(gray)) / (np.max(gray) - np.min(gray) + 1e-5)
smoothness = np.clip(smoothness, 0.20, 0.82) * 255.0
smooth_img = Image.fromarray(smoothness.astype(np.uint8))
smooth_img.save("Assets/Textures/Mangrove_Mud_Surface_Smoothness.png", format="PNG")
print("Saved smoothness map!")
print("All texture generation successfully completed!")
