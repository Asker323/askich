using UnityEngine;
namespace Askich
{
    public sealed partial class ArenaGame
    {
        void TickTown(float dt)
        {
            if(townPaused||Town.pendingEvent!=0)return;
            if(Town.AdvanceClock(dt*townSpeed)){sound.Play("upgrade");Save();}
            townSaveTimer+=dt;if(townSaveTimer>=5){townSaveTimer=0;Save();}
        }
        Rect PlotArea(int i)=>new Rect(35+i%5*151+(i/5%2)*12,202+i/5*93,139,91);
        void DrawVillageMap(Settlement town)
        {
            var ground=MedievalArt.Ground(false);GUI.DrawTexture(new Rect(22,191,780,393),ground.texture,ScaleMode.StretchToFill);
            Panel(new Rect(22,145,780,42));Text(new Rect(34,153,755,32),movePlot>=0?"Перенос: "+Settlement.Names[town.buildings[movePlot]]+". Выбери свободный участок.":town.NextObjective,new GUIStyle(smallStyle){fontSize=17});
            for(int r=0;r<4;r++)Box(new Rect(23,280+r*93,778,9),new Color(.37f,.31f,.2f,.85f));
            for(int i=0;i<20;i++)
            {
                Rect r=PlotArea(i);int type=town.buildings[i],level=town.levels[i];
                bool hovered=r.Contains(Event.current.mousePosition);
                if(movePlot>=0&&type==0)Box(new Rect(r.x+7,r.y+43,r.width-14,36),new Color(.3f,.7f,.32f,.22f));
                if(i==selectedPlot||hovered)Box(new Rect(r.x+7,r.y+43,r.width-14,36),new Color(.72f,.64f,.32f,i==selectedPlot?.26f:.12f));
                if(type!=0)
                {
                    DrawSprite(new Rect(r.x+16,r.y-8,110,89),BuildingSprite(type,level));
                    if(type>=3&&type<=6&&level>=2)DrawSprite(new Rect(r.x+1,r.y+39,36,37),MedievalArt.Prop(8));
                    if(type>=3&&type<=6&&level>=3)DrawSprite(new Rect(r.x+101,r.y+30,35,46),MedievalArt.Prop(7));
                    Box(new Rect(r.x+3,r.y+76,133,17),new Color(.08f,.09f,.05f,.85f));
                    Text(new Rect(r.x+3,r.y+75,133,22),Settlement.Names[type]+" "+level,new GUIStyle(smallStyle){fontSize=12,alignment=TextAnchor.UpperCenter});
                }
                else
                {
                    Box(new Rect(r.x+55,r.y+52,28,25),new Color(.09f,.12f,.06f,.6f));
                    Text(new Rect(r.x+55,r.y+50,28,28),"+",new GUIStyle(textStyle){alignment=TextAnchor.MiddleCenter});
                }
                if(i==town.pendingPlot)
                {
                    for(int pole=0;pole<3;pole++)Box(new Rect(r.x+23+pole*42,r.y+13,4,62),new Color(.66f,.47f,.25f));
                    Box(new Rect(r.x+22,r.y+22,90,4),gold);Box(new Rect(r.x+22,r.y+53,90,4),gold);
                    Bar(new Rect(r.x+18,r.y+2,100,7),1-(town.pendingDays-town.dayProgress)/town.pendingLevel,gold);
                    Text(new Rect(r.x+9,r.y+9,120,22),"СТРОИТСЯ",new GUIStyle(smallStyle){fontSize=12,alignment=TextAnchor.MiddleCenter});
                }
                if(GUI.Button(r,GUIContent.none,GUIStyle.none))
                {
                    if(movePlot>=0){if(town.MoveBuilding(movePlot,i)){movePlot=-1;Save();}}
                    selectedPlot=i;
                    if(movePlot<0)settlementTab=0;
                }
            }
            // Workers travel between homes and productive buildings; no extra economy is hidden here.
            for(int i=0;i<20;i++)if(town.buildings[i]>=3&&town.buildings[i]<=6)
            {
                Rect plot=PlotArea(i);Vector2 home=new Vector2(414,plot.yMax+7),work=new Vector2(plot.center.x,plot.yMax+7);
                float t=Mathf.PingPong(Time.time*.09f+i*.19f,1);Vector2 pos=Vector2.Lerp(home,work,t);
                DrawSprite(new Rect(pos.x-9,pos.y-27,18,34),AnimatedArt.Villager(i%2,(int)(Time.time*6+i)%4));
            }
            for(int i=0;i<3;i++){float x=100+i*230+Mathf.Sin(Time.time*.4f+i)*23;DrawSprite(new Rect(x,550,23,23),AnimatedArt.Villager(2,(int)(Time.time*4+i)%4));}
            string balance=(town.FoodBalance>=0?"+":"")+town.FoodBalance;
            Text(new Rect(28,588,775,25),"За день: дерево +"+(town.Sum(4)*22-town.ForgeBatches*4)+" · камень +"+town.Sum(5)*16+" · железо +"+town.ForgeBatches*8+" · еда "+balance+" после снабжения",smallStyle);
            if(movePlot>=0&&Button(new Rect(600,656,200,38),"Отменить перенос"))movePlot=-1;
        }
        void TownEvent(Settlement town)
        {
            Box(new Rect(0,0,1280,720),new Color(0,0,0,.8f));Panel(new Rect(260,180,760,360));
            string title=town.pendingEvent==1?"КУПЕЦ У ВОРОТ":town.pendingEvent==2?"ПУТНИКИ ПРОСЯТ КРОВ":"ОГНИ У КУРГАНА";
            string description=town.pendingEvent==1?"Повозка с камнем остановилась у деревни. Купцу нужны брёвна для ремонта моста.":town.pendingEvent==2?"Две семьи пришли с разорённого пограничья. Им нужны свободные места и запас еды.":"По ночам у старого кургана видно мерцание. Дозор просит припасы, чтобы проверить слухи.";
            Text(new Rect(290,205,700,50),title,headingStyle);Text(new Rect(290,266,700,100),description,textStyle);
            Text(new Rect(290,365,700,28),"Время остановлено до твоего решения.",smallStyle);
            bool can=town.pendingEvent==1?town.wood>=25:town.pendingEvent==2?town.food>=20&&town.population+2<=town.Capacity:town.food>=15;
            string first=town.pendingEvent==1?"25 дерева → 25 камня":town.pendingEvent==2?"Принять · 20 еды / +2 жителя":"Отправить дозор · 15 еды / +8 доверия";
            string second=town.pendingEvent==1?"Дать ночлег · +3 довольства":town.pendingEvent==2?"Указать путь · +10 еды / −3 довольства":"Продолжить рубку · +20 дерева / −4 довольства";
            if(Button(new Rect(290,412,700,44),first,can)){town.ChooseEvent(0);Save();}
            if(Button(new Rect(290,470,700,44),second)){town.ChooseEvent(1);Save();}
        }
    }
}
