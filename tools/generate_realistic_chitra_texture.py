import math
import numpy as np
from PIL import Image

def build_texture():
    obj_path = r'Assets\Models\Deer\Mesh_Deer.obj'
    out_path = r'Assets\Textures\Tex_Chitra_Deer_2K.png'
    size = 2048

    print(f"Reading OBJ from {obj_path}...")
    verts = []
    uvs = []
    faces = []

    with open(obj_path, 'r', encoding='utf-8') as f:
        for line in f:
            if line.startswith('v '):
                parts = line.strip().split()
                verts.append([float(parts[1]), float(parts[2]), float(parts[3])])
            elif line.startswith('vt '):
                parts = line.strip().split()
                uvs.append([float(parts[1]), float(parts[2])])
            elif line.startswith('f '):
                parts = line.strip().split()[1:]
                vis = []
                uvis = []
                for p in parts:
                    vals = p.split('/')
                    vis.append(int(vals[0]) - 1)
                    uvis.append(int(vals[1]) - 1 if len(vals) > 1 and vals[1] else -1)
                if len(vis) == 3:
                    faces.append((vis, uvis))
                elif len(vis) == 4:
                    faces.append(([vis[0], vis[1], vis[2]], [uvis[0], uvis[1], uvis[2]]))
                    faces.append(([vis[0], vis[2], vis[3]], [uvis[0], uvis[2], uvis[3]]))

    pos_buffer = np.zeros((size, size, 3), dtype=np.float32)
    has_data = np.zeros((size, size), dtype=bool)

    for vis, uvis in faces:
        if any(ui < 0 for ui in uvis): continue
        v0, v1, v2 = np.array(verts[vis[0]]), np.array(verts[vis[1]]), np.array(verts[vis[2]])
        uv0 = np.array(uvs[uvis[0]]) * (size - 1)
        uv1 = np.array(uvs[uvis[1]]) * (size - 1)
        uv2 = np.array(uvs[uvis[2]]) * (size - 1)

        min_x = max(0, int(math.floor(min(uv0[0], uv1[0], uv2[0]))))
        max_x = min(size - 1, int(math.ceil(max(uv0[0], uv1[0], uv2[0]))))
        min_y = max(0, int(math.floor(min(uv0[1], uv1[1], uv2[1]))))
        max_y = min(size - 1, int(math.ceil(max(uv0[1], uv1[1], uv2[1]))))

        denom = (uv1[1] - uv2[1]) * (uv0[0] - uv2[0]) + (uv2[0] - uv1[0]) * (uv0[1] - uv2[1])
        if abs(denom) < 1e-6: continue

        for py in range(min_y, max_y + 1):
            for px in range(min_x, max_x + 1):
                w0 = ((uv1[1] - uv2[1]) * (px - uv2[0]) + (uv2[0] - uv1[0]) * (py - uv2[1])) / denom
                w1 = ((uv2[1] - uv0[1]) * (px - uv2[0]) + (uv0[0] - uv2[0]) * (py - uv2[1])) / denom
                w2 = 1.0 - w0 - w1
                if w0 >= -0.015 and w1 >= -0.015 and w2 >= -0.015:
                    img_y = size - 1 - py
                    pos_buffer[img_y, px] = w0 * v0 + w1 * v1 + w2 * v2
                    has_data[img_y, px] = True

    active_ys, active_xs = np.where(has_data)
    num_pts = len(active_ys)
    print(f"Rasterized {num_pts} active UV surface pixels. Vectorizing colors...")

    pos = pos_buffer[active_ys, active_xs]
    px = pos[:, 0]
    py = pos[:, 1]
    pz = pos[:, 2]
    abs_x = np.abs(px)

    # Biological Colors
    col_fawn = np.array([194.0, 116.0, 54.0])       # Rich warm golden-rufous
    col_chestnut = np.array([162.0, 88.0, 36.0])    # Upper back rich chestnut
    col_dorsal = np.array([52.0, 28.0, 14.0])       # Dark chocolate dorsal stripe
    col_white = np.array([248.0, 244.0, 238.0])     # Cream white spots/throat
    col_belly = np.array([244.0, 238.0, 230.0])     # Underbelly
    col_antler_beam = np.array([222.0, 212.0, 194.0]) # Polished ivory bone
    col_antler_base = np.array([75.0, 50.0, 32.0])   # Dark burr
    col_hoof = np.array([28.0, 24.0, 24.0])         # Black cloven hooves
    col_nose = np.array([22.0, 18.0, 18.0])         # Muzzle nose pad

    # Base coat gradient
    coat_t = np.clip((py - (-10.0)) / 28.0, 0.0, 1.0)[:, None]
    cols = col_fawn[None, :] * (1.0 - coat_t * 0.45) + col_chestnut[None, :] * (coat_t * 0.45)

    # Dark Dorsal Stripe
    dorsal_mask = (abs_x < 2.8) & (py > 8.0) & (pz >= -48.0) & (pz <= 28.0)
    st = np.clip(1.0 - (abs_x[dorsal_mask] / 2.6), 0.0, 1.0)[:, None]
    cols[dorsal_mask] = cols[dorsal_mask] * (1.0 - st * 0.85) + col_dorsal[None, :] * (st * 0.85)

    # Throat Bib
    throat_mask = (abs_x < 4.2) & (py >= 12.0) & (py <= 28.0) & (pz >= 34.0) & (pz <= 54.0)
    tt = (1.0 - (abs_x[throat_mask] / 4.2))[:, None]
    cols[throat_mask] = cols[throat_mask] * (1.0 - tt * 0.95) + col_white[None, :] * (tt * 0.95)

    # Underbelly & Inner Legs
    belly_mask = (py < -8.0) & (abs_x < 6.8) & (pz >= -40.0) & (pz <= 18.0)
    bt = np.clip((-8.0 - py[belly_mask]) / 6.0, 0.0, 1.0)[:, None]
    cols[belly_mask] = cols[belly_mask] * (1.0 - bt * 0.95) + col_belly[None, :] * (bt * 0.95)

    # Antlers
    antler_mask = (py > 44.0) & (pz > 15.0)
    antler_t = np.clip((py[antler_mask] - 44.0) / 8.0, 0.0, 1.0)[:, None]
    cols[antler_mask] = col_antler_base[None, :] * (1.0 - antler_t) + col_antler_beam[None, :] * antler_t

    # Cloven Hooves
    hoof_mask = (py < -58.0)
    cols[hoof_mask] = col_hoof[None, :]

    # Nose pad
    nose_mask = (pz > 64.0) & (py >= 26.0) & (py <= 39.0) & (abs_x < 4.2)
    cols[nose_mask] = col_nose[None, :]

    # Delicate, Authentic Chitra Spots on flanks
    # Construct spot centers
    spot_centers = []
    row_configs = [
        (16.5, 9, -28.0, 16.0),
        (13.2, 11, -33.0, 19.0),
        (9.8,  12, -35.0, 21.0),
        (6.4,  12, -36.0, 21.0),
        (3.0,  11, -34.0, 19.0),
        (-0.5, 9,  -31.0, 16.0),
        (-4.0, 7,  -26.0, 12.0)
    ]
    for ry, count, zmin, zmax in row_configs:
        for s in range(count):
            frac = s / (count - 1) if count > 1 else 0.5
            sz = zmin + frac * (zmax - zmin) + math.sin(ry * 1.5 + s) * 0.8
            sy = ry + math.sin(s * 2.3) * 0.5
            spot_centers.append((14.2, sy, sz))

    haunch_spots = [
        (13.8, 4.0, -38.0), (14.2, 0.0, -40.0), (13.5, -4.0, -36.0),
        (12.5, -8.0, -34.0), (11.0, 7.0, -34.0), (13.0, -2.0, -42.0)
    ]
    spot_centers.extend(haunch_spots)
    sc_arr = np.array(spot_centers)

    flank_mask = (abs_x > 4.5) & (py >= -7.0) & (py <= 19.0) & (pz >= -44.0) & (pz <= 25.0)
    flank_indices = np.where(flank_mask)[0]

    if len(flank_indices) > 0:
        f_abs_x = abs_x[flank_indices]
        f_py = py[flank_indices]
        f_pz = pz[flank_indices]

        # Vectorized distance to closest spot
        # Chunk computation to keep memory low
        spot_radius = 1.05
        min_d = np.full(len(flank_indices), 999.0, dtype=np.float32)

        for sc in sc_arr:
            dx = (f_abs_x - sc[0]) * 0.85
            dy = (f_py - sc[1])
            dz = (f_pz - sc[2]) / 1.12
            d = np.sqrt(dx*dx + dy*dy + dz*dz)
            min_d = np.minimum(min_d, d)

        in_spot = min_d < spot_radius
        spot_idx = flank_indices[in_spot]
        snorm = min_d[in_spot] / spot_radius
        alpha = np.cos(snorm * np.pi * 0.5)[:, None]
        cols[spot_idx] = cols[spot_idx] * (1.0 - alpha) + col_white[None, :] * alpha

    # Write to image buffer
    output = np.zeros((size, size, 3), dtype=np.uint8)
    output[active_ys, active_xs] = np.clip(cols, 0, 255).astype(np.uint8)

    # 4-pass edge dilation
    dilated = output.copy()
    mask = has_data.copy()
    for _ in range(4):
        new_mask = mask.copy()
        for dy, dx in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
            shifted = np.roll(np.roll(dilated, dy, axis=0), dx, axis=1)
            shifted_mask = np.roll(np.roll(mask, dy, axis=0), dx, axis=1)
            fill_indices = (~mask) & shifted_mask
            dilated[fill_indices] = shifted[fill_indices]
            new_mask |= fill_indices
        mask = new_mask

    img = Image.fromarray(dilated, 'RGB')
    img.save(out_path, format='PNG')
    print(f"SUCCESS: Baked authentic Chitra Deer 2K texture to {out_path}!")

if __name__ == '__main__':
    build_texture()
