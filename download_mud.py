import urllib.request
import json
import os

target_dir = r"F:\SundarbanGuardian\Assets\Textures\PBR_Mud"
os.makedirs(target_dir, exist_ok=True)

asset = "brown_mud_leaves_01"
req = urllib.request.Request(f"https://api.polyhaven.com/files/{asset}", headers={"User-Agent": "Mozilla/5.0"})

with urllib.request.urlopen(req, timeout=15) as resp:
    data = json.loads(resp.read().decode())

downloads = {
    "Diffuse": "brown_mud_diff_1k.jpg",
    "Rough": "brown_mud_rough_1k.jpg",
    "AO": "brown_mud_ao_1k.jpg",
    "nor_dx": "brown_mud_nor_1k.jpg"
}

for map_type, file_name in downloads.items():
    if map_type in data:
        res1k = data[map_type].get("1k", {})
        fmt = "jpg" if "jpg" in res1k else ("png" if "png" in res1k else None)
        if fmt:
            url = res1k[fmt]["url"]
            dest_path = os.path.join(target_dir, file_name)
            if not os.path.exists(dest_path):
                print(f"Downloading {map_type} from {url}...")
                urllib.request.urlretrieve(url, dest_path)
                print(f"Done! Size: {os.path.getsize(dest_path)} bytes")
            else:
                print(f"{file_name} already exists.")

print("All PBR maps ready!")
