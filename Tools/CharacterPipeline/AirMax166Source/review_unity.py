from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[3];src=ROOT/'Logs/Tasks/MINI-166/Repair/AirMaxFinalStatic';out=ROOT/'Logs/Tasks/MINI-166/AirMaxIntegration'
font=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',28)
for kind,suffix in [('Main-Characters','Full'),('Shoe-Fit','Idle-00')]:
 canvas=Image.new('RGB',(1280,875),(29,36,43));d=ImageDraw.Draw(canvas)
 for i,(who,model) in enumerate([('Franki','90'),('Sacat','97')]):
  im=Image.open(src/(who+'-AM'+model+'-'+suffix+'.png')).convert('RGB');canvas.paste(im,(640*i,65));d.text((640*i+28,18),who+' / Air Max '+model,font=font,fill='white')
 canvas.save(out/(kind+'.jpg'),quality=92)
# Check sampled run poses from each character's default design, preserving the full body.
src=ROOT/'Logs/Tasks/MINI-166/Repair/AirMaxAfter';canvas=Image.new('RGB',(1280,870),(29,36,43));d=ImageDraw.Draw(canvas)
for row,(who,model) in enumerate([('Franki','90'),('Sacat','97')]):
 d.text((20,row*435+4),who+' / sampled run poses',font=font,fill='white')
 for col,frame in enumerate([0,3,6,9]):
  im=Image.open(src/(who+'-AM'+model+'-Run-'+str(frame).zfill(2)+'.png')).convert('RGB');im.thumbnail((320,395));canvas.paste(im,(col*320,row*435+40))
canvas.save(out/'Motion-Check.jpg',quality=90)
print('AIRMAX_UNITY_REVIEW_SHEETS_READY')
