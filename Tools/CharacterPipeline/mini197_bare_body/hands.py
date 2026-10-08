"""MINI-197 hand rebuild: separated anatomical fingers + finger joints moved into the right digits.

The MINI-105 AccuRig hand rig is shifted one finger (Index chain in the thumb, Mid/Ring/Pinky in
index/middle/ring, no bones in the pinky, Thumb a 13 mm stub at the wrist) and the donor fingers are fused.
Design (left hand, world metres; right hand mirrored about its own wrist joint):
  palm length wrist->MCP ~0.105 (hand 0.20 for a 1.85 m body), finger centre spacing 0.023,
  finger length MCP->tip: index .088 mid .095 ring .090 pinky .072, phalanges 48/30/22 %,
  cross-section width x height at the base: index 19.5x18 mm, mid 20x18.5, ring 19x17.5, pinky 16.5x15, 25 % taper,
  relaxed spread index -5, mid 0, ring +4, pinky +9 degrees; 4 degrees curl per joint.
The thumb keeps the donor geometry: Thumb2/Thumb3 go where the donor's mis-named Index1/Index2 joints sit
(thumb MCP and IP), Thumb1 at the CMC inside the palm."""
import numpy as np, math
FINGERS={ # name: (y offset from wrist joint, MCP x offset, length, base width, base height, spread deg)
 'Index':(-0.037,0.103,0.088,0.0195,0.0180,-5.0),
 'Mid':  (-0.014,0.108,0.095,0.0200,0.0185, 0.0),
 'Ring': ( 0.009,0.103,0.090,0.0190,0.0175, 4.0),
 'Pinky':( 0.031,0.091,0.072,0.0165,0.0150, 9.0)}
MCP_DZ=-0.012   # knuckle centre below the wrist joint (palm is thicker than the fingers)
PHAL=(0.48,0.30,0.22); CURL=4.0; TAPER=0.75
def rot_z(v,deg):
    a=math.radians(deg); c,s=math.cos(a),math.sin(a); return np.array([c*v[0]-s*v[1],s*v[0]+c*v[1],v[2]])
def rot_about(v,axis,deg):
    a=math.radians(deg); axis=axis/np.linalg.norm(axis)
    return v*math.cos(a)+np.cross(axis,v)*math.sin(a)+axis*axis.dot(v)*(1-math.cos(a))
def design(bone,side):
    """bone(name)->world head. Returns {digit:[j1,j2,j3,tip]} in world for this side (sg=+1 left)."""
    sg=1 if side=='L' else -1
    wr=bone(f'CC_Base_{side}_Hand'); J={}
    for f,(dy,dx,L,w,h,spread) in FINGERS.items():
        mcp=wr+np.array([sg*dx,dy,MCP_DZ])
        d=np.array([sg*1.0,0,0]); d=rot_z(d,sg*spread)
        lat=np.cross(np.array([0,0,1.0]),d); pts=[mcp]; p=mcp.copy()
        for i,fr in enumerate(PHAL):
            d=rot_about(d,lat,CURL)   # +angle about lat=z x d bends toward -Z (palm side) on both hands
            p=p+d*L*fr; pts.append(p.copy())
        J[f]=pts
    # thumb: donor 'Index1'/'Index2' joints are the thumb MCP/IP; tip continues the IP->'Index3' direction
    t2=bone(f'CC_Base_{side}_Index1'); t3=bone(f'CC_Base_{side}_Index2'); t4=bone(f'CC_Base_{side}_Index3')
    dirt=(t4-t3)/np.linalg.norm(t4-t3); tip=t3+dirt*0.030
    a=(t3-t2)/np.linalg.norm(t3-t2); t1=t2-a*0.040+np.array([0,0,0.004])   # CMC ~4 cm back along the thumb axis, inside the palm
    J['Thumb']=[t1,t2,t3,tip]
    return J
def ring_frame(d,up=np.array([0,0,1.0])):
    d=d/np.linalg.norm(d); x=np.cross(up,d); x/=np.linalg.norm(x); z=np.cross(d,x); return d,x,z
