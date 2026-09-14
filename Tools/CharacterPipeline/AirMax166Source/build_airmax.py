"""MINI-166 isolated footwear revision. All dimensions authored in mm.
Run Blender --background --python build_airmax.py -- --model 90 --views hero,side,top,front,back,clay
Outputs stay beside this script. No Unity or other project writes.
"""
import bpy, math, json, sys, argparse
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parent
ap=argparse.ArgumentParser();ap.add_argument('--model',choices=['90','97'],default='90');ap.add_argument('--views',default='hero,side,top,front,back,clay');ap.add_argument('--samples',type=int,default=32)
a=ap.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
MODEL=a.model;OUT=P/('AM'+MODEL);OUT.mkdir(exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for old in list(bpy.data.materials): bpy.data.materials.remove(old)
sc=bpy.context.scene;sc.unit_settings.system='METRIC';sc.unit_settings.length_unit='MILLIMETERS'

def material(name,col,rough=.5,metal=0,texture=None):
 m=bpy.data.materials.new(name);m.diffuse_color=(*col,1);m.use_nodes=True
 nd=m.node_tree.nodes;bs=nd.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*col,1);bs.inputs['Roughness'].default_value=rough;bs.inputs['Metallic'].default_value=metal
 if texture:
  tex=nd.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=texture[0];tex.inputs['Detail'].default_value=2
  bump=nd.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.22;bump.inputs['Distance'].default_value=texture[1]/1000
  m.node_tree.links.new(tex.outputs['Fac'],bump.inputs['Height']);m.node_tree.links.new(bump.outputs['Normal'],bs.inputs['Normal'])
 return m
white=material('Warm white foam',(.78,.79,.77),.62,texture=(150,.10))
black=material('Carbon rubber',(.013,.017,.021),.75,texture=(180,.12))
lining=material('Dark textile lining',(.022,.027,.033),.92,texture=(220,.13))
meshmat=material('Woven mesh',(.62,.64,.65),.82,texture=(240,.22))
grey=material('Soft grey suede',(.31,.34,.36),.85,texture=(180,.13))
silver=material('Silver synthetic panels',(.49,.53,.57),.32,.48,texture=(280,.025))
reflect=material('Reflective piping',(.7,.74,.75),.3,.22)
red=material('Infrared TPU' if MODEL=='90' else 'Red embroidery',(.88,.045,.055) if MODEL=='90' else (.58,.008,.019),.48)
laces=material('Woven ivory laces',(.77,.78,.74),.86,texture=(300,.1))
thread=material('Seam thread',(.55,.57,.55),.83)
airmat=material('Clear smoked Air bladder',(.67,.71,.69),.11)
bs=airmat.node_tree.nodes.get('Principled BSDF');bs.inputs['Transmission Weight'].default_value=.82;bs.inputs['IOR'].default_value=1.42
bs.inputs['Coat Weight'].default_value=.35
support=material('Internal Air pillars',(.42,.48,.43),.45)

def mesh(name,v,f,m,smooth=True):
 me=bpy.data.meshes.new(name);me.from_pydata([tuple(c/1000 for c in p) for p in v],[],f);me.update()
 ob=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(ob);me.materials.append(m)
 for face in me.polygons: face.use_smooth=smooth
 return ob

def cube(name,center,dims,m,bevel=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=tuple(v/1000 for v in center));o=bpy.context.object;o.name=name;o.dimensions=tuple(v/1000 for v in dims)
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(m)
 if bevel:
  b=o.modifiers.new('Physical edge radius','BEVEL');b.width=bevel/1000;b.segments=3
  bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=b.name)
  for f in o.data.polygons:f.use_smooth=True
  n=o.modifiers.new('Corner normals','WEIGHTED_NORMAL')
 return o

def curve(name,pts,r,m,cyclic=False):
 cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.resolution_u=2;cu.bevel_depth=r/1000;cu.bevel_resolution=2
 sp=cu.splines.new('POLY');sp.points.add(len(pts)-1)
 for p,co in zip(sp.points,pts):p.co=(*(v/1000 for v in co),1)
 sp.use_cyclic_u=cyclic
 ob=bpy.data.objects.new(name,cu);bpy.context.collection.objects.link(ob);cu.materials.append(m);return ob

