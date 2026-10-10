import json

# Let's inspect the GLB structure or use Unity MCP
import struct

with open('Assets/ExternalModels/mangrove_environment.glb', 'rb') as f:
    magic, version, length = struct.unpack('<III', f.read(12))
    chunk_len, chunk_type = struct.unpack('<II', f.read(8))
    json_bytes = f.read(chunk_len)
    gltf = json.loads(json_bytes.decode('utf-8'))

print('Materials in GLB:')
for i, m in enumerate(gltf.get('materials', [])):
    print(f"Material {i}: {m.get('name')}")
    pbr = m.get('pbrMetallicRoughness', {})
    print(f"  baseColorFactor: {pbr.get('baseColorFactor')}")
    print(f"  baseColorTexture: {pbr.get('baseColorTexture')}")
    print(f"  metallicFactor: {pbr.get('metallicFactor')}, roughnessFactor: {pbr.get('roughnessFactor')}")
    print(f"  normalTexture: {m.get('normalTexture')}")

print('\nTextures in GLB:')
for i, t in enumerate(gltf.get('textures', [])):
    print(f"Texture {i}: source={t.get('source')}, sampler={t.get('sampler')}")

print('\nImages in GLB:')
for i, img in enumerate(gltf.get('images', [])):
    print(f"Image {i}: name={img.get('name')}, mimeType={img.get('mimeType')}, bufferView={img.get('bufferView')}")

print('\nMeshes count:', len(gltf.get('meshes', [])))
terrain_meshes = [m for m in gltf.get('meshes', []) if 'terrain' in m.get('name', '').lower() or 'chunk' in m.get('name', '').lower() or 'ground' in m.get('name', '').lower() or 'mud' in m.get('name', '').lower()]
print('Terrain meshes found:', len(terrain_meshes))
if terrain_meshes:
    print('Sample terrain mesh:', terrain_meshes[0].get('name'))
    for p in terrain_meshes[0].get('primitives', []):
        print('  Primitive material:', p.get('material'))
        print('  Primitive attributes:', list(p.get('attributes', {}).keys()))
