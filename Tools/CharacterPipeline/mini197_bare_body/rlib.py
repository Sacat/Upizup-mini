import bpy,math
def setup_render(w=480,h=900,samples=24):
    sc=bpy.context.scene
    sc.render.engine='CYCLES'; sc.cycles.device='CPU'; sc.cycles.samples=samples
    sc.cycles.use_denoising=False
    sc.render.resolution_x=w; sc.render.resolution_y=h; sc.render.film_transparent=False
    sc.view_settings.view_transform='Standard'
    w0=bpy.data.worlds.new('w'); sc.world=w0; w0.use_nodes=True
    w0.node_tree.nodes['Background'].inputs[0].default_value=(0.62,0.72,0.85,1); w0.node_tree.nodes['Background'].inputs[1].default_value=0.6
    def light(name,rot,e):
        l=bpy.data.objects.new(name,bpy.data.lights.new(name,'SUN')); l.data.energy=e; l.rotation_euler=rot; sc.collection.objects.link(l)
    light('key',(math.radians(50),0,math.radians(30)),3.2)
    light('rim',(math.radians(60),0,math.radians(200)),1.5)
    light('fill',(math.radians(75),0,math.radians(-70)),0.8)
    cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam
    return cam
def turntable(cam,center_z,height,out,cx=0,views=(('front',0),('side',90),('back',180),('tq',35)),lens=85):
    sc=bpy.context.scene; cam.data.lens=lens
    d=height*lens/36*1.08
    files=[]
    for name,ang in views:
        r=math.radians(ang)
        cam.location=(cx+d*math.sin(r), -d*math.cos(r), center_z)
        cam.rotation_euler=(math.pi/2,0,r)
        sc.render.filepath=f'{out}_{name}.png'; bpy.ops.render.render(write_still=True); files.append(sc.render.filepath)
    return files
def closeup(cam,out,center,size,views=(('front',0),('side',90),('back',180),('tq',35)),lens=85):
    sc=bpy.context.scene; cam.data.lens=lens; d=size*lens/36; files=[]
    for name,ang in views:
        r=math.radians(ang); cam.location=(center[0]+d*math.sin(r), center[1]-d*math.cos(r), center[2]); cam.rotation_euler=(math.pi/2,0,r)
        sc.render.filepath=f'{out}_{name}.png'; bpy.ops.render.render(write_still=True); files.append(sc.render.filepath)
    return files
