import urllib.request
import json

target_id = 'C2208422957-POCLOUD'

# Query December 2025
url = f'https://cmr.earthdata.nasa.gov/search/granules.json?collection_concept_id={target_id}&temporal=2025-12-01T00:00:00Z,2025-12-25T23:59:59Z&page_size=10'
req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
with urllib.request.urlopen(req) as resp:
    data = json.loads(resp.read().decode('utf-8'))

print(f"Total granules found for Dec 2025 (temporal only): {len(data['feed']['entry'])}")
for g in data['feed']['entry'][:3]:
    print(f"  {g['title']} | {g['time_start']}")

# Now test with bounding_box
bb_url = f'https://cmr.earthdata.nasa.gov/search/granules.json?collection_concept_id={target_id}&temporal=2025-12-01T00:00:00Z,2025-12-25T23:59:59Z&bounding_box=89.5,21.0,91.5,22.0&page_size=10'
bb_req = urllib.request.Request(bb_url, headers={'User-Agent': 'Mozilla/5.0'})
with urllib.request.urlopen(bb_req) as resp:
    bb_data = json.loads(resp.read().decode('utf-8'))

print(f"Total granules found with bounding_box: {len(bb_data['feed']['entry'])}")
