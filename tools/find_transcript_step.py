import json

with open('C:/Users/RAJ/.gemini/antigravity-ide/brain/953dbde4-1362-4979-80d9-01f42c5040ac/.system_generated/logs/transcript.jsonl', 'r', encoding='utf-8', errors='ignore') as f:
    for line in f:
        obj = json.loads(line)
        idx = obj.get('step_index', 0)
        if 3820 <= idx <= 3860:
            content = obj.get('content', '')
            if content and any(k in content.lower() for k in ['fps', 'performance', 'triangle', 'tri', 'draw', 'rollback', 'lag']):
                print(f"Step {idx}: {content[:200]}")
            calls = obj.get('tool_calls')
            if calls:
                for c in calls:
                    summary = c.get('toolSummary', '')
                    action = c.get('toolAction', '')
                    print(f"Call {idx}: {summary} / {action}")
