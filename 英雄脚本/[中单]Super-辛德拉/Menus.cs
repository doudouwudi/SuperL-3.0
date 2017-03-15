using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using static Dark_Syndra.Skins;
using EloBuddy.SDK;

namespace Dark_Syndra
{
    internal class Menus
    {
        public const string DrawingsMenuId = "drawingsmenuid";
        public const string MiscMenuId = "miscmenuid";
        public static Menu FirstMenu;
        public static Menu DrawingsMenu;
        public static Menu ComboMenu;
        public static Menu HarassMenu;
        public static Menu LaneClearMenu;
        public static Menu FleeMenu;
        public static Menu MiscMenu;
        public static Menu KillStealMenu;

        public static ColorSlide QColorSlide;
        public static ColorSlide WColorSlide;
        public static ColorSlide EColorSlide;
        public static ColorSlide QEColorSlide;
        public static ColorSlide RColorSlide;
        public static ColorSlide DamageIndicatorColorSlide;

        public static void CreateMenu()
        {

            FirstMenu = MainMenu.AddMenu("Super-辛德拉",
                Player.Instance.ChampionName.ToLower() + "Syndra");
            ComboMenu = FirstMenu.AddSubMenu("• 连招 ");
            HarassMenu = FirstMenu.AddSubMenu("• 骚扰");
            LaneClearMenu = FirstMenu.AddSubMenu("• 清线");
            FleeMenu = FirstMenu.AddSubMenu("• 逃跑");
            KillStealMenu = FirstMenu.AddSubMenu("• 抢头");
            DrawingsMenu = FirstMenu.AddSubMenu("• 线圈", DrawingsMenuId);
            MiscMenu = FirstMenu.AddSubMenu("• 其他", MiscMenuId);


            ComboMenu.AddGroupLabel("连招设置");
            ComboMenu.Add("Q", new CheckBox("- 使用 Q"));
            ComboMenu.Add("W", new CheckBox("- 使用 W"));
            ComboMenu.Add("QE", new CheckBox("- 使用 Q - E"));
            ComboMenu.Add("R", new CheckBox("- 使用 R"));
            ComboMenu.AddSeparator();
            ComboMenu.AddLabel("R 对谁使用");
            foreach (var Enemy in EntityManager.Heroes.Enemies)
            {
                ComboMenu.Add(Enemy.ChampionName, new CheckBox("R 使用在" + Enemy.ChampionName));
            }
            ComboMenu.AddSeparator();
            ComboMenu.Add("Ignite", new CheckBox("- 使用点燃"));

            //ComboMenu.AddGroupLabel("Summoner Settings");
            //ComboMenu.Add("Smite", new CheckBox("- Use Smite"));
            //ComboMenu.Add("Ignite", new CheckBox("- Use Ignite"));

            HarassMenu.AddGroupLabel("骚扰设置");
            HarassMenu.Add("Q", new CheckBox("- 使用 Q"));
            HarassMenu.Add("W", new CheckBox("- 使用 W"));
            HarassMenu.Add("Qe", new CheckBox("- 使用 Q - E"));
            HarassMenu.Add("manaSlider", new Slider ("蓝量高于 [{0}%] 开启骚扰", 50, 0 , 100));

            HarassMenu.AddGroupLabel("自动骚扰");
            HarassMenu.Add("AutoQ", new CheckBox("- Q",false));
            HarassMenu.Add("AutoW", new CheckBox("- W", false));
            HarassMenu.AddLabel("*任何时候都自动释放技能骚扰*");
            //HarassMenu.AddLabel("*Autoharass will come soon*");
            //HarassMenu.AddLabel("*Autoharass will come soon*");


            LaneClearMenu.AddGroupLabel("清线设置");
            LaneClearMenu.Add("Q", new CheckBox("- 使用 Q"));
            LaneClearMenu.Add("W", new CheckBox("- 使用 W"));
            LaneClearMenu.Add("E", new CheckBox("- 使用 E"));
            LaneClearMenu.Add("manaSlider", new Slider("蓝量高于 [{0}%] 使用技能清线", 50, 0, 100));
            LaneClearMenu.AddSeparator();
            LaneClearMenu.AddGroupLabel("打野设置");
            LaneClearMenu.Add("QJungle", new CheckBox("- 使用 Q"));
            LaneClearMenu.Add("WJungle", new CheckBox("- 使用 W"));
            LaneClearMenu.Add("EJungle", new CheckBox("- 使用 E", false));
            LaneClearMenu.Add("ManaSliderJungle", new Slider("蓝量高于 [{0}%] 使用技能打野", 50, 0, 100));

            KillStealMenu.AddGroupLabel("抢头设置");
            KillStealMenu.Add("Q", new CheckBox("- 使用 Q"));
            KillStealMenu.Add("W", new CheckBox("- 使用 W"));
            KillStealMenu.Add("E", new CheckBox("- 使用 E"));
            KillStealMenu.Add("R", new CheckBox("- 使用 R",false));


            FleeMenu.AddGroupLabel("逃跑设置");
            FleeMenu.Add("E", new CheckBox("- 使用 Q - E 推鼠标位置"));
            FleeMenu.AddLabel("* 鼠标必须在E范围内");
            MiscMenu.AddGroupLabel("其他设置");
            MiscMenu.Add("Interrupt", new CheckBox("- 自动打断"));
            MiscMenu.Add("Gapcloser", new CheckBox("- 防突进"));

            MiscMenu.AddGroupLabel("修改皮肤");

            var skinList = SkinsDB.FirstOrDefault(list => list.Champ == Player.Instance.Hero);
            if (skinList != null)
            {
                MiscMenu.Add("SkinComboBox", new ComboBox("更换皮肤：", skinList.Skins));
                MiscMenu.Get<ComboBox>("skinComboBox").OnValueChange +=
                    delegate (ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
                    {
                        Player.Instance.SetSkinId(sender.CurrentValue);
                    };
            }

            DrawingsMenu.AddGroupLabel("线圈设置");
            DrawingsMenu.Add("readyDraw", new CheckBox(" - 只在技能冷却完毕时绘制线圈"));
            DrawingsMenu.Add("damageDraw", new CheckBox(" - 显示伤害指示器"));
            DrawingsMenu.Add("perDraw", new CheckBox(" - 显示百分比伤害"));
            DrawingsMenu.Add("statDraw", new CheckBox(" - 显示伤害统计", false));
            DrawingsMenu.AddGroupLabel("Spells");
            DrawingsMenu.Add("readyDraw", new CheckBox(" - Draw Spell Range only if Spell is Ready."));
            DrawingsMenu.Add("qDraw", new CheckBox("- 画出范围 Q"));
            DrawingsMenu.Add("wDraw", new CheckBox("- 画出范围 W", false));
            DrawingsMenu.Add("eDraw", new CheckBox("- 画出范围 E", false));
            DrawingsMenu.Add("qeDraw", new CheckBox("- 画出范围 QE", false));
            DrawingsMenu.Add("rDraw", new CheckBox("- 画出范围 R", false));
            DrawingsMenu.AddLabel("技能冷却完毕画出技能线圈");
            DrawingsMenu.AddGroupLabel("Drawings Color");
            QColorSlide = new ColorSlide(DrawingsMenu, "qColor", Color.CornflowerBlue, "Q 颜色:");
            WColorSlide = new ColorSlide(DrawingsMenu, "wColor", Color.White, "W 颜色:");
            EColorSlide = new ColorSlide(DrawingsMenu, "eColor", Color.Coral, "E 颜色:");
            RColorSlide = new ColorSlide(DrawingsMenu, "rColor", Color.Red, "R 颜色:");
            DamageIndicatorColorSlide = new ColorSlide(DrawingsMenu, "healthColor", Color.Gold,
                "伤害显示器颜色:");

            MiscMenu.AddGroupLabel("自动加点");
            MiscMenu.Add("activateAutoLVL", new CheckBox("开启自动加点", false));
            MiscMenu.AddLabel("自动升级大招");
            MiscMenu.Add("firstFocus", new ComboBox("主点技能", new List<string> { "Q", "W", "E" }));
            MiscMenu.Add("secondFocus", new ComboBox("副点技能", new List<string> { "Q", "W", "E" }, 1));
            MiscMenu.Add("thirdFocus", new ComboBox("最后升级", new List<string> { "Q", "W", "E" }, 2));
            MiscMenu.Add("delaySlider", new Slider("加点延迟", 200, 150, 500));
        }
    }
}