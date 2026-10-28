namespace Askich
{
    public sealed class HistoricalItem
    {
        public readonly string name,period,note,source;
        public readonly int faction,slot,bonus;
        public HistoricalItem(int faction,int slot,string name,string period,string note,string source,int bonus)
        {this.faction=faction;this.slot=slot;this.name=name;this.period=period;this.note=note;this.source=source;this.bonus=bonus;}
        public string Effect => slot==0?"+"+bonus+" урона":slot==1?"+"+bonus+" здоровья":"+"+bonus+" выносливости";
    }
    public static class HistoricalItems
    {
        public static readonly string[] Slots={"Оружие","Доспех","Защита головы"};
        public static readonly HistoricalItem[] All={
            new HistoricalItem(0,0,"Копьё с треугольным наконечником","Тип: XII–XV вв.","Название описывает форму наконечника. Игровой экземпляр — вымышленный; музейный пример происходит с Куликова поля.","https://ar.culture.ru/ru/subject/nakonechnik-kopya-xiv-v",6),
            new HistoricalItem(0,1,"Кольчуга","Образец: XIII век","Кольчатый доспех. В основу карточки положен предмет из коллекции Брянского краеведческого музея.","https://ar.culture.ru/ru/subject/kolchuga-27",20),
            new HistoricalItem(0,2,"Шлем по образцу Ярослава Всеволодовича","Образец: XII–XIII вв.","Вымышленная копия музейного образца из Оружейной палаты. Это не найденный игроком подлинный княжеский шлем.","https://vision.kreml.ru/en-Us/about-museums/museum-collection/arms-and-armour/",18),
            new HistoricalItem(1,0,"Меч с дисковидным навершием","Франция, XIII век","Прототип: французский меч 25.188.12 в Metropolitan Museum. Археологическое описание, не личное имя оружия.","https://www.metmuseum.org/art/collection/search/32782",8),
            new HistoricalItem(1,1,"Хауберк · hauberk","Франция, XIII век","Длинная кольчужная рубаха. Такой доспех изображён на надгробии Жана д’Аллюи; бонус — игровая условность.","https://resources.metmuseum.org/resources/metpublications/pdf/A_Crusaders_Sword_The_Metropolitan_Museum_Journal_v_26_1991.pdf",28),
            new HistoricalItem(1,2,"Кольчужный капюшон · coif","Франция, XIII век","Кольчужная защита головы. На надгробии Жана д’Аллюи капюшон соединён с рубахой; отдельный слот условен.","https://resources.metmuseum.org/resources/metpublications/pdf/A_Crusaders_Sword_The_Metropolitan_Museum_Journal_v_26_1991.pdf",14),
            new HistoricalItem(2,0,"Топор по находкам из Гьермундбю","Норвегия, X век","В погребальном комплексе найдены два топора. Здесь это семейное наследие; точная музейная форма в спрайте не воспроизводится.","https://www.historiskmuseum.no/english/exhibitions/undermappe-utstillinger/handout-vikingr-web-engelsk.pdf",10),
            new HistoricalItem(2,1,"Кольчуга по находке из Гьермундбю","Норвегия, X век","Прототип — кольчатый доспех из комплекса Гьермундбю. В мире игры это старое наследие, а не новая технология XIII века.","https://www.historiskmuseum.no/english/exhibitions/undermappe-utstillinger/handout-vikingr-web-engelsk.pdf",18),
            new HistoricalItem(2,2,"Шлем по образцу Гьермундбю","Норвегия, 950–975 гг.","Вымышленная копия шлема с защитой вокруг глаз. Дата относится к прототипу; игровой бонус не историческое измерение.","https://www.vikingtidsmuseet.no/english/viking-age/power-and-weapons/the-gjermundbu-helmet.html",22)
        };
    }
}