def interp(x,pts):
 if x<=pts[0][0]:return pts[0][1]
 for ii,((x0,y0),(x1,y1)) in enumerate(zip(pts,pts[1:])):
  if x<=x1:
   t=(x-x0)/(x1-x0); prev=pts[max(0,ii-1)]; nex=pts[min(len(pts)-1,ii+2)]; m0=(y1-prev[1])/(x1-prev[0]); m1=(nex[1]-y0)/(nex[0]-x0); val=(2*t**3-3*t*t+1)*y0+(t**3-2*t*t+t)*(x1-x0)*m0+(-2*t**3+3*t*t)*y1+(t**3-t*t)*(x1-x0)*m1; return max(min(y0,y1),min(max(y0,y1),val))
 return pts[-1][1]
# Explicit longitudinal stations; no whole-shoe ellipse or collapsed collar.
WIDTH=[(-145,1),(-141,18),(-133,32),(-118,40),(-90,41),(-55,39),(-20,42),(20,49),(55,52),(88,49),(115,39),(134,24),(144,5),(145,1)]
if MODEL=='97':WIDTH=[(x,w*.95) for x,w in WIDTH]
TOP=[(-145,104),(-139,104),(-121,104),(-96,102),(-70,96),(-40,103),(-8,93),(27,77),(65,58),(100,46),(125,39),(140,29),(145,21)]
if MODEL=='97':TOP=[(x,z-4 if x<110 else z-2) for x,z in TOP]
def width(x):
 # Elliptic end-cap curvature avoids the finite-slope pointed toe of a table endpoint.
 if x>=115:return max(.8,39*(.95 if MODEL=='97' else 1)*math.sqrt(max(0,1-((x-115)/30)**2)))
 return interp(x,WIDTH)
def spring(x):return 10*max(0,(x-85)/60)**2+1.5*max(0,(-x-125)/20)**2
def sole_top(x):return interp(x,[(-145,31),(-95,30),(-40,25),(30,21),(90,19),(145,18)])+spring(x)
def base(x):return sole_top(x)-.7
def top(x):return max(base(x)+5,interp(x,TOP))
def archmax(x):return interp(x,[(-145,1.57),(-134,1.20),(-117,.88),(-90,.83),(-62,.83),(-40,.9),(40,.90),(66,1.13),(83,1.57),(145,1.57)])
def surf(x,ang,side=1,offset=0):
 w=max(.7,width(x)-1.8)
 y=side*((w*math.cos(ang))+offset*math.cos(ang))
 z=base(x)+(top(x)-base(x))*math.sin(ang)+offset*math.sin(ang)
 return (x,y,z)
XS=[-145+i*290/112 for i in range(113)]

def patch(name,x0,x1,lo,hi,m,side=1,offset=.7,nu=45,nv=8):
 vs=[]
 for i in range(nu+1):
  x=x0+(x1-x0)*i/nu;l=lo(x) if callable(lo) else lo;h=hi(x) if callable(hi) else hi
  for j in range(nv+1):vs.append(surf(x,l+(h-l)*j/nv,side,offset))
 fs=[]
 for i in range(nu):
  for j in range(nv):
   k=i*(nv+1)+j;f=(k,k+nv+1,k+nv+2,k+1);fs.append(f[::-1] if side==1 else f)
 return mesh(name,vs,fs,m)

# Upper has actual open collar and throat; separate tongue occupies only throat.
for side in [-1,1]:
 o=patch('Upper mesh shell '+str(side),-145,145,0,archmax,meshmat,side,0,112,20)
 sol=o.modifiers.new('Upper textile thickness','SOLIDIFY');sol.thickness=.0009;sol.offset=-1
 # Foot opening edge is continuous around heel, with separate tongue at its front.
 pts=[surf(x,archmax(x),side,.2) for x in XS if x<63]
 curve('Padded collar and throat '+str(side),pts,2.1 if MODEL=='90' else 1.8,lining)
 patch('Inside collar lining '+str(side),-140,-38,lambda x:max(.0,archmax(x)-.23),archmax,lining,side,-1.0,50,5)
 # Mudguard is a physical strip with shaped upper boundary.
 def mud(x):return interp(x,[(-145,.35),(-112,.32),(-65,.25),(-20,.38),(32,.43),(84,.54),(128,.61),(145,.32)])
 patch('Mudguard '+str(side),-145,145,.015,mud,black if MODEL=='90' else silver,side,.9,95,8)
 curve('Mudguard seam '+str(side),[surf(x,mud(x)-.035,side,1.22) for x in XS[2:-2]],.22,thread)
 if MODEL=='90':
  patch('Quarter suede overlay '+str(side),-137,66,lambda x:mud(x)+.045,lambda x:min(archmax(x)-.09,mud(x)+.34),grey,side,.8,65,8)
  # Toe overlay follows the vamp rather than closing its central mesh.
  patch('Toe cap overlay '+str(side),69,143,lambda x: .56,lambda x:min(1.56,.76+(x-69)*.015),grey,side,1.0,38,6)
 else:
  # Four tapered wave panels conform to the same upper surface.
  for band in range(4):
   def wave(x,b=band):return min(archmax(x)-.045,.34+b*.15+.08*math.sin((x+40)/48))
   patch('Ripple band %s %s'%(side,band),-145,145,lambda x,b=band:max(.04,wave(x,b)-.034),lambda x,b=band:wave(x,b)+.034,silver,side,1.05,100,3)
   curve('Reflective wave %s %s'%(side,band),[surf(x,wave(x,band)+.041,side,1.3) for x in XS],.48,reflect)
