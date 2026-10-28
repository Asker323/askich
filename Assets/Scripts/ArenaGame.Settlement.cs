using UnityEngine;
namespace Askich
{
    public sealed partial class ArenaGame
    {
        bool townPaused;int townSpeed=1,movePlot=-1;float townSaveTimer;
        int selectedPlot=7,buildType=2,settlementTab,raidTarget,raidTactic;
        float raidElapsed;
        bool raidFinished;
        Settlement Town => profile.settlements[faction];
        static readonly string[] GovernmentNames={"Совет старейшин","Единоличная власть","Вече"};
        static readonly string[] Tactics={"Штурм лагеря","Обход укреплений","Перерезать снабжение"};
        void OpenTown() {profile.Validate();selectedPlot=7;settlementTab=0;movePlot=-1;mode=Mode.Settlement;}
        void DrawSprite(Rect area,Sprite sprite)
        {
            Rect r=sprite.rect;float scale=Mathf.Min(area.width/r.width,area.height/r.height);
            Rect fit=new Rect(area.center.x-r.width*scale/2,area.yMax-r.height*scale,r.width*scale,r.height*scale);
            GUI.DrawTextureWithTexCoords(fit,sprite.texture,new Rect(r.x/sprite.texture.width,r.y/sprite.texture.height,r.width/sprite.texture.width,r.height/sprite.texture.height),true);
        }
        Sprite BuildingSprite(int type,int level)
        {
            if(type==1||type==2)return MedievalArt.House(faction,level-1);
            int index=type==3?6:type==4?7:type==5?10:type==6?9:type==7?(level==1?12:level==2?13:1):type==8?(level==1?13:level==2?14:2):type==9?5:15;
            return MedievalArt.Prop(index);
        }
        void TownScreen()
        {
            var town=Town;
            bool priorEnabled=GUI.enabled;GUI.enabled=priorEnabled && town.pendingEvent==0;
            Box(new Rect(0,0,1280,720),new Color(.09f,.1f,.065f));
            Text(new Rect(27,17,780,45),"ЗЕМЛИ · "+CampaignData.Countries[faction].ToUpper(),headingStyle);
            for(int f=0;f<3;f++)if(Button(new Rect(855+f*130,13,120,36),CampaignData.Countries[f])){faction=f;profile.selectedFaction=f;Save();}
            Panel(new Rect(22,67,1236,69));
            Text(new Rect(40,77,1200,28),"День "+town.day+"     Дерево "+town.wood+"     Камень "+town.stone+"     Руда "+town.ore+"     Железо "+town.iron+"     Еда "+town.food+"     Жители "+town.population+" / "+town.Capacity,new GUIStyle(textStyle){fontSize=18});
            Text(new Rect(40,108,1200,25),"Отряд: "+town.soldiers+" / "+town.ArmyCapacity+"     Довольство: "+town.morale+"     Легитимность: "+town.legitimacy+"     "+GovernmentNames[town.government]+" · "+(town.constitution==0?"единое управление":"конфедерация"),smallStyle);
            DrawVillageMap(town);
            Text(new Rect(27,610,770,88),settlementTab==1?"Игровая модель власти: единоличная власть (диктатура) требует казарм II, даёт +20% силы отряду, но -4 довольства в день. Вече ускоряет рост и повышает довольство. Средневековое вече не тождественно современной демократии.":town.chronicle,smallStyle);
            Panel(new Rect(825,151,433,555));
            string[] tabs={"Стройка","Хартия","Набеги"};for(int i=0;i<3;i++)if(Button(new Rect(839+i*137,165,128,36),tabs[i]))settlementTab=i;
            if(settlementTab==0)TownBuild(town);
            if(settlementTab==1)TownGovernment(town);
            if(settlementTab==2)TownRaids(town);
            if(Button(new Rect(845,598,120,45),townPaused?"Продолжить":"Пауза"))townPaused=!townPaused;
            if(Button(new Rect(975,598,120,45),"Скорость ×"+townSpeed))townSpeed=townSpeed==1?3:1;
            if(Button(new Rect(1105,598,132,45),"Закончить день",town.pendingEvent==0)){town.AdvanceClock(30);Save();}
            Bar(new Rect(845,646,392,4),town.dayProgress,gold);
            if(Button(new Rect(845,662,392,35),"Главное меню · Esc")){Save();mode=Mode.Menu;}
            GUI.enabled=priorEnabled; if(town.pendingEvent!=0)TownEvent(town);
        }
        void TownBuild(Settlement town)
        {
            int type=town.buildings[selectedPlot];
            Text(new Rect(845,214,390,45),"Участок "+(selectedPlot+1)+" · "+Settlement.Names[type],headingStyle);
            if(type==0)
            {
                for(int n=2;n<=10;n++)
                {
                    int j=n-2;
                    if(Button(new Rect(845+j%2*198,272+j/2*40,190,35),(buildType==n?"• ":"")+Settlement.Names[n]))buildType=n;
                }
                type=buildType;
            }
            else
            {
                DrawSprite(new Rect(915,265,240,122),BuildingSprite(type,town.levels[selectedPlot]));
                Text(new Rect(845,395,390,44),Settlement.Effects[type],smallStyle);
                if(type!=1&&Button(new Rect(845,443,392,30),movePlot<0?"Перенести · выбери свободный участок":"Отменить перенос",town.pendingPlot<0))movePlot=movePlot<0?selectedPlot:-1;
            }
            int next=town.levels[selectedPlot]+1;string reason=town.BuildReason(selectedPlot,type);
            Vector3Int cost=Settlement.Cost(type,Mathf.Min(next,3));
            Text(new Rect(845,477,390,48),next>3?"Постройка полностью улучшена.":"Цена: "+cost.x+" дерева · "+cost.y+" камня · "+cost.z+" железа\nСрок: "+next+" дн. · ступень "+next,smallStyle);
            if(Button(new Rect(845,534,392,44),reason==""?(next==1?"Построить":"Улучшить"):reason,reason=="")){town.Build(selectedPlot,type);Save();}
        }
        void TownGovernment(Settlement town)
        {
            Text(new Rect(845,218,390,45),"Устройство и власть",headingStyle);
            Text(new Rect(845,260,390,59),"Реформа: 40 еды и 25 легитимности.\nДвор II + дом собраний; поддержка ≥ 50.",smallStyle);
            for(int i=0;i<2;i++)
            {
                string reason=town.ReformReason(true,i);
                if(Button(new Rect(845+i*198,327,190,40),i==0?"Унитарное":"Конфедерация",reason=="")){town.Reform(true,i);Save();}
            }
            Text(new Rect(845,375,390,50),"Единое: +8 защиты. Союз: +5 еды за пашню.\nСоюз: двор III, торговля II и три дома.",smallStyle);
            for(int i=0;i<3;i++)
            {
                string reason=town.ReformReason(false,i);
                if(Button(new Rect(845,437+i*43,392,38),GovernmentNames[i]+(town.government==i?" · принято":""),reason=="")){town.Reform(false,i);Save();}
            }
        }
        void TownRaids(Settlement town)
        {
            Text(new Rect(845,215,390,38),"Соседи под управлением ИИ",headingStyle);
            Text(new Rect(845,260,390,45),"Новый воин: 10 еды и 2 железа.\nНужны казармы и место в отряде.",smallStyle);
            if(Button(new Rect(845,312,190,38),"Нанять воина",town.Sum(7)>0&&town.soldiers<town.ArmyCapacity&&town.food>=10&&town.iron>=2)){town.Recruit();Save();}
            if(Button(new Rect(1045,312,190,38),"Обмен 30Д → 10Ж",town.Sum(9)>0&&town.wood>=30)){town.Trade(1);Save();}
            for(int i=0;i<3;i++)if(Button(new Rect(845,365+i*39,392,34),(raidTarget==i?"• ":"")+Settlement.Rivals[i]+" · "+Settlement.Strength[i]))raidTarget=i;
            if(Button(new Rect(845,490,392,35),"Тактика: "+Tactics[raidTactic]))raidTactic=(raidTactic+1)%3;
            string reason=town.RaidReason(raidTarget);
            Text(new Rect(845,530,390,22),"Сила: "+town.Power(raidTarget,raidTactic)+" / "+Settlement.Strength[raidTarget]+" · Провизия: "+(15+raidTarget*10),smallStyle);
            if(Button(new Rect(845,555,392,35),reason==""?"Начать набег":reason,reason=="")){raidElapsed=0;raidFinished=false;mode=Mode.Raid;}
        }
        void TickRaid(float dt)
        {
            if(raidFinished)return;raidElapsed+=Mathf.Min(dt,.1f);
            if(raidElapsed>=6){Town.ResolveRaid(raidTarget,raidTactic);raidFinished=true;Save();}
        }
        void RaidScreen()
        {
            var town=Town;Panel(new Rect(80,55,1120,610));
            Text(new Rect(110,75,1060,60),"НАБЕГ · "+Settlement.Rivals[raidTarget].ToUpper(),new GUIStyle(titleStyle){fontSize=36});
            Text(new Rect(170,145,940,50),"Противник: ИИ · "+Tactics[raidTactic],textStyle);
            DrawSprite(new Rect(800,220,240,240),MedievalArt.Prop(raidTarget==0?12:raidTarget==1?1:2));
            float travel=Mathf.Clamp01(raidElapsed/3)*460;
            for(int i=0;i<Mathf.Min(6,Mathf.Max(3,town.soldiers));i++)MedievalArt.Portrait(new Rect(140+travel+i%3*45,280+i/3*75,100,100),faction,raidElapsed>3?3:1+(int)(raidElapsed*5)%2);
            for(int i=0;i<3;i++)MedievalArt.Portrait(new Rect(820+i*55,345,95,95),raidTarget==2?5:3,raidElapsed>3?3:0);
            Bar(new Rect(180,478,920,15),raidElapsed/6,gold);
            Text(new Rect(180,520,920,66),raidFinished?town.chronicle:"Отряд выполняет выбранную тактику. Бой рассчитывается по силе, подготовке и укреплениям противника.",textStyle);
            if(raidFinished&&Button(new Rect(440,595,400,45),"Вернуться в поселение"))mode=Mode.Settlement;
        }
    }
}