def finger_mesh(pts,w,h,start_back=0.022,nring=14,step=0.003):
    """Closed tapered capsule along polyline pts=[mcp,j2,j3,tip], starting start_back inside the palm."""
    pts=[np.asarray(p,float) for p in pts]
    d0=(pts[1]-pts[0]); d0/=np.linalg.norm(d0); poly=[pts[0]-d0*start_back]+pts
    seg=np.array([np.linalg.norm(b-a) for a,b in zip(poly[:-1],poly[1:])]); cum=np.r_[0,np.cumsum(seg)]; total=cum[-1]
    s_mcp=cum[1]; joints=cum[2:4]
    def at(s):
        k=min(np.searchsorted(cum,s,side='right')-1,len(seg)-1); t=(s-cum[k])/seg[k]
        return poly[k]+(poly[k+1]-poly[k])*t, (poly[k+1]-poly[k])/seg[k]
    rtip=0.5*min(w,h)*TAPER
    verts=[];faces=[];rings=[]
    S=np.arange(0,total-rtip,step).tolist()+[total-rtip+rtip*q for q in (0.35,0.6,0.8,0.92,0.98)]
    for s in S:
        p,d=at(min(s,total-1e-5)); _,x,z=ring_frame(d)
        f=np.clip((s-s_mcp)/(total-s_mcp),0,1); sc=1-(1-TAPER)*f
        for jt in joints: sc*=1+0.04*math.exp(-((s-jt)/0.006)**2)       # slight knuckle fullness
        cw,ch=0.5*w*sc,0.5*h*sc
        if s>total-rtip:                                                 # rounded tip
            q=(s-(total-rtip))/rtip; k=math.sqrt(max(0,1-q*q)); cw*=k; ch*=k; p=p+d*0  
        if s<0.004:                                                      # rounded start inside the palm
            k=math.sqrt(max(0.15,s/0.004)); cw*=k; ch*=k
        ring=[]
        for i in range(nring):
            a=2*math.pi*i/nring; ring.append(len(verts)); verts.append(p+x*cw*math.cos(a)+z*ch*math.sin(a))
        rings.append(ring)
    for r0,r1 in zip(rings[:-1],rings[1:]):
        for i in range(nring): faces.append((r0[i],r0[(i+1)%nring],r1[(i+1)%nring],r1[i]))
    # end caps
    c0=len(verts); verts.append(at(0)[0]); c1=len(verts); verts.append(poly[-1])
    for i in range(nring):
        faces.append((c0,rings[0][(i+1)%nring],rings[0][i])); faces.append((c1,rings[-1][i],rings[-1][(i+1)%nring]))
    return np.array(verts),faces
def chain(J,digit):
    """bone names per segment of the digit polyline."""
    return [f'{digit}1',f'{digit}2',f'{digit}3']
def seg_dist(P,a,b):
    ab=b-a; t=np.clip(((P-a)@ab)/ab.dot(ab),0,1); return np.linalg.norm(P-(a+np.outer(t,ab)),axis=1),t
def hand_weights(P,J,side,wrist,blend=0.006):
    """Per-vertex weights for hand-region points P: nearest digit decides the finger (no cross-finger bleed),
    smooth blends across each joint along that digit, Hand bone for the palm."""
    pre=f'CC_Base_{side}_'
    best=np.full(len(P),np.inf); bestd=np.array(['']*len(P),dtype=object); arc=np.zeros(len(P))
    for dg,pts in J.items():
        back=pts[0]-(pts[1]-pts[0])/np.linalg.norm(pts[1]-pts[0])*0.03
        poly=[back]+list(pts); cum=0.0; dmin=np.full(len(P),np.inf); amin=np.zeros(len(P))
        for a,b in zip(poly[:-1],poly[1:]):
            d,t=seg_dist(P,a,b); L=np.linalg.norm(b-a); m=d<dmin; dmin[m]=d[m]; amin[m]=cum+t[m]*L; cum+=L
        m=dmin<best; best[m]=dmin[m]; bestd[m]=dg; arc[m]=amin[m]
    W={}
    def add(name,w):
        W.setdefault(pre+name,np.zeros(len(P))); W[pre+name]+=w
    for dg,pts in J.items():
        m=bestd==dg
        if not m.any(): continue
        L=[0.03]+[np.linalg.norm(b-a) for a,b in zip(pts[:-1],pts[1:])]; c=np.cumsum([0]+L)   # c: back, j1, j2, j3, tip
        s=arc[m]; ss=lambda x0: np.clip((s-x0+blend)/(2*blend),0,1)
        b1=ss(c[1]); b2=ss(c[2]); b3=ss(c[3])
        wH=1-b1; w1=b1-b2; w2=b2-b3; w3=b3
        for nm,w in (('Hand',wH),(f'{dg}1',w1),(f'{dg}2',w2),(f'{dg}3',w3)):
            full=np.zeros(len(P)); full[m]=w; add(nm,full)
    return W
