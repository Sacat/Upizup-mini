import ast,math,json,csv
from pathlib import Path
P=Path(__file__).resolve().parent
source=ast.parse((P/'build_airmax.py').read_text(encoding='utf-8-sig'))
funcs=[n for n in source.body if isinstance(n,ast.FunctionDef) and n.name in ['interp','width','spring','sole_top','base','top','archmax']]
for model in ['90','97']:
 d=json.loads((P/('AM'+model)/'measurements.json').read_text());g={'math':math,'MODEL':model,'WIDTH':d['stations_width_mm'],'TOP':d['stations_top_mm']}
 exec(compile(ast.Module(body=funcs,type_ignores=[]),'profile-functions','exec'),g)
 with (P/('AM'+model)/'profile-samples-mm.csv').open('w',newline='') as f:
  w=csv.writer(f);w.writerow(['X_heel_to_toe_mm','half_width_mm','toe_spring_mm','sole_top_Z_mm','upper_base_Z_mm','upper_max_Z_mm','opening_angle_radians'])
  for x in range(-145,146):w.writerow([x]+[round(g[k](x),6) for k in ['width','spring','sole_top','base','top','archmax']])
print('PROFILE_SAMPLES_COMPLETE')
