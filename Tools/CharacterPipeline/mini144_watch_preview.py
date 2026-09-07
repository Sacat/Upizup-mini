"""Original unbranded gold watch concept. Preview only, no Unity assets changed."""
import bpy, math, os, json
from mathutils import Vector
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'))
OUT=os.path.join(ROOT,'Logs','Tasks','MINI-144')
os.makedirs(OUT,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)

def material(name,color,metal=0,rough=.4):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Metallic'].default_value=metal; p.inputs['Roughness'].default_value=rough
    return m
gold=material('Polished warm gold',(.83,.53,.16),1,.22)
brushed=material('Brushed gold links',(.64,.38,.105),1,.35)
black=material('Deep charcoal dial',(.009,.014,.018),0,.65)
light=material('Ivory luminous markers',(.9,.88,.7),.2,.28)
objects=[]
def finish(o,name,mat):
    o.name=name; o.data.materials.append(mat); objects.append(o)
    for p in o.data.polygons:p.use_smooth=True
    return o
def cube(name,loc,scale,mat,bevel=.0007):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc); o=bpy.context.object; o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    mod=o.modifiers.new('Soft machined edges','BEVEL'); mod.width=bevel; mod.segments=3
    o.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
    return finish(o,name,mat)
def cyl(name,loc,r,depth,mat,vertices=48):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=depth,location=loc)
    o=bpy.context.object; b=o.modifiers.new('Machined rim','BEVEL'); b.width=.00035;b.segments=2
    o.modifiers.new('Weighted normals','WEIGHTED_NORMAL');return finish(o,name,mat)
def torus(name,z,major,minor,mat):
    bpy.ops.mesh.primitive_torus_add(major_segments=64,minor_segments=8,location=(0,0,z),major_radius=major,minor_radius=minor)
    return finish(bpy.context.object,name,mat)

# Wrist runs along X. Bracelet wraps the Y/Z section; crown lies to the right.
cube('Rounded case',(0,0,.026),(.042,.044,.008),brushed,.005)
cyl('Case back',(0,0,.021),.019,.002,brushed)
cyl('Dial',(0,0,.0306),.0179,.001,black)
torus('Polished bezel',.031,.019,.0018,gold)
torus('Inner dial rim',.0313,.0178,.00035,brushed)
for i in range(60):
    a=2*math.pi*i/60
    major=i%5==0
    r=.0156 if major else .0165
    o=cube('Hour baton' if major else 'Minute tick',(math.sin(a)*r,math.cos(a)*r,.03145),
        (.00065 if major else .00022,.0026 if major else .0008,.0004 if major else .00012),gold if major else light,.00008)
    o.rotation_euler.z=-a
def hand(name,angle,length,width,mat,z):
    a=math.radians(angle)
    o=cube(name,(math.sin(a)*length*.4,math.cos(a)*length*.4,z),(width,length,.00035),mat,.00015)
    o.rotation_euler.z=-a
hand('Hour hand',-55,.010,.0011,gold,.0321)
hand('Minute hand',60,.014,.0007,light,.0326)
hand('Second hand',170,.015,.00022,gold,.033)
cyl('Hand pin',(0,0,.0333),.00085,.0006,gold,24)
crown=cyl('Winding crown',(.023,0,.027),.0024,.004,gold,32); crown.rotation_euler.y=math.pi/2
for y in [-.025,.025]:
    for x in [-.012,.012]:cube('Case lug',(x,y,.022),(.007,.009,.009),gold,.0016)
# Three-link bracelet with separate reflective faces, fit adjustable later.
for i in range(3,26):
    a=2*math.pi*i/28
    # Band wraps Y/Z around wrist; width is X and wrist extends along X.
    y=.031*math.sin(a); z=.026*math.cos(a)-.003
    tangent=math.atan2(-.026*math.sin(a),.031*math.cos(a))
    for x,w,mat in [(-.0073,.005,brushed),(0,.009,gold),(.0073,.005,brushed)]:
        o=cube('Bracelet link',(x,y,z),(w,.0068,.0032),mat,.0007)
        o.rotation_euler.x=tangent
cube('Clasp',(0,0,-.030),(.019,.014,.003),brushed,.001)

# Studio setup. This is a close-up design review, not an in-game screenshot.
floor=material('Studio slate',(.022,.028,.035),.15,.44)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.034))
bpy.context.object.data.materials.append(floor)
world=bpy.data.worlds.new('Studio world'); bpy.context.scene.world=world; world.color=(.20,.20,.20)
def area(name,loc,power,size):
    bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.name=name;o.data.energy=power;o.data.shape='DISK';o.data.size=size
    o.rotation_euler=(Vector((0,0,0))-o.location).to_track_quat('-Z','Y').to_euler()
area('Large softbox',(.08,-.06,.15),1.6,.12)
area('Gold rim',(-.1,.07,.09),2.4,.08)
area('Front fill',(.03,.12,.13),1,.10)
bpy.ops.object.camera_add(location=(.102,-.130,.145));cam=bpy.context.object
cam.rotation_euler=(Vector((0,0,.002))-cam.location).to_track_quat('-Z','Y').to_euler()
cam.data.type='ORTHO';cam.data.ortho_scale=.112; bpy.context.scene.camera=cam
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=40
scene.render.resolution_x=1000;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX';scene.render.image_settings.file_format='PNG'
scene.render.filepath=os.path.join(OUT,'Gold-Watch-Preview.png')
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Gold-Watch-Preview.blend'))
bpy.ops.render.render(write_still=True)
dg=bpy.context.evaluated_depsgraph_get(); tris=0
for o in objects:
    e=o.evaluated_get(dg);m=e.to_mesh();tris+=sum(len(p.vertices)-2 for p in m.polygons);e.to_mesh_clear()
with open(os.path.join(OUT,'watch-preview-audit.json'),'w') as f:
    json.dump({'triangles_with_preview_bevels':tris,'parts':len(objects),'status':'concept only; merge/bake/LOD and wrist fit pending','externalCredits':0},f,indent=2)
print('MINI144_WATCH_PREVIEW_COMPLETE')
