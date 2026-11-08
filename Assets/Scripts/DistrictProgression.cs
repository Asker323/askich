using System;
using System.IO;
namespace Askich {
 public enum DistrictSkill {Strength,Agility,Endurance,Mechanics,Craft}
 [Serializable] public sealed class DistrictProgressionData {
  public float[] xp=new float[5];
  public float[] books=new float[2];
  public int day=1,finds,lessonDay,lessonCompletedDay;
  public float minute=990,lessonSeconds;
  public bool engineDismantled;
 }
 public sealed class DistrictProgression {
  public const float ChapterSeconds=360,BookSeconds=ChapterSeconds*3;
  public DistrictProgressionData Data {get;private set;}=new DistrictProgressionData();
  static readonly float[] thresholds={0,100,300,650,1100,1800};
  public int Level(DistrictSkill skill){int n=0;while(n<5&&Data.xp[(int)skill]>=thresholds[n+1])n++;return n;}
  public float NextThreshold(DistrictSkill skill)=>thresholds[Math.Min(5,Level(skill)+1)];
  public float Fraction(DistrictSkill skill){int n=Level(skill);return n==5?1:(Data.xp[(int)skill]-thresholds[n])/(thresholds[n+1]-thresholds[n]);}
  public void Gain(DistrictSkill skill,float amount){if(amount>0&&!float.IsNaN(amount)&&!float.IsInfinity(amount))Data.xp[(int)skill]=Math.Min(2500,Data.xp[(int)skill]+amount);}
  public void Load(DistrictProgressionData data){Validate(data);Data=data;}
  public void Advance(float seconds){Data.minute+=Math.Max(0,seconds)*.8f;while(Data.minute>=1440){Data.minute-=1440;Data.day++;}}
  public int Read(int book,float seconds){
   float old=Data.books[book];int before=(int)Math.Floor(old/ChapterSeconds);
   float next=Math.Min(BookSeconds,old+Math.Max(0,seconds));float boundary=(float)Math.Round(next/ChapterSeconds)*ChapterSeconds;if(Math.Abs(next-boundary)<.001f)next=boundary;
   Data.books[book]=next;int after=(int)Math.Floor(next/ChapterSeconds);
   if(after>before)Gain(book==0?DistrictSkill.Mechanics:DistrictSkill.Craft,(after-before)*100);
   return after-before;
  }
  static bool Range(float v,float low,float high)=>!float.IsNaN(v)&&!float.IsInfinity(v)&&v>=low&&v<=high;
  public static void Validate(DistrictProgressionData data){
   if(data==null||data.xp==null||data.xp.Length!=5||data.books==null||data.books.Length!=2||data.day<1||data.day>999999||!Range(data.minute,0,1439.9999f)||data.finds<0||data.finds>31||data.lessonDay<0||data.lessonDay>data.day||data.lessonCompletedDay<0||data.lessonCompletedDay>data.day||!Range(data.lessonSeconds,0,90))throw new InvalidDataException("Неверные время и развитие персонажа");
   foreach(float xp in data.xp)if(!Range(xp,0,2500))throw new InvalidDataException("Неверный опыт навыка");
   foreach(float read in data.books)if(!Range(read,0,BookSeconds))throw new InvalidDataException("Неверный прогресс книги");
  }
 }
}
