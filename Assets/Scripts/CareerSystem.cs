using System;
using UnityEngine;

namespace SunsetState
{
    public sealed partial class GameRoot
    {
        public int[] CareerXP=new int[18];
        static readonly int[] RankThresholds={0,200,500,1000,1800,3000};
        static readonly string[][] RankNames={
            new[]{"Новичок","Работник","Специалист","Профессионал","Эксперт","Уважаемый житель"},
            new[]{"Кадет","Офицер","Сержант","Лейтенант","Капитан","Шеф полиции"},
            new[]{"Стажёр","Агент","Старший агент","Спецагент","Начальник отдела","Директор бюро"},
            new[]{"Рекрут","Рядовой","Ефрейтор","Сержант","Лейтенант","Полковник"},
            new[]{"Интерн","Фельдшер","Врач","Старший врач","Заведующий","Главный врач"},
            new[]{"Стажёр","Пожарный","Спасатель","Командир расчёта","Начальник смены","Начальник части"},
            new[]{"Новичок","Водитель","Опытный водитель","Профи","Диспетчер","Глава таксопарка"},
            new[]{"Стажёр","Водитель","Экспедитор","Дальнобойщик","Логист","Директор перевозок"},
            new[]{"Ученик","Автослесарь","Механик","Старший механик","Мастер","Владелец мастерской"},
            new[]{"Новичок района","Свояк","Боец","Авторитет","Правая рука","Лидер района"},
            new[]{"Помощник","Секретарь","Инспектор","Советник","Заместитель мэра","Мэр"},
            new[]{"Стажёр","Экзаменатор","Инструктор","Старший инструктор","Заведующий","Директор центра"},
            new[]{"Стажёр","Надзиратель","Старший надзиратель","Начальник караула","Заместитель начальника","Начальник тюрьмы"},
            new[]{"Новенький","Работник блока","Опытный","Доверенный","Старожил","Легенда блока"},
            new[]{"Мелкий вор","Налётчик","Бандит","Опасный преступник","Криминальный авторитет","Легенда подполья"},new[]{"Стажёр коллегии","Помощник адвоката","Адвокат","Старший защитник","Партнёр","Глава коллегии"},new[]{"Новичок семьи","Посыльный","Боец","Капореджиме","Консильери","Дон"},new[]{"Кандидат","Избранный губернатор","Глава кабинета","Реформатор","Лидер штата","Почётный губернатор"}};
        public int RankFor(int role){int rank=0;for(int i=1;i<RankThresholds.Length;i++)if(CareerXP[role]>=RankThresholds[i])rank=i;return rank;}
        public string RankTitle=>RankNames[RoleIndex][RankFor(RoleIndex)];
        public int MissionPay(int basePay)=>Mathf.RoundToInt(basePay*(1+RankFor(RoleIndex)*.1f+((StateLaws&2)!=0?.1f:0)));
        public void AwardXP(int amount)
        {
            int previous=RankFor(RoleIndex);CareerXP[RoleIndex]=Mathf.Min(1000000,CareerXP[RoleIndex]+amount);
            if(RankFor(RoleIndex)>previous)Notify("ПОВЫШЕНИЕ: "+RankTitle+" · надбавка "+RankFor(RoleIndex)*10+"%");
        }
        void DrawCareer(float left,float h)
        {
            int rank=RankFor(RoleIndex);Label(left,90,1050,45,"КАРЬЕРА / "+Roles[RoleIndex].Name.ToUpper(),heading);
            GUI.color=Roles[RoleIndex].Color;Label(left,155,1050,80,RankTitle,title);GUI.color=Color.white;
            Label(left,245,1000,50,"Опыт: "+CareerXP[RoleIndex]+" XP · Надбавка к оплате заданий: +"+rank*10+"%\n100 XP за подработку · 220 XP за сюжетную главу · 35 XP за задержание",body);
            for(int i=0;i<6;i++)
            {
                float y=325+i*46;RectBox(new Rect(left,y,1080,39),i<=rank?new Color(.17f,.26f,.16f):new Color(.09f,.14f,.11f));
                Label(left+16,y+8,690,30,(i<=rank?"✓ ":"  ")+(i+1)+". "+RankNames[RoleIndex][i],body);
                Label(left+750,y+8,290,30,RankThresholds[i]+" XP  /  +"+i*10+"% к оплате",small);
            }
            Label(left,h-75,1080,40,"Повышение происходит автоматически. Опыт и звание каждой роли сохраняются отдельно. Tab — сменить роль.",small);
        }
        void LoadCareer(int[] xp){CareerXP=xp??new int[Roles.Length];Array.Resize(ref CareerXP,Roles.Length);for(int i=0;i<CareerXP.Length;i++)CareerXP[i]=Mathf.Clamp(CareerXP[i],0,1000000);}
    }
}
