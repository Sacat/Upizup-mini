"""Round gold watch: intentionally lightweight mesh, atlas and two LODs."""
import bpy, math, os, json
from mathutils import Matrix
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'))
OUT=os.path.join(ROOT,'Assets','UpIzUpMini','Art','Accessories','GoldWatchMobile')
LOG=os.path.join(ROOT,'Logs','Tasks','MINI-145')
os.makedirs(OUT,exist_ok=True);os.makedirs(LOG,exist_ok=True)
COLORS=[(.83,.53,.16),(.64,.38,.105),(.018,.024,.03),(.90,.88,.70)]
METAL=[.92,.80,0,.15]; GLOSS=[.72,.48,.25,.50]
bpy.ops.wm.read_factory_settings(use_empty=True)
for name,metal in [('WatchPalette',False),('WatchMetallic',True)]:
    img=bpy.data.images.new(name,width=32,height=8,alpha=True)
    pixels=[]
    for y in range(8):
        for x in range(32):
            slot=x//8
            pixels.extend((METAL[slot],METAL[slot],METAL[slot],GLOSS[slot]) if metal else (*COLORS[slot],1))
    img.pixels=pixels;img.filepath_raw=os.path.join(OUT,name+'.png');img.file_format='PNG';img.save()

def build(lod):
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    parts=[]; seg=32 if lod==0 else 20
    def add(o,slot):
        o.data.materials.clear()
        uv=o.data.uv_layers.new(name='Palette') if not o.data.uv_layers else o.data.uv_layers.active
        for loop in uv.data:loop.uv=((slot+.5)/4,.5)
        parts.append(o);return o
    def cube(loc,size,slot):
        bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.scale=size
        bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return add(o,slot)
    def cyl(loc,r,depth,slot,n=None):
        bpy.ops.mesh.primitive_cylinder_add(vertices=n or seg,radius=r,depth=depth,location=loc)
        o=bpy.context.object
        for p in o.data.polygons:p.use_smooth=len(p.vertices)==4
        return add(o,slot)
    def ring(z,r,t,slot):
        bpy.ops.mesh.primitive_torus_add(major_segments=seg,minor_segments=4,location=(0,0,z),major_radius=r,minor_radius=t)
        o=bpy.context.object
        for p in o.data.polygons:p.use_smooth=True
        return add(o,slot)
    cyl((0,0,.026),.021,.008,1)
    cyl((0,0,.0304),.0179,.001,2)
    ring(.031,.019,.0018,0)
    for i in range(12):
        a=2*math.pi*i/12
        o=cube((math.sin(a)*.0156,math.cos(a)*.0156,.0315),(.0007,.0026,.00035),0);o.rotation_euler.z=-a
    for degrees,length,width,slot,z in [(-55,.010,.0011,0,.0321),(60,.014,.0007,3,.0325),(170,.015,.00022,0,.0329)]:
        a=math.radians(degrees);o=cube((math.sin(a)*length*.4,math.cos(a)*length*.4,z),(width,length,.00025),slot);o.rotation_euler.z=-a
    cyl((0,0,.0332),.00085,.0005,0,8)
    crown=cyl((.022,0,.026),.0022,.004,0,12 if lod==0 else 8);crown.rotation_euler.y=math.pi/2
    # User-approved design, 90 degrees clockwise when looking at the dial.
    # Rotate the circular case/dial/crown only; bracelet, lugs and saved wrist
    # profiles remain untouched so this cannot turn the band across the arm.
    face_turn=Matrix.Rotation(-math.pi/2,4,'Z')
    bpy.context.view_layer.update()
    for part in parts:part.matrix_world=face_turn @ part.matrix_world
    for y in [-.020,.020]:
        for x in [-.011,.011]:cube((x,y,.022),(.005,.008,.005),0)
    total=28 if lod==0 else 18
    first=3 if lod==0 else 2
    for i in range(first,total-first+1):
        a=2*math.pi*i/total;y=.031*math.sin(a);z=.026*math.cos(a)-.003
        tangent=math.atan2(-.026*math.sin(a),.031*math.cos(a))
        for x,w,slot in ([(-.0073,.005,1),(0,.009,0),(.0073,.005,1)] if lod==0 else [(0,.019,0)]):
            o=cube((x,y,z),(w,.0064 if lod==0 else .0095,.003),slot);o.rotation_euler.x=tangent
    cube((0,0,-.030),(.019,.014,.003),1)
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();body=bpy.context.object
    body.name='GoldWatch_LOD'+str(lod)
    bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    mat=bpy.data.materials.get('WatchAtlas') or bpy.data.materials.new('WatchAtlas');body.data.materials.append(mat)
    for p in body.data.polygons:p.material_index=0
    tris=sum(len(p.vertices)-2 for p in body.data.polygons)
    assert tris <= (2500 if lod==0 else 1200),tris
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,body.name+'.fbx'),use_selection=True,object_types={'MESH'},
        apply_unit_scale=True,bake_space_transform=False,axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='STRIP')
    return {'lod':lod,'triangles':tris,'vertices':len(body.data.vertices),'renderers':1,'materials':1}
audit={'lods':[build(0),build(1)],'textureSizes':[32,8],'status':'isolated candidate; wrist fit approval pending'}
with open(os.path.join(LOG,'mobile-watch-audit.json'),'w') as f:json.dump(audit,f,indent=2)
print('MINI145_MOBILE_WATCH_PASS',json.dumps(audit))
