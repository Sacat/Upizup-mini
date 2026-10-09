import bpy,sys,os,numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
sys.path.insert(0,'scripts'); import scene_lib as sl
from garlib import load_body
A='/home/user/Upizup-mini/Assets/UpIzUpMini/Art/Characters/Garments/Outfits166/'
R='/home/user/Upizup-mini/Docs/CharacterPipeline/MINI-197/BareBody/'
for char,fbx in (('SacatBare',R+'04-NeckTorso/SacatBare_04NeckTorso.fbx'),('FrankiBare',R+'05-Franki/FrankiBare.fbx')):
    arm,parts,bm,bvh=load_body(fbx)
    feet=[p for p in parts if 'Feet' in p.name][0]; F=np.array([feet.matrix_world@v.co for v in feet.data.vertices])
    for shoe in ('AM90','AM97','shoes_mike90','shoes_mike97','shoes_mike270'):
        path=A+f'{char}_{shoe}.asset'
        if not os.path.exists(path): continue
        m=sl.load_yaml_mesh(path); V=np.array(m[0] if isinstance(m,tuple) else m['verts'],float)
        Vb=np.c_[-V[:,0],-V[:,2],V[:,1]]   # Unity -> Blender (front -Y)
        F_=m[1] if isinstance(m,tuple) else m['tris']
        sb=BVHTree.FromPolygons([tuple(v) for v in Vb],[tuple(t) for t in np.array(F_).reshape(-1,3)])
        # foot verts outside the shoe: parity ray test
        def inside(p):
            n=0;o=Vector(p);d=Vector((0.013,0.007,1)).normalized()
            for _ in range(40):
                h=sb.ray_cast(o,d,3)
                if h[0] is None: break
                n+=1;o=h[0]+d*1e-4
            return n%2==1
        lowF=F[F[:,2]<0.10]
        sd=[]
        for p in lowF:
            loc,n,_,d=sb.find_nearest(Vector(tuple(p)))
            sd.append(d if np.dot(np.array(p)-np.array(loc),np.array(n))>0 else -d)
        sd=np.array(sd); out=sd[sd>0.003]
        print(char,shoe,'shoe bbox',Vb.min(0).round(3),Vb.max(0).round(3),'| foot bbox',F.min(0).round(3),F.max(0).round(3),'| foot verts below 10cm poking out',len(out),'of',len(lowF))
