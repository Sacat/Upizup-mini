"""Export approved Blender shoes to Unity-readable mesh JSON/palette, no FBX rig remap.
Source stays immutable. Run with Blender --background --python this_file.
"""
import bpy,math,json,struct,zlib
from pathlib import Path
SOURCE=Path(__file__).resolve().parent/'AirMax166Source'
OUT=Path(r'E:/Unity/Up Iz Up Mini/Assets/UpIzUpMini/Art/Characters/Garments/AirMax166')
LOG=Path(r'E:/Unity/Up Iz Up Mini/Logs/Tasks/MINI-166/AirMaxIntegration')
OUT.mkdir(exist_ok=True);stats={}
def png(path,colors):
 def srgb(v):return 12.92*v if v<=.0031308 else 1.055*v**(1/2.4)-.055
 rows=[]
 for y in range(32):
  row=bytearray([0])
  for x in range(32):
   idx=((31-y)//8)*4+x//8;col=colors[idx] if idx<len(colors) else (1,1,1)
   row.extend(bytes(round(max(0,min(1,srgb(c)))*255) for c in col[:3]))
  rows.append(row)
 def chunk(k,data):return struct.pack('>I',len(data))+k+data+struct.pack('>I',zlib.crc32(k+data)&0xffffffff)
 path.write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',32,32,8,2,0,0,0))+chunk(b'IDAT',zlib.compress(b''.join(rows)))+chunk(b'IEND',b''))
for model in ['90','97']:
 bpy.ops.wm.open_mainfile(filepath=str(SOURCE/('AM'+model)/('AirMax'+model+'.blend')))
 mats=[];colors=[];names=[]
 for mat in bpy.data.materials:
  if mat.name in ['Studio floor','Inspection clay']:continue
  if mat.use_nodes:
   node=mat.node_tree.nodes.get('Principled BSDF')
   if node:mats.append(mat);colors.append(tuple(node.inputs['Base Color'].default_value));names.append(mat.name)
 assert len(mats)<=16
 bpy.ops.object.select_all(action="DESELECT")
 records=[];before=0;removed_knit=0
 for ob in list(bpy.context.scene.objects):
  if ob.type not in ['MESH','CURVE'] or ob.name.startswith('Studio'):continue
  if ob.name.startswith('Toe mesh knit'):removed_knit+=1;continue
  # Reduce only microtessellation; retain silhouette objects, Air supports and ribbon shape.
  if ob.type=='CURVE':ob.data.bevel_resolution=0
  bpy.context.view_layer.objects.active=ob;ob.select_set(True)
  if ob.type=='CURVE':bpy.ops.object.convert(target='MESH')
  deps=bpy.context.evaluated_depsgraph_get();ev=ob.evaluated_get(deps);me=ev.to_mesh();me.calc_loop_triangles();cnt=len(me.loop_triangles);before+=cnt;ev.to_mesh_clear()
  if cnt>160:
   dec=ob.modifiers.new('Runtime tessellation reduction','DECIMATE');dec.ratio=.11 if ob.name.startswith('Side swoosh') else (.32 if ob.name.startswith('Transparent Air') else .4)
  ob.select_set(False);records.append(ob)
 deps=bpy.context.evaluated_depsgraph_get();vs=[];ns=[];uv=[];groups=[[],[],[]];lookup={};dropped=0
 for ob in records:
  ev=ob.evaluated_get(deps);me=ev.to_mesh();me.calc_loop_triangles();normalmat=ob.matrix_world.inverted().transposed()
  for t in me.loop_triangles:
   pts=[ob.matrix_world@me.vertices[i].co for i in t.vertices]
   if (pts[1]-pts[0]).cross(pts[2]-pts[0]).length<1e-12:dropped+=1;continue
   mat=me.materials[t.material_index];mi=names.index(mat.name)
   slot=2 if 'Air bladder' in mat.name else (0 if mat.name in ['Woven mesh','Silver synthetic panels','Soft grey suede'] else 1)
   for vi,li,pt in zip(t.vertices,t.loops,pts):
    n=normalmat@(t.normal if ob.name in ['Thin rubber outsole','Sculpted foam midsole'] else me.corner_normals[li].vector);n.normalize();u=((mi%4+.5)/4,(mi//4+.5)/4)
    key=tuple(round(v,7) for v in pt)+tuple(round(v,5) for v in n)+(mi,)
    if key not in lookup:
     lookup[key]=len(vs)//3;vs.extend(round(v,8) for v in pt);ns.extend(round(v,7) for v in n);uv.extend(u)
    groups[slot].append(lookup[key])
  ev.to_mesh_clear()
 data={'positions':vs,'normals':ns,'uv':uv,'opaqueTint':groups[0],'opaqueFixed':groups[1],'air':groups[2]}
 (OUT/('AM'+model+'.json')).write_text(json.dumps(data,separators=(',',':')))
 png(OUT/('AM'+model+'Palette.png'),colors)
 stats[model]={'source':str(SOURCE/('AM'+model)/('AirMax'+model+'.blend')),'vertices_single':len(vs)//3,'triangles_single':sum(map(len,groups))//3,'triangles_before_reduction_without_knit':before,'discarded_degenerate_triangles':dropped,'omitted_microknit_objects':removed_knit,'submeshes':3,'palette_px':32,'palette_materials':names}
(LOG/'export-budget.json').write_text(json.dumps(stats,indent=2));print('AIRMAX_UNITY_EXPORT_PASS',json.dumps(stats))
