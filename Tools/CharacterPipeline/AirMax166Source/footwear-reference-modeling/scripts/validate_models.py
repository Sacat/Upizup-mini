import bpy, json, math
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parent
results=[]
for model in ['90','97']:
 out=P/('AM'+model);bpy.ops.wm.open_mainfile(filepath=str(out/('AirMax'+model+'.blend')))
 deps=bpy.context.evaluated_depsgraph_get();m=json.loads((out/'measurements.json').read_text());r={'model':model,'air_cavity_checks':[],'finite_vertices':True,'source_bounds_m':None}
 points=[];triangles=0;zero=0
 for o in bpy.context.scene.objects:
  if o.type not in ('MESH','CURVE') or o.name.startswith('Studio'):continue
  ev=o.evaluated_get(deps);me=ev.to_mesh();me.calc_loop_triangles();triangles+=len(me.loop_triangles)
  for v in me.vertices:
   pt=o.matrix_world@v.co;points.append(pt.copy());r['finite_vertices'] &= all(math.isfinite(c) for c in pt)
  for t in me.loop_triangles:
   v0,v1,v2=[me.vertices[i].co for i in t.vertices]
   if (v1-v0).cross(v2-v0).length<1e-14:zero+=1
  ev.to_mesh_clear()
 r['source_bounds_m']=[[min(p[i] for p in points) for i in range(3)],[max(p[i] for p in points) for i in range(3)]];r['evaluated_triangles']=triangles;r['near_zero_area_triangles']=zero
 foam=bpy.data.objects['Sculpted foam midsole'].evaluated_get(deps)
 for x,L,z,h in m['window_specs_x_length_z_height_mm']:
  hit=foam.ray_cast(Vector((x/1000,-.2,z/1000)),Vector((0,1,0)),distance=.4)[0]
  r['air_cavity_checks'].append({'x_mm':x,'z_mm':z,'foam_blocks_tunnel':bool(hit)});assert not hit,'Air cavity blocked'
 hit,loc,n,ix,obj,mat=bpy.context.scene.ray_cast(deps,Vector((-.09,0,.2)),Vector((0,0,-1)))
 r['collar_opening_ray']={'hit':obj.name if obj else None,'height_m':loc.z if hit else None}
 assert hit and loc.z<.06,'Collar unexpectedly capped at upper height'
 tongue=bpy.data.objects['Separate padded tongue'].evaluated_get(deps);tested=0;minclear=10
 for ob in bpy.context.scene.objects:
  if not ob.name.startswith('Crossed lace'):continue
  for v in ob.data.vertices:
   p=ob.matrix_world@v.co
   hit,loc,n,face=tongue.ray_cast(Vector((p.x,p.y,.5)),Vector((0,0,-1)),distance=1)
   if hit:
    tested+=1;minclear=min(minclear,p.z-loc.z)
 r['lace_vertices_above_tongue_tested']=tested;r['minimum_lace_clearance_mm']=minclear*1000
 assert minclear>-.0001,'Lace passes through tongue'
 assert r['finite_vertices'];assert .28<r['source_bounds_m'][1][0]-r['source_bounds_m'][0][0]<.31
 # Reopen actual GLB in a clean scene and compare bounds; import is not Unity proof.
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 bpy.ops.import_scene.gltf(filepath=str(out/('AirMax'+model+'.glb')))
 pts=[];r['export_meshes']=0;r['export_triangles']=0
 for ob in bpy.context.scene.objects:
  if ob.type!='MESH':continue
  r['export_meshes']+=1;ob.data.calc_loop_triangles();r['export_triangles']+=len(ob.data.loop_triangles)
  pts.extend([ob.matrix_world@v.co for v in ob.data.vertices])
 r['export_bounds_m']=[[min(p[i] for p in pts) for i in range(3)],[max(p[i] for p in pts) for i in range(3)]]
 r['export_bounds_max_error_mm']=max(abs(r['export_bounds_m'][j][i]-r['source_bounds_m'][j][i])*1000 for i in range(3) for j in range(2))
 assert r['export_bounds_max_error_mm']<.1,'Export scale/axis/bounds mismatch'
 r['status']='PASS';results.append(r);print('AIRMAX_VALIDATED',model,json.dumps(r))
(P/'validation.json').write_text(json.dumps(results,indent=2));print('AIRMAX_VALIDATION_COMPLETE')
