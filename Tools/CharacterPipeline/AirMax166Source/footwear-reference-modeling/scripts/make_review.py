from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json
P=Path(__file__).resolve().parent
font='C:/Windows/Fonts/segoeui.ttf';bold='C:/Windows/Fonts/segoeuib.ttf'
def f(n,b=False):return ImageFont.truetype(bold if b else font,n)
def sheet(name,views):
 W=1600;cellw=760;cellh=540;out=Image.new('RGB',(W,120+len(views)*cellh+70),'#101820');d=ImageDraw.Draw(out)
 d.text((40,24),'AIR MAX 90 / 97',font=f(34,True),fill='white');d.text((40,69),'Blender geometry review | thinner sole + visible Air | MINI-166',font=f(20),fill='#aebfc9')
 for row,view in enumerate(views):
  for col,model in enumerate(['90','97']):
   x=40+col*780;y=120+row*cellh;im=Image.open(P/('AM'+model)/(view+'.png')).convert('RGB');im.thumbnail((760,490));out.paste(im,(x,y+35))
   d.text((x,y),'AIR MAX '+model+' / '+view.upper(),font=f(21,True),fill='white')
 d.text((40,out.height-42),'Authoring previews. Dimensions are estimates. Not yet fitted or integrated in Unity.',font=f(19),fill='#aebfc9');out.save(P/name,quality=93)
sheet('AirMax-Review.jpg',['hero','side','top']);sheet('AirMax-Elevations-Clay.jpg',['front','back','clay']);sheet('AirMax-Heroes.jpg',['hero'])
# Small-distance check: same renders, reduced only. Not a game camera or performance test.
out=Image.new('RGB',(760,310),'#101820');d=ImageDraw.Draw(out);d.text((24,15),'SMALL IMAGE READABILITY / NOT IN-GAME',font=f(22,True),fill='white')
for i,m in enumerate(['90','97']):
 im=Image.open(P/('AM'+m)/'hero.png').convert('RGB');im.thumbnail((320,220));out.paste(im,(35+i*375,65));d.text((35+i*375,275),'AM'+m,font=f(18),fill='white')
out.save(P/'AirMax-Small-View.jpg',quality=90)
# Geometry measurements with source-relative elevations.
out=Image.new('RGB',(1600,1060),'#f2f3ef');d=ImageDraw.Draw(out);d.text((40,25),'MEASUREMENTS / MODEL AUTHORING ESTIMATES',font=f(31,True),fill='#17232b');d.text((40,75),'Millimetres. Not Nike factory dimensions. Exact profiles and camera transforms are in measurements.json.',font=f(20),fill='#394b55')
for col,m in enumerate(['90','97']):
 x=40+col*790;im=Image.open(P/('AM'+m)/'side.png').convert('RGB');im.thumbnail((740,500));out.paste(im,(x,130));j=json.loads((P/('AM'+m)/'measurements.json').read_text());yy=665
 lines=['AIR MAX '+m,'Nominal length: 290 mm','Maximum width: '+('104' if m=='90' else '98.8')+' mm','Heel sole-top elevation: %.1f mm'%j['heel_sole_top_mm'],'Forefoot sole-top elevation: %.1f mm'%j['forefoot_sole_top_mm'],'Rubber layer: 4.2 mm; toe spring: up to 10 mm','Air cavities (X centre / length / Z centre / height):']
 lines+=['  '+ ' / '.join(str(v) for v in w)+' mm' for w in j['window_specs_x_length_z_height_mm']]
 for line in lines:d.text((x,yy),line,font=f(21,yy==665),fill='#17232b');yy+=32
out.save(P/'AirMax-Measurements.jpg',quality=93)
for m in ['90','97']:
 for view in ['hero','side','top','front','back','clay']:
  im=Image.open(P/('AM'+m)/(view+'.png')).convert('RGB');im.thumbnail((1050,700));im.save(P/('AM'+m)/(view+'-inspect.jpg'),quality=85)
print('REVIEW_SHEETS_COMPLETE')
