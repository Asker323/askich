using System.Collections.Generic;
using UnityEngine;
namespace Askich
{
    public sealed partial class ArenaGame
    {
        sealed class GearDrop {public SpriteRenderer view;public Vector2 position;public int item;}
        readonly List<GearDrop> gearDrops=new List<GearDrop>();
        void DropGear(Vector2 position)
        {
            if(kills%3!=0)return;
            int id=faction*3+(kills/3-1)%3;
            if((profile.ownedItems&(1<<id))!=0)return;
            foreach(var drop in gearDrops)if(drop.item==id)return;
            var view=MedievalArt.Make("Historical equipment",MedievalArt.Prop(8),position,.65f,actors,Sort(position));
            NeonVisual.Make("Equipment marker","ring",new Color(.85f,.73f,.38f),Vector2.zero,new Vector2(.9f,.35f),-2,view.transform);
            gearDrops.Add(new GearDrop{view=view,position=position,item=id});
        }
        void PickGear()
        {
            for(int i=gearDrops.Count-1;i>=0;i--)
            {
                var drop=gearDrops[i];if(Vector2.Distance(playerPos,drop.position)>1.5f)continue;
                profile.ownedItems|=1<<drop.item;Save();
                notice="Найдено: "+HistoricalItems.All[drop.item].name+". Доступно в арсенале.";noticeTime=4;
                Destroy(drop.view.gameObject);gearDrops.RemoveAt(i);
            }
        }
        void ApplyEquipment()
        {
            for(int i=faction*3;i<faction*3+3;i++)if(profile.equippedItems[i]==i+1&&(profile.ownedItems&(1<<i))!=0)
            {
                var item=HistoricalItems.All[i];if(item.slot==0)damage+=item.bonus;if(item.slot==1)maxHealth+=item.bonus;if(item.slot==2)maxStamina+=item.bonus;
            }
            health=maxHealth;stamina=maxStamina;
        }
        bool EquipItem(int id)
        {
            if(id<0||id>=9||(profile.ownedItems&(1<<id))==0)return false;
            profile.equippedItems[id]=profile.equippedItems[id]==id+1?0:id+1;Save();return true;
        }
        void ArmoryScreen()
        {
            Panel(new Rect(50,35,1180,650));
            Text(new Rect(90,53,1100,52),"ИСТОРИЧЕСКИЙ АРСЕНАЛ",titleStyle);
            for(int f=0;f<3;f++)if(Button(new Rect(110+f*360,119,340,40),CampaignData.Countries[f])){faction=f;profile.selectedFaction=f;Save();}
            for(int slot=0;slot<3;slot++)
            {
                int id=faction*3+slot;var item=HistoricalItems.All[id];Rect r=new Rect(82+slot*375,181,365,399);Panel(r);
                Text(new Rect(r.x+18,r.y+14,329,28),HistoricalItems.Slots[slot].ToUpper(),smallStyle);
                Text(new Rect(r.x+18,r.y+48,329,88),item.name,headingStyle);
                Text(new Rect(r.x+18,r.y+143,329,27),item.period,smallStyle);
                Text(new Rect(r.x+18,r.y+180,329,115),item.note,smallStyle);
                bool owned=(profile.ownedItems&(1<<id))!=0;
                Text(new Rect(r.x+18,r.y+297,329,30),owned?item.Effect:"Найди в походе · ящик + E",smallStyle);
                if(Button(new Rect(r.x+18,r.y+338,208,42),profile.equippedItems[id]==id+1?"Снять":"Надеть",owned))EquipItem(id);
                if(Button(new Rect(r.x+238,r.y+338,110,42),"Источник"))Application.OpenURL(item.source);
            }
            Text(new Rect(95,592,790,67),"Бонусы действуют со следующего похода. Предметы сохраняются после поражения.\nНаборы сторон различаются для игры; предметы не были исключительной собственностью одного народа. Внешний вид героя остаётся базовым.",smallStyle);
            if(Button(new Rect(955,610,235,46),"Назад · Esc"))mode=Mode.Menu;
        }
    }
}
