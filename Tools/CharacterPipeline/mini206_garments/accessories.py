import bpy,sys,os,math,json,numpy as np
from mathutils import Vector
sys.path.insert(0,'scripts'); from garlib import load_body
R='/home/user/Upizup-mini/Docs/CharacterPipeline/MINI-197/BareBody/'
def U(p): return [round(-p[0],4),round(p[2],4),round(-p[1],4)]          # Blender (front -Y) -> Unity character frame (x right,y up,z fwd)
out={}
for char,fbx in (('Sacat','04-NeckTorso/SacatBare_04NeckTorso.fbx'),('Franki','05-Franki/FrankiBare.fbx')):
    arm,parts,bm,bvh=load_body(R+fbx)
    B=lambda n: np.array(arm.matrix_world@arm.pose.bones[n].head)
    names=[b.name for b in arm.pose.bones]
    eyes=[n for n in names if 'Eye' in n and ('L_' in n or 'R_' in n)]
    head=B('CC_Base_Head'); fa=B('CC_Base_L_Forearm'); hand=B('CC_Base_L_Hand'); sp=B('CC_Base_Spine02')
    hp=[p for p in parts if 'Head' in p.name][0]; H=np.array([hp.matrix_world@v.co for v in hp.data.vertices])
    def ray(o,d):
        h=bvh.ray_cast(Vector(o),Vector(d),2); return np.array(h[0]) if h[0] else None
    E=np.mean([B(n) for n in eyes],axis=0) if eyes else np.array([0,H[:,1].min()+0.03,head[2]+0.09])
    bridge=ray((0,-0.6,E[2]),(0,1,0)); 
    temple_L=ray((0.6,E[1]+0.035,E[2]),(-1,0,0)); temple_R=ray((-0.6,E[1]+0.035,E[2]),(1,0,0))
    top=H[np.argmax(H[:,2])]
    ear_z=E[2]-0.025; band=H[np.abs(H[:,2]-ear_z)<0.01]; earL=band[np.argmax(band[:,0])]; earR=band[np.argmin(band[:,0])]
    ring=[]
    for a in np.linspace(0,2*np.pi,48,endpoint=False):
        c=np.array([0,(H[:,1].min()+H[:,1].max())/2,top[2]-0.07]); d=(math.cos(a),math.sin(a),0)
        p=ray(tuple(c+np.array(d)*0.4),tuple(-np.array(d)))
        if p is not None: ring.append(p)
    ring=np.array(ring); circ=float(np.sum(np.linalg.norm(np.diff(np.vstack([ring,ring[:1]]),axis=0),axis=1)))
    fdir=(hand-fa)/np.linalg.norm(hand-fa); wrist=hand-fdir*0.035
    rads=[]
    for a in np.linspace(0,2*np.pi,24,endpoint=False):
        u=np.cross(fdir,[0,0,1]); u/=np.linalg.norm(u); v=np.cross(fdir,u); d=math.cos(a)*u+math.sin(a)*v
        p=ray(tuple(wrist+d*0.2),tuple(-d)); 
        if p is not None: rads.append(np.linalg.norm(p-wrist))
    wr=float(np.median(rads)); wtop=ray(tuple(wrist+np.array([0,0,0.2])),(0,0,-1))
    items={
     'shades_ray':{'bone':'Head','cc_bone':'CC_Base_Head','anchor':(bridge+np.array([0,-0.012,0])) if bridge is not None else E,
                   'euler_character':[0,0,0],'measured':{'eye_mid':U(E),'nose_bridge':U(bridge) if bridge is not None else None,'temple_width_m':round(float(temple_L[0]-temple_R[0]),4) if temple_L is not None and temple_R is not None else None},
                   'note':'lens plane 12 mm in front of the nose bridge, centred on the eyes; arms reach the temples'},
     'cap_mike':{'bone':'Head','cc_bone':'CC_Base_Head','anchor':np.array([0,(H[:,1].min()+H[:,1].max())/2-0.005,top[2]-0.045]),'euler_character':[-8,0,0],
                 'measured':{'head_top':U(top),'head_circumference_at_band_m':round(circ,4)},'note':'crown centre 4.5 cm below the head top, peak forward, tilted 8 deg nose-down; scale the cap so its inner band matches the circumference'},
     'headphones_studio':{'bone':'Head','cc_bone':'CC_Base_Head','anchor':(earL+earR)/2,'euler_character':[0,0,0],
                 'measured':{'ear_L':U(earL),'ear_R':U(earR),'ear_span_m':round(float(earL[0]-earR[0]),4),'head_top':U(top)},'note':'midpoint between the ears; cups 1 cm outside each ear, band over the head top'},
     'watch_rollie':{'bone':'LeftLowerArm','cc_bone':'CC_Base_L_Forearm','anchor':wrist,'euler_character':[0,0,0],
                 'measured':{'wrist_centre':U(wrist),'wrist_radius_m':round(wr,4),'wrist_top_surface':U(wtop) if wtop is not None else None,'forearm_dir':U(fdir)},
                 'note':'3.5 cm up the forearm from the wrist joint; face on the back of the wrist (up in the T-pose); strap inner radius = wrist radius + 2 mm'},
     'chain_gold':{'bone':'Chest','cc_bone':'CC_Base_Spine02','anchor':sp,'euler_character':[0,0,0],'measured':{},'note':'use the fitted Chain_<Char>_<Outfit>.fbx meshes (already in place, skinned to Spine02/NeckTwist01): offset 0'}}
    res={}
    for k,it in items.items():
        bpos=B(it['cc_bone']); a=it['anchor']
        res[k]={'humanBodyBone':it['bone'],'ccBone':it['cc_bone'],'anchor_character_unity':U(a),'offset_from_bone_character_unity':U(a-bpos+np.array([0,0,0]))[:1]+[round(float(a[2]-bpos[2]),4),round(float(-(a[1]-bpos[1])),4)],
                'euler_character_unity':it['euler_character'],'localScale':[1,1,1],'measured':it['measured'],'note':it['note']}
    out[char]=res
    print(char,{k:v['anchor_character_unity'] for k,v in res.items()}, 'eyes',eyes)
os.makedirs('g2/acc',exist_ok=True)
for c,r in out.items():
    json.dump({'character':c,'frame':'Unity character frame: x right, y up, z forward, metres, bind pose (T-pose); offset = anchor - bone head','useManualPlacement':False,'items':r},open(f'g2/acc/{c}_AccessoryPlacements.json','w'),indent=1)
