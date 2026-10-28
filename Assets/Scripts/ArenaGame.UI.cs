using UnityEngine;

namespace Askich
{
    public sealed partial class ArenaGame
    {
        GUIStyle titleStyle, headingStyle, textStyle, smallStyle, buttonStyle;
        readonly Color parchment=new Color(.91f,.85f,.69f);
        readonly Color gold=new Color(.75f,.57f,.29f);
        void Styles()
        {
            if(titleStyle!=null) return;
            var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleStyle=new GUIStyle {font=font,fontSize=54,fontStyle=FontStyle.Bold,normal={textColor=parchment},alignment=TextAnchor.MiddleCenter};
            headingStyle=new GUIStyle(titleStyle){fontSize=26,alignment=TextAnchor.UpperLeft,wordWrap=true};
            textStyle=new GUIStyle(headingStyle){fontSize=21,fontStyle=FontStyle.Normal};
            smallStyle=new GUIStyle(textStyle){fontSize=16};
            buttonStyle=new GUIStyle(textStyle){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,fontSize=20};
        }
        void Box(Rect r,Color c) { Color old=GUI.color; GUI.color=c; GUI.DrawTexture(r,Texture2D.whiteTexture); GUI.color=old; }
        void Panel(Rect r) { Box(r,gold); Box(new Rect(r.x+1,r.y+1,r.width-2,r.height-2),new Color(.11f,.095f,.07f,.98f)); }
        void Text(Rect r,string value,GUIStyle style=null) => GUI.Label(r,value,style??textStyle);
        bool Button(Rect r,string value,bool enabled=true)
        {
            bool hover=r.Contains(Event.current.mousePosition);
            Box(r,enabled?(hover?new Color(.36f,.28f,.16f):new Color(.24f,.2f,.13f)):new Color(.15f,.14f,.12f));
            Box(new Rect(r.x,r.y+r.height-2,r.width,2),enabled?gold:Color.gray);
            Color old=GUI.color; if(!enabled) GUI.color=new Color(.6f,.6f,.6f);
            bool clicked=GUI.Button(r,value,new GUIStyle(buttonStyle){fontSize=r.height<40?15:value.Length>35?16:20}); GUI.color=old; return enabled&&clicked;
        }
        void Bar(Rect r,float fraction,Color color) { Box(r,new Color(.055f,.05f,.04f)); Box(new Rect(r.x+2,r.y+2,(r.width-4)*Mathf.Clamp01(fraction),r.height-4),color); }
        void OnGUI()
        {
            Styles(); Matrix4x4 old=GUI.matrix; float scale=ScreenLayout.Scale;
            GUI.matrix=Matrix4x4.TRS(ScreenLayout.Offset,Quaternion.identity,Vector3.one*scale);
            if(mode==Mode.Playing||mode==Mode.Paused||mode==Mode.Dying) Hud();
            if(TouchControls.Enabled&&mode==Mode.Playing)TouchHud();
            if(mode!=Mode.Playing&&mode!=Mode.Dying) Box(new Rect(0,0,1280,720),new Color(.025f,.022f,.017f,.72f));
            switch(mode)
            {
                case Mode.Menu: Menu(); break;
                case Mode.Settings: SettingsScreen(); break;
                case Mode.Settlement: TownScreen(); break;
                case Mode.Armory: ArmoryScreen(); break;
                case Mode.Raid: RaidScreen(); break;
                case Mode.Faction: FactionScreen(); break;
                case Mode.Story: StoryScreen(); break;
                case Mode.Upgrade: UpgradeScreen(); break;
                case Mode.Shop: ShopScreen(); break;
                case Mode.Paused:
                    Panel(new Rect(400,210,480,300)); Text(new Rect(430,235,420,60),"ПРИВАЛ",titleStyle);
                    if(Button(new Rect(450,320,380,54),"Продолжить · Esc")) mode=Mode.Playing;
                    if(Button(new Rect(450,452,380,40),"Настройки")){settingsReturn=mode;mode=Mode.Settings;}
                    if(Button(new Rect(450,390,380,54),"Завершить поход")) { Finish(false); }
                    break;
                case Mode.Defeat: case Mode.Victory: ResultScreen(); break;
            }
            GUI.matrix=old;
        }
        void Menu()
        {
            Panel(new Rect(125,65,1030,590));
            Text(new Rect(150,83,980,70),"GOIDA",titleStyle);
            Text(new Rect(160,159,960,32),"КЛЯТВА НА ГРАНИЦЕ",new GUIStyle(headingStyle){alignment=TextAnchor.MiddleCenter});
            MedievalArt.Portrait(new Rect(165,235,250,250),0); MedievalArt.Portrait(new Rect(865,235,250,250),1);
            Text(new Rect(425,218,430,90),"Три знамени. Одна осаждённая земля.\nКопьё, меч или топор — выбери свой путь.",new GUIStyle(textStyle){alignment=TextAnchor.MiddleCenter});
            if(Button(new Rect(445,308,390,48),"Начать поход · Enter")) mode=Mode.Faction;
            if(Button(new Rect(445,365,390,45),"Моё поселение")) OpenTown();
            if(Button(new Rect(445,418,390,45),"Арсенал · исторические предметы")) mode=Mode.Armory;
            if(Button(new Rect(445,471,390,42),"Отделка · "+profile.credits+" серебра")) mode=Mode.Shop;
            if(Button(new Rect(445,521,190,36),"Настройки")) {settingsReturn=mode;mode=Mode.Settings;}
            if(Button(new Rect(645,521,190,36),"Выйти")) Application.Quit();
            Text(new Rect(200,568,880,45),"Средневековая фантазия с вымышленными событиями.\n5 заданий · 2 локации · 3 героя · один опасный секрет",new GUIStyle(smallStyle){alignment=TextAnchor.MiddleCenter});
            Text(new Rect(170,627,940,25),"Asker323  /  2.3.0                                       M — звук: "+(sound.Muted?"выключен":"включён"),smallStyle);
        }
        void FactionScreen()
        {
            Text(new Rect(100,35,1080,65),"ПОД КАКИМ ЗНАМЕНЕМ?",titleStyle);
            for(int i=0;i<3;i++)
            {
                Rect r=new Rect(100+i*365,128,350,495); Panel(r); Box(new Rect(r.x+1,r.y+1,348,5),CampaignData.Colors[i]);
                MedievalArt.Portrait(new Rect(r.x+65,r.y+17,220,220),i);
                Text(new Rect(r.x+24,r.y+230,300,35),CampaignData.Countries[i].ToUpper(),headingStyle);
                Text(new Rect(r.x+24,r.y+270,300,28),CampaignData.Classes[i]);
                Text(new Rect(r.x+24,r.y+307,300,27),CampaignData.Weapons[i],smallStyle);
                Text(new Rect(r.x+24,r.y+349,300,70),CampaignData.Traits[i],smallStyle);
                if(Button(new Rect(r.x+24,r.y+430,302,45),(i+1)+" · Выбрать"+((profile.completedFactions&(1<<i))!=0?"  ✓":""))) { faction=i; StartRun(); }
            }
            Text(new Rect(110,650,1060,30),"У каждой стороны своя завязка и герой. Задания и карты общие.   Esc — назад",smallStyle);
        }
        void StoryScreen()
        {
            Panel(new Rect(100,70,1080,580));
            int portrait=wave==3||orderAlly&&wave==4?6:faction;
            MedievalArt.Portrait(new Rect(115,180,280,280),portrait);
            Text(new Rect(140,100,990,42),wave==0?"ПРОЛОГ · "+CampaignData.Countries[faction].ToUpper():wave==3?"ПРЕДЛОЖЕНИЕ ОРДЕНА":"ПОСЛЕ БОЯ · "+wave,headingStyle);
            Text(new Rect(405,170,730,40),portrait==6?"Брат Матье":CampaignData.Speakers[faction],headingStyle);
            Text(new Rect(405,225,735,300),CampaignData.Brief(faction,wave,orderAlly));
            if(wave==3)
            {
                if(Button(new Rect(160,548,460,60),"1 · Союз с орденом\nМатье сражается рядом")) ContinueStory(true);
                if(Button(new Rect(650,548,460,60),"2 · Своими силами\n+15% урона и 30 серебра")) ContinueStory(false);
            }
            else if(Button(new Rect(670,555,430,55),wave==0?"В путь · Enter":"К походной кузнице · Enter")) ContinueStory(false);
        }
        void UpgradeScreen()
        {
            Text(new Rect(100,70,1080,65),"ПОХОДНАЯ КУЗНИЦА",titleStyle);
            Text(new Rect(210,150,860,50),"Припасы за выполненное задание: выбери одно улучшение.",new GUIStyle(textStyle){alignment=TextAnchor.MiddleCenter});
            string[] names={"Заточить оружие","Укрепить доспех","Облегчить снаряжение"};
            string[] benefits={"+20% урона\nдля лёгкого и тяжёлого удара","+25 к здоровью\nи полное лечение","+25 к выносливости\n+4 к восстановлению в секунду"};
            for(int i=0;i<3;i++)
            {
                Rect r=new Rect(105+i*365,245,340,290); Panel(r);
                Text(new Rect(r.x+25,r.y+35,290,70),names[i],headingStyle);
                Text(new Rect(r.x+25,r.y+122,290,75),benefits[i]);
                if(Button(new Rect(r.x+25,r.y+218,290,48),(i+1)+" · Улучшить")) Upgrade(i);
            }
            Text(new Rect(200,584,880,60),"Далее: "+CampaignData.Mission(faction,wave+1),new GUIStyle(textStyle){alignment=TextAnchor.MiddleCenter});
        }
        void ShopScreen()
        {
            Text(new Rect(100,40,1080,65),"СНАРЯЖЕНИЕ",titleStyle);
            Text(new Rect(150,115,980,50),"Серебро: "+profile.credits+"   ·   Отделка меняет цвет одежды и доспеха. Характеристики героя сохраняются.",smallStyle);
            string[] names={"Походный доспех","Серебряная отделка","Ветеранская позолота"};
            for(int i=0;i<3;i++)
            {
                Rect r=new Rect(105+i*365,183,340,390); Panel(r);
                Color old=GUI.color; GUI.color=i==1?new Color(.82f,.91f,1):i==2?new Color(1,.86f,.62f):Color.white;
                MedievalArt.Portrait(new Rect(r.x+70,r.y+10,200,200),faction); GUI.color=old;
                Text(new Rect(r.x+20,r.y+220,300,60),names[i],headingStyle);
                bool owned=(profile.unlockedSkins&(1<<i))!=0; int price=i==1?180:i==2?520:0;
                if(Button(new Rect(r.x+20,r.y+310,300,55),profile.selectedSkin==i?"Надето":owned?"Надеть":"Открыть · "+price,owned||profile.credits>=price)) BuySkin(i);
            }
            if(Button(new Rect(450,615,380,50),"Назад · Esc")) mode=Mode.Menu;
        }
        void Hud()
        {
            Box(new Rect(0,0,1280,91),new Color(.07f,.061f,.043f,.96f));
            Text(new Rect(24,10,320,27),CampaignData.Classes[faction],smallStyle);
            Bar(new Rect(24,42,235,17),health/maxHealth,new Color(.62f,.18f,.14f));
            Bar(new Rect(24,65,235,12),stamina/maxStamina,new Color(.55f,.55f,.25f));
            Text(new Rect(270,40,110,40),Mathf.CeilToInt(health)+" / "+maxHealth,smallStyle);
            Text(new Rect(390,13,660,30),wave+" / 5 · "+CampaignData.Mission(faction,wave),headingStyle);
            Text(new Rect(390,54,700,25),CampaignData.Objective(wave),smallStyle);
            Text(new Rect(1100,12,170,60),"Серебро: "+profile.credits+"\nВраги: "+(remaining+enemies.Count),smallStyle);
            Box(new Rect(0,649,1280,71),new Color(.07f,.061f,.043f,.96f));
            Text(new Rect(24,662,1240,25),TouchControls.Enabled?"Джойстик — движение · Цель выбирается автоматически · Удерживай щит для блока · ВЗЯТЬ — предметы и пленные":"WASD — шаг   ЛКМ — удар   ПКМ — щит   Q — тяжёлый удар   Пробел — перекат   E — освободить   Esc — пауза",smallStyle);
            Text(new Rect(24,691,1200,24),LocationName+"    ·    "+(wave==2?"Обоз: "+Mathf.RoundToInt((objectivePos.x+9)/18*100)+"%   Прочность: "+Mathf.CeilToInt(objectiveHealth):wave==3?"Освобождено: "+rescued+" / 3":wave==4?"Оборона: "+Mathf.Max(0,Mathf.CeilToInt(35-missionTime))+" сек.   Ворота: "+Mathf.CeilToInt(objectiveHealth):"Береги выносливость: щит защищает только спереди"),smallStyle);
            DrawCombatLesson();
            if(noticeTime>0) Text(new Rect(350,110,580,35),notice,new GUIStyle(textStyle){alignment=TextAnchor.MiddleCenter});
            foreach(var e in enemies)
            {
                if(e.hp>=e.maxHp&&e.kind!=3) continue;
                Vector3 screen=gameCamera.WorldToScreenPoint(e.pos+Vector2.up*(e.kind==3?2:1.5f));
                Vector2 canvas=ScreenLayout.ToUI(screen); float x=canvas.x,y=canvas.y;
                Bar(new Rect(x-30,y,60,6),e.hp/e.maxHp,new Color(.7f,.2f,.12f));
            }
            foreach(var drop in gearDrops) if(Vector2.Distance(playerPos,drop.position)<1.5f) Text(new Rect(350,572,580,28),"E — подобрать: "+HistoricalItems.All[drop.item].name,smallStyle);
            if(wave==3) foreach(var captive in captives)
            {
                if(Vector2.Distance(playerPos,captive.transform.position)<1.5f) Text(new Rect(490,600,300,32),"E — освободить пленного",smallStyle);
            }
        }
        void ResultScreen()
        {
            bool won=mode==Mode.Victory;
            Panel(new Rect(180,95,920,535));
            Text(new Rect(220,118,840,65),won?"КЛЯТВА ИСПОЛНЕНА":"ПОХОД ПРЕРВАН",titleStyle);
            string ending=faction==0?"Бережки снова встречают обозы. Воевода получает письмо:\nземлю удержали люди, которым было что защищать.":faction==1?"Над Сен-Рош поднято знамя. Но в новой клятве гарнизона\nесть слова о тех, кто живёт за стенами крепости.":"У фьорда зажглись очаги. Запасов хватит до весны,\nа имя того, кто продал дорогу, уже знает весь берег.";
            Text(new Rect(255,225,770,140),won?ending+"\n\n"+(orderAlly?"Матье запечатывает реликвию. Ночью колокол молчит.":"Дружина погребает реликвию. Страх больше не правит крепостью."):"Даже верный удар не заменит осторожности.\nИспользуй щит спереди, отходи во время вражеского замаха\nи оставляй силы для переката.\n\n"+notice);
            Text(new Rect(255,390,770,70),"Задание: "+wave+" / 5     Побеждено: "+kills+"     Очки: "+score+"\nСобрано серебра: "+runCredits+" · Серебро и открытое снаряжение сохранены.");
            if(Button(new Rect(260,510,360,58),"Новый поход")) mode=Mode.Faction;
            if(Button(new Rect(655,510,360,58),"К главному меню")) mode=Mode.Menu;
        }
    }
}