# Dark footbed below the opening; not a cap over it.
cube('Inset footbed',(-89,0,47),(83,53,4),lining,8)
# Tongue is a longitudinal arched patch and overlaps the vamp under the laces.
def tongue_z(x):return top(x)+1.8+8*max(0,(-x-30)/22)**2
def tongue_surface(x,y):
 w=max(.7,width(x)-1.8); return base(x)+(top(x)-base(x))*math.sqrt(max(.01,1-(y/w)**2))+1.8+8*max(0,(-x-30)/22)**2
def tongue_w(x):return interp(x,[(-52,19),(-30,24),(10,25),(45,23),(73,18)])
vs=[];fs=[]
for i in range(45):
 x=-52+125*i/44
 for j in range(13):
  v=-1+2*j/12;vs.append((x,v*tongue_w(x),tongue_surface(x,v*tongue_w(x))))
for i in range(44):
 for j in range(12):k=i*13+j;fs.append((k,k+13,k+14,k+1))
o=mesh('Separate padded tongue',vs,fs,meshmat);s=o.modifiers.new('Tongue padding','SOLIDIFY');s.thickness=.002
curve('Tongue crown piping',[(-52,y,tongue_surface(-52,y)) for y in range(-19,20)],1.2,lining if MODEL=='97' else grey)
# Eyestays: follow upper shoulder, keep throat visible. 90 TPU blocks, 97 textile loops.
for side in [-1,1]:
 for k,x in enumerate([-28,-10,8,26,44,59]):
  y=side*(tongue_w(x)+2);z=tongue_surface(x,y)
  if MODEL=='90':
   ob=cube('TPU lace support %s %s'%(side,k),(x,y,z-1),(12,8,3),red if k in (0,1,4,5) else grey,1.2)
   for dx in [-3,0,3]:curve('TPU groove',[(x+dx,y-2,z+.6),(x+dx,y+2,z+.6)],.22,lining)
  else:
   curve('Woven eyelet loop',[(x-2,y+side*3,z-2),(x-2,y,z+1),(x+2,y,z+1),(x+2,y+side*3,z-2)],.85,lining)
# Ribbon laces with predictable path interpolation and controlled over/under.
def ribbon(name,pts,w=3.5,th=.65):
 vs=[]
 for i,p in enumerate(pts):
  d=Vector(pts[min(i+1,len(pts)-1)])-Vector(pts[max(0,i-1)]);d.normalize()
  across=Vector((0,0,1)).cross(d)
  if across.length<.01:across=Vector((0,1,0))
  across=across.normalized()*w/2
  for sg,h in [(-1,-th/2),(1,-th/2),(1,th/2),(-1,th/2)]:vs.append(tuple(Vector(p)+across*sg+Vector((0,0,h))))
 fs=[]
 for i in range(len(pts)-1):
  for j in range(4):fs.append((i*4+j,i*4+(j+1)%4,(i+1)*4+(j+1)%4,(i+1)*4+j))
 fs.extend([(3,2,1,0),tuple((len(pts)-1)*4+j for j in range(4))]);return mesh(name,vs,fs,laces)
rows=[59,44,26,8,-10,-28]
for i in range(len(rows)-1):
 for side in [-1,1]:
  x0,x1=rows[i],rows[i+1];pts=[]
  for j in range(25):
   t=j/24;x=x0+(x1-x0)*t;y=(1-t)*side*(tongue_w(x0)+1)+t*(-side)*(tongue_w(x1)+1)
   # Sag follows tongue dome; crossing strands differ 1.6mm at centre.
   z=tongue_surface(x,y)+2.0+(1.6 if side==1 else 0)*math.sin(math.pi*t)
   pts.append((x,y,z))
  ribbon('Crossed lace %s %s'%(i,side),pts,3.3 if MODEL=='90' else 2.7)
