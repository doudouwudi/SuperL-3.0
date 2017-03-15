#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Sivir.cs" company="EloBuddy">
// 
// Marksman Master
// Copyright (C) 2016 by gero
// All rights reserved
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/. 
// </copyright>
// <summary>
// 
// Email: geroelobuddy@gmail.com
// PayPal: geroelobuddy@gmail.com
// </summary>
// ---------------------------------------------------------------------
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Constants;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using EloBuddy.SDK.Rendering;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Sivir
{
    internal class Sivir : ChampionPlugin
    {
        protected static Spell.Skillshot Q { get; }
        protected static Spell.Active W { get; }
        protected static Spell.Active E { get; }
        protected static Spell.Active R { get; }

        internal static Menu ComboMenu { get; set; }
        internal static Menu HarassMenu { get; set; }
        internal static Menu LaneClearMenu { get; set; }
        internal static Menu DrawingsMenu { get; set; }
        internal static Menu SpellBlockerMenu { get; set; }

        private static readonly ColorPicker[] ColorPicker;
        private static bool _changingRangeScan;

        protected static bool IsPostAttack { get; private set; }
        protected static bool IsPreAttack { get; private set; }

        protected static MissileClient QMissileClient { get; private set; }

        static Sivir()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 1200, SkillShotType.Linear, 250, 1350, 90)
            {
                AllowedCollisionCount = int.MaxValue
            };
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Active(SpellSlot.E);
            R = new Spell.Active(SpellSlot.R);

            ColorPicker = new ColorPicker[1];

            ColorPicker[0] = new ColorPicker("SivirQ", new ColorBGRA(243, 109, 160, 255));

            ChampionTracker.Initialize(ChampionTrackerFlags.LongCastTimeTracker | ChampionTrackerFlags.PostBasicAttackTracker);

            BlockableSpells.Initialize();
            BlockableSpells.OnBlockableSpell += BlockableSpells_OnBlockableSpell;

            Game.OnPostTick += args => IsPostAttack = false;

            Orbwalker.OnPreAttack += (sender, args) =>
            {
                IsPreAttack = true;
            };

            ChampionTracker.OnPostBasicAttack += (sender, args) =>
            {
                if (!args.Sender.IsMe)
                    return;

                IsPostAttack = true;
                IsPreAttack = false;
            };

            ChampionTracker.OnLongSpellCast += ChampionTracker_OnLongSpellCast;

            GameObject.OnCreate += (sender, args) =>
            {
                var missile = sender as MissileClient;

                if (missile != null && missile.SpellCaster.IsMe &&
                    (missile.SData.Name.Equals("SivirQMissile", StringComparison.CurrentCultureIgnoreCase) ||
                     missile.SData.Name.Equals("SivirQMissileReturn", StringComparison.CurrentCultureIgnoreCase)))
                {
                    QMissileClient = missile;
                }
            };

            GameObject.OnDelete += (sender, args) =>
            {
                var missile = sender as MissileClient;

                if (missile != null && missile.SpellCaster.IsMe &&
                    (missile.SData.Name.Equals("SivirQMissile", StringComparison.CurrentCultureIgnoreCase) ||
                     missile.SData.Name.Equals("SivirQMissileReturn", StringComparison.CurrentCultureIgnoreCase)))
                {
                    QMissileClient = null;
                }
            };
        }

        private static void ChampionTracker_OnLongSpellCast(object sender, OnLongSpellCastEventArgs e)
        {
            if (e.IsTeleport)
                return;

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && Q.IsReady() && Settings.Combo.UseQ && (Player.Instance.Mana - 60 > (R.IsReady() ? 100 : 0)))
            {
                Q.CastMinimumHitchance(e.Sender, 65);
            }
        }

        private static void BlockableSpells_OnBlockableSpell(AIHeroClient sender,
            BlockableSpells.OnBlockableSpellEventArgs args)
        {
            if (!args.Enabled || !E.IsReady())
                return;

            E.Cast();
        }

        protected override void OnDraw()
        {
            if (_changingRangeScan)
                Circle.Draw(Color.White,
                    LaneClearMenu["Plugins.Sivir.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue, Player.Instance);

            if (Settings.Drawings.DrawQ && (!Settings.Drawings.DrawSpellRangesWhenReady || Q.IsReady()))
                Circle.Draw(ColorPicker[0].Color, Q.Range, Player.Instance);

            if (QMissileClient == null || !Settings.Drawings.DrawQMissile)
                return;

            new Geometry.Polygon.Rectangle(Player.Instance.Position, QMissileClient.GetMissileFixedYPosition(), 145)
                .Draw(System.Drawing.Color.White, 3);

            new Geometry.Polygon.Rectangle(Player.Instance.Position, QMissileClient.GetMissileFixedYPosition(), 90)
                .Draw(System.Drawing.Color.YellowGreen, 2);
        }
        protected override void OnInterruptible(AIHeroClient sender, InterrupterEventArgs args)
        {
        }

        protected override void OnGapcloser(AIHeroClient sender, GapCloserEventArgs args)
        {
        }

        protected override void CreateMenu()
        {
            ComboMenu = MenuManager.Menu.AddSubMenu("Combo");
            ComboMenu.AddGroupLabel("Combo mode settings for Sivir addon");

            ComboMenu.AddLabel("Boomerang Blade (Q) settings :");
            ComboMenu.Add("Plugins.Sivir.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Ricochet (W) settings :");
            ComboMenu.Add("Plugins.Sivir.ComboMenu.UseW", new CheckBox("Use W"));
            ComboMenu.AddSeparator(5);

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("Harass mode settings for Sivir addon");

            HarassMenu.AddLabel("Boomerang Blade (Q) settings :");
            HarassMenu.Add("Plugins.Sivir.HarassMenu.UseQ", new CheckBox("Use Q"));
            HarassMenu.Add("Plugins.Sivir.HarassMenu.AutoHarass", new CheckBox("Auto harass immobile targets"));
            HarassMenu.Add("Plugins.Sivir.HarassMenu.MinManaQ", new Slider("Min mana percentage ({0}%) to use Q", 80, 1));
            HarassMenu.AddSeparator(5);

            HarassMenu.AddLabel("Ricochet (W) settings :");
            HarassMenu.Add("Plugins.Sivir.HarassMenu.UseW", new CheckBox("Use W"));
            HarassMenu.Add("Plugins.Sivir.HarassMenu.MinManaW", new Slider("Min mana percentage ({0}%) to use W", 80, 1));
            HarassMenu.AddSeparator(5);

            LaneClearMenu = MenuManager.Menu.AddSubMenu("Clear");
            LaneClearMenu.AddGroupLabel("Clear mode settings for Sivir addon");

            LaneClearMenu.AddLabel("Basic settings :");
            LaneClearMenu.Add("Plugins.Sivir.LaneClearMenu.EnableLCIfNoEn", new CheckBox("Enable lane clear only if no enemies nearby"));
            var scanRange = LaneClearMenu.Add("Plugins.Sivir.LaneClearMenu.ScanRange", new Slider("Range to scan for enemies", 1500, 300, 2500));
            scanRange.OnValueChange += (a, b) =>
            {
                _changingRangeScan = true;
                Core.DelayAction(() =>
                {
                    if (!scanRange.IsLeftMouseDown && !scanRange.IsMouseInside)
                    {
                        _changingRangeScan = false;
                    }
                }, 2000);
            };
            LaneClearMenu.Add("Plugins.Sivir.LaneClearMenu.AllowedEnemies", new Slider("Allowed enemies amount", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Boomerang Blade (Q) settings :");
            LaneClearMenu.Add("Plugins.Sivir.LaneClearMenu.UseQInLaneClear", new CheckBox("Use Q in Lane Clear"));
            LaneClearMenu.Add("Plugins.Sivir.LaneClearMenu.UseQInJungleClear", new CheckBox("Use Q in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Sivir.LaneClearMenu.MinManaQ", new Slider("Min mana percentage ({0}%) to use Q", 80, 1));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Ricochet (W) settings :");
            LaneClearMenu.Add("Plugins.Sivir.LaneClearMenu.UseWInLaneClear", new CheckBox("Use W in Lane Clear"));
            LaneClearMenu.Add("Plugins.Sivir.LaneClearMenu.UseWInJungleClear", new CheckBox("Use W in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Sivir.LaneClearMenu.MinManaW", new Slider("Min mana percentage ({0}%) to use W", 80, 1));

            BlockableSpells.BuildMenu();

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawings settings for Sivir addon");

            DrawingsMenu.AddLabel("Basic settings :");
            DrawingsMenu.Add("Plugins.Sivir.DrawingsMenu.DrawSpellRangesWhenReady",
                new CheckBox("Draw spell ranges only when they are ready"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Boomerang Blade (Q) settings :");
            DrawingsMenu.Add("Plugins.Sivir.DrawingsMenu.DrawQ", new CheckBox("Draw Q range", false));
            DrawingsMenu.Add("Plugins.Sivir.DrawingsMenu.DrawQMissile", new CheckBox("Draw Q missile"));
            DrawingsMenu.Add("Plugins.Twitch.DrawingsMenu.DrawQColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[0].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
        }

        protected override void PermaActive()
        {
            Modes.PermaActive.Execute();
        }

        protected override void ComboMode()
        {
            Modes.Combo.Execute();
        }

        protected override void HarassMode()
        {
            Modes.Harass.Execute();
        }

        protected override void LaneClear()
        {
            Modes.LaneClear.Execute();
        }

        protected override void JungleClear()
        {
            Modes.JungleClear.Execute();
        }

        protected override void LastHit()
        {
            Modes.LastHit.Execute();
        }

        protected override void Flee()
        {
            Modes.Flee.Execute();
        }

        protected static class Settings
        {
            internal static class Combo
            {
                public static bool UseQ => MenuManager.MenuValues["Plugins.Sivir.ComboMenu.UseQ"];

                public static bool UseW => MenuManager.MenuValues["Plugins.Sivir.ComboMenu.UseW"];
            }

            internal static class Harass
            {
                public static bool UseQ => MenuManager.MenuValues["Plugins.Sivir.HarassMenu.UseQ"];

                public static bool AutoHarass => MenuManager.MenuValues["Plugins.Sivir.HarassMenu.AutoHarass"];

                public static int MinManaQ => MenuManager.MenuValues["Plugins.Sivir.HarassMenu.MinManaQ", true];

                public static bool UseW => MenuManager.MenuValues["Plugins.Sivir.HarassMenu.UseW"];

                public static int MinManaW => MenuManager.MenuValues["Plugins.Sivir.HarassMenu.MinManaW", true];
            }

            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies => MenuManager.MenuValues["Plugins.Sivir.LaneClearMenu.EnableLCIfNoEn"];

                public static int ScanRange => MenuManager.MenuValues["Plugins.Sivir.LaneClearMenu.ScanRange", true];

                public static int AllowedEnemies => MenuManager.MenuValues["Plugins.Sivir.LaneClearMenu.AllowedEnemies", true];

                public static bool UseQInLaneClear => MenuManager.MenuValues["Plugins.Sivir.LaneClearMenu.UseQInLaneClear"];

                public static bool UseQInJungleClear => MenuManager.MenuValues["Plugins.Sivir.LaneClearMenu.UseQInJungleClear"];

                public static int MinManaQ => MenuManager.MenuValues["Plugins.Sivir.LaneClearMenu.MinManaQ", true];

                public static bool UseWInLaneClear => MenuManager.MenuValues["Plugins.Sivir.LaneClearMenu.UseWInLaneClear"];

                public static bool UseWInJungleClear => MenuManager.MenuValues["Plugins.Sivir.LaneClearMenu.UseWInJungleClear"];

                public static int WMinMana => MenuManager.MenuValues["Plugins.Sivir.LaneClearMenu.MinManaW", true];
            }

            internal static class Drawings
            {
                public static bool DrawSpellRangesWhenReady => MenuManager.MenuValues["Plugins.Sivir.DrawingsMenu.DrawSpellRangesWhenReady"];

                public static bool DrawQ => MenuManager.MenuValues["Plugins.Sivir.DrawingsMenu.DrawQ"];

                public static bool DrawQMissile => MenuManager.MenuValues["Plugins.Sivir.DrawingsMenu.DrawQMissile"];
            }
        }

        protected static class BlockableSpells
        {
            private static readonly HashSet<BlockableSpellData> BlockableSpellsHashSet = new HashSet<BlockableSpellData>
            {
                new BlockableSpellData(Champion.Alistar, "[Q] Headbutt", SpellSlot.W),
                new BlockableSpellData(Champion.Amumu, "[R] Curse of the Sad Mummy", SpellSlot.R) {NeedsAdditionalLogics = true},
                new BlockableSpellData(Champion.Anivia, "[E] Frostbite", SpellSlot.E),
                new BlockableSpellData(Champion.Akali, "[Q] Mark of the Assassin", SpellSlot.Q),
                new BlockableSpellData(Champion.Akali, "[E] Crescent Slash", SpellSlot.E) {NeedsAdditionalLogics = true},
                new BlockableSpellData(Champion.Akali, "[R] Shadow Dance", SpellSlot.R),
                new BlockableSpellData(Champion.Annie, "[Q] Disintegrate", SpellSlot.Q),
                new BlockableSpellData(Champion.Azir, "[R] Emperor's Divide", SpellSlot.R) {NeedsAdditionalLogics = true},
                new BlockableSpellData(Champion.Bard, "[R] Tempered Fate", SpellSlot.R) {NeedsAdditionalLogics = true},
                new BlockableSpellData(Champion.Blitzcrank, "[Q] Rocket Grab", SpellSlot.Q) {NeedsAdditionalLogics = true},
                new BlockableSpellData(Champion.Blitzcrank, "[E] Power Fist", SpellSlot.E)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "powerfistattack"
                },
                new BlockableSpellData(Champion.Blitzcrank, "[R] Static Field", SpellSlot.R) {NeedsAdditionalLogics = true},
                new BlockableSpellData(Champion.Brand, "[R] Pyroclasm", SpellSlot.R),
                new BlockableSpellData(Champion.Braum, "[Passive] Concussive Blows", SpellSlot.Unknown)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "braumbasicattackpassiveoverride"
                },
                new BlockableSpellData(Champion.Caitlyn, "[R] Ace in the Hole", SpellSlot.R) {NeedsAdditionalLogics = true},
                new BlockableSpellData(Champion.Cassiopeia, "[R] Petrifying Gaze", SpellSlot.R) {NeedsAdditionalLogics = true},
                new BlockableSpellData(Champion.Chogath, "[R] Feast", SpellSlot.R),
                new BlockableSpellData(Champion.Darius, "[R] Noxian Guillotine", SpellSlot.R),
                new BlockableSpellData(Champion.Diana, "[E] Moonfall", SpellSlot.E) {NeedsAdditionalLogics = true},
                new BlockableSpellData(Champion.Diana, "[R] Lunar Rush", SpellSlot.R),
                new BlockableSpellData(Champion.Evelynn, "[E] Ravage", SpellSlot.E),
                new BlockableSpellData(Champion.Evelynn, "[R] Agony's Embrace", SpellSlot.R) {NeedsAdditionalLogics = true},
                new BlockableSpellData(Champion.FiddleSticks, "[Q] Terrify", SpellSlot.Q),
                new BlockableSpellData(Champion.Fiora, "[R] Grand Challenge", SpellSlot.R),
                new BlockableSpellData(Champion.Fizz, "[Q] Urchin Strike", SpellSlot.Q),
                new BlockableSpellData(Champion.Galio, "[R] Idol of Durand", SpellSlot.R),
                new BlockableSpellData(Champion.Gangplank, "[Q] Parrrley", SpellSlot.Q),
                new BlockableSpellData(Champion.Garen, "[Q] Decisive Strike", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "garenqattack"
                },
                new BlockableSpellData(Champion.Garen, "[R] Demacian Justice", SpellSlot.R),
                new BlockableSpellData(Champion.Gnar, "[R] GNAR!", SpellSlot.R),
                new BlockableSpellData(Champion.Gragas, "[W] Drunken Rage", SpellSlot.W)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "drunkenrage"
                },
                new BlockableSpellData(Champion.Graves, "[R] Collateral Damage", SpellSlot.R)
                {
                    NeedsAdditionalLogics = true
                },
                new BlockableSpellData(Champion.Hecarim, "[E] Devastating Charge", SpellSlot.E)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "hecarimrampattack"
                },
                new BlockableSpellData(Champion.Hecarim, "[R] Onslaught of Shadows", SpellSlot.R)
                {
                    NeedsAdditionalLogics = true
                },
                new BlockableSpellData(Champion.Illaoi, "[W] Harsh Lesson", SpellSlot.W)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "illaoiwattack"
                },
                new BlockableSpellData(Champion.Irelia, "[E] Equilibrium Strike", SpellSlot.E),
                new BlockableSpellData(Champion.Janna, "[W] Zephyr", SpellSlot.W),
                new BlockableSpellData(Champion.Janna, "[R] Monsoon", SpellSlot.R) {NeedsAdditionalLogics = true},
                new BlockableSpellData(Champion.JarvanIV, "[E-Q] Demacian Standard => Dragon Strike combo", SpellSlot.Q) {NeedsAdditionalLogics = true},
                new BlockableSpellData(Champion.JarvanIV, "[R] Cataclysm", SpellSlot.R),
                new BlockableSpellData(Champion.Jax, "[Q] Leap Strike", SpellSlot.Q),
                new BlockableSpellData(Champion.Jax, "[W] Empower", SpellSlot.W)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "jaxempowertwo"
                },
                new BlockableSpellData(Champion.Jayce, "[Q] To The Skies!", SpellSlot.Q),
                new BlockableSpellData(Champion.Jayce, "[E] Thundering Blow", SpellSlot.E),
                new BlockableSpellData(Champion.Jhin, "[Passive] 4th auto attack", SpellSlot.Unknown)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "jhinpassiveattack"
                },
                new BlockableSpellData(Champion.Jhin, "[Q] Dancing Grenade", SpellSlot.Q),
                new BlockableSpellData(Champion.Kalista, "[E] Rend", SpellSlot.E)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "kalistaexpungemarker"
                },
                //new BlockableSpellData(Champion.Karma, "", SpellSlot.W)
                //{
                    //NeedsAdditionalLogics = true,
                    //AdditionalDelay = 1800
                //},
                new BlockableSpellData(Champion.Karthus, "[R] Requiem", SpellSlot.R)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalDelay = 2800
                },
                new BlockableSpellData(Champion.Kassadin, "[Q] Null Sphere", SpellSlot.Q),
                new BlockableSpellData(Champion.Kassadin, "[W] Nether Blade", SpellSlot.W)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "netherblade"
                },
                new BlockableSpellData(Champion.Katarina, "[Q] Bouncing Blades", SpellSlot.Q),
                new BlockableSpellData(Champion.Katarina, "[Q] Bouncing Blades => Empowered auto attack", SpellSlot.Unknown)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "katarinaqmark"
                },
                new BlockableSpellData(Champion.Kayle, "[Q] Reckoning", SpellSlot.Q),
                new BlockableSpellData(Champion.Kennen, "[Passive] Mark of the Storm", SpellSlot.Unknown)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "kennenmegaproc"
                },
                new BlockableSpellData(Champion.Kennen, "[W] Electrical Surge", SpellSlot.W),
                new BlockableSpellData(Champion.Khazix, "[Q] Taste Their Fear", SpellSlot.Q),
                new BlockableSpellData(Champion.Kindred, "[E] Mounting Dread", SpellSlot.E),
                new BlockableSpellData(Champion.Kled, "[Q] Beartrap on a Rope", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "kledqmark"
                },
                new BlockableSpellData(Champion.KogMaw, "[Passive] Icathian Surprise", SpellSlot.Unknown)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalDelay = 3800
                },
                new BlockableSpellData(Champion.Leblanc, "[R] Mimic", SpellSlot.R),
                new BlockableSpellData(Champion.LeeSin, "[R] Dragon's Rage", SpellSlot.R),
                new BlockableSpellData(Champion.Leona, "[Q] Shield of Daybreak", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "leonashieldofdaybreakattack"
                },
                new BlockableSpellData(Champion.Lissandra, "[W] Ring of Frost", SpellSlot.W)
                {
                    NeedsAdditionalLogics = true
                },
                new BlockableSpellData(Champion.Lissandra, "[R] Frozen Tomb", SpellSlot.R),
                new BlockableSpellData(Champion.Lucian, "[Q] Piercing Light", SpellSlot.Q),
                new BlockableSpellData(Champion.Lulu, "[W] Whimsy (polymorph)", SpellSlot.W),
                new BlockableSpellData(Champion.Malphite, "[Q] Seismic Shard", SpellSlot.Q),
                new BlockableSpellData(Champion.Malphite, "[R] Unstoppable Force", SpellSlot.R)
                {
                    NeedsAdditionalLogics = true
                },
                new BlockableSpellData(Champion.Malzahar, "[R] Malefic Visions", SpellSlot.R),
                new BlockableSpellData(Champion.Maokai, "[W] Twisted Advance", SpellSlot.W),
                new BlockableSpellData(Champion.MasterYi, "[Q] Alpha Strike", SpellSlot.Q),
                new BlockableSpellData(Champion.MissFortune, "[Q] Double Up", SpellSlot.Q),
                new BlockableSpellData(Champion.Mordekaiser, "[Q] => 1st attack ", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "mordekaiserqattack"
                },
                new BlockableSpellData(Champion.Mordekaiser, "[Q] => 2nd attack ", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "mordekaiserqattack1"
                },
                new BlockableSpellData(Champion.Mordekaiser, "[Q] => 3rd attack ", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "mordekaiserqattack2"
                },
                new BlockableSpellData(Champion.Mordekaiser, "[R] Children of the Grave", SpellSlot.R),
                new BlockableSpellData(Champion.Morgana, "[R] Soul Shackles", SpellSlot.R)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "soulshackles"
                },
                new BlockableSpellData(Champion.Nautilus, "[R] Depth Charge", SpellSlot.R) {NeedsAdditionalLogics = true},
                new BlockableSpellData(Champion.Nasus, "[Q] Siphoning Strike", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "nasusqattack"
                },
                new BlockableSpellData(Champion.Nasus, "[W] Wither", SpellSlot.W),
                new BlockableSpellData(Champion.Nami, "[W] Ebb and Flow", SpellSlot.W),
                new BlockableSpellData(Champion.Nidalee, "[Q] Takedown", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "nidaleetakedownattack"
                },
                new BlockableSpellData(Champion.Nocturne, "[E] Unspeakable Horror", SpellSlot.E)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "nocturneunspeakablehorror"
                },
                new BlockableSpellData(Champion.Nunu, "[E] Ice Blast", SpellSlot.E),
                new BlockableSpellData(Champion.Olaf, "[E] Reckless Swing", SpellSlot.E),
                new BlockableSpellData(Champion.Pantheon, "[W] Aegis of Zeonia", SpellSlot.W),
                new BlockableSpellData(Champion.Poppy, "[E] Heroic Charge", SpellSlot.E),
                new BlockableSpellData(Champion.Quinn, "[E] Vault", SpellSlot.E),
                new BlockableSpellData(Champion.Rammus, "[E] Puncturing Taunt", SpellSlot.E),
                new BlockableSpellData(Champion.Renekton, "[W] Cull the Meek", SpellSlot.W)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "renektonexecute"
                },
                new BlockableSpellData(Champion.Renekton, "[Empowered W] Cull the Meek", SpellSlot.W)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "renektonsuperexecute"
                },
                new BlockableSpellData(Champion.Rengar, "[Q] Savagery", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "rengarqbase"
                },
                new BlockableSpellData(Champion.Rengar, "[Q] Savagery => Empowered", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "rengarqemp"
                },
                new BlockableSpellData(Champion.Ryze, "[W] Rune Prison", SpellSlot.W),
                new BlockableSpellData(Champion.Sejuani, "[E] Flail of the Northern Winds", SpellSlot.E)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "sejuanifrost"
                },
                new BlockableSpellData(Champion.Shaco, "[E] Two-Shiv Poison", SpellSlot.E),
                new BlockableSpellData(Champion.Shyvana, "[Q] Twin Bite", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "shyvanadoubleattackhit"
                },
                new BlockableSpellData(Champion.Singed, "[E] Fling", SpellSlot.E),
                new BlockableSpellData(Champion.Skarner, "[E] Fracture => Empowered auto attack", SpellSlot.E)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "skarnerpassiveattack"
                },
                new BlockableSpellData(Champion.Skarner, "[R] Impale", SpellSlot.R),
                new BlockableSpellData(Champion.Swain, "[E] Torment", SpellSlot.E),
                new BlockableSpellData(Champion.Sona, "[Q] Hymn of Valor", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true
                },
                new BlockableSpellData(Champion.Syndra, "[R] Unleashed Power", SpellSlot.R),
                new BlockableSpellData(Champion.TahmKench, "[W] Devour", SpellSlot.W),
                new BlockableSpellData(Champion.Talon, "[Q] Noxian Diplomacy", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "talonnoxiandiplomacyattack"
                },
                new BlockableSpellData(Champion.Teemo, "[Q] Blinding Dart", SpellSlot.Q),
                new BlockableSpellData(Champion.Tristana, "[E] Explosive Charge", SpellSlot.E)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalDelay = 3800
                },
                new BlockableSpellData(Champion.Tristana, "[R] Buster Shot", SpellSlot.R),
                new BlockableSpellData(Champion.Trundle, "[Q] Chomp", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "trundleq"
                },
                new BlockableSpellData(Champion.Trundle, "[R] Subjugate", SpellSlot.R),
                new BlockableSpellData(Champion.TwistedFate, "[W] Pick A Card", SpellSlot.W)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "goldcardpreattack"
                },
                new BlockableSpellData(Champion.Twitch, "[E] Contaminate", SpellSlot.E),
                new BlockableSpellData(Champion.Udyr, "[E] Bear Stance", SpellSlot.E)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "udyrbearattack"
                },
                new BlockableSpellData(Champion.Urgot, "[R] Hyper-Kinetic Position Reverser", SpellSlot.R),
                new BlockableSpellData(Champion.Vayne, "[E] Condemn", SpellSlot.E),
                new BlockableSpellData(Champion.Veigar, "[R] Primordial Burst", SpellSlot.R),
                new BlockableSpellData(Champion.Vi, "[R] Assault and Battery", SpellSlot.R) {NeedsAdditionalLogics = true},
                new BlockableSpellData(Champion.Viktor, "[Q] Siphon Power", SpellSlot.Q),
                new BlockableSpellData(Champion.Viktor, "[Q] Siphon Power => Empowered auto attack", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "viktorqbuff"
                },
                new BlockableSpellData(Champion.Vladimir, "[Q] Transfusion", SpellSlot.Q),
                new BlockableSpellData(Champion.Volibear, "[Q] Rolling Thunder", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "volibearqattack"
                },
                new BlockableSpellData(Champion.Volibear, "[W] Frenzy", SpellSlot.W),
                new BlockableSpellData(Champion.XinZhao, "[Q] Three Talon Strike", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "xenzhaothrust3"
                },
                new BlockableSpellData(Champion.XinZhao, "[R] Crescent Sweep", SpellSlot.R),
                new BlockableSpellData(Champion.MonkeyKing, "[Q] Crushing Blow", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "monkeykingqattack"
                },
                new BlockableSpellData(Champion.MonkeyKing, "[E] Nimbus Strike", SpellSlot.E),
                new BlockableSpellData(Champion.Warwick, "[Q] Hungering Strike", SpellSlot.Q),
                new BlockableSpellData(Champion.Warwick, "[R] Infinite Duress", SpellSlot.R)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "infiniteduresschannel"
                },
                new BlockableSpellData(Champion.Yasuo, "[E-Q] Sweeping Blade => Steel Tempest combo", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "yasuoq3w"
                },
                new BlockableSpellData(Champion.Yorick, "[Q] Last Rites", SpellSlot.Q)
                {
                    NeedsAdditionalLogics = true,
                    AdditionalBuffName = "yorickqattack"
                },
                new BlockableSpellData(Champion.Zed, "[R] Death Mark", SpellSlot.R) {NeedsAdditionalLogics = true, AdditionalDelay = 200},
                new BlockableSpellData(Champion.Zilean, "[E] Time Warp", SpellSlot.E)
            };

            public delegate void OnBlockableSpellEvent(AIHeroClient sender, OnBlockableSpellEventArgs args);

            public static event OnBlockableSpellEvent OnBlockableSpell;

            public static void Initialize()
            {
                BlockableSpellsHashSet.RemoveWhere(x => EntityManager.Heroes.Enemies.All(k => k.Hero != x.ChampionName));

                if (BlockableSpellsHashSet.Count <= 0)
                    return;

                Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
                Game.OnTick += Game_OnTick;
                Obj_AI_Base.OnBasicAttack += Obj_AI_Base_OnBasicAttack;
            }

            public static void BuildMenu()
            {
                if (BlockableSpellsHashSet.Count < 1)
                    return;

                SpellBlockerMenu = MenuManager.Menu.AddSubMenu("Spell blocker");
                SpellBlockerMenu.AddGroupLabel("Spell blocker settings for Sivir addon");
                SpellBlockerMenu.Add("Plugins.Sivir.SpellBlockerMenu.Enabled", new CheckBox("Enable Spell blocker"));

                SpellBlockerMenu.AddLabel("Spell blocker enabled for :");
                SpellBlockerMenu.AddSeparator(5);

                foreach (var enemy in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x=> BlockableSpellsHashSet.Any(k=>k.ChampionName == x.Hero)))
                {
                    SpellBlockerMenu.AddLabel(enemy.ChampionName + " :");

                    foreach (var spell in BlockableSpellsHashSet.Where(x => x.ChampionName == enemy.Hero))
                    {
                        SpellBlockerMenu.Add(
                            $"Plugins.Sivir.SpellBlockerMenu.Enabled.{spell.ChampionName}.{spell.SpellSlot}{spell.AdditionalBuffName}",
                            new CheckBox(spell.ChampionName + " | " + spell.SpellName));
                    }
                    SpellBlockerMenu.AddSeparator(2);
                }
            }

            public static bool IsEnabledFor(AIHeroClient unit, SpellSlot slot, string additionalName)
                =>
                    SpellBlockerMenu != null &&
                    SpellBlockerMenu["Plugins.Sivir.SpellBlockerMenu.Enabled"].Cast<CheckBox>().CurrentValue
                    && SpellBlockerMenu[$"Plugins.Sivir.SpellBlockerMenu.Enabled.{unit.ChampionName}.{slot}{additionalName}"] != null &&
                    SpellBlockerMenu[
                        $"Plugins.Sivir.SpellBlockerMenu.Enabled.{unit.ChampionName}.{slot}{additionalName}"]
                        .Cast<CheckBox>().CurrentValue;

            private static void Invoke(AIHeroClient sender, SpellSlot spellSlot, string additionalName, bool isAutoAttack, float additionalDelay)
            {
                OnBlockableSpell?.Invoke(sender,
                    new OnBlockableSpellEventArgs(sender.Hero, spellSlot, IsEnabledFor(sender, spellSlot, additionalName),
                        isAutoAttack, additionalDelay));
            }
            
            private static void Obj_AI_Base_OnBasicAttack(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
            {
                var enemy = sender as AIHeroClient;

                if (enemy == null || args.Target == null || !args.Target.IsMe)
                    return;

                if (enemy.Hero == Champion.Tristana)
                {
                    var buff = Player.Instance.Buffs.FirstOrDefault(x => x.Name.Equals("tristanaecharge", StringComparison.CurrentCultureIgnoreCase));

                    if (buff != null && buff.Count >= 3)
                    {
                        Invoke(enemy, SpellSlot.E, string.Empty, false, 0);
                        return;
                    }
                }

                if (enemy.Hero == Champion.Kennen && args.IsAutoAttack())
                {
                    var data = BlockableSpellsHashSet.FirstOrDefault(x => x.ChampionName == Champion.Kennen && x.SpellSlot == SpellSlot.Unknown);

                    if (data == null)
                        return;

                    var buff = Player.Instance.Buffs.FirstOrDefault(x => x.Name.Equals("kennenmarkofstorm", StringComparison.CurrentCultureIgnoreCase));

                    if (buff != null && (buff.Count == 2) && args.SData.Name.Equals(data.AdditionalBuffName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        Invoke(enemy, data.SpellSlot, data.AdditionalBuffName, true, data.AdditionalDelay);
                        return;
                    }
                }

                if (enemy.Hero == Champion.Katarina && args.IsAutoAttack())
                {
                    var data = BlockableSpellsHashSet.FirstOrDefault(x => x.ChampionName == Champion.Katarina && x.SpellSlot == SpellSlot.Unknown);

                    if (data == null)
                        return;

                    var buff = Player.Instance.Buffs.FirstOrDefault(x => x.Name.Equals(data.AdditionalBuffName, StringComparison.CurrentCultureIgnoreCase));

                    if (buff != null)
                    {
                        Invoke(enemy, data.SpellSlot, data.AdditionalBuffName, true, data.AdditionalDelay);
                        return;
                    }
                }

                if (enemy.Hero == Champion.Jax && args.IsAutoAttack())
                {
                    var data = BlockableSpellsHashSet.FirstOrDefault(x => x.ChampionName == Champion.Jax && x.SpellSlot == SpellSlot.W);

                    if (data == null)
                        return;

                    var buff = enemy.Buffs.FirstOrDefault(x => x.Name.Equals(data.AdditionalBuffName, StringComparison.CurrentCultureIgnoreCase));

                    if (buff != null)
                    {
                        Invoke(enemy, data.SpellSlot, data.AdditionalBuffName, true, data.AdditionalDelay);
                        return;
                    }
                }
                
                if (enemy.Hero == Champion.Renekton && args.IsAutoAttack())
                {
                    foreach (var data in
                            from data in
                                BlockableSpellsHashSet.Where(
                                    x => x.ChampionName == Champion.Renekton && x.SpellSlot == SpellSlot.W)
                            let buff = enemy.Buffs.FirstOrDefault(
                                    x =>
                                        x.Name.Equals(data.AdditionalBuffName, StringComparison.CurrentCultureIgnoreCase))
                            where buff != null
                            select data)
                    {
                        Invoke(enemy, data.SpellSlot, data.AdditionalBuffName, true, data.AdditionalDelay);
                        return;
                    }
                }

                if (enemy.Hero == Champion.Rengar && args.IsAutoAttack())
                {
                    foreach (
                        var data in
                            from data in BlockableSpellsHashSet.Where(
                                    x => x.ChampionName == Champion.Rengar && x.SpellSlot == SpellSlot.Q)
                            let buff =
                                enemy.Buffs.Find(
                                    x =>
                                        x.Name.Equals(data.AdditionalBuffName, StringComparison.CurrentCultureIgnoreCase))
                            where buff != null
                            select data)
                    {
                        Invoke(enemy, data.SpellSlot, data.AdditionalBuffName, true, data.AdditionalDelay);
                        return;
                    }
                }

                if (enemy.Hero == Champion.Kassadin && args.IsAutoAttack())
                {
                    var data = BlockableSpellsHashSet.FirstOrDefault(x => x.ChampionName == Champion.Kassadin && x.SpellSlot == SpellSlot.W);

                    if (data == null)
                        return;

                    var buff = enemy.Buffs.FirstOrDefault(x => x.Name.Equals(data.AdditionalBuffName, StringComparison.CurrentCultureIgnoreCase));

                    if (buff != null)
                    {
                        Invoke(enemy, data.SpellSlot, data.AdditionalBuffName, true, data.AdditionalDelay);
                        return;
                    }
                }

                foreach (var blockableSpellData in BlockableSpellsHashSet.Where(x => x.ChampionName == enemy.Hero && !string.IsNullOrWhiteSpace(x.AdditionalBuffName) && args.SData.Name.Equals(x.AdditionalBuffName, StringComparison.CurrentCultureIgnoreCase)))
                {
                    Invoke(enemy, blockableSpellData.SpellSlot, blockableSpellData.AdditionalBuffName, true, blockableSpellData.AdditionalDelay);
                }
            }

            private static void Game_OnTick(EventArgs args)
            {
                if (StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).Any(x => x.Hero == Champion.KogMaw))
                {
                    var enemy = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).FirstOrDefault(x => x.Hero == Champion.KogMaw);

                    if(enemy == null)
                        return;

                    var buff = enemy.Buffs.FirstOrDefault(x => x.Name.Equals("kogmawicathiansurprise", StringComparison.CurrentCultureIgnoreCase));

                    if (buff != null && ((buff.EndTime - Game.Time) * 1000 < 350) && (enemy.Distance(Player.Instance) < 370))
                    {
                        Invoke(enemy, SpellSlot.Unknown, string.Empty, false, 0);
                    }
                }
                if (StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).Any(x => x.Hero == Champion.Karthus))
                {
                    var buff = Player.Instance.Buffs.FirstOrDefault(x => x.Name.Equals("karthusfallenonetarget", StringComparison.CurrentCultureIgnoreCase));

                    if (buff != null && buff.Caster.GetType() == typeof(AIHeroClient) && (buff.EndTime - Game.Time) * 1000 < 350)
                    {
                        Invoke(buff.Caster as AIHeroClient, SpellSlot.R, string.Empty, false, 0);
                    }
                }
                if (StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).Any(x => x.Hero == Champion.Tristana))
                {
                    var buff = Player.Instance.Buffs.FirstOrDefault(x => x.Name.Equals("tristanaecharge", StringComparison.CurrentCultureIgnoreCase));

                    if (buff != null && buff.Caster.GetType() == typeof(AIHeroClient) && ((buff.EndTime - Game.Time) * 1000 < 350))
                    {
                        Invoke(buff.Caster as AIHeroClient, SpellSlot.E, string.Empty, false, 0);
                    }
                }

                if (StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).Any(x => x.Hero == Champion.Morgana))
                {
                    var data = BlockableSpellsHashSet.FirstOrDefault(x => x.ChampionName == Champion.Morgana && x.SpellSlot == SpellSlot.R);

                    if (data == null)
                        return;

                    var buff = Player.Instance.Buffs.FirstOrDefault(x => x.Name.Equals(data.AdditionalBuffName, StringComparison.CurrentCultureIgnoreCase));

                    if (buff != null && ((buff.EndTime - Game.Time) * 1000 < 350))
                    {
                        var morgana = buff.Caster as AIHeroClient;

                        if (morgana == null)
                            return;

                        Invoke(morgana, SpellSlot.R, data.AdditionalBuffName, false, 0);
                    }
                }

                if (StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).Any(x => x.Hero == Champion.Kled))
                {
                    var data = BlockableSpellsHashSet.FirstOrDefault(x => x.ChampionName == Champion.Kled && x.SpellSlot == SpellSlot.Q);

                    if (data == null)
                        return;

                    var buff = Player.Instance.Buffs.FirstOrDefault(x => x.Name.Equals(data.AdditionalBuffName, StringComparison.CurrentCultureIgnoreCase));

                    if (buff != null && ((buff.EndTime - Game.Time) * 1000 < 350))
                    {
                        var kled = buff.Caster as AIHeroClient;

                        if (kled == null)
                            return;

                        Invoke(kled, SpellSlot.Q, data.AdditionalBuffName, false, 0);
                    }
                }

                if (StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).All(x => x.Hero != Champion.Nocturne))
                    return;
                {
                    var data = BlockableSpellsHashSet.FirstOrDefault(x => x.ChampionName == Champion.Nocturne && x.SpellSlot == SpellSlot.E);

                    if (data == null)
                        return;

                    var buff = Player.Instance.Buffs.FirstOrDefault(x => x.Name.Equals(data.AdditionalBuffName, StringComparison.CurrentCultureIgnoreCase));

                    if (buff == null || ((buff.EndTime - Game.Time)*1000 > 350))
                        return;

                    var nocturne = buff.Caster as AIHeroClient;

                    if (nocturne == null)
                        return;

                    Invoke(nocturne, SpellSlot.E, data.AdditionalBuffName, false, 0);
                }
            }

            private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
            {
                if (BlockableSpellsHashSet.Count == 0)
                {
                    Console.WriteLine("[DEBUG] Not found any spells that can be blocked ...");
                    return;
                }

                var enemy = sender as AIHeroClient;

                if (enemy == null)
                    return;
                
                foreach (var blockableSpellData in BlockableSpellsHashSet.Where(data => data.ChampionName == enemy.Hero))
                {
                    if (blockableSpellData.NeedsAdditionalLogics == false && args.Target != null && args.Target.IsMe && args.Slot == blockableSpellData.SpellSlot)
                    {
                        Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0);
                    }
                    else if (blockableSpellData.NeedsAdditionalLogics)
                    {
                        if (args.SData.IsAutoAttack() && args.Target != null && args.Target.IsMe && !string.IsNullOrWhiteSpace(blockableSpellData.AdditionalBuffName) &&
                            blockableSpellData.AdditionalBuffName == args.SData.Name.ToLowerInvariant())
                        {
                            OnBlockableSpell?.Invoke(enemy, new OnBlockableSpellEventArgs(enemy.Hero, args.Slot, IsEnabledFor(enemy, blockableSpellData.SpellSlot, blockableSpellData.AdditionalBuffName), true, blockableSpellData.AdditionalDelay));
                        }

                        switch (enemy.Hero)
                        {
                            case Champion.Azir:
                            {
                                if (args.Slot == blockableSpellData.SpellSlot && (enemy.Distance(Player.Instance) < 300))
                                {
                                    Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0);
                                }
                                break;
                            }
                            case Champion.Amumu:
                            {
                                if (args.Slot == blockableSpellData.SpellSlot &&
                                    (enemy.Distance(Player.Instance) < 1100))
                                {
                                    Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0);
                                }
                                break;
                            }
                            case Champion.Akali:
                            {
                                if (args.Slot == blockableSpellData.SpellSlot && (enemy.Distance(Player.Instance) < 325))
                                {
                                    Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0);
                                }
                                break;
                            }
                            case Champion.Bard:
                            {
                                if (args.Slot == blockableSpellData.SpellSlot &&
                                    new Geometry.Polygon.Circle(args.End, 325).IsInside(Player.Instance))
                                {
                                    Core.DelayAction(
                                        () => Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0),
                                        (int) Math.Max(enemy.Distance(Player.Instance)/2000*1000 - 300, 0));
                                }
                                break;
                            }
                            case Champion.Diana:
                            {
                                if (args.Slot == blockableSpellData.SpellSlot &&
                                    new Geometry.Polygon.Circle(args.End, 225).IsInside(Player.Instance))
                                {
                                    Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0);
                                }
                                break;
                            }
                            case Champion.Caitlyn:
                            {
                                if (args.Slot == blockableSpellData.SpellSlot && args.Target != null && args.Target.IsMe)
                                {
                                    Core.DelayAction(
                                        () =>
                                            Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0),
                                        (int)
                                            Math.Max(
                                                enemy.Distance(Player.Instance)/args.SData.MissileSpeed*1000 + 500, 0));
                                }
                                break;
                            }
                            case Champion.Gnar:
                            {
                                if (args.Slot == blockableSpellData.SpellSlot &&
                                    enemy.IsInRangeCached(Player.Instance, 590))
                                {
                                    Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0);
                                }
                                break;
                            }
                            case Champion.Kalista:
                            {
                                if (args.Slot == blockableSpellData.SpellSlot &&
                                    Player.Instance.HasBuff(blockableSpellData.AdditionalBuffName))
                                {
                                    Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0);
                                }
                                break;
                            }
                            case Champion.JarvanIV:
                            {
                                if (args.Slot == blockableSpellData.SpellSlot)
                                {
                                    var flag =
                                        ObjectManager.Get<Obj_AI_Minion>()
                                            .FirstOrDefault(
                                                x => x.Name.Equals("beacon", StringComparison.CurrentCultureIgnoreCase));

                                    if (flag == null)
                                        continue;

                                    var endPos = enemy.Position.Extend(args.End, 790).To3D();
                                    var flagpolygon = new Geometry.Polygon.Circle(flag.Position, 150);
                                    var qPolygon = new Geometry.Polygon.Rectangle(enemy.Position, endPos, 180);
                                    var playerpolygon = new Geometry.Polygon.Circle(Player.Instance.Position,
                                        Player.Instance.BoundingRadius);

                                    for (var i = 0; i <= 800; i += 100)
                                    {
                                        if (flagpolygon.IsInside(enemy.Position.Extend(args.End, i)) &&
                                            playerpolygon.Points.Any(x => qPolygon.IsInside(x)))
                                        {
                                            Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0);
                                        }
                                    }
                                }
                                break;
                            }
                            case Champion.Blitzcrank:
                            {
                                switch (args.Slot)
                                {
                                    case SpellSlot.Q:
                                        const int speed = 1800;
                                        var eta = (int) (enemy.DistanceCached(Player.Instance)/speed) - 250;
                                        var endPos = enemy.Position.Extend(args.End,
                                            enemy.DistanceCached(args.End) > 1050 ? 1050 : enemy.DistanceCached(args.End));

                                        Core.DelayAction(() =>
                                        {
                                            if (endPos.IsInRange(Player.Instance, 100))
                                            {
                                                Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0);
                                            }
                                        }, eta);
                                        break;
                                    case SpellSlot.R:
                                        Invoke(enemy, SpellSlot.R, blockableSpellData.AdditionalBuffName, false, 0);
                                        break;
                                    default:
                                        continue;
                                }
                                break;
                            }
                            case Champion.Warwick:
                            {
                                if ((args.Slot == blockableSpellData.SpellSlot ||
                                     args.SData.Name.Equals(blockableSpellData.AdditionalBuffName,
                                         StringComparison.CurrentCultureIgnoreCase)) &&
                                    ((args.End.Distance(Player.Instance) < 500) || args.Target.IsMe))
                                {
                                    Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0);
                                }
                                break;
                            }
                            case Champion.Cassiopeia:
                            {
                                if (args.Slot == blockableSpellData.SpellSlot)
                                {
                                    var endPos =
                                        enemy.Position.Extend(args.End,
                                            args.End.DistanceCached(enemy) > 850 ? 850 : args.End.DistanceCached(enemy))
                                            .To3D();

                                    var rPolygon = new Geometry.Polygon.Sector(enemy.Position, endPos, 850, 80 * (float) (Math.PI/180F));
                                    var playerpolygon = new Geometry.Polygon.Circle(Player.Instance.Position,
                                        Player.Instance.BoundingRadius);

                                    if (playerpolygon.Points.Any(x => rPolygon.IsInside(x)))
                                    {
                                        Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0);
                                    }
                                }
                                break;
                            }
                            case Champion.Evelynn:
                            {
                                if (args.Slot == blockableSpellData.SpellSlot)
                                {
                                    var endPos =
                                        enemy.Position.Extend(args.End,
                                            args.End.DistanceCached(enemy) > 650 ? 650 : args.End.DistanceCached(enemy))
                                            .To3D();

                                    if (endPos.IsInRangeCached(Player.Instance, 500))
                                    {
                                        Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0);
                                    }
                                }
                                break;
                            }
                            case Champion.Janna:
                            {
                                if (args.Slot == blockableSpellData.SpellSlot &&
                                    enemy.IsInRangeCached(Player.Instance, 875))
                                {
                                    Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0);
                                }
                                break;
                            }
                            case Champion.Lissandra:
                            {
                                if (args.Slot == blockableSpellData.SpellSlot &&
                                    enemy.IsInRangeCached(Player.Instance, 900))
                                {
                                    Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0);
                                }
                                break;
                            }
                            case Champion.Malphite:
                            {
                                if (args.Slot == blockableSpellData.SpellSlot)
                                {
                                    var speed = enemy.MoveSpeed - 335 + 1835;
                                    var eta = (int) (enemy.DistanceCached(Player.Instance)/speed*1000 + 250) -
                                              400;
                                    var endPos = enemy.Position.Extend(args.End,
                                        enemy.DistanceCached(args.End) > 1000
                                            ? 1000
                                            : enemy.DistanceCached(args.End));

                                    Core.DelayAction(() =>
                                    {
                                        if (endPos.IsInRangeCached(Player.Instance, 300))
                                        {
                                            Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0);
                                        }
                                    }, eta);
                                }
                                break;
                            }
                            case Champion.Sona:
                            {
                                if (args.Slot == blockableSpellData.SpellSlot)
                                {
                                    var speed = enemy.MoveSpeed - 335 + 1835;
                                    var eta = (int) (enemy.DistanceCached(Player.Instance)/speed*1000 + 250) -
                                              400;
                                    var endPos = enemy.Position.Extend(args.End,
                                        enemy.DistanceCached(args.End) > 1000
                                            ? 1000
                                            : enemy.DistanceCached(args.End));

                                    Core.DelayAction(() =>
                                    {
                                        if (endPos.IsInRangeCached(Player.Instance, 300))
                                        {
                                            Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0);
                                        }
                                    }, eta);
                                }
                                break;
                            }
                            case Champion.Sejuani:
                            {
                                if (args.Slot == blockableSpellData.SpellSlot &&
                                    Player.Instance.HasBuff(blockableSpellData.AdditionalBuffName))
                                {
                                    Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0);
                                }
                                break;
                            }
                            case Champion.Graves:
                            {
                                if (args.Slot == blockableSpellData.SpellSlot)
                                {
                                    var endPos = enemy.Position.Extend(args.End, 1000).To3D();
                                    var rPolygon = new Geometry.Polygon.Rectangle(enemy.Position,
                                        endPos, 130);
                                    var playerpolygon =
                                        new Geometry.Polygon.Circle(Player.Instance.Position,
                                            Player.Instance.BoundingRadius);

                                    if (playerpolygon.Points.Any(x => rPolygon.IsInside(x)))
                                    {
                                        Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0);
                                    }
                                }
                                break;
                            }
                            case Champion.Hecarim:
                            {
                                if (args.Slot == blockableSpellData.SpellSlot)
                                {
                                    var endPos = enemy.Position.Extend(args.End, 1000).To3D();
                                    var rPolygon = new Geometry.Polygon.Rectangle(
                                        enemy.Position, endPos, 300);
                                    var playerpolygon =
                                        new Geometry.Polygon.Circle(Player.Instance.Position,
                                            Player.Instance.BoundingRadius);

                                    if (playerpolygon.Points.Any(x => rPolygon.IsInside(x)))
                                    {
                                        Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0);
                                    }
                                }
                                break;
                            }
                            case Champion.Nautilus:
                            {
                                if (args.Slot == blockableSpellData.SpellSlot &&
                                    args.Target != null && args.Target.IsMe)
                                {
                                    Core.DelayAction(
                                        () => Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0),
                                        (int) Math.Max(
                                            enemy.Distance(Player.Instance)/
                                            args.SData.MissileSpeed*1000 - 300, 0));
                                }
                                break;
                            }
                            case Champion.Vi:
                            {
                                if (args.Slot == blockableSpellData.SpellSlot &&
                                    args.Target != null && args.Target.IsMe)
                                {
                                    Core.DelayAction(() =>
                                    {
                                        if (enemy.Distance(Player.Instance) < 350)
                                            Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0);

                                    },
                                        (int) Math.Max(enemy.Distance(Player.Instance)/
                                                       args.SData.MissileSpeed*1000 - 400,
                                            0));
                                }
                                break;
                            }
                            case Champion.Yasuo:
                            {
                                if (args.SData.Name.Equals(
                                    blockableSpellData.AdditionalBuffName,
                                    StringComparison.CurrentCultureIgnoreCase) &&
                                    enemy.IsInRangeCached(Player.Instance, 380))
                                {
                                    Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0);
                                }
                                break;
                            }
                            case Champion.Morgana:
                            {
                                if (
                                    args.Slot == blockableSpellData.SpellSlot &&
                                    enemy.IsInRangeCached(Player.Instance, 600))
                                {
                                    Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0);
                                }
                                break;
                            }
                            case Champion.Zed:
                            {
                                if (args.Slot ==
                                    blockableSpellData.SpellSlot && args.Target != null && args.Target.IsMe)
                                {
                                    Core.DelayAction(
                                        () => Invoke(enemy, args.Slot, blockableSpellData.AdditionalBuffName, false, 0),
                                        300);
                                }
                                break;
                            }
                        }
                    }
                }
            }

            public class OnBlockableSpellEventArgs : EventArgs
            {
                public Champion ChampionName { get; private set; }
                public bool IsAutoAttack { get; }
                public SpellSlot SpellSlot { get; }
                public float AdditionalDelay { get; private set; }
                public bool Enabled { get; }

                public OnBlockableSpellEventArgs(Champion championName, SpellSlot spellSlot, bool enabled, bool isAutoAttack, float additionalDelay)
                {
                    ChampionName = championName;
                    SpellSlot = spellSlot;
                    Enabled = enabled;
                    IsAutoAttack = isAutoAttack;
                    AdditionalDelay = additionalDelay;
                }
            }

            private class BlockableSpellData
            {
                public Champion ChampionName { get; }
                public bool NeedsAdditionalLogics { get; set; }
                public string AdditionalBuffName { get; set; }
                public SpellSlot SpellSlot { get; }
                public string SpellName { get; }
                public float AdditionalDelay { get; set; }

                public BlockableSpellData(Champion championName, string spellName, SpellSlot spellSlot)
                {
                    ChampionName = championName;
                    SpellSlot = spellSlot;
                    SpellName = spellName;
                }
            }
        }
    }
}