import struct, json
import numpy as np

with open('Assets/ExternalModels/mangrove_environment.glb', 'rb') as f:
    f.seek(12)
    jlen, _ = struct.unpack('<II', f.read(8))
    gltf = json.loads(f.read(jlen).decode('utf-8'))
    f.seek(12 + 8 + jlen)
    blen, btype = struct.unpack('<II', f.read(8))
    bin_data = f.read(blen)

# Let's inspect the nodes and scene hierarchy in gltf
nodes = gltf.get('nodes', [])
print(f"Total nodes in GLB: {len(nodes)}")
for i, n in enumerate(nodes[:25]):
    print(f"Node {i}: {n.get('name')}, children={len(n.get('children', []))}")

# Check water mesh
water_meshes = [m for m in gltf.get('meshes', []) if 'water' in m.get('name', '').lower()]
print(f"\nWater meshes: {len(water_meshes)}")
for wm in water_meshes[:5]:
    print("Water mesh:", wm.get('name'))
    prim = wm['primitives'][0]
    pos_acc = gltf['accessors'][prim['attributes']['POSITION']]
    print("  pos min/max:", pos_acc.get('min'), pos_acc.get('max'))

# Check terrain bounds across all 400 chunks
t_min_x, t_max_x = 1e9, -1e9
t_min_z, t_max_z = 1e9, -1e9
t_min_y, t_max_y = 1e9, -1e9
for m in gltf.get('meshes', []):
    if 'terrain' in m.get('name', '').lower():
        prim = m['primitives'][0]
        pos_acc = gltf['accessors'][prim['attributes']['POSITION']]
        pmin = pos_acc.get('min', [0,0,0])
        pmax = pos_acc.get('max', [0,0,0])
        # chunk position might come from node transform
print("Checked terrain meshes.")
