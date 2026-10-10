import re

with open('Assets/Scenes/Trial_MangroveEnvironment.unity', 'r', encoding='utf-8', errors='ignore') as f:
    text = f.read()

gameobjects = re.findall(r'm_Name:\s*(.*)', text)
print(f'Total GameObjects in scene: {len(gameobjects)}')
print('First 30 GameObjects:')
for g in gameobjects[:30]:
    print('  -', g)

materials = set(re.findall(r'm_FileID:\s*\d+,\s*guid:\s*([a-f0-9]+)', text))
print(f'Total GUID references: {len(materials)}')

guid_matches = re.findall(r'guid:\s*([a-f0-9]+)', text)
from collections import Counter
counts = Counter(guid_matches).most_common(15)
print('Most common GUIDs:')
for guid, c in counts:
    print(f'  {guid}: {c}')
