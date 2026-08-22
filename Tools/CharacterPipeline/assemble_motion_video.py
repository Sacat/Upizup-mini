import bpy
import os
import sys

args = sys.argv[sys.argv.index("--") + 1:]
frames, output = os.path.abspath(args[0]), os.path.abspath(args[1])
files = sorted(name for name in os.listdir(frames) if name.lower().endswith(".png"))
if not files:
    raise RuntimeError("No Unity motion frames were found")
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.resolution_x = 960
scene.render.resolution_y = 540
scene.render.resolution_percentage = 100
scene.render.fps = 18
scene.frame_start = 1
scene.frame_end = len(files)
bpy.ops.sequencer.image_strip_add(directory=frames + os.sep, files=[{"name": name} for name in files], frame_start=1, channel=1)
scene.render.image_settings.file_format = "FFMPEG"
scene.render.ffmpeg.format = "MPEG4"
scene.render.ffmpeg.codec = "H264"
scene.render.ffmpeg.constant_rate_factor = "MEDIUM"
scene.render.filepath = output
bpy.ops.render.render(animation=True)
print("MINI107_MOTION_VIDEO_PASS")
