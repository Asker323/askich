using System.Collections.Generic;
using UnityEngine;

namespace Askich
{
    public static class MedievalArt
    {
        static Texture2D warriors, props;
        static readonly Dictionary<int, Sprite> frames = new Dictionary<int, Sprite>();
        static readonly Dictionary<int, Sprite> objects = new Dictionary<int, Sprite>();
        public static Texture2D Warriors => warriors != null ? warriors : warriors = Resources.Load<Texture2D>("Medieval/warriors");
        // Explicit top-left pixel rectangles avoid adjacent silhouettes on the generated atlas.
        static readonly Rect[] WarriorBounds = {
            new Rect(64,11,151,211),new Rect(292,11,161,215),new Rect(512,6,157,219),new Rect(721,25,217,195),
            new Rect(68,242,131,206),new Rect(273,246,154,202),new Rect(492,248,154,200),new Rect(738,245,200,203),
            new Rect(66,457,141,211),new Rect(279,462,153,211),new Rect(520,466,138,207),new Rect(722,466,208,207),
            new Rect(66,689,135,206),new Rect(296,689,132,213),new Rect(515,691,131,212),new Rect(733,690,179,205),
            new Rect(73,918,134,210),new Rect(299,920,138,213),new Rect(526,925,135,209),new Rect(738,903,185,230),
            new Rect(37,1148,174,218),new Rect(268,1150,175,221),new Rect(510,1152,143,219),new Rect(703,1153,233,213),
            new Rect(59,1386,145,229),new Rect(277,1388,152,237),new Rect(507,1388,150,237),new Rect(722,1387,224,236)
        };
        static readonly Rect[] PropBounds = {
            new Rect(11,52,302,277),new Rect(342,14,328,307),new Rect(723,7,216,336),new Rect(1006,8,228,341),
            new Rect(7,346,329,326),new Rect(371,407,298,239),new Rect(711,365,230,303),new Rect(997,431,234,223),
            new Rect(31,719,266,225),new Rect(411,691,172,257),new Rect(674,734,274,202),new Rect(1018,696,205,241),
            new Rect(11,946,315,293),new Rect(365,1004,257,198),new Rect(673,1008,299,222),new Rect(995,959,247,276)
        };
        static Rect TextureRect(Rect r, Texture2D texture) => new Rect(r.x-1,texture.height-r.yMax-1,r.width+2,r.height+2);
        public static Sprite Warrior(int row, int frame)
        {
            int key = row * 4 + frame;
            if (frames.TryGetValue(key, out var cached) && cached != null) return cached;
            Texture2D atlas = Warriors;
            if (atlas == null) throw new System.InvalidOperationException("Missing medieval warrior atlas.");
            Rect rect = TextureRect(WarriorBounds[key],atlas);
            float center = frame==0?135:frame==1?360:frame==2?584:800;
            var sprite = Sprite.Create(atlas,rect,new Vector2((center-rect.x)/rect.width,.025f),155,0,SpriteMeshType.FullRect);
            frames[key] = sprite; return sprite;
        }
        public static Sprite Prop(int index)
        {
            if (objects.TryGetValue(index, out var cached) && cached != null) return cached;
            if (props == null) props = Resources.Load<Texture2D>("Medieval/props");
            if (props == null) throw new System.InvalidOperationException("Missing medieval prop atlas.");
            Rect rect=TextureRect(PropBounds[index],props);
            var sprite = Sprite.Create(props,rect,new Vector2(.5f,.06f),Mathf.Max(rect.width,rect.height),0,SpriteMeshType.FullRect);
            objects[index] = sprite; return sprite;
        }
        public static SpriteRenderer Make(string name, Sprite sprite, Vector2 pos, float scale, Transform parent, int order = 0)
        {
            var obj = new GameObject(name); obj.transform.SetParent(parent, false);
            obj.transform.position = pos; obj.transform.localScale = Vector3.one * scale;
            var view = obj.AddComponent<SpriteRenderer>(); view.sprite = sprite; view.sortingOrder = order; return view;
        }
        public static void Portrait(Rect area, int row, int frame = 0)
        {
            Rect rect=TextureRect(WarriorBounds[row*4+frame],Warriors);
            float scale=Mathf.Min(area.width/rect.width,area.height/rect.height);
            Rect fit=new Rect(area.center.x-rect.width*scale/2,area.center.y-rect.height*scale/2,rect.width*scale,rect.height*scale);
            GUI.DrawTextureWithTexCoords(fit,Warriors,new Rect(rect.x/Warriors.width,rect.y/Warriors.height,rect.width/Warriors.width,rect.height/Warriors.height),true);
        }
        static Texture2D houses;
        static readonly Rect[] HouseBounds={new Rect(61,179,291,247),new Rect(419,121,389,312),new Rect(842,29,380,404),new Rect(42,553,325,271),new Rect(421,491,383,341),new Rect(842,432,397,408),new Rect(53,968,301,214),new Rect(398,885,414,300),new Rect(830,835,403,371)};
        public static Sprite House(int faction,int tier)
        {
            int id=Mathf.Clamp(faction,0,2)*3+Mathf.Clamp(tier,0,2),key=100+id;
            if(objects.TryGetValue(key,out var cached))return cached;
            if(houses==null)houses=Resources.Load<Texture2D>("Medieval/houses");
            if(houses==null)throw new System.InvalidOperationException("Missing house atlas");
            Rect r=TextureRect(HouseBounds[id],houses);
            var sprite=Sprite.Create(houses,r,new Vector2(.5f,.05f),Mathf.Max(r.width,r.height),0,SpriteMeshType.FullRect);objects[key]=sprite;return sprite;
        }
        public static Sprite Ground(bool fortress)
        {
            int key = fortress ? -2 : -1;
            if (objects.TryGetValue(key, out var cached) && cached != null) return cached;
            const int w = 512, h = 256;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[w * h]; var random = new System.Random(fortress ? 17 : 8);
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                float noise = (float)random.NextDouble() * .07f;
                Color color = fortress ? new Color(.25f + noise, .245f + noise, .21f + noise) : new Color(.19f + noise, .24f + noise, .12f + noise * .5f);
                if (fortress)
                {
                    if (y % 12 == 0 || (x + ((y / 12) % 2) * 12) % 24 == 0) color *= .68f;
                }
                else if (Mathf.Abs(y - h * .42f - Mathf.Sin(x * .018f) * 8) < 22 || Mathf.Abs(x - w * .5f) < 14)
                    color = new Color(.33f + noise, .28f + noise, .18f + noise);
                pixels[y * w + x] = color;
            }
            texture.SetPixels(pixels); texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, w, h), Vector2.one * .5f, w / 27f);
            objects[key] = sprite; return sprite;
        }
    }
}
