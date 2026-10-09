import UnityPy, os, glob, numpy as np
R='/home/user/Upizup-mini'
def guid_str(g):
    if isinstance(g,str) and len(g)==32: return g
    b=bytes(g) if not isinstance(g,(bytes,bytearray)) else g
    if isinstance(g,str): b=g.encode('latin1')
    return ''.join(f'{x&0xf:x}{x>>4:x}' for x in b)
class Scene:
    def __init__(self,path):
        self.env=UnityPy.load(path); self.sf=self.env.file
        self.objs={o.path_id:o for o in self.env.objects}; self.cache={}
        self.g2p={}
        for meta in glob.glob(R+'/Assets/**/*.meta',recursive=True):
            try:
                with open(meta,errors='ignore') as f:
                    for line in f:
                        if line.startswith('guid:'): self.g2p[line.split()[1]]=meta[:-5]; break
            except Exception: pass
    def tt(self,pid):
        if pid not in self.cache: self.cache[pid]=self.objs[pid].read_typetree()
        return self.cache[pid]
    def ext(self,ptr):
        fid=ptr['m_FileID']
        if fid==0: return ('scene',ptr['m_PathID'])
        e=self.sf.externals[fid-1]; g=guid_str(e.guid)
        return (os.path.relpath(self.g2p[g],R) if g in self.g2p else 'missing:'+g+':'+str(getattr(e,'path','')),ptr['m_PathID'])
    def gameobjects(self):
        for o in self.env.objects:
            if o.type.name=='GameObject': yield o.path_id,self.tt(o.path_id)
    def comps(self,go):
        out={}
        for c in go.get('m_Component',[]):
            p=c['component']
            if p['m_FileID']!=0 or p['m_PathID'] not in self.objs: continue
            out.setdefault(self.objs[p['m_PathID']].type.name,[]).append(p['m_PathID'])
        return out
    def world_matrix(self,tr_pid):
        M=np.eye(4); pid=tr_pid
        while pid:
            t=self.tt(pid)
            if 'm_LocalRotation' not in t: break
            q=t['m_LocalRotation']; p=t['m_LocalPosition']; s=t['m_LocalScale']
            x,y,z,w=q['x'],q['y'],q['z'],q['w']
            Rm=np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])
            L=np.eye(4); L[:3,:3]=Rm*np.array([s['x'],s['y'],s['z']]); L[:3,3]=[p['x'],p['y'],p['z']]
            M=L@M; pid=t['m_Father']['m_PathID'] if t['m_Father']['m_FileID']==0 else 0
        return M
    def path(self,tr_pid):
        names=[]; pid=tr_pid
        while pid:
            t=self.tt(pid)
            if 'm_GameObject' not in t: names.append('<prefab>'); break
            names.append(self.tt(t['m_GameObject']['m_PathID']).get('m_Name','?'))
            pid=t['m_Father']['m_PathID'] if t['m_Father']['m_FileID']==0 else 0
        return '/'.join(reversed(names))
    def active_in_hierarchy(self,tr_pid):
        pid=tr_pid
        while pid:
            t=self.tt(pid)
            if 'm_GameObject' not in t: return True      # stripped prefab-instance transform: assume active
            if not self.tt(t['m_GameObject']['m_PathID']).get('m_IsActive',True): return False
            pid=t['m_Father']['m_PathID'] if t['m_Father']['m_FileID']==0 else 0
        return True
_mesh_envs={}
def load_mesh(scene,ref):
    """ref=(file,path_id) -> (verts Nx3, tris Mx3) local space."""
    f,pid=ref
    if f=='scene': o=scene.objs[pid]
    else:
        if f not in _mesh_envs: _mesh_envs[f]=UnityPy.load(os.path.join(R,f))
        e=_mesh_envs[f]; o=[x for x in e.objects if x.path_id==pid][0]
    m=o.read()
    try:
        v=np.array(m.m_Vertices,dtype=float).reshape(-1,3) if hasattr(m,'m_Vertices') and len(m.m_Vertices) else None
    except Exception: v=None
    exp=m.export() if hasattr(m,'export') else None
    V=[];F=[]
    for line in exp.splitlines():
        if line.startswith('v '): V.append([float(a) for a in line.split()[1:4]])
        elif line.startswith('f '): F.append([int(a.split('/')[0])-1 for a in line.split()[1:4]])
    V=np.array(V); V[:,0]*=-1   # UnityPy OBJ export mirrors X
    return V,np.array(F,dtype=int)

def load_yaml_mesh(path):
    """Parse a text-serialized Unity Mesh .asset (stream 0, float32 position at channel 0). Returns local V,F."""
    import re as _re
    txt=open(path).read()
    vc=int(_re.search(r'm_VertexCount: (\d+)',txt).group(1))
    fmt=int(_re.search(r'm_IndexFormat: (\d+)',txt).group(1))
    ib=bytes.fromhex(_re.search(r'm_IndexBuffer: ([0-9a-f]*)',txt).group(1))
    data=bytes.fromhex(_re.search(r'_typelessdata: ([0-9a-f]*)',txt).group(1))
    chans=_re.findall(r'- stream: (\d+)\n\s+offset: (\d+)\n\s+format: (\d+)\n\s+dimension: (\d+)',txt)
    sizes={0:4,1:2,2:1,3:1,4:2,5:2,6:1,7:1,8:2,9:2,10:4,11:4}
    stride=sum(sizes[int(f)]*(int(d)&0xf) for s,o,f,d in chans if int(s)==0)
    V=np.frombuffer(data[:stride*vc],dtype=np.uint8).reshape(vc,stride)[:,0:12].copy().view(np.float32).reshape(vc,3).astype(float)
    I=np.frombuffer(ib,dtype=np.uint16 if fmt==0 else np.uint32).astype(int)
    return V,I[:len(I)//3*3].reshape(-1,3)
_orig_load_mesh=load_mesh
def load_mesh(scene,ref):
    f,pid=ref
    if f!='scene' and f.endswith('.asset'):
        p=os.path.join(R,f)
        with open(p,'rb') as fh: head=fh.read(5)
        if head==b'%YAML': return load_yaml_mesh(p)
    return _orig_load_mesh(scene,ref)
