"""Reproduce approved MINI-141 exports and approval-only botanical bud preview.
Run Blender --background --python Tools/ArtPreview/mini142_export.py.
No Unity writes; palette PNG stores sRGB, Unity mesh coordinates X,Z,Y.
"""
import bpy, math, random, json, zlib, struct, sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(r'E:\Unity\Up Iz Up Mini')
OUT=ROOT/'Logs/Tasks/MINI-142/Export'; OUT.mkdir(parents=True,exist_ok=True)
BUDS=ROOT/'Logs/Tasks/MINI-142/Buds'; BUDS.mkdir(parents=True,exist_ok=True)
source=(ROOT/'Tools/ArtPreview/mini141_preview.py').read_text()
ns={}
exec(source[:source.index("cube('Sample ground'")].replace('OUT=Path(r"E:\\Unity\\Up Iz Up Mini\\Logs\\Tasks\\MINI-141")','OUT=Path(r"E:\\Unity\\Up Iz Up Mini\\Logs\\Tasks\\MINI-142\\Export")'),ns)
palette=[]; summary={}
def color_index(material):
    c=tuple(round(float(v),6) for v in material.diffuse_color[:3]) if material else (.5,.5,.5)
    if c not in palette: palette.append(c)
    return palette.index(c)
def part(name,objects,origin=(0,0,0),decimate=False,use_uv=False):
    vs=[]; normals=[]; tris=[]; uv=[]
    for obj in sorted(objects,key=lambda o:o.name):
        if obj.type!='MESH': continue
        if decimate:
            mod=obj.modifiers.new('MINI142 distant simplification','DECIMATE'); mod.ratio=.5
        deps=bpy.context.evaluated_depsgraph_get(); ev=obj.evaluated_get(deps); me=ev.to_mesh(); me.calc_loop_triangles()
        normal_matrix=obj.matrix_world.to_3x3().inverted().transposed()
        for tri in me.loop_triangles:
            material=me.materials[tri.material_index] if me.materials else None
            cell=color_index(material) if not use_uv else 0; u=((cell%8)+.5)/8; v=((cell//8)+.5)/8
            base=len(vs)
            for li in tri.loops:
                loop=me.loops[li]; p=obj.matrix_world@me.vertices[loop.vertex_index].co-Vector(origin)
                n=(normal_matrix@me.corner_normals[li].vector).normalized()
                if use_uv: u,v=me.uv_layers.active.data[li].uv
                vs.append(dict(x=round(p.x,6),y=round(p.z,6),z=round(p.y,6)))
                normals.append(dict(x=round(n.x,6),y=round(n.z,6),z=round(n.y,6))); uv.append(dict(x=u,y=v))
            tris.extend([base,base+2,base+1])
        ev.to_mesh_clear()
        if decimate: obj.modifiers.remove(mod)
    return dict(name=name,vertices=vs,normals=normals,triangles=tris,uv=uv)
def asset(name,groups,origin=(0,0,0),lod=False):
    parts=[part(n,obs,origin) for n,obs in groups.items() if obs]
    points=[v for p in parts for v in p['vertices']]
    bounds={k:dict(min=min(v[k] for v in points),max=max(v[k] for v in points)) for k in ('x','y','z')}
    data=dict(name=name,parts=parts,bounds=bounds,dimensions={k:round(b['max']-b['min'],6) for k,b in bounds.items()},palette='palette.png')
    if lod: data['lods']=[dict(name='LOD1',parts=[part(n,obs,origin,True) for n,obs in groups.items()])]
    (OUT/(name+'.json')).write_text(json.dumps(data,separators=(',',':')))
    summary[name]=dict(triangles=sum(len(p['triangles'])//3 for p in parts),dimensions=data['dimensions'],parts=list(groups))
    if lod: summary[name]['lod1Triangles']=sum(len(p['triangles'])//3 for p in data['lods'][0]['parts'])
def build_objects(fn,*args):
    before=set(bpy.data.objects); fn(*args); return set(bpy.data.objects)-before
def export_approved_cannabis():
    global palette
    # Isolated additive path: existing house/crop buffers and palette never written.
    palette=[tuple(c) for c in json.loads((OUT/'manifest.json').read_text())['paletteLinearColors']]
    local_source=Path(__file__).read_text()
    crop_source=source[source.index('# Crop silhouettes:'):source.index("cube('Studio ground'")]
    exec(crop_source,ns)
    globals()['crop']=crop_source
    exec(local_source[local_source.rindex('# Approval-only revision.'):local_source.rindex("ns['cannabis'](-.75")],globals())
    plants=[]
    for name,x,purple in [('CannabisGreen',-.75,False),('CannabisPurple',.75,True)]:
        obs=build_objects(ns['cannabis'],x,0,purple)
        groups={'Foliage':[], 'Fruit_A':[], 'Fruit_B':[], 'Pistils':[]}
        cores=sorted([o for o in obs if 'cola' in o.name.lower()],key=lambda o:o.name)
        for o in sorted(obs,key=lambda obj:obj.name):
            key='Pistils' if 'pistil' in o.name.lower() else ('Fruit_A' if any(w in o.name.lower() for w in ('cola','calyx')) else 'Foliage')
            if key=='Fruit_A':
                closest=min(range(len(cores)),key=lambda i:(o.location-cores[i].location).length_squared)
                key='Fruit_A' if closest%2==0 else 'Fruit_B'
            groups[key].append(o)
        plants.append((name,x,groups))
    textured=[o for _,_,groups in plants for key in ('Fruit_A','Fruit_B','Pistils') for o in groups[key]]
    # Retain every source object during baking: Generated coordinates stay local,
    # preserving the approved procedural appearance rather than changing on join.
    bpy.ops.object.select_all(action='DESELECT')
    for o in textured: o.select_set(True)
    bpy.context.view_layer.objects.active=textured[0]
    # Primitive UVs already exist. Deterministic per-object tile packing avoids an
    # expensive general island solver while retaining source object coordinates.
    grid=math.ceil(math.sqrt(len(textured)))
    for i,o in enumerate(textured):
        layer=o.data.uv_layers.active
        if layer is None: raise RuntimeError('Missing primitive UV: '+o.name)
        for item in layer.data:
            item.uv=((i%grid+.08+item.uv.x*.84)/grid,(i//grid+.08+item.uv.y*.84)/grid)
    print('CANNABIS_UV_READY '+str(len(textured)),flush=True)
    sc=bpy.context.scene; sc.render.engine='CYCLES'; sc.cycles.samples=8
    sc.render.bake.use_pass_direct=False; sc.render.bake.use_pass_indirect=False; sc.render.bake.use_pass_color=True
    sc.render.bake.margin=2
    materials=set(m for o in textured for m in o.data.materials)
    # One temporary joined bake target avoids Cycles resync for 1022 objects.
    # Explicit per-vertex Generated coordinates retain each source's procedural
    # mapping after join; interpolation is affine and matches original bounds.
    bpy.ops.object.select_all(action='DESELECT')
    copies=[]
    for o in textured:
        attr=o.data.attributes.new('source_generated','FLOAT_VECTOR','POINT')
        coords=[v.co for v in o.data.vertices]
        lo=Vector(tuple(min(v[k] for v in coords) for k in range(3))); hi=Vector(tuple(max(v[k] for v in coords) for k in range(3)))
        for v,item in zip(o.data.vertices,attr.data): item.vector=tuple((v.co[k]-lo[k])/max(hi[k]-lo[k],1e-8) for k in range(3))
        copy=o.copy(); copy.data=o.data.copy(); bpy.context.collection.objects.link(copy); copy.select_set(True); copies.append(copy)
        o.hide_render=True
    for m in materials:
        attrnode=m.node_tree.nodes.new('ShaderNodeAttribute'); attrnode.attribute_name='source_generated'
        for node in m.node_tree.nodes:
            if node.bl_idname in ('ShaderNodeTexNoise','ShaderNodeTexVoronoi'): m.node_tree.links.new(attrnode.outputs['Vector'],node.inputs['Vector'])
    bpy.context.view_layer.objects.active=copies[0]; bpy.ops.object.join(); bake_target=bpy.context.object
    print('CANNABIS_JOINED_BAKE_READY',flush=True)
    for suffix,baketype,colorspace in [('Albedo','DIFFUSE','sRGB'),('Normal','NORMAL','Non-Color')]:
        img=bpy.data.images.new('Cannabis'+suffix,width=1024,height=1024,alpha=False)
        img.colorspace_settings.name=colorspace
        for m in materials:
            node=m.node_tree.nodes.new('ShaderNodeTexImage'); node.image=img; m.node_tree.nodes.active=node
        bpy.ops.object.bake(type=baketype)
        img.filepath_raw=str(OUT/('Cannabis'+suffix+'.png')); img.file_format='PNG'; img.save()
    bpy.data.objects.remove(bake_target,do_unlink=True)
    for o in textured: o.hide_render=False
    report={}
    for name,x,groups in plants:
        parts=[]; lodparts=[]
        for key,obs in groups.items():
            p=part(key,obs,(x,0,0),use_uv=key!='Foliage')
            p['material']='palette' if key=='Foliage' else 'CannabisBaked'
            parts.append(p)
            # Distant LOD drops subpixel pistils and halves supported flower solids.
            if key!='Pistils':
                p1=part(key,obs,(x,0,0),decimate=key.startswith('Fruit'),use_uv=key!='Foliage')
                p1['material']=p['material']; lodparts.append(p1)
        points=[v for p in parts for v in p['vertices']]
        bounds={k:dict(min=min(v[k] for v in points),max=max(v[k] for v in points)) for k in ('x','y','z')}
        data=dict(name=name,parts=parts,lods=[dict(name='LOD1',parts=lodparts)],bounds=bounds,dimensions={k:b['max']-b['min'] for k,b in bounds.items()},palette='palette.png')
        (OUT/(name+'.json')).write_text(json.dumps(data,separators=(',',':')))
        report[name]=dict(lod0Triangles=sum(len(p['triangles'])//3 for p in parts),lod1Triangles=sum(len(p['triangles'])//3 for p in lodparts),bounds=bounds,parts={p['name']:len(p['triangles'])//3 for p in parts})
    report['materials']={'Foliage':'Existing unchanged palette.png, opaque two-sided','Fruit and Pistils':'CannabisAlbedo.png sRGB + CannabisNormal.png imported as NormalMap; white tint; opaque; recalculate Unity tangents after assigning exported UV/normals'}
    report['notes']=['User approved latest MINI142 elongated calyx preview; no shape redesign','All four parts share grounded root pivot; grow together. Fruit_A/B alternate whole colas for two-tone hybrids; Pistils separate','1024px shared bake for both strains retains more tiny relief than 512px; Unity mobile max-size512 can be evaluated','LOD1 removes tiny pistils and decimates flower solids 50%; foliage retained','Unity X,Z,Y with reversed winding; normal texture tangent basis requires Unity visual check','Cannabis source provisional stage colours should not overwrite mature baked albedo','Existing palette and house/crop files unchanged']
    (OUT/'cannabis-manifest.json').write_text(json.dumps(report,indent=2))
    print('CANNABIS_BUFFERS_READY '+json.dumps(report),flush=True)
    # Verify the shipped maps, not the richer unbaked procedural nodes.
    for m in materials:
        nodes=m.node_tree.nodes; links=m.node_tree.links
        albedo=nodes.new('ShaderNodeTexImage'); albedo.image=bpy.data.images['CannabisAlbedo']
        normal=nodes.new('ShaderNodeTexImage'); normal.image=bpy.data.images['CannabisNormal']
        normalmap=nodes.new('ShaderNodeNormalMap')
        links.new(albedo.outputs['Color'],nodes.get('Principled BSDF').inputs['Base Color'])
        links.new(normal.outputs['Color'],normalmap.inputs['Color']); links.new(normalmap.outputs[0],nodes.get('Principled BSDF').inputs['Normal'])
    ns['cube']('Neutral soil pedestal',(0,0,-.07),(2.7,1.8,.14),ns['soil'],.025)
    ns['cube']('Studio floor',(0,0,-.20),(200,200,.1),ns['stage'])
    ns['setup']((3,-6,3.1),(0,0,.9),3.4)
    sc.render.resolution_x=1280; sc.render.resolution_y=1000; sc.cycles.samples=24
    sc.render.filepath=str(BUDS/'Cannabis-Baked-FullPlant.png'); bpy.ops.render.render(write_still=True)
    cam=sc.camera; cam.location=(1.7,-2.5,2.0); target=Vector((.75,0,1.52)); cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler(); cam.data.ortho_scale=.72
    sc.render.filepath=str(BUDS/'Cannabis-Baked-BudClose.png'); bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(BUDS/'Cannabis-BakedExport.blend'))
    print('CANNABIS_EXPORT_DONE '+json.dumps(report),flush=True)
if '--cannabis-only' in sys.argv:
    export_approved_cannabis(); sys.exit(0)
for two in (False,True):
    objects=build_objects(ns['house'],0,two)
    asset('HouseTwoStorey' if two else 'HouseOneStorey',{'Body':objects},(0,1.8,0),True)
    for o in objects: bpy.data.objects.remove(o,do_unlink=True)
objects=build_objects(ns['grass'],0,0,0,1)
asset('GrassTuft',{'Foliage':objects})
crop=source[source.index('# Crop silhouettes:'):source.index("cube('Studio ground'")]
exec(crop,ns)
objects=build_objects(ns['carrot'],0,0)
asset('Carrot',{'Body':[o for o in objects if 'root' in o.name.lower()], 'Foliage':[o for o in objects if 'root' not in o.name.lower()]},(0,0,1.0))
objects=build_objects(ns['banana_plant'],0,0)
fruit=lambda o:any(o.name.startswith(w) for w in ['Curved banana fruit','Banana curved middle','Banana tip turn'])
asset('Banana',{'Foliage':[o for o in objects if not fruit(o)],'Fruit':[o for o in objects if fruit(o)]})
def srgb(c): return round(255*(12.92*c if c<=.0031308 else 1.055*c**(1/2.4)-.055))
def chunk(t,d): return struct.pack('>I',len(d))+t+d+struct.pack('>I',zlib.crc32(t+d)&0xffffffff)
raw=bytearray()
for y in range(128):
    raw.append(0)
    for x in range(128):
        cell=(7-y//16)*8+x//16; c=palette[cell] if cell<len(palette) else (.5,.5,.5)
        raw.extend([srgb(v) for v in c]+[255])
(OUT/'palette.png').write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',128,128,8,6,0,0,0))+chunk(b'sRGB',b'\x00')+chunk(b'IDAT',zlib.compress(raw))+chunk(b'IEND',b''))
(OUT/'manifest.json').write_text(json.dumps(dict(assets=summary,paletteLinearColors=palette,notes=['Unity X,Z,Y reflected from Blender; reversed triangle winding','One atlas material; opaque two-sided recommended for leaf and roof planes','Carrot crown y=0.13; root tip y=-0.9; soil at zero','Banana all parts share the root pivot and full growth scaling','Cannabis intentionally NOT exported pending user approval']),indent=2))
print('MINI142_EXPORT_READY '+json.dumps(summary),flush=True)
if '--exports-only' in sys.argv: sys.exit(0)
# Approval-only revision. No revised cannabis buffers are exported.
ns['reset'](); exec(crop,ns); random.seed(142)
budcolors=[ns['mat']('Calyx olive '+str(i),c) for i,c in enumerate([(.17,.23,.065),(.24,.30,.10),(.32,.36,.15),(.13,.18,.045),(.22,.14,.21)])]
def resin_material(purple):
    m=ns['mat']('Procedural resin olive purple' if purple else 'Procedural resin olive',(.12,.19,.045))
    nodes=m.node_tree.nodes; links=m.node_tree.links; bs=nodes.get('Principled BSDF')
    tex=nodes.new('ShaderNodeTexNoise'); tex.inputs['Scale'].default_value=9; tex.inputs['Detail'].default_value=4.5; tex.inputs['Roughness'].default_value=.78
    ramp=nodes.new('ShaderNodeValToRGB'); ramp.color_ramp.elements.remove(ramp.color_ramp.elements[1])
    colors=[(.20,(.035,.048,.011,1)),(.44,(.10,.14,.029,1)),(.62,(.20,.24,.071,1)),(.74,((.11,.075,.085,1) if purple else (.25,.285,.13,1)))]
    ramp.color_ramp.elements[0].position=colors[0][0]; ramp.color_ramp.elements[0].color=colors[0][1]
    for p,c in colors[1:]: ramp.color_ramp.elements.new(p).color=c
    links.new(tex.outputs['Fac'],ramp.inputs[0])
    micro=nodes.new('ShaderNodeTexVoronoi'); micro.inputs['Scale'].default_value=155
    speck=nodes.new('ShaderNodeMath'); speck.operation='LESS_THAN'; speck.inputs[1].default_value=.095; links.new(micro.outputs['Distance'],speck.inputs[0])
    mix=nodes.new('ShaderNodeMixRGB'); mix.blend_type='MIX'; mix.inputs[2].default_value=(.48,.46,.29,1)
    links.new(speck.outputs[0],mix.inputs[0]); links.new(ramp.outputs[0],mix.inputs[1]); links.new(mix.outputs[0],bs.inputs['Base Color'])
    bump=nodes.new('ShaderNodeBump'); bump.inputs['Strength'].default_value=.65; bump.inputs['Distance'].default_value=.021
    links.new(tex.outputs['Fac'],bump.inputs['Height'])
    fine=nodes.new('ShaderNodeBump'); fine.inputs['Strength'].default_value=.23; fine.inputs['Distance'].default_value=.0011
    links.new(micro.outputs['Distance'],fine.inputs['Height']); links.new(bump.outputs[0],fine.inputs['Normal']); links.new(fine.outputs[0],bs.inputs['Normal'])
    bs.inputs['Roughness'].default_value=.82
    return m
resin=[resin_material(False),resin_material(True)]
tan=ns['mat']('Fine muted tan pistils',(.34,.20,.083))
def refined_bud(p,scale=.15,purple=False):
    p=Vector(p)
    calyx_centres=[]
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,radius=1,location=p+Vector((0,0,scale*1.02)))
    core=bpy.context.object; core.name='Hidden narrow tapered cola core'; core.scale=(scale*.27,scale*.26,scale*1.06)
    core.data.materials.append(resin[int(purple)])
    for v in core.data.vertices:
        taper=1-.32*max(0,v.co.z); jitter=1+random.uniform(-.15,.15)
        v.co.x*=taper*jitter; v.co.y*=taper*jitter
    for face in core.data.polygons: face.use_smooth=True
    # Dense small pointed calyxes cover the narrow internal support completely.
    for ring in range(7):
        t=ring/6; count=6
        for j in range(count):
            a=j*math.tau/count+ring*2.13+random.uniform(-.16,.16)
            radius=scale*(.27-.15*t)+random.uniform(-.015,.015)*scale; z=scale*(.12+1.90*t+random.uniform(-.05,.05))
            pos=p+Vector((math.cos(a)*radius,math.sin(a)*radius,z))
            calyx_centres.append((pos,a))
            o=ns['ico']('Small overlapping pointed calyx',pos,(scale*.18,scale*.16,scale*.285),resin[int(purple)],1)
            o.rotation_euler=(math.sin(a)*.26,-math.cos(a)*.26,a)
            for vertex in o.data.vertices:
                vertex.co.x*=random.uniform(.86,1.12); vertex.co.y*=random.uniform(.86,1.12)
                if vertex.co.z>.45: vertex.co.z*=1.14
    for k in range(15):
        centre,a=calyx_centres[(k*11)%len(calyx_centres)]
        pos=centre+Vector((math.cos(a)*scale*.14,math.sin(a)*scale*.14,scale*.10))
        d=Vector((math.cos(a)*scale*.11,math.sin(a)*scale*.11,scale*.14))
        mid=pos+d; tip=mid+Vector((-math.sin(a)*scale*.08,math.cos(a)*scale*.08,-scale*.015))
        ns['rod']('Fine tan pistil',pos,mid,scale*.009,tan,3)
        ns['rod']('Fine tan pistil curl',mid,tip,scale*.0045,tan,3)
    for k in range(7):
        a=k*2.4; base=p+Vector((0,0,scale*(.15+k*.27)))
        ns['leaf']('Small resin sugar leaf',base,base+Vector((math.cos(a)*scale*.62,math.sin(a)*scale*.62,scale*.34)),scale*.075,ns['greens'][0])
ns['bud']=refined_bud
original_rod=ns['rod']; original_leaf=ns['leaf']
def slender_rod(name,a,b,r,m,n=8):
    return original_rod(name,a,b,r*(.58 if name in ('Central plant stalk','Branch') else 1),m,n)
def slender_leaf(name,a,b,width,m):
    return original_leaf(name,a,b,width*(.72 if name=='Five finger leaf' else 1),m)
ns['rod']=slender_rod; ns['leaf']=slender_leaf
ns['cannabis'](-.75,0,False); ns['cannabis'](.75,0,True)
plants=[o for o in bpy.data.objects if o.type=='MESH']; ns['count_group']('revised_pair',plants)
ns['cube']('Neutral soil pedestal',(0,0,-.07),(2.7,1.8,.14),ns['soil'],.025)
ns['cube']('Studio floor',(0,0,-.20),(200,200,.1),ns['stage'])
ns['setup']((3,-6,3.1),(0,0,.9),3.4)
sc=bpy.context.scene; sc.render.resolution_x=1280; sc.render.resolution_y=1000; sc.cycles.samples=32
sc.render.filepath=str(BUDS/'Cannabis-FullPlant.png'); bpy.ops.render.render(write_still=True)
cam=sc.camera; cam.location=(1.7,-2.5,2.0); target=Vector((.75,0,1.52)); cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler(); cam.data.ortho_scale=.72
sc.render.filepath=str(BUDS/'Cannabis-BudClose.png'); bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(BUDS/'Cannabis-ApprovalPreview.blend'))
(BUDS/'budget.json').write_text(json.dumps(dict(trianglesPerPlant=ns['stats']['revised_pair']['evaluated_triangles']//2,status='Awaiting user approval; not exported or integrated',lighting='AgX daylight studio, 32 cycles samples',cameras='Full 1280x1000 orthographic 3.4m; close orthographic .72m'),indent=2))
print('MINI142_BUDS_DONE',flush=True)
