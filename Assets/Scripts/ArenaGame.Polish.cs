using System.Collections.Generic;
using UnityEngine;
namespace Askich
{
    public sealed partial class ArenaGame
    {
        bool pendingStrike,strikeHeavy;
        float strikeDelay,attackDuration,impactFlash,blockFlash,nextFootstep;
        Vector2 strikeDirection,playerVelocity;
        readonly List<SpriteRenderer> corpses=new List<SpriteRenderer>();
        Mode settingsReturn;
        float deathElapsed;
        void TickDeath(float dt)
        {
            deathElapsed+=dt;
            if(faction==0)player.sprite=AnimatedArt.Rus(5,Mathf.Min(3,(int)(deathElapsed*5)));
            else player.transform.localRotation=Quaternion.Euler(0,0,-Mathf.Min(80,deathElapsed*100));
            if(deathElapsed>=1)mode=Mode.Defeat;
        }
        void TickStrike(float dt)
        {
            if(!pendingStrike)return;
            strikeDelay-=dt;
            if(strikeDelay<=0){pendingStrike=false;ResolveStrike(strikeHeavy);}
        }
        void UpdatePlayerPose(bool moving)
        {
            if(faction!=0)return;
            int state=impactFlash>0?4:blocking||blockFlash>0?3:attackPose>0?2:moving?1:0;
            int frame=state==2?Mathf.Min(3,(int)((1-attackPose/Mathf.Max(.01f,attackDuration))*4)):state==4?Mathf.Min(3,(int)((1-impactFlash/.22f)*4)):state==3?(blockFlash>0?2:1):(int)(elapsed*(moving?8:3))%4;
            player.sprite=AnimatedArt.Rus(state,frame);
        }
        void Learn(int flag)
        {if((profile.tutorialFlags&flag)==0){profile.tutorialFlags|=flag;Save();}}
        void DrawCombatLesson()
        {
            if(wave!=1||(profile.tutorialFlags&15)==15)return;
            string hint=(profile.tutorialFlags&1)==0?"Двигайся: WASD / левый джойстик":(profile.tutorialFlags&2)==0?"Удар: ЛКМ / кнопка оружия. Попадание — после замаха.":(profile.tutorialFlags&4)==0?"Прими удар щитом: повернись к врагу и удерживай ПКМ / ЩИТ":"Уклонение: Пробел / ПЕРЕКАТ. Оставляй 25 выносливости.";
            Panel(new Rect(300,590,680,48));Text(new Rect(312,600,656,36),hint,smallStyle);
        }
        void Impact(Vector2 position,Color color)
        {
            int count=profile.lowEffects?3:7;
            for(int i=0;i<count;i++)
            {
                var v=NeonVisual.Make("Impact spark","diamond",color,position,new Vector2(.09f,.035f),210,effects);
                var spark=v.gameObject.AddComponent<ImpactSpark>();float a=i*Mathf.PI*2/count;
                spark.velocity=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(1.5f+i*.15f);
            }
        }
        void VillageHouse(Vector2 position,float size,float solid)
        {
            MedievalArt.Make("Village home",MedievalArt.House(faction,0),position,size,world,Sort(position));
            obstacles.Add(new Obstacle{pos=position+Vector2.up*.35f,radius=solid});
        }
        void PopulateVillage()
        {
            for(int i=0;i<6;i++)
            {
                Vector2 origin=i<3?new Vector2(-7+i*7,3.2f):new Vector2(-6+(i-3)*6,-4.9f);
                int kind=i<4?i%2:2;
                var v=MedievalArt.Make("Village resident",AnimatedArt.Villager(kind,0),origin,1,world,Sort(origin));
                var resident=v.gameObject.AddComponent<VillageResident>();resident.origin=origin;resident.kind=kind;resident.phase=i*1.7f;resident.game=this;
            }
        }
        public bool ThreatNear(Vector2 position)
        {if(mode!=Mode.Playing)return false;foreach(var e in enemies)if((e.pos-position).sqrMagnitude<12)return true;return false;}
        void ApplySettings()
        {
            Application.targetFrameRate=profile.lowEffects?60:120;QualitySettings.vSyncCount=0;
            QualitySettings.antiAliasing=profile.lowEffects?0:2;
            sound.SetVolumes(profile.effectsVolume,profile.ambienceVolume);
        }
        void SettingsScreen()
        {
            Panel(new Rect(260,110,760,510));Text(new Rect(300,140,680,60),"НАСТРОЙКИ",titleStyle);
            Text(new Rect(310,230,640,32),"Звуки боя и шаги: "+Mathf.RoundToInt(profile.effectsVolume*100)+"%",textStyle);
            float fx=GUI.HorizontalSlider(new Rect(310,275,660,35),profile.effectsVolume,0,1);
            Text(new Rect(310,320,640,32),"Ветер: "+Mathf.RoundToInt(profile.ambienceVolume*100)+"%",textStyle);
            float ambience=GUI.HorizontalSlider(new Rect(310,365,660,35),profile.ambienceVolume,0,1);
            if(fx!=profile.effectsVolume||ambience!=profile.ambienceVolume){profile.effectsVolume=fx;profile.ambienceVolume=ambience;ApplySettings();}
            if(Button(new Rect(310,415,660,45),profile.lowEffects?"Графика: экономная · 60 кадров/с":"Графика: полная · до 120 кадров/с")){profile.lowEffects=!profile.lowEffects;ApplySettings();Save();}
            if(Button(new Rect(310,480,320,42),"Повторить подсказки")){profile.tutorialFlags=0;Save();}
            if(Button(new Rect(650,480,320,42),sound.Muted?"Включить звук":"Выключить звук"))sound.Toggle();
            if(Button(new Rect(440,550,400,45),"Сохранить и вернуться")){Save();mode=settingsReturn;}
        }
        void OnApplicationPause(bool paused){if(paused)Save();}
        void OnApplicationQuit(){Save();}
    }
    public sealed class ImpactSpark:MonoBehaviour
    {
        public Vector2 velocity;float age;
        void Update(){age+=Time.deltaTime;transform.position+=(Vector3)(velocity*Time.deltaTime);velocity+=Vector2.down*Time.deltaTime*4;var s=GetComponent<SpriteRenderer>();Color c=s.color;c.a=Mathf.Clamp01(1-age/.24f);s.color=c;if(age>=.24f)Destroy(gameObject);}
    }
    public sealed class FallenWarrior:MonoBehaviour
    {
        float age;Vector3 size;SpriteRenderer view;
        void Start(){view=GetComponent<SpriteRenderer>();size=transform.localScale;var motion=GetComponent<WarriorMotion>();if(motion!=null)motion.enabled=false;}
        void Update(){age+=Time.deltaTime;float t=Mathf.SmoothStep(0,1,age/.32f);transform.localRotation=Quaternion.Euler(0,0,t*(view.flipX?70:-70));transform.localScale=new Vector3(size.x,size.y*(1-t*.45f),1);view.color=new Color(.6f,.55f,.5f,Mathf.Clamp01(5-age));if(age>=5)Destroy(gameObject);}
    }
    public sealed class VillageResident:MonoBehaviour
    {
        public Vector2 origin;public int kind;public float phase;public ArenaGame game;
        void Update()
        {
            float t=Time.time*.35f+phase;bool fleeing=game!=null&&game.ThreatNear(origin);
            Vector2 goal=fleeing?origin+Vector2.up*(origin.y>0?1:-.6f):origin+new Vector2(Mathf.Sin(t)*1.1f,Mathf.Cos(t*.7f)*.2f);
            Vector2 before=transform.position,p=Vector2.MoveTowards(before,goal,Time.deltaTime*(fleeing?2:.7f));transform.position=p;
            var v=GetComponent<SpriteRenderer>();v.sprite=AnimatedArt.Villager(kind,(int)(Time.time*(fleeing?9:5)+phase)%4);
            if(Mathf.Abs(p.x-before.x)>.001f)v.flipX=p.x<before.x;v.sortingOrder=100-Mathf.RoundToInt(p.y*10);
        }
    }
}
