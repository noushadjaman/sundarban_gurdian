import urllib.request
import json

url = 'https://cmr.earthdata.nasa.gov/search/collections.json?keyword=SMAP+CAP+L3+salinity'
req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
with urllib.request.urlopen(req) as resp:
    data = json.loads(resp.read().decode('utf-8'))

entries = data['feed']['entry']
print(f'Found {len(entries)} collections:')
target_id = None
for e in entries:
    title = e['title']
    cid = e['id']
    print(f'  {cid} -> {title}')
    if 'V5.0' in title and '8-Day' in title:
        target_id = cid

if target_id:
    print(f'\nChecking latest granules for: {target_id}')
    g_url = f'https://cmr.earthdata.nasa.gov/search/granules.json?collection_concept_id={target_id}&sort_key=-start_date&page_size=5'
    g_req = urllib.request.Request(g_url, headers={'User-Agent': 'Mozilla/5.0'})
    with urllib.request.urlopen(g_req) as g_resp:
        g_data = json.loads(g_resp.read().decode('utf-8'))
    granules = g_data['feed']['entry']
    print(f'Latest {len(granules)} granules:')
    for g in granules:
        print(f"  Granule: {g.get('title')}")
        print(f"  Start: {g.get('time_start')}, End: {g.get('time_end')}")
