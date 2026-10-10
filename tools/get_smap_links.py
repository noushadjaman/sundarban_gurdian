import urllib.request
import json

target_id = 'C2208422957-POCLOUD'
url = f'https://cmr.earthdata.nasa.gov/search/granules.json?collection_concept_id={target_id}&temporal=2025-12-15T00:00:00Z,2025-12-20T23:59:59Z'
req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
with urllib.request.urlopen(req) as resp:
    data = json.loads(resp.read().decode('utf-8'))

granules = data['feed']['entry']
print(f"Found {len(granules)} granules:")
for g in granules:
    print(f"Title: {g['title']}")
    for link in g.get('links', []):
        href = link.get('href', '')
        if 'dap' in href or 'nc' in href or 'podaac' in href:
            print(f"  Link ({link.get('rel', '')}): {href}")
