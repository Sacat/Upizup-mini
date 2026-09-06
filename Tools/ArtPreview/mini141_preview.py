"""MINI-141: local Blender mesh previews, no Unity integration or external credits."""
import bpy, math, random, json
from mathutils import Vector
from pathlib import Path
random.seed(141)
OUT=Path(r"E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-141")
OUT.mkdir(parents=True,exist_ok=True)
stats={}
def reset():
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    for m in list(bpy.data.materials): bpy.data.materials.remove(m)
def mat(name,c,rough=.7,metal=0):
    m=bpy.data.materials.new(name); m.diffuse_color=(*c,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value=(*c,1); bs.inputs['Roughness'].default_value=rough
    bs.inputs['Metallic'].default_value=metal
    return m
def cube(name,p,s,m,bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=p); o=bpy.context.object; o.name=name
    o.scale=s; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(m)
    if bevel:
        b=o.modifiers.new('small real edge','BEVEL'); b.width=bevel; b.segments=1
        o.modifiers.new('weighted normals','WEIGHTED_NORMAL')
    return o
def mesh(name,vs,fs,m):
    me=bpy.data.meshes.new(name); me.from_pydata(vs,[],fs); me.update()
    o=bpy.data.objects.new(name,me); bpy.context.collection.objects.link(o); o.data.materials.append(m); return o
def rod(name,a,b,r,m,n=8):
    d=Vector(b)-Vector(a)
    bpy.ops.mesh.primitive_cone_add(vertices=n,radius1=r,radius2=r*.85,depth=d.length,location=(Vector(a)+Vector(b))/2)
    o=bpy.context.object; o.name=name; o.rotation_euler=d.to_track_quat('Z','Y').to_euler(); o.data.materials.append(m); return o
def ico(name,p,s,m,sub=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=p)
    o=bpy.context.object; o.name=name; o.scale=s; o.data.materials.append(m)
    for poly in o.data.polygons: poly.use_smooth=True
    return o
def label(txt,p,size=.3):
    bpy.ops.object.text_add(location=p); o=bpy.context.object; o.name='label '+txt; o.data.body=txt
    o.data.size=size; o.data.align_x='CENTER'; o.rotation_euler=(math.radians(68),0,0)
    o.data.materials.append(ink)
def setup(campos,target,scale):
    sc=bpy.context.scene; sc.render.engine='CYCLES'; sc.cycles.samples=24
    sc.cycles.use_denoising=True
    sc.view_settings.exposure=.6
    sc.render.resolution_x=1500; sc.render.resolution_y=1000; sc.render.resolution_percentage=100
    sc.world.color=(.3,.3,.3)
    sc.world.use_nodes=True; sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.65,.77,.88,1)
    sc.world.node_tree.nodes['Background'].inputs[1].default_value=.5
    bpy.ops.object.light_add(type='AREA',location=(-9,-12,18)); bpy.context.object.data.energy=2200; bpy.context.object.data.shape='DISK'; bpy.context.object.data.size=10
    bpy.ops.object.light_add(type='SUN',location=(0,0,12)); sun=bpy.context.object; sun.rotation_euler=(.45,-.4,-.4); sun.data.energy=1.5; sun.data.angle=.12
    bpy.ops.object.camera_add(location=campos); cam=bpy.context.object; cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.type='ORTHO'; cam.data.ortho_scale=scale; sc.camera=cam
    sc.view_settings.view_transform='AgX'
def save_render(name):
    sc=bpy.context.scene
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(name+'.blend')))
    sc.render.filepath=str(OUT/(name+'.png')); bpy.ops.render.render(write_still=True)
def count_group(name,objects):
    deps=bpy.context.evaluated_depsgraph_get()
    tris=0
    for o in objects:
        if o.type!='MESH': continue
        me=o.evaluated_get(deps).to_mesh(); me.calc_loop_triangles(); tris+=len(me.loop_triangles)
        o.evaluated_get(deps).to_mesh_clear()
    stats[name]={'evaluated_triangles':tris,'mesh_objects':sum(o.type=='MESH' for o in objects)}
def grass(x,y,z,scale=1):
    vs=[]; fs=[]
    for i in range(7):
        a=random.uniform(0,6.28); h=random.uniform(.14,.34)*scale; w=.026*scale
        px=x+random.uniform(-.13,.13)*scale; py=y+random.uniform(-.13,.13)*scale
        k=len(vs); dx=math.cos(a)*w; dy=math.sin(a)*w
        vs += [(px-dx,py-dy,z),(px+dx,py+dy,z),(px+math.sin(a)*h*.3,py+math.cos(a)*h*.3,z+h)]
        fs.append((k,k+1,k+2))
    mesh('Grass tuft seven opaque blades',vs,fs,random.choice(greens))
