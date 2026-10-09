import re,sys,numpy as np
txt=open(sys.argv[1]).read()
docs=re.split(r'^--- !u!(\d+) &(-?\d+).*$',txt,flags=re.M)
objs={}
for i in range(1,len(docs),3): objs[docs[i+1]]=(docs[i],docs[i+2])
names={}; tr={}
for fid,(cls,body) in objs.items():
    if cls=='1':
        m=re.search(r'm_Name: ?(.*)',body); names[fid]=m.group(1).strip()
    if cls=='4':
        gm=re.search(r'm_GameObject: \{fileID: (-?\d+)',body)
        if not gm or 'm_LocalPosition' not in body: continue
        g=gm.group(1)
        def v(key,n):
            m=re.search(key+r': \{([^}]*)\}',body); d=dict(kv.split(':') for kv in m.group(1).replace(' ','').split(','))
            return [float(d[k]) for k in 'xyzw'[:n]]
        f=re.search(r'm_Father: \{fileID: (-?\d+)',body).group(1)
        tr[fid]=dict(go=g,p=v('m_LocalPosition',3),q=v('m_LocalRotation',4),s=v('m_LocalScale',3),f=f)
def qm(q):
    x,y,z,w=q; return np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])
def world(fid):
    t=tr[fid]; M=np.eye(4); M[:3,:3]=qm(t['q'])@np.diag(t['s']); M[:3,3]=t['p']
    return world(t['f'])@M if t['f']!='0' and t['f'] in tr else M
def path(fid):
    t=tr[fid]; n=names.get(t['go'],'?'); return (path(t['f'])+'/' if t['f']!='0' and t['f'] in tr else '')+n
for fid in tr:
    M=world(fid); n=names.get(tr[fid]['go'],'')
    print('%-70s pos %s  scale %s'%(path(fid)[:70],np.round(M[:3,3],4),np.round(np.linalg.norm(M[:3,:3],axis=0),4)))
