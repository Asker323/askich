using UnityEngine;
namespace Askich
{
    public static class WorkshopArt
    {
        static readonly Rect[] bounds={new Rect(26,47,416,367),new Rect(464,85,419,328),new Rect(902,68,419,352),new Rect(1340,49,414,370),new Rect(27,471,444,353),new Rect(534,431,323,417),new Rect(882,515,419,288),new Rect(1318,479,437,362)};
        static readonly Sprite[] sprites=new Sprite[8];static Texture2D atlas;
        public static Sprite Building(int type)
        {
            int i=Mathf.Clamp(type-3,0,7);if(sprites[i]!=null)return sprites[i];
            if(atlas==null)atlas=Resources.Load<Texture2D>("Medieval/workshops");
            if(atlas==null)throw new System.InvalidOperationException("Missing workshops atlas");
            Rect b=bounds[i];Rect r=new Rect(b.x-2,atlas.height-b.yMax-2,b.width+4,b.height+4);
            return sprites[i]=Sprite.Create(atlas,r,new Vector2(.5f,.04f),Mathf.Max(r.width,r.height),0,SpriteMeshType.FullRect);
        }
    }
}
