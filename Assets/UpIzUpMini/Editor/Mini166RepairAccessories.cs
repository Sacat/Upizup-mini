using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
namespace UpIzUpMini.EditorTools {
public static partial class Mini166Repair {
 static Shape Cap(Surface s){
  var result=new Shape(s);int head=s.Bone("Head");var used=s.triangles.SelectMany(t=>t).Distinct().Select(i=>s.vertices[i]).ToArray();float top=used.Max(v=>v.p.y)+.006f;var scalp=used.Where(v=>v.p.y>top-.09f).ToArray();
  float rx=Mathf.Max(.083f,scalp.Max(v=>Mathf.Abs(v.p.x))+.006f);float front=scalp.Max(v=>v.p.z),back=scalp.Min(v=>v.p.z);float rz=(front-back)/2+.01f,cz=(front+back)/2;
  float bottom=top-.105f;
  Vector3 Crown(float a,float t)=>new Vector3(rx*Mathf.Cos(t)*Mathf.Sin(a),bottom+.106f*Mathf.Sin(t),cz+rz*Mathf.Cos(t)*Mathf.Cos(a));
  for(int ring=0;ring<8;ring++)for(int i=0;i<36;i++){float a=i*Mathf.PI/18,b=(i+1)*Mathf.PI/18,t=ring*Mathf.PI/16,u=(ring+1)*Mathf.PI/16;result.Quad(Crown(a,t),Crown(a,u),Crown(b,u),Crown(b,t),0,head,true);}
  for(int seam=0;seam<6;seam++){var points=Enumerable.Range(0,10).Select(i=>Crown(seam*Mathf.PI/3,i*Mathf.PI/20)+Vector3.up*.0008f).ToList();result.Tube(points,.0007f,1,head,4);}
  // MINI-166: "fix the cap brim size" - the bill projected .108f forward
  // and drooped .022f down per unit t, which put its outer edge at eye/
  // nose height instead of above the brow. Shortened the forward reach
  // and flattened the droop; width (x*rx) and attachment height (bottom)
  // are unchanged.
  Vector3 Brim(float x,float t,float thick){float inner=cz+rz*Mathf.Sqrt(Mathf.Max(0,1-x*x*.8f));return new Vector3(x*rx*1.04f,bottom-.006f-t*.008f+x*x*.006f+thick,inner+t*.058f*(1-x*x*.32f));}
  for(int i=0;i<18;i++)for(int j=0;j<5;j++){float a=-1+2*i/18f,b=-1+2*(i+1)/18f,t=j/5f,u=(j+1)/5f;result.Quad(Brim(a,t,0),Brim(b,t,0),Brim(b,u,0),Brim(a,u,0),0,head,true);result.Quad(Brim(a,t,-.003f),Brim(a,u,-.003f),Brim(b,u,-.003f),Brim(b,t,-.003f),0,head,true);}
  Word(result,"LACOS",new Vector3(-.022f,bottom+.026f,cz+rz*.96f+.003f),.007f,2,head);
  return result;
 }
 static Shape Shoes(Surface s,bool wave){
  var result=new Shape(s,5);
  foreach(int sign in new[]{-1,1}){
   string side=sign<0?"Left":"Right";Vector3 foot=s.Rest(side+"Foot"),toe=s.Rest(side+"ToeBase");int bone=s.Bone(side+"Foot");
   float cx=(foot.x+toe.x)/2,cz=(foot.z+toe.z)/2+.008f;float length=Mathf.Max(.14f,(toe.z-foot.z)*.5f+.075f);float width=wave?.064f:.069f;float ground=.003f;
   Vector3 Ring(float a,float y,float scale){float z=Mathf.Cos(a);float w=width*(z>0?1:.79f);return new Vector3(cx+Mathf.Sin(a)*w*scale,ground+y,cz+z*length*scale);}
   float[] ys=wave?new[]{0f,.012f,.023f,.046f,.061f}:new[]{0f,.013f,.022f,.048f,.061f};float[] scales={.95f,1.025f,1.035f,1.02f,.96f};
   for(int row=0;row<4;row++)for(int i=0;i<28;i++){float a=i*Mathf.PI/14,b=(i+1)*Mathf.PI/14;int slot=row==0?2:1;result.Quad(Ring(a,ys[row],scales[row]),Ring(b,ys[row],scales[row]),Ring(b,ys[row+1],scales[row+1]),Ring(a,ys[row+1],scales[row+1]),slot,bone,true);}
   Vector3 Upper(float a,float t){float z=Mathf.Cos(a),baseHeight=.061f;float height=Mathf.Lerp(.06f,.103f,(1-z)*.5f);float taper=Mathf.Cos(t*Mathf.PI/2);Vector3 p=Ring(a,baseHeight+height*Mathf.Sin(t*Mathf.PI/2),.96f*taper);p.z+=.006f*t;return p;}
   for(int row=0;row<7;row++)for(int i=0;i<28;i++){float a=i*Mathf.PI/14,b=(i+1)*Mathf.PI/14;float t=row/7f,u=(row+1)/7f;result.Quad(Upper(a,t),Upper(b,t),Upper(b,u),Upper(a,u),0,bone,true);}
   // Air window: full-length on 97, heel-only on 90, inset beneath sole trim.
   for(int sideSign=-1;sideSign<=1;sideSign+=2){
    float z0=cz-length*.7f,z1=cz+length*(wave?.68f:-.15f),x=cx+sideSign*width*.97f;
    result.Quad(new Vector3(x,ground+.023f,z0),new Vector3(x,ground+.023f,z1),new Vector3(x,ground+.043f,z1),new Vector3(x,ground+.043f,z0),2,bone,true);
    for(int i=0;i<(wave?7:3);i++){float z=Mathf.Lerp(z0,z1,(i+.5f)/(wave?7:3));result.Tube(new List<Vector3>{new Vector3(x+sideSign*.001f,ground+.025f,z),new Vector3(x+sideSign*.001f,ground+.041f,z)},.002f,wave?4:3,bone,4);}
   }
   if(wave){for(int band=0;band<4;band++){float t=.07f+band*.115f;var points=Enumerable.Range(0,41).Select(i=>Upper(i*Mathf.PI/20,t)+Vector3.up*.0015f).ToList();result.Tube(points,.0019f,4,bone,5);}}
   else{
    for(int sideSign=-1;sideSign<=1;sideSign+=2){float x=cx+sideSign*width*.86f;result.Quad(new Vector3(x,ground+.064f,cz-length*.65f),new Vector3(x,ground+.079f,cz+length*.40f),new Vector3(x*.0f+cx+sideSign*width*.56f,ground+.126f,cz+length*.18f),new Vector3(cx+sideSign*width*.6f,ground+.126f,cz-length*.54f),2,bone,true);result.Quad(new Vector3(x,ground+.08f,cz+.01f),new Vector3(x,ground+.082f,cz+.044f),new Vector3(cx+sideSign*width*.52f,ground+.13f,cz+.039f),new Vector3(cx+sideSign*width*.52f,ground+.13f,cz+.005f),3,bone,true);}
   }
   for(int i=0;i<5;i++){float z=cz+.01f+i*.014f,q=(z-cz)/length;float y=ground+.061f+Mathf.Sqrt(Mathf.Max(0,1-q*q-.10f))*Mathf.Lerp(.06f,.103f,(1-q)/2)+.003f;result.Tube(new List<Vector3>{new Vector3(cx-.021f,y,z),new Vector3(cx+.021f,y,z+.007f)},.002f,4,bone,5);}
   // Heel pull tab follows the heel, not the ankle skin.
   result.Tube(new List<Vector3>{new Vector3(cx-.009f,ground+.118f,cz-length*.85f),new Vector3(cx-.009f,ground+.162f,cz-length*.69f),new Vector3(cx+.009f,ground+.162f,cz-length*.69f),new Vector3(cx+.009f,ground+.118f,cz-length*.85f)},.003f,2,bone,5);
  }
  return result;
 }
 static void Word(Shape shape,string text,Vector3 origin,float height,int slot,int bone=-1){
  var glyphs=new Dictionary<char,string>{['M']="101111111101101",['I']="111010010010111",['K']="101101110101101",['E']="111100110100111",['L']="100100100100111",['A']="010101111101101",['C']="111100100100111",['O']="111101101101111",['S']="111100111001111"};float pixel=height/5;
  // MINI-166: "your wardrobe and accessories building are poor" - inspected
  // the actual rendered evidence and found the chest/cap logo text was
  // rendering as a horizontal mirror ("MIKE" -> "3XIM"). This quad strip
  // faces the viewer front-on, so the original position/winding math
  // (unchanged below) reads backwards on screen. Rather than touch that
  // position/quad-winding code - which risks flipping face normals and
  // culling the text entirely - feed it a pre-mirrored *source* picture:
  // characters in reverse order and each glyph's bits reversed column-
  // wise (col -> 2-col). Mirroring the source picture is exactly undone
  // by the view's own mirror, restoring correct on-screen reading order.
  // Verified by an actual re-render (not just compile), see
  // Docs/WorkPackets/MINI-166.md.
  for(int c=0;c<text.Length;c++){char ch=text[text.Length-1-c];if(!glyphs.TryGetValue(ch,out string glyph))continue;for(int row=0;row<5;row++)for(int col=0;col<3;col++){if(glyph[row*3+(2-col)]!='1')continue;var p=origin+new Vector3((c*4+col)*pixel,-row*pixel,0);shape.Quad(p,p+Vector3.down*pixel*.85f,p+new Vector3(pixel*.85f,-pixel*.85f,0),p+Vector3.right*pixel*.85f,slot,bone,true);}}
 }
}
}
