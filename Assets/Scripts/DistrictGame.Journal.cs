using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  void TaskButton(Rect area,string label,bool enabled,System.Action action){FillRect(area,enabled?new Color(.23f,.35f,.30f):new Color(.13f,.18f,.18f));GUI.color=enabled?Color.white:new Color(.56f,.61f,.57f);GUI.Label(new Rect(area.x+12,area.y+9,area.width-24,area.height-14),label,small);GUI.color=Color.white;if(Event.current.type==EventType.Repaint&&pointer.TakeClick(area)&&enabled)action();}
  string SkillEffect(int skill){int level=progression.Level((DistrictSkill)skill);if(level==0&&skill<3)return skill==0?"Растёт при ударах по заражённым и толчках":skill==1?"Растёт при обыске и тихом передвижении":"Растёт при ходьбе и беге";switch(skill){case 0:return "+"+(level*3)+"% урона оружием · кулак +"+(level*6)+"%";case 1:return "Обыск быстрее на "+(level*4)+"% · тихий шаг +"+(level*4)+"%";case 2:return "Бег расходует на "+(level*4)+"% меньше сил · восстановление +"+(level*5)+"%";case 3:return level==0?"Первая глава откроет разбор двигателя": "Разбор двигателя · выход металла: "+(4+Mathf.Min(2,level-1));default:return level==0?"Ткань и перевязки · уровень 1 откроет копьё":"Открыто копьё · скорость изготовления + "+(level*7)+"%";}}
  void DrawJournal(){
   EnsureInventoryArt();FillRect(new Rect(0,0,1280,720),new Color(.01f,.02f,.025f,.8f));FillRect(GridPanel,new Color(.04f,.065f,.07f));
   GUI.Label(new Rect(130,114,650,45),"ПЕРСОНАЖ",large);GUI.Label(new Rect(680,124,280,35),"День "+progression.Data.day+" · "+ClockText,small);
   string[] tabs={"Навыки","Ремесло","Места"};for(int i=0;i<3;i++){int tab=i;var area=new Rect(130+i*190,174,180,39);if(Button(area,tabs[i]))journalTab=tab;if(journalTab==i)FillRect(new Rect(area.x,area.y+36,area.width,3),MedicalAccent);}
   if(journalTab==0){
    for(int i=0;i<5;i++){
     var skill=(DistrictSkill)i;int level=progression.Level(skill);float y=230+i*65;FillRect(new Rect(130,y,490,57),new Color(.085f,.13f,.13f));GUI.Label(new Rect(143,y+5,335,25),SkillNames[i]+" · "+level+" / 5",text);GUI.Label(new Rect(143,y+29,460,25),SkillEffect(i),small);GUI.Label(new Rect(483,y+7,125,24),level==5?"МАКС.":Mathf.FloorToInt(progression.Data.xp[i])+" / "+progression.NextThreshold(skill),small);FillRect(new Rect(143,y+54,460,3),new Color(.17f,.22f,.20f));FillRect(new Rect(143,y+54,460*progression.Fraction(skill),3),MedicalAccent);
    }
    for(int book=0;book<2;book++){
     int chosen=book;float y=230+book*156;int kind=book==0?22:23;GUI.DrawTexture(new Rect(650,y,60,60),ItemIcon(kind),ScaleMode.ScaleToFit,true);GUI.Label(new Rect(720,y+2,420,32),book==0?"Устройство автомобиля":"Домашняя мастерская",text);
     float read=progression.Data.books[book];GUI.Label(new Rect(720,y+37,420,55),"Главы: "+Mathf.FloorToInt((read+.001f)/360)+" / 3 · прочитано "+Mathf.FloorToInt(read/60)+" мин\nОдна глава — 6 минут · можно прерывать",small);
     string reason=ProgressionReasonForBook(book);TaskButton(new Rect(720,y+95,420,43),reason??"Читать / продолжить",reason==null&&medicalAction==MedicalAction.None,()=>StartReading(chosen));
    }
   }else if(journalTab==1){
    for(int i=0;i<Recipes.Length;i++){
     int recipe=i;float y=231+i*107;GUI.DrawTexture(new Rect(135,y+3,54,54),ItemIcon(Recipes[i].result.kind),ScaleMode.ScaleToFit,true);GUI.Label(new Rect(202,y,445,28),Recipes[i].name,text);GUI.Label(new Rect(202,y+31,445,31),Recipes[i].detail,small);var reason=RecipeReason(i);TaskButton(new Rect(202,y+64,440,36),reason??"Изготовить · "+Mathf.CeilToInt(Recipes[i].seconds/(1+.07f*progression.Level(DistrictSkill.Craft)))+" с",reason==null&&medicalAction==MedicalAction.None,()=>StartRecipe(recipe));
    }
    GUI.Label(new Rect(710,237,415,40),"Мастерская на АЗС",text);GUI.Label(new Rect(710,284,415,123),"Разбор двигателя требует механики 1 и гаечного ключа. Работа шумная. При движении или ранении она прервётся.",small);var why=ProgressionReason(MedicalAction.Salvage);TaskButton(new Rect(710,431,415,52),why??"Разобрать двигатель · шумно",why==null&&medicalAction==MedicalAction.None,()=>BeginMedical(MedicalAction.Salvage));
    GUI.Label(new Rect(710,508,415,50),"Чистый бинт со спиртом по-прежнему создаётся в снаряжении.",small);
   }else{
    string[] titles={"АЗС «Восточная»","Семейный дом · №5","Дом связиста"};
    string[] details={"На восточной дороге: магазин, инструмент и старый двигатель. Руководство по механике лежит на столе.","Западнее дома с радио. На столе — книга по домашнему ремеслу.","У приёмника можно выбрать учебную частоту. 90 секунд рядом дают 35 опыта ремесла, один раз за игровой день. Передача привлекает заражённых."};
    for(int i=0;i<3;i++){float y=234+i*103;GUI.Label(new Rect(142,y,750,32),titles[i],text);GUI.Label(new Rect(142,y+35,940,61),details[i],small);}
    bool near=Vector2.Distance(hero.p,radioPos)<=2&&Sight(hero.p,radioPos);TaskButton(new Rect(730,532,400,42),near?"Настроить учебную частоту":"Для настройки подойди к радио",near,()=>TuneTrainingRadio());
   }
   if(medicalAction!=MedicalAction.None){GUI.Label(new Rect(142,589,800,28),medicalAction==MedicalAction.Read?"Чтение · "+Mathf.FloorToInt(progression.Data.books[readingBook]%360)+" / 360 с":"Работа продолжается · мир не на паузе",small);if(Button(new Rect(965,579,170,40),"Прервать")){CancelMedical();Save();}}
   else GUI.Label(new Rect(142,589,950,28),"Навыки растут от практики. Книги остаются у тебя. Мир в этом окне продолжает жить.",small);
   if(Button(new Rect(1000,116,150,48),"Закрыть"))journalOpen=false;
  }
  string ProgressionReasonForBook(int book){int previous=readingBook;readingBook=book;string reason=ProgressionReason(MedicalAction.Read);readingBook=previous;return reason;}
  void DrawNewItemActions(DistrictItem item,int slot){
   if(Edible(item.kind)){
    selectedFood=item.kind;GUI.Label(new Rect(905,260,245,85),item.kind==14?"+22 сытости · −4 воды":item.kind==15?"+32 сытости · +6 воды":"+18 сытости · +16 сил",small);TaskButton(new Rect(905,355,245,58),nourishment<95?"Поесть":"Пока сыт",nourishment<95&&medicalAction==MedicalAction.None,()=>EatSelected(slot));return;
   }
   if(item.kind==22||item.kind==23){int book=item.kind-22;float read=progression.Data.books[book];GUI.Label(new Rect(905,260,245,90),"Главы: "+Mathf.FloorToInt((read+.001f)/360)+" / 3\n6 минут на главу. Прочитанное сохраняется.",small);string reason=ProgressionReasonForBook(book);TaskButton(new Rect(905,365,245,68),reason??"Читать / продолжить",reason==null&&medicalAction==MedicalAction.None,()=>StartReading(book));return;}
   GUI.Label(new Rect(905,260,245,90),item.kind==21?"Инструмент для разбора двигателя. Нужна механика 1.":"Материал для изготовления вещей. Рецепты — в окне персонажа.",small);if(Button(new Rect(905,365,245,58),"Открыть ремесло")){suppliesOpen=false;journalOpen=true;journalTab=1;}
  }
  void MakeProgressionIcon(Transform parent,int kind){
   void P(string name,Vector3 p,Vector3 size,Color c){Cube(name,p,size,c,parent);}
   var paper=new Color(.70f,.66f,.51f);var metal=new Color(.39f,.42f,.39f);var rust=new Color(.32f,.23f,.16f);
   if(kind==22||kind==23){var cover=kind==22?new Color(.16f,.30f,.39f):new Color(.36f,.27f,.17f);P("Book pages",Vector3.zero,new Vector3(.43f,.08f,.33f),paper);P("Book upper cover",new Vector3(0,.05f,0),new Vector3(.48f,.025f,.37f),cover);P("Book lower cover",new Vector3(0,-.05f,0),new Vector3(.48f,.025f,.37f),cover);P("Book spine",new Vector3(-.235f,0,0),new Vector3(.035f,.12f,.37f),cover);for(int i=0;i<3;i++)P("Printed book title",new Vector3(.03f,.065f,-.08f+i*.07f),new Vector3(.28f,.006f,.024f),paper);return;}
   if(kind==21){P("Wrench handle",Vector3.zero,new Vector3(.095f,.07f,.55f),metal);for(int side=-1;side<=1;side+=2)P("Wrench jaw",new Vector3(side*.09f,0,.30f),new Vector3(.07f,.085f,.18f),metal);P("Wrench jaw base",new Vector3(0,0,.22f),new Vector3(.23f,.085f,.08f),metal);return;}
   if(kind==20){for(int i=0;i<16;i++){float a=i*Mathf.PI/8;var part=Cube("Coiled wire",new Vector3(Mathf.Cos(a)*.22f,0,Mathf.Sin(a)*.22f),new Vector3(.09f,.025f,.025f),new Color(.38f,.27f,.15f),parent);part.transform.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);}return;}
   if(kind==18){for(int i=0;i<3;i++)P("Rough plank",new Vector3((i-1)*.15f,i%2*.04f,0),new Vector3(.13f,.045f,.65f),new Color(.40f,.30f,.19f));return;}
   if(kind==19){P("Bent sheet metal",Vector3.zero,new Vector3(.46f,.045f,.30f),rust);P("Engine metal scrap",new Vector3(.1f,.08f,0),new Vector3(.18f,.13f,.2f),metal);return;}
   if(kind==17){for(int i=0;i<3;i++)P("Folded cloth strips",new Vector3(i*.025f,i*.035f,0),new Vector3(.4f,.03f,.22f),i==1?new Color(.40f,.44f,.37f):paper);return;}
   if(kind==15){var can=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Destroy(can.GetComponent<Collider>());can.name="Tin of beans";can.transform.SetParent(parent,false);can.transform.localScale=new Vector3(.32f,.18f,.32f);can.GetComponent<Renderer>().sharedMaterial=Mat(metal);P("Beans red label",new Vector3(0,0,-.16f),new Vector3(.27f,.23f,.025f),new Color(.48f,.23f,.16f));P("Tin label stripe",new Vector3(0,0,-.18f),new Vector3(.20f,.06f,.014f),paper);return;}
   if(kind==14){P("Crackers paper pouch",Vector3.zero,new Vector3(.35f,.12f,.44f),new Color(.57f,.44f,.23f));for(int i=0;i<3;i++)P("Cracker label",new Vector3(0,.067f,-.1f+i*.1f),new Vector3(.24f,.008f,.04f),paper);return;}
   P("Chocolate foil",Vector3.zero,new Vector3(.3f,.06f,.48f),metal);for(int x=0;x<2;x++)for(int z=0;z<3;z++)P("Chocolate square",new Vector3(-.07f+x*.14f,.045f,-.15f+z*.14f),new Vector3(.13f,.035f,.13f),new Color(.21f,.13f,.08f));
  }
 }
}