for side in [-1,1]:
 pts=[]
 for i in range(49):
  t=i/48*2*math.pi;x=-33+10*math.sin(t);y=side*(5+14*(1-math.cos(t))/2);pts.append((x,y,tongue_z(x)+4+2*math.sin(t)))
 ribbon('Tied bow loop '+str(side),pts,2.7)
 ribbon('Lace tail '+str(side),[(-33-i*.6,side*(5+i*.65),tongue_z(-33-i*.6)+3) for i in range(20)],2.7)
# Sole plan is an explicit two-sided longitudinal perimeter.
per=[(x,width(x)) for x in XS]+[(x,-width(x)) for x in reversed(XS)]

def sole(name,bottom,upper,m,expand=0):
 vs=[];n=len(per)
 for h,scal in [(bottom,1),(upper,1)]:
  for x,y in per:vs.append((x,y+math.copysign(expand,y),h(x)))
 fs=[tuple(reversed(range(n))),tuple(range(n,2*n))]
 fs +=[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 ob=mesh(name,vs,fs,m,False)
 for face in ob.data.polygons[2:]:face.use_smooth=True
 return ob
sole('Thin rubber outsole',lambda x:spring(x),lambda x:4.2+spring(x),black,.3)
foam=sole('Sculpted foam midsole',lambda x:4.0+spring(x),sole_top,white,0)
# Closed cutter crosses both sidewalls, so a visible lens cannot hide behind solid foam.
windows=[(-98,70,16,13)] if MODEL=='90' else [(-127,26,15,13),(3,214,13.5,11)]
for idx,(x,length,z,h) in enumerate(windows):
 cutter=cube('Temporary Air opening cutter',(x,0,z),(length,140,h),black,4.3 if MODEL=='90' else 3.8)
 bpy.context.view_layer.objects.active=foam;mod=foam.modifiers.new('Actual Air cavity '+str(idx),'BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
 bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)
 # Air bladder occupies cavity with 1mm clearance; visible solid volumes and pillars.
 for side in [-1,1]:
  pts=[]
  for j in range(65):
   xx=x-length/2+4+(length-8)*j/64;pts.append((xx,side*(width(xx)-2.3),z))
  # Each bladder follows width to stay inside the sidewall at every station.
  v=[];f=[]
  for j,p in enumerate(pts):
   for k in range(12):
    t=2*math.pi*k/12;v.append((p[0],p[1]+math.cos(t)*2.2,p[2]+math.sin(t)*(h/2-1.0)))
  for j in range(64):
   for k in range(12):f.append((j*12+k,j*12+(k+1)%12,(j+1)*12+(k+1)%12,(j+1)*12+k))
  f.extend([tuple(reversed(range(12))),tuple(64*12+k for k in range(12))]);mesh('Transparent Air bladder %s %s'%(idx,side),v,f,airmat)
  for j in range(1,max(3,round(length/13))):
   xx=x-length/2+length*j/max(3,round(length/13));cube('Internal Air chamber pillar',(xx,side*(width(xx)-4.7),z),(3.1,3.8,h-3),support,1.2)
# 90 heel cassette fascia is a ring around the cavity, not a painted window.
if MODEL=='90':
 for side in [-1,1]:
  x0,x1=-140,-53;vs=[]
  for x,z in [(x0,5),(x1,6),(x1,29),(x0,32),(-132,9.5),(-64,9.5),(-64,22.5),(-132,22.5)]:vs.append((x,side*(width(x)+.8),z))
  mesh('Infrared Air cassette rim '+str(side),vs,[(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],red)
  for i in range(12):
   x=-130+i*5;curve('Cassette rib',[(x,side*(width(x)+1),25),(x+.4,side*(width(x)+1),28)],.4,red)
# A slight edge radius preserves separation between rubber and foam.
be=foam.modifiers.new('Molded sole edge','BEVEL');be.width=.0007;be.segments=2
foam.modifiers.new('Sole weighted normals','WEIGHTED_NORMAL')
# Discrete outsole tread blocks rather than increasing the whole sole height.
for x in range(-127,124,13):
 for y in range(-39,40,13):
  if abs(y)<width(x)-7:
   cube('Waffle tread' if MODEL=='90' else 'Traction pod',(x,y,spring(x)-.3),(9,9,.9),red if MODEL=='97' and -25<x<5 else black,.6)
# Side markings are real attached curved planar patches, never billboards.
def side_xyz(x,z,side,offset=1.8):
 ratio=max(0,min(.985,(z-base(x))/(top(x)-base(x))));return (x,side*((width(x)-1.8)*math.sqrt(1-ratio*ratio)+offset),z)
for side in [-1,1]:
 segments=[[(40,69),(37,57),(28,49),(17,52)],[(17,52),(-8,60),(-70,77),(-103,85)],[(-103,85),(-63,76),(-7,63),(20,59)],[(20,59),(30,57),(36,61),(40,69)]]
 coords=[]
 for seg in segments:
  for kk in range(16):
   t=kk/16;coords.append(tuple((1-t)**3*seg[0][d]+3*(1-t)**2*t*seg[1][d]+3*(1-t)*t*t*seg[2][d]+t**3*seg[3][d] for d in range(2)))
 if MODEL=='97':coords=[(x*.40+5,(z-53)*.40+39) for x,z in coords]
 from mathutils.geometry import tessellate_polygon
 vs=[];fs=[]
 poly=[Vector((x,z,0)) for x,z in coords]
 for rawtri in tessellate_polygon([poly]):
  tri=[poly[t] if isinstance(t,int) else t for t in rawtri]
  grid={};n=10
  for ii in range(n+1):
   for jj in range(n+1-ii):
    q=tri[0]+(tri[1]-tri[0])*(ii/n)+(tri[2]-tri[0])*(jj/n);grid[ii,jj]=len(vs);vs.append(side_xyz(q.x,q.y,side,2))
  for ii in range(n):
   for jj in range(n-ii):
    fs.append((grid[ii,jj],grid[ii+1,jj],grid[ii,jj+1]))
    if jj<n-ii-1:fs.append((grid[ii+1,jj],grid[ii+1,jj+1],grid[ii,jj+1]))
 mesh('Side swoosh '+str(side),vs,fs,reflect if MODEL=='90' else red)
 # Small side badge on AM90.
 if MODEL=='90':
  coords=[(-65,46),(-26,46),(-26,55),(-65,56)]
  mesh('Ribbed side badge '+str(side),[side_xyz(x,z,side,2.5) for x,z in coords],[(0,1,2,3)],red)
  for i in range(12):
   x=-63+i*3;curve('Badge raised rib',[side_xyz(x,47,side,3),side_xyz(x,53,side,3)],.32,red)
# Rear reinforcement/pull loop sits at heel, independent of collar opening.
if MODEL=='97':
 ribbon('Heel pull loop',[(-137,0,82),(-144,0,109),(-139,0,117),(-134,0,109),(-136,0,91)],7,1)
 ribbon('Tongue pull loop',[(-47,-4,106),(-55,-4,119),(-55,4,119),(-47,4,106)],3,1)
else:
 pass # Rear badge is projected to the actual heel below, not buried in a flat plane.
# Mesh weave: shallow aligned dashes on exposed toe; this is preview geometry, removable LOD detail.
for x in range(84,127,3):
 for y in range(-27,28,3):
  w=max(.7,width(x)-1.8)
  if abs(y)<w*.65:
   ang=math.acos(abs(y)/w);z=base(x)+(top(x)-base(x))*math.sin(ang)
   curve('Toe mesh knit',[(x-.6,y-.35,z+.2),(x+.6,y+.35,z+.2)],.13,thread)
# Close the two end cross-sections; the sides alone are not closed solids.
for xx,label in [(-145,'Heel'),(145,'Toe')]:
 vv=[surf(xx,math.pi*k/32,1,.15) for k in range(33)]
 vv.append((xx,0,base(xx)));ff=[(33,k,k+1) for k in range(32)]
 mesh(label+' closed end',vv,ff,meshmat if xx<0 else (grey if MODEL=='90' else silver))
# Rear badge: solve its attachment against the heel surface at each elevation.
def rear_point(y,z):
 def yy(x):
  ratio=max(0,min(.999,(z-base(x))/(top(x)-base(x))));return max(.7,width(x)-1.8)*math.sqrt(1-ratio*ratio)
 low,high=-145.,-112.
 for k in range(30):
  mid=(low+high)/2
  if yy(mid)<abs(y):low=mid
  else:high=mid
 return ((low+high)/2-.8,y,z)
if MODEL=='90':
 vv=[];ff=[]
 for i in range(19):
  y=-18+36*i/18
  for j in range(11):vv.append(rear_point(y,72+20*j/10))
 for i in range(18):
  for j in range(10):k=i*11+j;ff.append((k,k+11,k+12,k+1))
 mesh('Surface fitted rear TPU badge',vv,ff,red)
 for z in [75,78,81,84,87,90]:curve('Rear badge rib',[(x-.35,y,zz) for x,y,zz in [rear_point(y,z) for y in range(-16,17)]],.35,red)

# Audit before studio assets. Save geometry in editable .blend, export material meshes as GLB.
asset_objects=list(bpy.context.scene.objects)
sc.render.engine='CYCLES';sc.cycles.samples=a.samples;sc.cycles.use_denoising=True
sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.27,.30,.35,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.35
sc.view_settings.view_transform='AgX';sc.view_settings.look='AgX - Medium High Contrast';sc.view_settings.exposure=0;sc.render.film_transparent=False
floor=material('Studio floor',(.19,.215,.25),.83)
cube('Studio ground',(0,0,-2.0),(20000,20000,2),floor)
for name,loc,power,size in [('Key',(100,-330,490),3.2,.45),('Fill',(-170,290,310),1.5,.35),('Rim',(-350,-30,300),2.5,.28)]:
 bpy.ops.object.light_add(type='AREA',location=tuple(v/1000 for v in loc));o=bpy.context.object;o.name=name;o.data.energy=power;o.data.shape='DISK';o.data.size=size;o.rotation_euler=(Vector((0,0,.04))-o.location).to_track_quat('-Z','Y').to_euler()

def camera(name,loc,target=(0,0,50),scale=355):
 bpy.ops.object.camera_add(location=tuple(v/1000 for v in loc));o=bpy.context.object;o.name=name;o.rotation_euler=(Vector(tuple(v/1000 for v in target))-o.location).to_track_quat('-Z','Y').to_euler();o.data.type='ORTHO';o.data.ortho_scale=scale/1000;o.data.clip_start=.001;o.data.clip_end=10;return o
cams={'hero':camera('Three-quarter',(340,-540,280),scale=350),'side':camera('Lateral elevation',(0,-700,54),scale=335),'top':camera('Plan',(0,0,700),(0,0,0),335),'front':camera('Toe elevation',(700,0,50),scale=165),'back':camera('Heel elevation',(-700,0,58),scale=165)}
sc.render.resolution_x=1440;sc.render.resolution_y=960;sc.render.resolution_percentage=100
# Export evaluated objects; procedural bump remains Blender-only and is explicitly documented.
bpy.ops.object.select_all(action='DESELECT')
for o in asset_objects:o.select_set(True)
bpy.context.view_layer.objects.active=asset_objects[0]
bpy.ops.export_scene.gltf(filepath=str(OUT/('AirMax'+MODEL+'.glb')),use_selection=True,export_apply=True,export_yup=True)
deps=bpy.context.evaluated_depsgraph_get();stats={'model':MODEL,'units':'millimeters in authoring; meters in Blender','dimensions_status':'artist estimates, NOT manufacturer measurements','old_game_sole_top_mm':61,'heel_sole_top_mm':sole_top(-95),'forefoot_sole_top_mm':sole_top(55),'window_specs_x_length_z_height_mm':windows,'stations_width_mm':WIDTH,'stations_top_mm':TOP,'objects':len(asset_objects),'evaluated_triangles':0,'cameras':{}}
for o in asset_objects:
 if o.type not in ('MESH','CURVE'):continue
 ev=o.evaluated_get(deps);me=ev.to_mesh();me.calc_loop_triangles();stats['evaluated_triangles']+=len(me.loop_triangles);ev.to_mesh_clear()
for name,cam in cams.items():stats['cameras'][name]={'position_m':list(cam.location),'rotation_euler':list(cam.rotation_euler),'orthographic_scale_m':cam.data.ortho_scale}
(OUT/'measurements.json').write_text(json.dumps(stats,indent=2))
sc.camera=cams['hero'];bpy.ops.wm.save_as_mainfile(filepath=str(OUT/('AirMax'+MODEL+'.blend')))
for view in a.views.split(','):
 if not view:continue
 sc.camera=cams['hero'] if view=='clay' else cams[view]
 if view=='clay':
  clay=material('Inspection clay',(.42,.42,.42),.75);bpy.context.view_layer.material_override=clay
 sc.render.filepath=str(OUT/(view+'.png'));bpy.ops.render.render(write_still=True)
 bpy.context.view_layer.material_override=None
print('AIRMAX_PREVIEW_COMPLETE',MODEL,json.dumps(stats))
