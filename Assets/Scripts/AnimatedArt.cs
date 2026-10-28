using System.Collections.Generic;
using UnityEngine;
namespace Askich
{
    // The generated silhouettes do not fit a uniform grid. Extract their connected
    // alpha islands once, so a neighbouring spear cannot leak into an attack frame.
    public static class AnimatedArt
    {
        static readonly Dictionary<int,Sprite> cache=new Dictionary<int,Sprite>();
        public static void Prewarm(){for(int s=0;s<6;s++)for(int f=0;f<4;f++)Rus(s,f);for(int k=0;k<3;k++)for(int f=0;f<4;f++)Villager(k,f);}
        static readonly RectInt[] warrior={
            new RectInt(40,24,200,223),new RectInt(293,23,199,224),new RectInt(549,23,196,224),new RectInt(802,22,199,225),
            new RectInt(29,276,225,227),new RectInt(281,276,223,227),new RectInt(535,276,221,228),new RectInt(788,276,218,227),
            new RectInt(20,540,198,208),new RectInt(259,543,295,205),new RectInt(527,554,299,194),new RectInt(793,541,219,207),
            new RectInt(33,778,213,229),new RectInt(295,779,175,228),new RectInt(553,803,162,204),new RectInt(790,781,216,226),
            new RectInt(28,1039,209,221),new RectInt(305,1057,175,203),new RectInt(561,1062,148,199),new RectInt(803,1044,183,217),
            new RectInt(16,1308,263,189),new RectInt(277,1343,191,145),new RectInt(504,1383,218,103),new RectInt(735,1400,273,94)};
        static readonly RectInt[] life={
            new RectInt(119,51,181,365),new RectInt(461,53,179,367),new RectInt(810,53,185,363),new RectInt(1158,50,186,370),
            new RectInt(114,451,181,362),new RectInt(470,451,180,358),new RectInt(811,451,190,362),new RectInt(1152,452,183,365),
            new RectInt(131,863,176,174),new RectInt(478,863,167,174),new RectInt(826,863,167,171),new RectInt(1164,863,182,174)};
        public static Sprite Rus(int state,int frame)
        {
            int id=Mathf.Clamp(state,0,5)*4+Mathf.Clamp(frame,0,3);
            float center=(id%4)*256+130;
            if(id==9)center=385;if(id==10)center=645;
            if(id>=22)center=warrior[id].center.x;
            return Get(id,"rus-animation",warrior[id],160,center);
        }
        public static Sprite Villager(int kind,int frame)
        {int id=Mathf.Clamp(kind,0,2)*4+Mathf.Clamp(frame,0,3);return Get(100+id,"village-life",life[id],300,life[id].center.x);}
        static Sprite Get(int key,string asset,RectInt b,float pixelsPerUnit,float center)
        {
            if(cache.TryGetValue(key,out var sprite))return sprite;
            var atlas=Resources.Load<Texture2D>("Medieval/"+asset);
            if(atlas==null)throw new System.InvalidOperationException("Missing animation atlas: "+asset);
            int pad=2,w=b.width+pad*2,h=b.height+pad*2;
            Color[] source=atlas.GetPixels(b.x-pad,atlas.height-b.yMax-pad,w,h);
            var visited=new bool[w*h];var best=new List<int>();var queue=new Queue<int>();
            for(int start=0;start<source.Length;start++)
            {
                if(visited[start]||source[start].a<.08f)continue;
                var island=new List<int>();queue.Enqueue(start);visited[start]=true;
                while(queue.Count>0)
                {
                    int i=queue.Dequeue();island.Add(i);int x=i%w,y=i/w;
                    void Add(int n){if(!visited[n]&&source[n].a>=.08f){visited[n]=true;queue.Enqueue(n);}}
                    if(x>0)Add(i-1);if(x<w-1)Add(i+1);if(y>0)Add(i-w);if(y<h-1)Add(i+w);
                }
                if(island.Count>best.Count)best=island;
            }
            var pixels=new Color[w*h];foreach(int i in best)pixels[i]=source[i];
            var texture=new Texture2D(w,h,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,name=asset+"_"+key};
            texture.SetPixels(pixels);texture.Apply(false,true);
            sprite=Sprite.Create(texture,new Rect(0,0,w,h),new Vector2((center-b.x+pad)/w,(pad+3f)/h),pixelsPerUnit,0,SpriteMeshType.FullRect);
            cache[key]=sprite;return sprite;
        }
    }
}