reset()
ink=mat('Ink',(.055,.085,.08))
cream=mat('Limewashed trim',(.82,.80,.67)); concrete=mat('Concrete',(.44,.47,.43))
glass=mat('Window deep blue',(.055,.12,.14),.27)
wood=mat('Painted timber',(.20,.30,.28))
roof=mat('Aged galvanised metal',(.44,.50,.51),.42,.55)
terracotta=mat('Warm red metal roof',(.40,.115,.075),.5,.25)
metal=mat('Rail dark metal',(.08,.14,.13),.45,.5)
greens=[mat('Grass '+str(i),c) for i,c in enumerate([(.17,.29,.08),(.27,.37,.12),(.34,.43,.16)])]
ground=mat('Warm grass ground',(.28,.36,.19))
road=mat('Unmarked asphalt',(.12,.15,.15))
wallm=[mat('Seafoam plaster',(.40,.65,.54)),mat('Warm peach plaster',(.69,.39,.28))]
def window(x,y,z):
    cube('Recess dark window',(x,y,z),(1.05,.06,1.16),glass)
    for xx in [x-.56,x+.56]: cube('Window side reveal',(xx,y-.04,z),(.085,.14,1.3),cream,.015)
    for zz in [z-.65,z+.65]: cube('Window sill lintel',(x,y-.06,zz),(1.22,.20,.09),cream,.015)
    cube('Mullion',(x,y-.1,z),(.06,.04,1.14),cream)
    for zz in [z-.26,z+.26]: cube('Glazing bar',(x,y-.11,zz),(1.04,.035,.04),cream)
def house(x,two=False):
    start=set(bpy.data.objects)
    levels=2 if two else 1; h=levels*2.75; w=5.6; d=4.7; y=1.8
    cube('Foundation',(x,y,.16),(w+.14,d+.14,.32),concrete,.025)
    cube('Plaster house shell',(x,y,h/2+.3),(w,d,h),wallm[int(two)],.035)
    for level in range(levels):
        z=.3+level*2.75
        cube('Horizontal concrete course',(x,y,z+.10),(w+.07,d+.07,.13),cream,.015)
        for wx in [-1.75,1.75]: window(x+wx,y-d/2-.045,z+1.50)
        cube('Door recess',(x,y-d/2-.08,z+1.04),(1.04,.08,2.08),ink)
        cube('Panelled timber door',(x,y-d/2-.13,z+1.03),(.87,.075,1.98),wood,.015)
        for dz in [.65,1.45]: cube('Door raised panel',(x,y-d/2-.18,z+dz),(.64,.025,.55),wood,.016)
        ico('Door handle',(x+.30,y-d/2-.23,z+1.02),(.025,.03,.025),cream)
        if level==1:
            cube('Balcony slab',(x,-1.12,z-.03),(w+.16,1.15,.16),concrete,.025)
            for xx in [x-w/2+.16,x+w/2-.16]:
                cube('Balcony support',(xx,-1.50,z-1.35),(.17,.17,2.65),cream,.015)
            rod('Balcony rail',(x-2.7,-1.68,z+.90),(x+2.7,-1.68,z+.90),.035,metal)
            for i in range(23):
                xx=x-2.65+i*5.3/22
                rod('Baluster',(xx,-1.68,z+.07),(xx,-1.68,z+.89),.018,metal,6)
    # Gabled metal roof with visible ribs.
    base=h+.30; ridge=base+.95
    roofmat=terracotta if two else roof
    vs=[(x-3.04,y-2.57,base),(x+3.04,y-2.57,base),(x-3.04,y,ridge),(x+3.04,y,ridge),(x-3.04,y+2.57,base),(x+3.04,y+2.57,base)]
    mesh('Two pitched roof planes',vs,[(0,1,3,2),(2,3,5,4)],roofmat)
    for i in range(36):
        xx=x-3.04+6.08*i/35
        rod('Metal roof rib',(xx,y-2.57,base+.025),(xx,y,ridge+.025),.014,roofmat,5)
        rod('Metal roof rib',(xx,y,ridge+.025),(xx,y+2.57,base+.025),.014,roofmat,5)
    for yy in [y-2.57,y+2.57]:
        cube('Roof fascia',(x,yy,base-.035),(6.15,.11,.14),cream,.01)
    for xx in [x-2.8,x+2.8]:
        mesh('Plastered gable',[(xx,y-2.35,base),(xx,y+2.35,base),(xx,y,ridge-.10)],[(0,1,2)],wallm[int(two)])
    rod('Gutter downpipe',(x+2.73,y-2.48,.4),(x+2.73,y-2.48,base-.1),.035,cream)
    for step in range(3):
        cube('Entry step',(x,-.87-step*.22,.24-step*.075),(1.5,.34,.12),concrete,.014)
    if not two:
        cube('Veranda canopy',(x,-1.13,2.66),(5.75,1.2,.12),roofmat,.02)
        for xx in [x-2.55,x+2.55]: cube('Veranda post',(xx,-1.56,1.46),(.13,.13,2.42),cream,.012)
    count_group('two_storey_house' if two else 'one_storey_house',set(bpy.data.objects)-start)
