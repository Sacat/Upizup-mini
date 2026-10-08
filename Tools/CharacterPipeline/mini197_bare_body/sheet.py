import sys
from PIL import Image
out=sys.argv[1]; ims=[Image.open(p).convert('RGB') for p in sys.argv[2:]]
W=sum(i.width for i in ims); o=Image.new('RGB',(W,ims[0].height),'white'); x=0
for i in ims: o.paste(i,(x,0)); x+=i.width
o.save(out)
