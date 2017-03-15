#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Urgot.cs" company="EloBuddy">
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

namespace Marksman_Master.Plugins.Urgot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EloBuddy;
    using EloBuddy.SDK;
    using EloBuddy.SDK.Constants;
    using EloBuddy.SDK.Enumerations;
    using EloBuddy.SDK.Menu;
    using EloBuddy.SDK.Menu.Values;
    using Color = System.Drawing.Color;
    using EloBuddy.SDK.Rendering;
    using Utils;
    using SharpDX;

    internal class Urgot : ChampionPlugin
    {
        protected static Spell.Skillshot Q { get; }
        protected static Spell.Active W { get; }
        protected static Spell.Skillshot E { get; }
        protected static Spell.Targeted R { get; }

        internal static Menu ComboMenu { get; set; }
        internal static Menu HarassMenu { get; set; }
        internal static Menu LaneClearMenu { get; set; }
        internal static Menu MiscMenu { get; set; }
        internal static Menu DrawingsMenu { get; set; }

        private static readonly ColorPicker[] ColorPicker;

        protected static List<Obj_AI_Base> CorrosiveDebufTargets { get; }

        private static float _lastScanTick;
        private static bool _changingRangeScan;

        protected static int FleeRange { get; } = 375;

        protected static bool HasEDebuff(Obj_AI_Base unit)
        {
            if (MenuManager.IsCacheEnabled && CachedHasBuff.Exist(unit.NetworkId))
            {
                return CachedHasBuff.Get(unit.NetworkId);
            }

            var result = CorrosiveDebufTargets.Any(x => x.IdEquals(unit));

            if (MenuManager.IsCacheEnabled)
            {
                CachedHasBuff.Add(unit.NetworkId, result);
            }

            return result;
        }

        protected static bool IsInQRange(Obj_AI_Base unit)
            => (unit.IsValidTarget(1300) && HasEDebuff(unit)) || unit.IsValidTarget(900);

        protected static float QCooldown
        {
            get
            {
                var cd = Q.Handle.CooldownExpires - Game.Time < 0 ? 0 : Q.Handle.CooldownExpires - Game.Time;

                return cd > Q.Handle.Cooldown ? Q.Handle.Cooldown : cd;
            }
        }
        
        protected static bool HasAnyOrbwalkerFlags
            =>
                (Orbwalker.ActiveModesFlags &
                 (Orbwalker.ActiveModes.Combo | Orbwalker.ActiveModes.Harass | Orbwalker.ActiveModes.LaneClear |
                  Orbwalker.ActiveModes.LastHit | Orbwalker.ActiveModes.JungleClear)) != 0;

        protected static Cache.Modules.CustomCache<int, bool> CachedHasBuff { get; } 

        static Urgot()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 900, SkillShotType.Linear, 150, 1600, 60)
            {
                AllowedCollisionCount = 0
            };
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Skillshot(SpellSlot.E, 900, SkillShotType.Circular, 250, 1500, 250)
            {
                AllowedCollisionCount = int.MaxValue
            };
            R = new Spell.Targeted(SpellSlot.R, 500);

            CorrosiveDebufTargets = new List<Obj_AI_Base>();

            ColorPicker = new ColorPicker[4];

            CachedHasBuff = StaticCacheProvider.Cache.Resolve<Cache.Modules.CustomCache<int, bool>>(200);

            ColorPicker[0] = new ColorPicker("UrgotQ", new ColorBGRA(10, 106, 138, 255));
            ColorPicker[1] = new ColorPicker("UrgotE", new ColorBGRA(177, 67, 191, 255));
            ColorPicker[2] = new ColorPicker("UrgotR", new ColorBGRA(177, 67, 191, 255));
            ColorPicker[3] = new ColorPicker("UrgotHpBar", new ColorBGRA(255, 134, 0, 255));

            DamageIndicator.Initalize(ColorPicker[3].Color);
            DamageIndicator.DamageDelegate = HandleDamageIndicator;

            ColorPicker[3].OnColorChange += (a, b) => DamageIndicator.Color = b.Color;

            TearStacker.Initializer(new Dictionary<SpellSlot, float> {{SpellSlot.Q, 2150}},
                () =>
                    (Player.Instance.CountEnemiesInRangeCached(1500) == 0) &&
                    (Player.Instance.CountEnemyMinionsInRangeCached(1500) == 0) && !HasAnyOrbwalkerFlags);

            ChampionTracker.Initialize(ChampionTrackerFlags.VisibilityTracker);
            ChampionTracker.OnLoseVisibility += ChampionTracker_OnLoseVisibility;

            Game.OnPreTick += args =>
            {
                if (Core.GameTickCount - _lastScanTick < 100)
                {
                    return;
                }

                CorrosiveDebufTargets.Clear();
            };

            Game.OnTick += Game_OnTick;

            GameObject.OnCreate += (sender, args) =>
            {
                if (!Settings.Combo.UseW || !HasAnyOrbwalkerFlags)
                    return;

                var missileClient = sender as MissileClient;

                if ((missileClient != null) && missileClient.SpellCaster.IsMe && W.IsReady() &&
                    ((missileClient.IsAutoAttack() && (missileClient.Target?.Type == GameObjectType.AIHeroClient)) || StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                        x =>
                            x.IsValidTarget() &&
                            missileClient.EndPosition.IsInRange(x, missileClient.SData.LineWidth)).Any()))
                {
                    W.Cast();
                }
            };
        }

        private static void ChampionTracker_OnLoseVisibility(object sender, ChampionTracker.OnLoseVisibilityEventArgs e)
        {
            if (!Q.IsReady() || (e?.LastPosition.Distance(Player.Instance) > 1300) || (e?.Data == null) || (e.Hero == null) ||
                (CorrosiveDebufTargets.Any(
                    x =>
                        (x.Type == GameObjectType.AIHeroClient) && x.IsValidTargetCached(1300)) && (e.Data?.LastHealth > Player.Instance.GetSpellDamageCached(e.Hero, SpellSlot.Q))))
                return;

            var data = e.Data;
            var castPosition =
                data.LastPosition.Extend(data.LastPath, e.Hero.MoveSpeed * Game.Time - data.LastVisibleGameTime).To3D();

            if (castPosition.IsInRange(Player.Instance, 1300) && data.LastBuffs.Any(
                x =>
                    (x.EndTime - Game.Time > .25f) &&
                    (x.Name.Equals("urgotcorrosivedebuff", StringComparison.CurrentCultureIgnoreCase) ||
                     x.DisplayName.Equals("urgotcorrosivedebuff", StringComparison.CurrentCultureIgnoreCase))))
            {
                Player.CastSpell(SpellSlot.Q, castPosition);
            }
        }
        
        private static void Game_OnTick(EventArgs args)
        {
            if (Core.GameTickCount - _lastScanTick < 100)
            {
                return;
            }

            var union = CorrosiveDebufTargets.Union(
                    ObjectManager.Get<Obj_AI_Base>().Where(x => x.IsValidTarget() && x.HasBuff("urgotcorrosivedebuff")));

            CorrosiveDebufTargets.AddRange(union);
            
            _lastScanTick = Core.GameTickCount;
        }

        protected override void OnDraw()
        {
            if (_changingRangeScan)
                Circle.Draw(SharpDX.Color.White,
                    LaneClearMenu["Plugins.Urgot.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue, Player.Instance);

            if (Settings.Drawings.DrawQ && (!Settings.Drawings.DrawSpellRangesWhenReady || Q.IsReady()))
                Circle.Draw(ColorPicker[0].Color, Q.Range, Player.Instance);
            if (Settings.Drawings.DrawE && (!Settings.Drawings.DrawSpellRangesWhenReady || E.IsReady()))
                Circle.Draw(ColorPicker[1].Color, E.Range, Player.Instance);
            if (Settings.Drawings.DrawR && (!Settings.Drawings.DrawSpellRangesWhenReady || R.IsReady()))
                Circle.Draw(ColorPicker[2].Color, R.Range, Player.Instance);
        }

        protected override void OnInterruptible(AIHeroClient sender, InterrupterEventArgs args)
        {
            if (!R.IsReady() || !Settings.Misc.UseRToInterrupt || !sender.IsValidTarget(R.Range) || sender.IsUnderTurret())
                return;

            if (args.Delay == 0)
            {
                R.Cast(sender);
            } else Core.DelayAction(() => R.Cast(sender), args.Delay);
        }

        protected override void OnGapcloser(AIHeroClient sender, GapCloserEventArgs args)
        {
            if (!R.IsReady() || (args.End.Distance(Player.Instance) > 350) || !Settings.Misc.UseRAgainstGapclosers ||
                !sender.IsValidTarget(R.Range) || sender.IsUnderTurret())
                return;

            if (args.Delay == 0)
            {
                R.Cast(sender);
            }
            else Core.DelayAction(() => R.Cast(sender), args.Delay);
        }

        private static float HandleDamageIndicator(Obj_AI_Base unit)
        {
            if (!Settings.Drawings.DrawInfo)
            {
                return 0;
            }

            var enemy = (AIHeroClient)unit;

            if (enemy == null)
                return 0;
            
            float damage = 0;

            if (Q.IsReady() && unit.IsValidTarget(Q.Range))
                damage += Player.Instance.GetSpellDamageCached(unit, SpellSlot.Q);
            if(E.IsReady() && unit.IsValidTarget(E.Range))
                damage += Player.Instance.GetSpellDamageCached(unit, SpellSlot.E);
            if (unit.IsValidTarget(Player.Instance.GetAutoAttackRange()))
                damage += Player.Instance.GetAutoAttackDamageCached(unit, true);

            return damage;
        }

        protected override void CreateMenu()
        {
            ComboMenu = MenuManager.Menu.AddSubMenu("Combo");
            ComboMenu.AddGroupLabel("Combo mode settings for Urgot addon");

            ComboMenu.AddLabel("Acid Hunter (Q) settings :");
            ComboMenu.Add("Plugins.Urgot.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Terror Capacitor (W) settings :");
            ComboMenu.Add("Plugins.Urgot.ComboMenu.UseW", new CheckBox("Use W"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Noxian Corrosive Charge (E) settings :");
            ComboMenu.Add("Plugins.Urgot.ComboMenu.UseE", new CheckBox("Use E"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Hyper-Kinetic Position Reverser (R) settings :");
            ComboMenu.Add("Plugins.Urgot.ComboMenu.UseR", new CheckBox("Use R"));
            ComboMenu.Add("Plugins.Urgot.ComboMenu.UseRToSwapPosUnderTower", new CheckBox("Try to swap enemy pos under tower"));
            ComboMenu.AddSeparator(5);

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("Harass mode settings for Urgot addon");

            HarassMenu.AddLabel("Acid Hunter (Q) settings :");
            HarassMenu.Add("Plugins.Urgot.HarassMenu.UseQ", new CheckBox("Use Q"));
            HarassMenu.Add("Plugins.Urgot.HarassMenu.MinManaQ", new Slider("Min mana percentage ({0}%) to use Q", 40, 1));
            HarassMenu.AddSeparator(5);

            HarassMenu.AddLabel("Noxian Corrosive Charge (E) settings :");
            HarassMenu.Add("Plugins.Urgot.HarassMenu.UseE", new CheckBox("Use E"));
            HarassMenu.Add("Plugins.Urgot.HarassMenu.MinManaE", new Slider("Min mana percentage ({0}%) to use E", 40, 1));
            HarassMenu.AddSeparator(5);
            
            LaneClearMenu = MenuManager.Menu.AddSubMenu("Clear");
            LaneClearMenu.AddGroupLabel("Lane clear settings for Urgot addon");

            LaneClearMenu.AddLabel("Basic settings :");
            LaneClearMenu.Add("Plugins.Urgot.LaneClearMenu.EnableLCIfNoEn", new CheckBox("Enable lane clear only if no enemies nearby"));
            var scanRange = LaneClearMenu.Add("Plugins.Urgot.LaneClearMenu.ScanRange", new Slider("Range to scan for enemies", 1500, 300, 2500));
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
            LaneClearMenu.Add("Plugins.Urgot.LaneClearMenu.AllowedEnemies", new Slider("Allowed enemies amount", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Acid Hunter (Q) settings :");
            LaneClearMenu.Add("Plugins.Urgot.LaneClearMenu.UseQInLaneClear", new CheckBox("Use Q in Lane Clear"));
            LaneClearMenu.Add("Plugins.Urgot.LaneClearMenu.UseQInJungleClear", new CheckBox("Use Q in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Urgot.LaneClearMenu.MinManaQ", new Slider("Min mana percentage ({0}%) to use Q", 50, 1));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Noxian Corrosive Charge (E) settings :");
            LaneClearMenu.Add("Plugins.Urgot.LaneClearMenu.UseEInLaneClear", new CheckBox("Use E in Lane Clear"));
            LaneClearMenu.Add("Plugins.Urgot.LaneClearMenu.UseEInJungleClear", new CheckBox("Use E in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Urgot.LaneClearMenu.MinManaE", new Slider("Min mana percentage ({0}%) to use E", 50, 1));

            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("Misc settings for Urgot addon");
            MiscMenu.AddLabel("Basic settings :");
            MiscMenu.Add("Plugins.Urgot.MiscMenu.EnableKillsteal", new CheckBox("Enable Killsteal"));
            MiscMenu.AddSeparator(2);
            MiscMenu.Add("Plugins.Urgot.MiscMenu.EnableTearStacker", new CheckBox("Enable Tear Stacker"));
            MiscMenu.Add("Plugins.Urgot.MiscMenu.TearStackerMinMana", new Slider("Tear Stacker => Min mana percentage : {0}%", 75));
            MiscMenu.Add("Plugins.Urgot.MiscMenu.EnableTearStacker", new CheckBox("Enable Tear Stacker")).OnValueChange +=
                (a, b) =>
                {
                    TearStacker.Enabled = b.NewValue;
                };

            MiscMenu.Add("Plugins.Urgot.MiscMenu.StackOnlyInFountain", new CheckBox("Stack only in fountain", false)).OnValueChange +=
                (a, b) =>
                {
                    TearStacker.OnlyInFountain = b.NewValue;
                };

            MiscMenu.Add("Plugins.Urgot.MiscMenu.TearStackerMinMana", new Slider("Tear Stacker => Min mana percentage : {0}%", 75)).OnValueChange +=
                (a, b) =>
                {
                    TearStacker.MinimumManaPercent = b.NewValue;
                };
            
            TearStacker.Enabled = Settings.Misc.EnableTearStacker;
            TearStacker.OnlyInFountain = Settings.Misc.StackOnlyInFountain;
            TearStacker.MinimumManaPercent = Settings.Misc.TearStackerMinMana;

            MiscMenu.AddSeparator(5);

            MiscMenu.AddLabel("Acid Hunter (Q) settings :");
            MiscMenu.Add("Plugins.Urgot.MiscMenu.AutoHarass",
                new KeyBind("Auto harass", true, KeyBind.BindTypes.PressToggle, 'T'));
            MiscMenu.AddLabel("Enables Auto harass on enemies with E debuff in Lane Clear and Harass mode !");
            MiscMenu.AddSeparator(5);

            MiscMenu.AddLabel("Terror Capacitor (W) settings :");
            MiscMenu.Add("Plugins.Urgot.MiscMenu.WMinDamage", new Slider("Auto W if incoming damage will deal more than {0}% of my hp", 10, 1));
            MiscMenu.AddSeparator(5);

            MiscMenu.AddLabel("Hyper-Kinetic Position Reverser (R) settings :");
            MiscMenu.Add("Plugins.Urgot.MiscMenu.UseRAgainstGapclosers", new CheckBox("Use R against gapclosers"));
            MiscMenu.Add("Plugins.Urgot.MiscMenu.UseRToInterrupt", new CheckBox("Use R to interrupt dangerous spells"));

            MenuManager.BuildAntiGapcloserMenu();
            MenuManager.BuildInterrupterMenu();

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawings settings for Urgot addon");

            DrawingsMenu.AddLabel("Basic settings :");
            DrawingsMenu.Add("Plugins.Urgot.DrawingsMenu.DrawSpellRangesWhenReady", new CheckBox("Draw spell ranges only when they are ready"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Acid Hunter (Q) settings :");
            DrawingsMenu.Add("Plugins.Urgot.DrawingsMenu.DrawQ", new CheckBox("Draw Q range"));
            DrawingsMenu.Add("Plugins.Urgot.DrawingsMenu.DrawQColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[0].Initialize(Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Noxian Corrosive Charge (E) settings :");
            DrawingsMenu.Add("Plugins.Urgot.DrawingsMenu.DrawE", new CheckBox("Draw E range"));
            DrawingsMenu.Add("Plugins.Urgot.DrawingsMenu.DrawEColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[1].Initialize(Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Hyper-Kinetic Position Reverser (R) settings :");
            DrawingsMenu.Add("Plugins.Urgot.DrawingsMenu.DrawR", new CheckBox("Draw R range", false));
            DrawingsMenu.Add("Plugins.Urgot.DrawingsMenu.DrawRColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[2].Initialize(Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Other settings :");
            DrawingsMenu.Add("Plugins.Urgot.DrawingsMenu.DrawInfo", new CheckBox("Draw Infos")).OnValueChange += (a, b) =>
            {
                if (b.NewValue)
                    DamageIndicator.DamageDelegate = HandleDamageIndicator;
                else if (!b.NewValue)
                    DamageIndicator.DamageDelegate = null;
            };
            DrawingsMenu.Add("Plugins.Urgot.DrawingsMenu.InfoColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[3].Initialize(Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddLabel("Draws damage indicator");
        }

        protected override void PermaActive()
        {
            switch (R.Level)
            {
                case 1:
                {
                    R.Range = 500;
                    break;
                }
                case 2:
                {
                    R.Range = 700;
                    break;
                }
                case 3:
                {
                    R.Range = 850;
                    break;
                }
                default:
                    R.Range = 0;
                    break;
            }

            Modes.PermaActive.Execute();
        }

        protected override void ComboMode()
        {
            if (!Player.Instance.HasSheenBuff())
            {
                Modes.Combo.Execute();
            }
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
                public static bool UseQ => MenuManager.MenuValues["Plugins.Urgot.ComboMenu.UseQ"];

                public static bool UseW => MenuManager.MenuValues["Plugins.Urgot.ComboMenu.UseW"];

                public static bool UseE => MenuManager.MenuValues["Plugins.Urgot.ComboMenu.UseE"];

                public static bool UseR => MenuManager.MenuValues["Plugins.Urgot.ComboMenu.UseR"];

                public static bool UseRToSwapPosUnderTower => MenuManager.MenuValues["Plugins.Urgot.ComboMenu.UseRToSwapPosUnderTower"];
            }

            internal static class Harass
            {
                public static bool UseQ => MenuManager.MenuValues["Plugins.Urgot.HarassMenu.UseQ"];

                public static int MinManaQ => MenuManager.MenuValues["Plugins.Urgot.HarassMenu.MinManaQ", true];

                public static bool UseE => MenuManager.MenuValues["Plugins.Urgot.HarassMenu.UseE"];

                public static int MinManaE => MenuManager.MenuValues["Plugins.Urgot.HarassMenu.MinManaE", true];
            }

            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies => MenuManager.MenuValues["Plugins.Urgot.LaneClearMenu.EnableLCIfNoEn"];

                public static int ScanRange => MenuManager.MenuValues["Plugins.Urgot.LaneClearMenu.ScanRange", true];

                public static int AllowedEnemies => MenuManager.MenuValues["Plugins.Urgot.LaneClearMenu.AllowedEnemies", true];

                public static bool UseQInLaneClear => MenuManager.MenuValues["Plugins.Urgot.LaneClearMenu.UseQInLaneClear"];

                public static bool UseQInJungleClear => MenuManager.MenuValues["Plugins.Urgot.LaneClearMenu.UseQInJungleClear"];

                public static int MinManaQ => MenuManager.MenuValues["Plugins.Urgot.LaneClearMenu.MinManaQ", true];

                public static bool UseEInLaneClear => MenuManager.MenuValues["Plugins.Urgot.LaneClearMenu.UseEInLaneClear"];

                public static bool UseEInJungleClear => MenuManager.MenuValues["Plugins.Urgot.LaneClearMenu.UseEInJungleClear"];

                public static int MinManaE => MenuManager.MenuValues["Plugins.Urgot.LaneClearMenu.MinManaE", true];
            }

            internal static class Misc
            {
                public static bool EnableKillsteal => MenuManager.MenuValues["Plugins.Urgot.MiscMenu.EnableKillsteal"];

                public static bool EnableTearStacker => MenuManager.MenuValues["Plugins.Urgot.MiscMenu.EnableTearStacker"];

                public static bool StackOnlyInFountain => MenuManager.MenuValues["Plugins.Urgot.MiscMenu.StackOnlyInFountain"]; 

                public static int TearStackerMinMana => MenuManager.MenuValues["Plugins.Urgot.MiscMenu.TearStackerMinMana", true];

                public static bool AutoHarass => MenuManager.MenuValues["Plugins.Urgot.MiscMenu.AutoHarass"];

                public static bool UseRAgainstGapclosers => MenuManager.MenuValues["Plugins.Urgot.MiscMenu.UseRAgainstGapclosers"];

                public static bool UseRToInterrupt => MenuManager.MenuValues["Plugins.Urgot.MiscMenu.UseRToInterrupt"];

                public static int MinDamage => MenuManager.MenuValues["Plugins.Urgot.MiscMenu.WMinDamage", true];
            }

            internal static class Drawings
            {
                public static bool DrawSpellRangesWhenReady => MenuManager.MenuValues["Plugins.Urgot.DrawingsMenu.DrawSpellRangesWhenReady"];

                public static bool DrawQ => MenuManager.MenuValues["Plugins.Urgot.DrawingsMenu.DrawQ"];

                public static bool DrawE => MenuManager.MenuValues["Plugins.Urgot.DrawingsMenu.DrawE"];

                public static bool DrawR => MenuManager.MenuValues["Plugins.Urgot.DrawingsMenu.DrawR"];

                public static bool DrawInfo => MenuManager.MenuValues["Plugins.Urgot.DrawingsMenu.DrawInfo"];
            }
        }
    }
}