cube('Sample ground',(0,1,-.10),(20,14,.18),ground)
cube('Sample Lalay sidewalk',(0,-2.52,.012),(20,1.05,.12),concrete,.015)
cube('Sample line free road',(0,-5.45,-.015),(20,4.7,.10),road)
house(-4.1); house(3.6,True)
for i in range(160):
    x=random.uniform(-9.4,9.4); y=random.uniform(-1.9,6.7)
    if (-7.1<x<-1.0 or .6<x<6.6) and -.9<y<4.5: continue
    if abs(x+4.1)<.9 or abs(x-3.6)<.9: continue
    grass(x,y,0,.9)
label('LALAY HOUSE FAMILY / BLENDER PREVIEW',(-.4,-7.9,.05),.39)
label('Proposed models + sparse grass | layout sample only',(-.4,-8.6,.05),.25)
setup((17,-27,20),(0,.1,2),23)
save_render('01-Houses-Grass-Preview')
# Crop silhouettes: actual mesh preview candidates.
reset()
ink=mat('Ink',(.05,.10,.08)); soil=mat('Soil',(.18,.105,.054)); stage=mat('Studio sage',(.56,.64,.53))
greens=[mat('Leaf '+str(i),c) for i,c in enumerate([(.055,.19,.045),(.13,.29,.06),(.23,.37,.09)])]
orange=mat('Carrot orange',(.9,.23,.025)); scar=mat('Root growth rings',(.43,.105,.018))
stem=mat('Stem olive',(.28,.34,.12)); trunk=mat('Banana pale fibrous trunk',(.38,.40,.14)); dry=mat('Old brown leaf sheath',(.36,.23,.11))
budm=[mat('Bud '+str(i),c) for i,c in enumerate([(.28,.39,.13),(.20,.29,.07),(.26,.17,.29)])]
pistil=mat('Amber bud accents',(.63,.28,.055)); banana=mat('Banana yellow green',(.62,.68,.09))
def leaf(name,a,b,width,m):
    a=Vector(a); b=Vector(b); d=b-a; side=d.cross(Vector((0,0,1)))
    if side.length<.01: side=Vector((1,0,0))
    side.normalize(); mid=a+d*.5+Vector((0,0,width*.14))
    return mesh(name,[a,mid-side*width,b,mid+side*width,mid+Vector((0,0,width*.12))],[(0,1,4),(1,2,4),(2,3,4),(3,0,4)],m)
def carrot(x,y):
    start=set(bpy.data.objects)
    n=10; vs=[]; fs=[]
    for z,r in [(.10,.015),(.23,.045),(.53,.11),(.82,.16),(1.02,.18),(1.13,.12)]:
        for i in range(n):
            a=i*math.tau/n; vs.append((x+r*math.cos(a),y+r*math.sin(a),z))
    for j in range(5):
        for i in range(n): a=j*n+i; b=j*n+(i+1)%n; fs.append((a,b,b+n,a+n))
    fs += [tuple(range(50,60))]
    mesh('Tapered carrot root',vs,fs,orange)
    for j in range(6):
        a=j*2.4; end=Vector((x+math.cos(a)*.30,y+math.sin(a)*.30,1.55+random.random()*.15))
        base=Vector((x,y,1.1)); rod('Carrot leaf stem',base,end,.012,greens[1],5)
        d=end-base
        for k in range(3):
            p=base+d*(.4+k*.18)
            for side in [-1,1]:
                q=p+Vector((math.cos(a+side*1.1)*.16,math.sin(a+side*1.1)*.16,.10))
                leaf('Fine carrot leaf',p,q,.04,greens[1])
    count_group('carrot',set(bpy.data.objects)-start)
