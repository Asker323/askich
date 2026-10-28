using System;
using UnityEngine;
namespace Askich
{
    [Serializable]
    public sealed class Settlement
    {
        public int wood=160,stone=100,iron=30,food=100,population=12,soldiers=4,day=1,morale=70,legitimacy=60;
        public int government,constitution,raidWins,ore;
        public int ForgeBatches => Mathf.Min(Sum(6),Mathf.Min((ore+Sum(5)*6)/4,(wood+Sum(4)*22)/4));
        public int[] buildings=new int[20],levels=new int[20],raidReady=new int[3];
        public int pendingPlot=-1,pendingType,pendingLevel,pendingDays;
        public string chronicle="Поселенцы основали новый двор. Построй дома и наладь снабжение.";
        public bool initialized;
        public float dayProgress;
        public int pendingEvent,eventsSeen;
        public string NextObjective => Count(3)<2?"Запас на зиму: построй вторую пашню":Sum(5)==0?"Камень для двора: построй каменоломню":Count(2)<2?"Новые семьи: построй второй жилой дом":Sum(6)==0?"Снаряжение дружины: построй кузницу":Sum(7)==0?"Собери защитников: построй казармы":Sum(8)==0?"Защити запасы: построй дозорную башню":Hall<2?"Сердце деревни: улучши двор до II ступени":Sum(9)==0?"Открой торговый путь: построй торговый двор":"Развивай торговлю, готовь набег или созови вече";
        public int FoodBalance => Sum(3)*(constitution==1?29:24)-population-soldiers;
        public bool MoveBuilding(int from,int to)
        {
            if(from<0||from>=20||to<0||to>=20||from==7||to==7||pendingPlot>=0||buildings[from]==0||buildings[to]!=0)return false;
            buildings[to]=buildings[from];levels[to]=levels[from];buildings[from]=levels[from]=0;
            chronicle="Постройка перенесена. Производство сохранено.";return true;
        }
        public bool AdvanceClock(float seconds)
        {
            if(pendingEvent!=0||seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return false;
            dayProgress+=Mathf.Min(seconds,30)/30;
            if(dayProgress<1)return false;
            dayProgress-=1;AdvanceDay();
            if(day%7==0){pendingEvent=1+eventsSeen%3;eventsSeen++;}
            return true;
        }
        public bool ChooseEvent(int choice)
        {
            if(pendingEvent==0||choice<0||choice>1)return false;
            if(pendingEvent==1) {if(choice==0){if(wood<25)return false;wood-=25;stone+=25;chronicle="Купец обменял брёвна на камень.";}else{morale+=3;chronicle="Купец заночевал у двора и рассказал новости.";}}
            if(pendingEvent==2) {if(choice==0){if(food<20||population+2>Capacity)return false;food-=20;population+=2;legitimacy+=5;chronicle="Две семьи получили кров и припасы.";}else{food+=10;morale-=3;chronicle="Путникам указали дорогу к соседям; те оставили провизию.";}}
            if(pendingEvent==3) {if(choice==0){if(food<15)return false;food-=15;legitimacy+=8;chronicle="Дозор нашёл заброшенный колокол. Ночных огней больше нет.";}else{morale-=4;wood+=20;chronicle="Лесорубы принесли брёвна. Огонь у кургана снова видели ночью.";}}
            pendingEvent=0;Validate();return true;
        }
        public void Validate()
        {
            if(!initialized) {buildings=new int[20];levels=new int[20];buildings[7]=1;levels[7]=1;buildings[6]=2;levels[6]=1;buildings[11]=3;levels[11]=1;buildings[12]=4;levels[12]=1;initialized=true;}
            if(buildings==null||buildings.Length!=20) Array.Resize(ref buildings,20);
            if(levels==null||levels.Length!=20) Array.Resize(ref levels,20);
            if(raidReady==null||raidReady.Length!=3) Array.Resize(ref raidReady,3);
            for(int i=0;i<20;i++){buildings[i]=Mathf.Clamp(buildings[i],0,10);levels[i]=buildings[i]==0?0:Mathf.Clamp(levels[i],1,3);}
            dayProgress=float.IsNaN(dayProgress)||float.IsInfinity(dayProgress)?0:Mathf.Clamp(dayProgress,0,.999f);
            pendingEvent=Mathf.Clamp(pendingEvent,0,3);eventsSeen=Mathf.Max(0,eventsSeen);
            // A capital is never removable; restore it if a malformed save omitted it.
            buildings[7]=1;levels[7]=Mathf.Clamp(levels[7],1,3);
            ore=Mathf.Clamp(ore,0,99999);wood=Mathf.Clamp(wood,0,99999);stone=Mathf.Clamp(stone,0,99999);iron=Mathf.Clamp(iron,0,99999);food=Mathf.Clamp(food,0,99999);
            population=Mathf.Clamp(population,6,200);soldiers=Mathf.Clamp(soldiers,0,ArmyCapacity);day=Mathf.Clamp(day,1,1000000);
            morale=Mathf.Clamp(morale,0,100);legitimacy=Mathf.Clamp(legitimacy,0,100);government=Mathf.Clamp(government,0,2);constitution=Mathf.Clamp(constitution,0,1);
            if(pendingPlot<0||pendingPlot>=20||pendingType<1||pendingType>10||pendingLevel<1||pendingLevel>3) pendingPlot=-1;
        }
        public int Hall => levels[7];
        public int Sum(int type) {int n=0;for(int i=0;i<20;i++)if(buildings[i]==type)n+=levels[i];return n;}
        public int Count(int type) {int n=0;foreach(int b in buildings)if(b==type)n++;return n;}
        public int Capacity => 8+Sum(2)*10;
        public int ArmyCapacity => Mathf.Min(population/2,4+Sum(7)*6);
        public static readonly string[] Names={"Пустой участок","Двор правителя","Жилой дом","Пашня","Лесной двор","Каменоломня","Кузница","Казармы","Дозорная башня","Торговый двор","Дом собраний"};
        public static readonly string[] Effects={"","Открывает новые ступени развития","+10 мест для жителей за уровень","+24 еды за уровень в день","+22 дерева за уровень в день","+16 камня и +6 руды за уровень в день","4 руды + 4 дерева → 8 железа за уровень; сила отряда","+6 мест в отряде за уровень","Защищает запасы от набегов ИИ","Обмен ресурсов; доход снабжения","Открывает реформы; укрепляет легитимность"};
        public static Vector3Int Cost(int type,int tier)
        {int[] w={0,90,45,40,35,45,60,70,55,65,75};int[] s={0,70,15,5,5,20,45,35,45,35,40};int[] iron={0,15,0,0,0,0,10,12,8,5,0};return new Vector3Int(w[type]*tier,s[type]*tier,iron[type]*tier);}
        public string BuildReason(int plot,int type)
        {
            if(plot<0||plot>=20||type<1||type>10)return "Недопустимый участок";
            if(pendingPlot>=0)return "Сначала заверши текущую стройку";
            if(buildings[plot]!=0&&buildings[plot]!=type)return "Участок занят";
            if(type==1&&plot!=7)return "Двор правителя уже есть";
            int tier=levels[plot]+1;
            if(tier>3)return "Достигнута третья ступень";
            if(type!=1&&tier>Hall)return "Сначала улучши двор правителя";
            if(type==1&&tier==2&&(population<18||Count(2)<2))return "Нужно 18 жителей и два жилых дома";
            if(type==1&&tier==3&&(population<28||Sum(6)<2||Sum(9)<2))return "Нужно 28 жителей, кузница II и торговый двор II";
            if((type==9||type==10)&&Hall<2)return "Откроется при дворе правителя II";
            Vector3Int c=Cost(type,tier);if(wood<c.x||stone<c.y||iron<c.z)return "Не хватает ресурсов";
            return "";
        }
        public bool Build(int plot,int type)
        {
            if(BuildReason(plot,type)!="")return false;
            int tier=levels[plot]+1;Vector3Int c=Cost(type,tier);wood-=c.x;stone-=c.y;iron-=c.z;
            pendingPlot=plot;pendingType=type;pendingLevel=tier;pendingDays=tier;
            chronicle=Names[type]+": строительство, дней до завершения — "+pendingDays;return true;
        }
        public void AdvanceDay()
        {
            day++;string note="День "+day+". ";
            int batches=ForgeBatches;wood+=Sum(4)*22-batches*4;stone+=Sum(5)*16;ore+=Sum(5)*6-batches*4;iron+=batches*8;
            if(Sum(6)>batches)note+="Кузнице не хватает руды или дерева. ";
            int harvest=Sum(3)*24+(constitution==1?Sum(3)*5:0),upkeep=population+soldiers;
            food+=harvest;
            if(food>=upkeep) {food-=upkeep;morale+=government==2?3:1;if(population<Capacity&&morale>=45)population=Mathf.Min(Capacity,population+(government==2?2:1));}
            else {food=0;morale-=12;population=Mathf.Max(6,population-1);note+="Нехватка еды: жители уходят. ";}
            if(government==1)morale-=4;
            legitimacy+=Sum(10)>0?2:0;
            if(pendingPlot>=0&&--pendingDays<=0){buildings[pendingPlot]=pendingType;levels[pendingPlot]=pendingLevel;note+=Names[pendingType]+" готово. ";pendingPlot=-1;}
            if(day%5==0)
            {
                int defence=Sum(8)*22+soldiers*4+(constitution==0?8:0),enemy=20+Hall*15;
                int loss=Mathf.Max(0,enemy-defence);
                wood=Mathf.Max(0,wood-loss);food=Mathf.Max(0,food-loss);note+=loss==0?"Дозор отразил налёт ИИ. ":"Налёт ИИ: потеряно до "+loss+" дерева и еды. ";
            }
            chronicle=note+"Снабжение: +"+harvest+" еды, расход "+upkeep+".";Validate();
        }
        public bool Recruit()
        {
            if(Sum(7)==0||soldiers>=ArmyCapacity||food<10||iron<2)return false;
            soldiers++;food-=10;iron-=2;chronicle="В отряд принят новый воин.";return true;
        }
        public bool Trade(int kind)
        {
            if(Sum(9)==0||wood<30||kind<0||kind>2)return false;
            wood-=30;if(kind==0)stone+=20;if(kind==1)iron+=10;if(kind==2)food+=30;return true;
        }
        public string ReformReason(bool structure,int option)
        {
            if(Hall<2||Sum(10)==0)return "Нужны двор II и дом собраний";
            if(legitimacy<50||food<40)return "Нужны 50 легитимности и 40 еды";
            if(structure&&option==1&&(Hall<3||Sum(9)<2||Count(2)<3))return "Конфедерация: двор III, торговля II, три дома";
            if(!structure&&option==1&&Sum(7)<2)return "Единоличная власть: казармы II";
            if((structure&&option==constitution)||(!structure&&option==government))return "Уже принято";
            if(option<0||option>(structure?1:2))return "Неизвестная реформа";
            return "";
        }
        public bool Reform(bool structure,int option)
        {
            if(ReformReason(structure,option)!="")return false;
            food-=40;legitimacy-=25;if(structure)constitution=option;else government=option;
            chronicle="Принята новая хартия. На восстановление доверия потребуется время.";return true;
        }
        public static readonly string[] Rivals={"Лагерь разбойников","Застава наёмников","Крепость коменданта"};
        public static readonly int[] Strength={36,80,140};
        public int Power(int target,int tactic)
        {float p=soldiers*(9+Sum(6)*2)*(government==1?1.2f:1)*(morale<35?.75f:1);if(tactic==target)p*=1.25f;return Mathf.RoundToInt(p);}
        public string RaidReason(int target)
        {if(target<0||target>2)return "Неизвестная цель";if(Sum(7)==0||soldiers<3)return "Нужны казармы и минимум три воина";if(day<raidReady[target])return "Разведка будет готова в день "+raidReady[target];if(food<15+target*10)return "Не хватает припасов";return "";}
        public bool ResolveRaid(int target,int tactic)
        {
            if(RaidReason(target)!=""||tactic<0||tactic>2)return false;
            int power=Power(target,tactic);bool victory=power>=Strength[target];
            food-=15+target*10;raidReady[target]=day+3;
            int casualties=victory?Mathf.Max(1,(target+2)*Strength[target]/Mathf.Max(1,power)):Mathf.Max(2,soldiers/3);
            casualties=Mathf.Min(soldiers,casualties);soldiers-=casualties;
            if(victory){wood+=45+target*35;stone+=25+target*25;iron+=8+target*10;food+=30;raidWins++;legitimacy+=8;}
            else {morale-=8;legitimacy-=5;}
            chronicle=(victory?"Победа":"Отступление")+": "+Rivals[target]+" (ИИ). Потери: "+casualties+". "+(victory?"Добыча доставлена в поселение.":"Укрепи отряд и подбери тактику.");Validate();return victory;
        }
    }
}
