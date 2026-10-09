"""Before/after render of the road network from extracted live geometry (Unity Y-up -> Blender Z-up)."""
import bpy,sys,os,json,math,numpy as np
sys.path.insert(0,os.path.dirname(__file__)); from geo_lib import load
npz,roadsjson,mode,out=sys.argv[-4:]
G=load(npz); mats={r['name']:os.path.basename(r['mats'][0]) for r in json.load(open(roadsjson)) if r.get('mats')}
bpy.ops.wm.read_factory_settings(use_empty=True); sc=bpy.context.scene
def mat(name,c,rough=0.8):
    m=bpy.data.materials.get(name) or bpy.data.materials.new(name); m.use_nodes=True
    b=m.node_tree.nodes['Principled BSDF']; b.inputs['Base Color'].default_value=(*c,1); b.inputs['Roughness'].default_value=rough; return m
srgb=lambda v:((v+0.055)/1.055)**2.4 if v>0.04045 else v/12.92
COL={'ExpansionAsphalt.mat':(0.16,0.17,0.17),'MainRoad.mat':(0.17,0.18,0.17),'SideRoad.mat':(0.28,0.29,0.27),'DirtTrack.mat':(0.42,0.32,0.22)}
for k,(W,F) in G.items():
    me=bpy.data.meshes.new(k); me.from_pydata(W[:,[0,2,1]].tolist(),[],F[:,::-1].tolist()); o=bpy.data.objects.new(k,me); sc.collection.objects.link(o)
    for p in me.polygons: p.use_smooth=k=='ExpansionTerrain'
    if k=='ExpansionTerrain': o.data.materials.append(mat('ground',(0.20,0.33,0.12)))
    elif k.endswith('_Deck'): o.data.materials.append(mat('deck',(0.45,0.43,0.40)))
    else:
        c=(COL['DirtTrack.mat'] if mats.get(k,'')=='DirtTrack.mat' else COL['ExpansionAsphalt.mat']) if mode=='after' else COL.get(mats.get(k,''),(0.2,0.2,0.2))
        o.data.materials.append(mat('road_'+str(c),tuple(srgb(x) for x in c),0.9))
sc.render.engine='CYCLES'; sc.cycles.device='CPU'; sc.cycles.samples=24; sc.render.resolution_x=640; sc.render.resolution_y=400
sc.view_settings.view_transform='Standard'
w=bpy.data.worlds.new('w'); sc.world=w; w.use_nodes=True; w.node_tree.nodes['Background'].inputs[0].default_value=(0.6,0.72,0.88,1)
l=bpy.data.objects.new('sun',bpy.data.lights.new('sun','SUN')); l.data.energy=3.5; l.rotation_euler=(math.radians(55),0,math.radians(35)); sc.collection.objects.link(l)
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam; cam.data.lens=28
from mathutils import Vector
SPOTS=[('E0_E1_overlap',(20,-80),( -14,-26,6)),('backstreet_crossing',(2,-172),(-16,18,5)),('junction_patch1',(231,75),(-18,-14,6)),('dog_life',(-63,-142),(14,-14,5)),('E0_E5_junction',(-37,-62),(16,-14,6)),('patch1_close',(229,72),(-7,-6,2.5)),('bridge01_E10',(260,-83.5),(-14,10,4)),('bridge03_inroad',(81,-147),(12,-12,4)),('farm_spur_dirt',(95,-133),(-14,-14,6))]
for name,(x,z),(dx,dz,h) in SPOTS:
    RW=np.vstack([v[0] for k,v in G.items() if k!='ExpansionTerrain']); i=np.argmin(np.hypot(RW[:,0]-x,RW[:,2]-z)); y=RW[i,1]
    T=G['ExpansionTerrain'][0]; j=np.argmin(np.hypot(T[:,0]-(x+dx),T[:,2]-(z+dz))); h=max(h,T[j,1]-y+3.0)
    tgt=Vector((x,z,y)); cam.location=tgt+Vector((dx,dz,h)); cam.rotation_euler=(tgt-cam.location).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath=f'{out}_{name}.png'; bpy.ops.render.render(write_still=True)
