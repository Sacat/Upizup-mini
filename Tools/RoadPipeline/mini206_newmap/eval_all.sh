#!/bin/bash
# usage: eval_all.sh <new.npz>   prints the acceptance numbers
S=/tmp/claude-0/-home-user-Upizup-mini/5e0ce088-1b94-5332-9321-9206d0066282/scratchpad; cd $S
bvenv/bin/python scripts/analyse_roads.py $1 > $1.an 2>&1
echo "worst slope change /0.5m:"; grep "max grade change" $1.an | awk '{print "  ",$1, $(NF-4)}' | sort -t' ' -k3 -r | head -3
echo "worst junction step:"; sed -n '/3) JUNCTIONS/,$p' $1.an | awk '{for(i=1;i<=NF;i++) if($i=="max") print "  ",$1,$3,$(i+1)}' | sort -k3 -n -r | head -2
bvenv/bin/python scripts/poke_check.py $1 2>/dev/null | head -1 | cut -c1-70
bvenv/bin/python scripts/export_apply.py out/geo.npz $1 out/roads.json $1.apply.json >/dev/null 2>&1
python3 -c "
import json;d=json.load(open('$1.apply.json'))
r=sorted(d['notMovedReport'],key=lambda x:-abs(x['dy'])); print('structures (not moved) worst:',[(x['dy'],x['path'].split('/')[-2][:30]) for x in r[:5]])
g=sorted(d['groundFollow'],key=lambda x:-abs(x['dy'])); print('props/NPCs ground-follow:',len(g),'worst',[(x['dy'],x['path'].split('/')[-1][:20]) for x in g[:4]])"
