namespace Askich
{
    public sealed partial class ArenaGame
    {
        void ContinueStory(bool acceptOrder)
        {
            if(mode!=Mode.Story) return;
            if(wave==0) { NextWave(); return; }
            if(wave==3) { orderAlly=acceptOrder; if(!acceptOrder) {damage*=1.15f; AddSilver(30);} }
            mode=Mode.Upgrade;
        }
        void Upgrade(int choice)
        {
            if(mode!=Mode.Upgrade || choice<0 || choice>2) return;
            if(choice==0) damage*=1.2f;
            if(choice==1) {maxHealth+=25;health=maxHealth;}
            if(choice==2) {maxStamina+=25;staminaRegen+=4;}
            NextWave();
        }
        bool BuySkin(int index)
        {
            if(index<0||index>2) return false;
            int price=index==1?180:index==2?520:0;
            if((profile.unlockedSkins&(1<<index))==0)
            {
                if(profile.credits<price) return false;
                profile.credits-=price; profile.unlockedSkins|=1<<index;
            }
            profile.selectedSkin=index; Save(); player.color=SkinColor(); return true;
        }
    }
}
