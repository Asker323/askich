using UnityEngine;
namespace Askich
{
    public sealed partial class ArenaGame
    {
        int tradeBatches=1;
        void TownEconomy(Settlement town)
        {
            Text(new Rect(845,215,390,36),"ПРОИЗВОДСТВО И ОБМЕН",new GUIStyle(headingStyle){fontSize=23});
            int batches=town.ForgeBatches;
            string production="Лес: +"+town.Sum(4)*22+" дерева / день\nКаменоломни: +"+town.Sum(5)*16+" камня, +"+town.Sum(5)*6+" руды\nКузницы: −"+batches*4+" дерева, −"+batches*4+" руды → +"+batches*8+" железа\nПашни: +"+town.Sum(3)*(town.constitution==1?29:24)+" еды; снабжение: −"+(town.population+town.soldiers);
            Text(new Rect(845,259,392,92),production,smallStyle);
            string bottleneck=town.FoodBalance<0?"Не хватает еды: построй или улучши пашню.":batches<town.Sum(6)?"Кузницы простаивают: нужны руда и дерево.":"Производство обеспечено. Итог начисляется в конце дня.";
            Text(new Rect(845,352,392,43),bottleneck,new GUIStyle(smallStyle){normal={textColor=town.FoodBalance<0||batches<town.Sum(6)?new Color(1,.65f,.35f):parchment}});
            int[] quantities={1,5,10};
            for(int i=0;i<3;i++)if(Button(new Rect(845+i*132,402,125,32),(tradeBatches==quantities[i]?"• ":"")+quantities[i]+(quantities[i]==1?" партия":" партий")))tradeBatches=quantities[i];
            for(int i=0;i<4;i++)
            {
                string reason=town.TradeReason(i,tradeBatches);
                string offer=(30*tradeBatches)+" дерева → "+Settlement.TradeYield[i]*tradeBatches+" "+Settlement.TradeGoods[i];
                if(Button(new Rect(845+i%2*198,445+i/2*49,190,43),offer,reason=="")){town.TradeBatch(i,tradeBatches);sound.Play("upgrade");Save();}
            }
            string availability=town.Sum(9)==0||town.wood<30*tradeBatches?town.TradeReason(0,tradeBatches):"Обмен мгновенный. Указана полная стоимость.";
            Text(new Rect(845,551,392,37),availability,smallStyle);
        }
    }
}
