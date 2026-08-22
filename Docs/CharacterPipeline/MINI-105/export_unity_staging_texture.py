import bpy
import os

BLEND = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\AccuRigOutput\Sacat-ModularBase-Rigged-VisualProof.blend"
OUT_DIR = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\UnityStaging\SacatModularBase\Textures"
OUT_IMAGE = os.path.join(OUT_DIR, "SacatModularBase_BaseColor_2K.png")

os.makedirs(OUT_DIR, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=BLEND)

image = bpy.data.images.get("Image_0")
if image is None:
    raise RuntimeError("Approved Sacat base-color image is missing")
if tuple(image.size) != (2048, 2048):
    image.scale(2048, 2048)

image.filepath_raw = OUT_IMAGE
image.file_format = "PNG"
image.save()
print(f"SACAT_UNITY_TEXTURE_STAGE_PASS path={OUT_IMAGE} size={tuple(image.size)}")