def banana_plant(x,y):
    start=set(bpy.data.objects)
    rod('Thick banana pseudostem',(x,y,0),(x+.07,y,2.5),.16,trunk,10)
    for a in [0,2,4]:
        rod('Brown old sheath',(x+.14*math.cos(a),y+.14*math.sin(a),.10),(x+.12*math.cos(a),y+.12*math.sin(a),1.6),.047,dry,6)
    crown=Vector((x+.07,y,2.5))
    for j in range(7):
        a=j*math.tau/7; direction=Vector((math.cos(a),math.sin(a),0))
        mid=crown+direction*.30+Vector((0,0,.22))
        end=crown+direction*1.65+Vector((0,0,-.22))
        rod('Banana petiole',crown,mid,.025,stem,6)
        side=Vector((-math.sin(a),math.cos(a),0)); vs=[]; fs=[]; previous=mid
        for section in range(9):
            t=section/8; centre=mid.lerp(end,t)+Vector((0,0,.25*math.sin(math.pi*t)))
            width=.28*math.sin(math.pi*t)**.55
            vs.extend([centre-side*width-Vector((0,0,width*.25)),centre,centre+side*width-Vector((0,0,width*.25))])
            if section:
                q=(section-1)*3; fs.extend([(q,q+1,q+4,q+3),(q+1,q+2,q+5,q+4)])
                rod('Banana midrib',previous,centre,.009,stem,5)
            previous=centre
        mesh('Long curved banana leaf',vs,fs,greens[j%3])
    # Bunch hangs from curved peduncle, not loose floating fruit.
    p=crown+Vector((0,-.38,.2)); q=crown+Vector((0,-.78,-.12)); bottom=q+Vector((0,0,-.80))
    rod('Bunch peduncle',crown,p,.042,stem); rod('Bunch peduncle',p,q,.038,stem); rod('Bunch stalk',q,bottom,.027,stem)
    for tier in range(4):
        z=q.z-.10-tier*.16
        for j in range(8):
            a=j*math.tau/8
            a0=Vector((q.x+math.cos(a)*.04,q.y+math.sin(a)*.04,z))
            a1=a0+Vector((math.cos(a)*.13,math.sin(a)*.13,-.08))
            a2=a1+Vector((math.cos(a)*.04,math.sin(a)*.04,-.14))
            a3=a2+Vector((-math.cos(a)*.035,-math.sin(a)*.035,-.07))
            rod('Curved banana fruit',a0,a1,.046,banana,7); rod('Banana curved middle',a1,a2,.044,banana,7); rod('Banana tip turn',a2,a3,.025,banana,6)
    ico('Banana flower',bottom+Vector((0,0,-.12)),(.10,.10,.20),budm[2],2)
    count_group('banana_plant',set(bpy.data.objects)-start)
def bud(p,scale=.15,purple=False):
    for k in range(7):
        z=(k/6)*scale*2.2; a=k*2.4; radius=scale*(.75-k*.06)
        ico('Clustered flower bud',(p[0]+math.cos(a)*radius*.35,p[1]+math.sin(a)*radius*.35,p[2]+z),(radius,radius,scale*.6),budm[2 if purple and k%2 else k%2],1)
    for k in range(6):
        a=k*2.4; pos=Vector(p)+Vector((math.cos(a)*scale*.7,math.sin(a)*scale*.7,k*.04))
        rod('Short amber flower detail',pos,pos+Vector((.025,0,.045)),.006,pistil,4)
def cannabis(x,y,purple=False):
    start=set(bpy.data.objects)
    rod('Central plant stalk',(x,y,0),(x,y,1.66),.025,stem,7)
    for j in range(6):
        a=j*2.4; z=.44+j*.15; end=Vector((x+math.cos(a)*.40,y+math.sin(a)*.40,z+.24))
        rod('Branch',(x,y,z),end,.012,stem,6); bud(end,.095,purple)
        for k in range(7):
            ang=a+(k-3)*.32; length=.48-abs(k-3)*.07
            target=end+Vector((math.cos(ang)*length,math.sin(ang)*length,-.07))
            leaf('Five finger leaf',end,target,.045,greens[k%3])
    bud((x,y,1.45),.145,purple)
    count_group('purple_plant' if purple else 'green_plant',set(bpy.data.objects)-start)
cube('Studio ground',(0,0,-.15),(12,8,.2),stage)
for x in [-4,-1.6,1.3,3.6]: cube('Soil base',(x,0,-.04),(1.55,1.4,.14),soil,.07)
carrot(-4,0); banana_plant(-1.6,0); cannabis(1.3,0); cannabis(3.6,0,True)
label('CARROT',(-4,-1.2,.1),.21); label('BANANA',(-1.6,-1.2,.1),.21)
label('BUSHERS',(1.3,-1.2,.1),.21); label('PURPLE',(3.6,-1.2,.1),.21)
label('CROP SHAPES / BLENDER PREVIEW',(-.1,-2.25,.1),.30)
label('Carrot root exposed for inspection; soil covers it during growth',(-.1,-2.8,.1),.17)
setup((5,-12,7),(0,0,1.0),11.6)
save_render('02-Crop-Preview')
(OUT/'mesh-budget.json').write_text(json.dumps(stats,indent=2))
print('MINI141_DONE '+json.dumps(stats))
