#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Jhin.cs" company="EloBuddy">
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
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using EloBuddy.SDK.Rendering;
using Marksman_Master.Cache.Modules;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Jhin
{
    internal class Jhin : ChampionPlugin
    {
        protected static Spell.Targeted Q { get; }
        protected static Spell.Skillshot W { get; }
        protected static Spell.Skillshot E { get; }
        protected static Spell.Skillshot R { get; }

        internal static Menu ComboMenu { get; set; }
        internal static Menu HarassMenu { get; set; }
        internal static Menu LaneClearMenu { get; set; }
        internal static Menu MiscMenu { get; set; }
        internal static Menu DrawingsMenu { get; set; }

        private static readonly ColorPicker[] ColorPicker;
        private static bool _changingRangeScan;
        private static float _lastRTime;
        private static float _lastETime;
        private static Vector3 _lastEPosition;

        protected static float LastLaneClear;

        private static CustomCache<int, float> Damages { get; }
        private static CustomCache<int, bool> HasBuff { get; }

        protected static Cache.Cache Cache;

        public static bool HasSpottedBuff(AIHeroClient unit)
        {
            if (MenuManager.IsCacheEnabled && HasBuff.Exist(unit.NetworkId))
            {
                return HasBuff[unit.NetworkId];
            }
            
            var hasBuff = unit.Buffs.Any(
                    b => b.IsActive && string.Equals(b.Name, "jhinespotteddebuff", StringComparison.InvariantCultureIgnoreCase));

            if (MenuManager.IsCacheEnabled)
            {
                HasBuff.Add(unit.NetworkId, hasBuff);
            }

            return hasBuff;
        }

        public static BuffInstance GetSpottedBuff(AIHeroClient unit)
            =>
                unit.Buffs.FirstOrDefault(
                    b => b.IsActive && (b.Name.ToLowerInvariant() == "jhinespotteddebuff"));

        public static bool HasReloadingBuff
            =>
                Player.Instance.Buffs.Any(
                    b => b.IsActive && (b.Name.ToLowerInvariant() == "jhinpassivereload"));

        public static BuffInstance GetReloadingBuff
            =>
                Player.Instance.Buffs.FirstOrDefault(
                    b => b.IsActive && (b.Name.ToLowerInvariant() == "jhinpassivereload"));

        public static bool HasAttackBuff
            =>
                Player.Instance.Buffs.Any(
                    b => b.IsActive && (b.Name.ToLowerInvariant() == "jhinpassiveattackbuff"));

        public static BuffInstance GetAttackBuff
            =>
                Player.Instance.Buffs.FirstOrDefault(
                    b => b.IsActive && (b.Name.ToLowerInvariant() == "jhinpassiveattackbuff"));


        public static bool IsCastingR { get; private set; }
        public static int GetCurrentShootsRCount { get; private set; }
        public static Vector3 REndPosition { get; private set; }
        public static bool IsPreAttack { get; private set; }

        static Jhin()
        {
            Q = new Spell.Targeted(SpellSlot.Q, 600);
            W = new Spell.Skillshot(SpellSlot.W, 2500, SkillShotType.Linear, 750, int.MaxValue, 40)
            {
                AllowedCollisionCount = -1
            };
            E = new Spell.Skillshot(SpellSlot.E, 750, SkillShotType.Circular, 750, null, 120);
            R = new Spell.Skillshot(SpellSlot.R, 3500, SkillShotType.Linear, 200, 5000, 80)
            {
                AllowedCollisionCount = -1
            };

            Cache = StaticCacheProvider.Cache;

            Damages = Cache.Resolve<CustomCache<int, float>>();
            Damages.RefreshRate = 1000;
            
            HasBuff = Cache.Resolve<CustomCache<int, bool>>();
            HasBuff.RefreshRate = 200;

            ColorPicker = new ColorPicker[5];

            ColorPicker[0] = new ColorPicker("JhinQ", new ColorBGRA(10, 106, 138, 255));
            ColorPicker[1] = new ColorPicker("JhinW", new ColorBGRA(177, 67, 191, 255));
            ColorPicker[2] = new ColorPicker("JhinE", new ColorBGRA(177, 67, 191, 255));
            ColorPicker[3] = new ColorPicker("JhinR", new ColorBGRA(177, 67, 191, 255));
            ColorPicker[4] = new ColorPicker("JhinHpBar", new ColorBGRA(255, 134, 0, 255));

            Orbwalker.OnPreAttack += (s, a) =>
            {
                IsPreAttack = true;

                if (!HasReloadingBuff)
                    return;

                a.Process = false;
                IsPreAttack = false;
            };
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Orbwalker.OnPostAttack += (target, args) => IsPreAttack = false;

            ChampionTracker.Initialize(ChampionTrackerFlags.VisibilityTracker);

            Spellbook.OnCastSpell += Spellbook_OnCastSpell;

            DamageIndicator.Initalize(ColorPicker[4].Color,(int) W.Range);
            DamageIndicator.DamageDelegate = HandleDamageIndicator;

            ColorPicker[4].OnColorChange += (a, b) => { DamageIndicator.Color = b.Color; };
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if ((args.Slot == SpellSlot.E) && ((Game.Time * 1000 - _lastETime < 3000) && (_lastEPosition.DistanceCached(args.EndPosition) < 300)))
            {
                args.Process = false;
            } else if (args.Slot == SpellSlot.E)
            {
                _lastETime = Game.Time*1000;
                _lastEPosition = args.EndPosition;
            }

            if ((args.Slot == SpellSlot.R) && (Player.Instance.Spellbook.GetSpell(SpellSlot.R).Name == "JhinRShot") && (Game.Time * 1000 - _lastRTime < Settings.Combo.RDelay+1000))
            {
                args.Process = false;
            }
            else if ((args.Slot == SpellSlot.R) && (Player.Instance.Spellbook.GetSpell(SpellSlot.R).Name == "JhinRShot"))
            {
                _lastRTime = Game.Time * 1000;
            }
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
                return;

            if(args.Slot != SpellSlot.R)
                return;

            switch (args.SData.Name.ToLowerInvariant())
            {
                case "jhinr":
                    IsCastingR = true;
                    GetCurrentShootsRCount = 4;
                    REndPosition = Player.Instance.Position.Extend(args.End, R.Range).To3D();
                    break;
                case "jhinrshot":
                    GetCurrentShootsRCount--;
                    break;
                default:
                    return;
            }
        }
        
        public static bool IsInsideRRange(Obj_AI_Base unit)
        {
            return new Geometry.Polygon.Sector(Player.Instance.Position, REndPosition, (float) (Math.PI/180f*55f), 3300)
                .IsInside(unit);
        }

        public static bool IsInsideRRange(Vector3 position)
        {
            return new Geometry.Polygon.Sector(Player.Instance.Position, REndPosition, (float) (Math.PI/180f*55f), 3300)
                .IsInside(position);
        }

        private static float HandleDamageIndicator(Obj_AI_Base unit)
        {
            if (!Settings.Drawings.DrawInfo)
            {
                return 0;
            }

            var enemy = (AIHeroClient) unit;

            if (enemy == null)
                return 0;

            if (MenuManager.IsCacheEnabled && Damages.Exist(unit.NetworkId))
            {
                return Damages.Get(unit.NetworkId);
            }
            
            var damage = 0f;

            if (!IsCastingR)
            {

                if (R.IsReady() && unit.IsValidTargetCached(R.Range))
                    damage += GetCurrentShootsRCount == 1
                        ? Damage.GetRDamage(unit, true)
                        : Damage.GetRDamage(unit)*(GetCurrentShootsRCount - 1) + Damage.GetRDamage(unit, true);
                if (Q.IsReady() && unit.IsValidTarget(Q.Range))
                    damage += Damage.GetQDamage(unit);
                if (W.IsReady() && unit.IsValidTarget(W.Range))
                    damage += Damage.GetWDamage(unit);
            }
            else
            {
                if (IsInsideRRange(unit))
                    damage += GetCurrentShootsRCount == 1 ? Damage.GetRDamage(unit, true) : Damage.GetRDamage(unit);
            }

            if (unit.IsValidTargetCached(Player.Instance.GetAutoAttackRange()))
            {
                damage += HasAttackBuff ? Damage.Get4ThShootDamage(unit) : Player.Instance.GetAutoAttackDamage(unit);
            }

            if (MenuManager.IsCacheEnabled)
            {
                Damages.Add(unit.NetworkId, damage);
            }
            
            return damage;
        }

        protected override void OnDraw()
        {
            if (_changingRangeScan)
                Circle.Draw(Color.White,
                    LaneClearMenu["Plugins.Jhin.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue, Player.Instance);

            if (Settings.Drawings.DrawQ && (!Settings.Drawings.DrawSpellRangesWhenReady || Q.IsReady()))
                Circle.Draw(ColorPicker[0].Color, Q.Range, Player.Instance);
            if (Settings.Drawings.DrawW && (!Settings.Drawings.DrawSpellRangesWhenReady || W.IsReady()))
                Circle.Draw(ColorPicker[1].Color, W.Range, Player.Instance);
            if (Settings.Drawings.DrawE && (!Settings.Drawings.DrawSpellRangesWhenReady || E.IsReady()))
                Circle.Draw(ColorPicker[2].Color, E.Range, Player.Instance);
            if (Settings.Drawings.DrawR && (!Settings.Drawings.DrawSpellRangesWhenReady || R.IsReady()) &&
                (Player.Instance.Spellbook.GetSpell(SpellSlot.R).Name == "JhinR"))
                Circle.Draw(ColorPicker[3].Color, R.Range, Player.Instance);

            if (!Settings.Drawings.DrawInfo)
                return;

            foreach (var unit in EntityManager.MinionsAndMonsters.EnemyMinions.Where(x=>x.IsValidTarget(Q.Range) && (x.Health < Damage.GetQDamage(x))))
            {
                Circle.Draw(Color.Green, 25, unit);
            }
        }

        protected override void OnInterruptible(AIHeroClient sender, InterrupterEventArgs args)
        {
        }

        protected override void OnGapcloser(AIHeroClient sender, GapCloserEventArgs args)
        {
            if(W.IsReady() && Settings.Misc.WAntiGapcloser && (args.End.DistanceCached(Player.Instance) < 350))
            {
                if (args.Delay == 0)
                {
                    W.CastMinimumHitchance(sender, 60);
                }
                else
                {
                    var target = sender; //fuck anonymous methods

                    Core.DelayAction(() => W.CastMinimumHitchance(target, 60), args.Delay);
                }
            }

            if (E.IsReady() && Settings.Misc.EAntiGapcloser && (Player.Instance.Mana - 50 > 100) && (args.End.DistanceCached(Player.Instance) < 350))
            {
                if (args.Delay == 0)
                {
                    if (sender.Hero == Champion.Caitlyn)
                    {
                        E.CastMinimumHitchance(sender, 60);
                    }
                    else
                    {
                        E.Cast(args.End);
                    }
                }
                else Core.DelayAction(() =>
                {
                    if (sender.Hero == Champion.Caitlyn)
                    {
                        E.CastMinimumHitchance(sender, 60);
                    }
                    else
                    {
                        E.Cast(args.End);
                    }
                }, args.Delay);
            }
        }

        protected override void CreateMenu()
        {
            ComboMenu = MenuManager.Menu.AddSubMenu("Combo");
            ComboMenu.AddGroupLabel("Combo mode settings for Jhin addon");

            ComboMenu.AddLabel("Dancing Grenade (Q) settings :");
            ComboMenu.Add("Plugins.Jhin.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Deadly Flourish (W) settings :");
            ComboMenu.Add("Plugins.Jhin.ComboMenu.UseW", new CheckBox("Use W"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Captive Audience (E) settings :");
            ComboMenu.Add("Plugins.Jhin.ComboMenu.UseE", new CheckBox("Use E"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Curtain Call (R) settings :");
            ComboMenu.Add("Plugins.Jhin.ComboMenu.UseR", new CheckBox("Use R"));
            ComboMenu.Add("Plugins.Jhin.ComboMenu.EnableFowPrediction", new CheckBox("Enable FoW prediction"));
            ComboMenu.Add("Plugins.Jhin.ComboMenu.RDelay", new Slider("Delay between shots", 0,  0, 2500));
            ComboMenu.Add("Plugins.Jhin.ComboMenu.RMode", new ComboBox("R mode", 0, "In Combo mode", "KeyBind", "Automatic"));
            ComboMenu.Add("Plugins.Jhin.ComboMenu.RKeybind", new KeyBind("R keybind", false, KeyBind.BindTypes.HoldActive, 'T'));
            ComboMenu.AddSeparator(5);

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("Harass mode settings for Jhin addon");

            HarassMenu.AddLabel("Dancing Grenade (Q) settings :");
            HarassMenu.Add("Plugins.Jhin.HarassMenu.UseQ", new CheckBox("Use Q", false));
            HarassMenu.Add("Plugins.Jhin.HarassMenu.MinManaQ", new Slider("Min mana percentage ({0}%) to use Q", 80, 1));
            HarassMenu.AddSeparator(5);

            HarassMenu.AddLabel("Deadly Flourish (W) settings :");
            HarassMenu.Add("Plugins.Jhin.HarassMenu.UseW", new CheckBox("Use W"));
            HarassMenu.Add("Plugins.Jhin.HarassMenu.MinManaW", new Slider("Min mana percentage ({0}%) to use W", 80, 1));
            HarassMenu.AddSeparator(5);

            LaneClearMenu = MenuManager.Menu.AddSubMenu("Clear");
            LaneClearMenu.AddGroupLabel("Lane clear settings for Jhin addon");

            LaneClearMenu.AddLabel("Basic settings :");
            LaneClearMenu.Add("Plugins.Jhin.LaneClearMenu.EnableLCIfNoEn", new CheckBox("Enable lane clear only if no enemies nearby"));
            var scanRange = LaneClearMenu.Add("Plugins.Jhin.LaneClearMenu.ScanRange", new Slider("Range to scan for enemies", 1500, 300, 2500));
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
            LaneClearMenu.Add("Plugins.Jhin.LaneClearMenu.AllowedEnemies", new Slider("Allowed enemies amount", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Dancing Grenade (Q) settings :");
            LaneClearMenu.Add("Plugins.Jhin.LaneClearMenu.UseQInLaneClear", new CheckBox("Use Q in Lane Clear", false));
            LaneClearMenu.Add("Plugins.Jhin.LaneClearMenu.UseQInJungleClear", new CheckBox("Use Q in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Jhin.LaneClearMenu.MinManaQ", new Slider("Minimum mana percentage ({0}%) to use Q", 50, 1));
            LaneClearMenu.Add("Plugins.Jhin.LaneClearMenu.MinMinionsKilledFromQ", new Slider("Minimum minions killed to use Q", 3, 1, 4));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Deadly Flourish (W) settings :");
            LaneClearMenu.Add("Plugins.Jhin.LaneClearMenu.UseWInLaneClear", new CheckBox("Use w in Lane Clear"));
            LaneClearMenu.Add("Plugins.Jhin.LaneClearMenu.UseWInJungleClear", new CheckBox("Use W in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Jhin.LaneClearMenu.MinManaW", new Slider("Min mana percentage ({0}%) to use W", 50, 1));

            MenuManager.BuildAntiGapcloserMenu();

            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("Misc settings for Jhin addon");
            MiscMenu.AddLabel("Basic settings :");
            MiscMenu.Add("Plugins.Jhin.MiscMenu.EnableKillsteal", new CheckBox("Enable Killsteal"));
            MiscMenu.AddSeparator(5);

            MiscMenu.AddLabel("Deadly Flourish (W) settings :");
            MiscMenu.Add("Plugins.Jhin.MiscMenu.WFowPrediction", new CheckBox("Use FoW prediction"));
            MiscMenu.Add("Plugins.Jhin.MiscMenu.WAntiGapcloser", new CheckBox("Cast against gapclosers"));
            MiscMenu.AddSeparator(5);
            MiscMenu.AddLabel("Captive Audience (E) settings :");
            MiscMenu.Add("Plugins.Jhin.MiscMenu.EAntiGapcloser", new CheckBox("Cast against gapclosers"));

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawings settings for Jhin addon");

            DrawingsMenu.AddLabel("Basic settings :");
            DrawingsMenu.Add("Plugins.Jhin.DrawingsMenu.DrawSpellRangesWhenReady",
                new CheckBox("Draw spell ranges only when they are ready"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Dancing Grenade (Q) settings :");
            DrawingsMenu.Add("Plugins.Jhin.DrawingsMenu.DrawQ", new CheckBox("Draw Q range"));
            DrawingsMenu.Add("Plugins.Jhin.DrawingsMenu.DrawQColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[0].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Deadly Flourish (W) settings :");
            DrawingsMenu.Add("Plugins.Jhin.DrawingsMenu.DrawW", new CheckBox("Draw W range"));
            DrawingsMenu.Add("Plugins.Jhin.DrawingsMenu.DrawWColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[1].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Captive Audience (E) settings :");
            DrawingsMenu.Add("Plugins.Jhin.DrawingsMenu.DrawE", new CheckBox("Draw E range"));
            DrawingsMenu.Add("Plugins.Jhin.DrawingsMenu.DrawEColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[2].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Curtain Call (R) settings :");
            DrawingsMenu.Add("Plugins.Jhin.DrawingsMenu.DrawR", new CheckBox("Draw R range"));
            DrawingsMenu.Add("Plugins.Jhin.DrawingsMenu.DrawRColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[3].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.Add("Plugins.Jhin.DrawingsMenu.DrawInfo", new CheckBox("Draw Infos")).OnValueChange += (a, b) =>
            {
                if (b.NewValue)
                    DamageIndicator.DamageDelegate = HandleDamageIndicator;
                else if (!b.NewValue)
                    DamageIndicator.DamageDelegate = null;
            };
            DrawingsMenu.Add("Plugins.Jhin.DrawingsMenu.InfoColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[4].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddLabel("Draws damage indicator and minions killable from Q");
        }

        protected override void PermaActive()
        {
            Orbwalker.DisableAttacking = IsCastingR;
            Orbwalker.DisableMovement = IsCastingR;

            if ((Player.Instance.Spellbook.GetSpell(SpellSlot.R).Name.ToLowerInvariant() == "jhinr") && !R.IsReady() &&
                (Player.Instance.Spellbook.GetSpell(SpellSlot.R).Name.ToLowerInvariant() != "jhinrshot"))
            {
                IsCastingR = false;
                REndPosition = Vector3.Zero;
            }

            Modes.PermaActive.Execute();
        }

        protected override void ComboMode()
        {
            if (!IsPreAttack && !Player.Instance.HasSheenBuff() && !IsCastingR)
            {
                Modes.Combo.Execute();
            }
        }

        protected override void HarassMode()
        {
            if (!IsPreAttack && !Player.Instance.HasSheenBuff() && !IsCastingR)
            {
                Modes.Harass.Execute();
            }
        }

        protected override void LaneClear()
        {
            if (!IsPreAttack && !IsCastingR)
            {
                Modes.LaneClear.Execute();
            }
        }

        protected override void JungleClear()
        {
            if (!IsPreAttack && !IsCastingR)
            {
                Modes.JungleClear.Execute();
            }
        }

        protected override void LastHit()
        {
            if (!IsCastingR)
            {
                Modes.LastHit.Execute();
            }
        }

        protected override void Flee()
        {
            Modes.Flee.Execute();
        }

        protected static class Settings
        {
            internal static class Combo
            {
                public static bool UseQ => MenuManager.MenuValues["Plugins.Jhin.ComboMenu.UseQ"];

                public static bool UseW => MenuManager.MenuValues["Plugins.Jhin.ComboMenu.UseW"];

                public static bool UseE => MenuManager.MenuValues["Plugins.Jhin.ComboMenu.UseE"];
                
                public static bool UseR => MenuManager.MenuValues["Plugins.Jhin.ComboMenu.UseR"];

                public static int RDelay => MenuManager.MenuValues["Plugins.Jhin.ComboMenu.RDelay", true];

                /// <summary>
                /// 0 - In Combo mode
                /// 1 - Keybind
                /// 2 - Automatic
                /// </summary>
                public static int RMode => MenuManager.MenuValues["Plugins.Jhin.ComboMenu.RMode", true];
                
                public static bool EnableFowPrediction => MenuManager.MenuValues["Plugins.Jhin.ComboMenu.EnableFowPrediction"];
                
                public static bool RKeybind => MenuManager.MenuValues["Plugins.Jhin.ComboMenu.RKeybind"];
            }

            internal static class Harass
            {
                public static bool UseQ => MenuManager.MenuValues["Plugins.Jhin.HarassMenu.UseQ"];

                public static int MinManaQ => MenuManager.MenuValues["Plugins.Jhin.HarassMenu.MinManaQ", true];

                public static bool UseW => MenuManager.MenuValues["Plugins.Jhin.HarassMenu.UseW"];

                public static int MinManaW => MenuManager.MenuValues["Plugins.Jhin.HarassMenu.MinManaW", true];
            }

            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies => MenuManager.MenuValues["Plugins.Jhin.LaneClearMenu.EnableLCIfNoEn"];

                public static int ScanRange => MenuManager.MenuValues["Plugins.Jhin.LaneClearMenu.ScanRange", true];

                public static int AllowedEnemies => MenuManager.MenuValues["Plugins.Jhin.LaneClearMenu.AllowedEnemies", true];

                public static bool UseQInLaneClear => MenuManager.MenuValues["Plugins.Jhin.LaneClearMenu.UseQInLaneClear"];

                public static bool UseQInJungleClear => MenuManager.MenuValues["Plugins.Jhin.LaneClearMenu.UseQInJungleClear"];

                public static int MinManaQ => MenuManager.MenuValues["Plugins.Jhin.LaneClearMenu.MinManaQ", true];

                public static int MinMinionsKilledFromQ => MenuManager.MenuValues["Plugins.Jhin.LaneClearMenu.MinMinionsKilledFromQ", true];
                
                public static bool UseWInLaneClear => MenuManager.MenuValues["Plugins.Jhin.LaneClearMenu.UseWInLaneClear"];

                public static bool UseWInJungleClear => MenuManager.MenuValues["Plugins.Jhin.LaneClearMenu.UseWInJungleClear"];

                public static int MinManaW => MenuManager.MenuValues["Plugins.Jhin.LaneClearMenu.MinManaW", true];
            }

            internal static class Misc
            {
                public static bool EnableKillsteal => MenuManager.MenuValues["Plugins.Jhin.MiscMenu.EnableKillsteal"];

                public static bool WFowPrediction => MenuManager.MenuValues["Plugins.Jhin.MiscMenu.WFowPrediction"];

                public static bool WAntiGapcloser => MenuManager.MenuValues["Plugins.Jhin.MiscMenu.WAntiGapcloser"];

                public static bool EAntiGapcloser => MenuManager.MenuValues["Plugins.Jhin.MiscMenu.EAntiGapcloser"];
            }

            internal static class Drawings
            {
                public static bool DrawSpellRangesWhenReady => MenuManager.MenuValues["Plugins.Jhin.DrawingsMenu.DrawSpellRangesWhenReady"];
                
                public static bool DrawQ => MenuManager.MenuValues["Plugins.Jhin.DrawingsMenu.DrawQ"];

                public static bool DrawW => MenuManager.MenuValues["Plugins.Jhin.DrawingsMenu.DrawW"];

                public static bool DrawE => MenuManager.MenuValues["Plugins.Jhin.DrawingsMenu.DrawE"];

                public static bool DrawR => MenuManager.MenuValues["Plugins.Jhin.DrawingsMenu.DrawR"];

                public static bool DrawInfo => MenuManager.MenuValues["Plugins.Jhin.DrawingsMenu.DrawInfo"];
            }
        }

        protected static class Damage
        {
            public static int[] QDamage { get; } = {0, 50, 75, 100, 125, 150};
            public static float[] QDamageTotalAdMod { get; } = {0, 0.3f, 0.35f, 0.4f, 0.45f, 0.5f};
            public static float QDamageBonusApMod { get; } = 0.6f;
            public static int[] WDamageOnChampions { get; } = { 0, 50, 85, 120, 155, 190 };
            public static float WDamageOnChampionsTotalAdMod { get; } = 0.5f;
            public static int[] RMinimumDamage { get; } = {0, 40, 100, 160};
            public static float RMinimumDamageTotalAdMod { get; } = 0.2f;
            public static int[] RMaximumDamage { get; } = { 0, 140, 350, 560 };
            public static float RMaximumDamageTotalAdMod { get; } = 0.7f;

            private static float _lastScanTick;
            private static float _lastAttackDamage;
            
            private static CustomCache<int, float> QDamages { get; } = Cache.Resolve<CustomCache<int, float>>();
            private static CustomCache<int, float> WDamages { get; } = Cache.Resolve<CustomCache<int, float>>();
            private static CustomCache<int, float> RDamages { get; } = Cache.Resolve<CustomCache<int, float>>();
            private static CustomCache<int, float> CritDamage { get; } = Cache.Resolve<CustomCache<int, float>>();
            private static CustomCache<int, bool> IsKillable { get; } = Cache.Resolve<CustomCache<int, bool>>();
            
            public static float GetRDamage(Obj_AI_Base unit, bool isFourthShoot = false)
            {
                if (!isFourthShoot && MenuManager.IsCacheEnabled && RDamages.Exist(unit.NetworkId))
                {
                    RDamages.RefreshRate = 1000;

                    return RDamages.Get(unit.NetworkId);
                }

                var missingHealthAdditionalDamagePercent = 1 + ( 100 - unit.HealthPercent ) * 0.025f;
                var minimumDamage = RMinimumDamage[R.Level] + GetRealAttackDamage() * RMinimumDamageTotalAdMod;
                var maximumDamage = RMaximumDamage[R.Level] + GetRealAttackDamage() * RMaximumDamageTotalAdMod;
                float damage;

                if (!isFourthShoot)
                {
                    damage = Math.Min(minimumDamage * missingHealthAdditionalDamagePercent, maximumDamage);
                }
                else
                {
                    damage = Math.Min(minimumDamage * missingHealthAdditionalDamagePercent, maximumDamage) * (Player.Instance.HasItem(ItemId.Infinity_Edge) ? 2.5f : 2f) * (1 + Player.Instance.FlatCritChanceMod);
                }

                var finalDamage = Player.Instance.CalculateDamageOnUnit(unit, DamageType.Physical, damage);

                if (!isFourthShoot && MenuManager.IsCacheEnabled)
                {
                    RDamages.Add(unit.NetworkId, finalDamage);
                }

                return finalDamage;
            }

            public static float GetRealAttackDamage()
            {
                if (Game.Time*1000 - _lastScanTick < 10000)
                {
                    return _lastAttackDamage;
                }

                float[] additionalAttackDamage =
                {
                    0, 0.02f, 0.03f, 0.04f, 0.05f, 0.06f, 0.07f, 0.08f, 0.1f, 0.12f, 0.14f,
                    0.16f, 0.18f, 0.2f, 0.24f, 0.28f, 0.32f, 0.36f, 0.4f
                };
                int[] attackSpeedItemsId =
                {
                    3006, 3153, 1042, 2015, 3115, 3046, 3094, 1043, 3085, 3087, 3101, 3078, 3091,
                    3086
                };

                var addicionalAdFromCritChance = 0f;
                var additionalAdFromAttackSpeed = 0f;
                var additionalAttackSpeed = (from i in attackSpeedItemsId
                    where Player.Instance.HasItem(i)
                    select Item.ItemData.FirstOrDefault(x => x.Key == (ItemId) i)
                    into data
                    select data.Value.Stats.PercentAttackSpeedMod).Sum();

                for (var i = 0f; i < 1; i += 0.1f)
                {
                    if ((Player.Instance.FlatCritChanceMod >= i) && (Player.Instance.FlatCritChanceMod < i + 0.1f))
                    {
                        addicionalAdFromCritChance = Player.Instance.TotalAttackDamage*(0.04f*(i*10));
                    }
                    if ((additionalAttackSpeed >= i) && (additionalAttackSpeed < i + 0.1f))
                    {
                        additionalAdFromAttackSpeed = Player.Instance.TotalAttackDamage*(0.025f*(i*10));
                    }
                }
                var totalAd = Player.Instance.TotalAttackDamage +
                              Player.Instance.TotalAttackDamage*additionalAttackDamage[Player.Instance.Level] +
                              additionalAdFromAttackSpeed + addicionalAdFromCritChance;

                _lastScanTick = Game.Time*1000;
                _lastAttackDamage = totalAd;

                return totalAd;
            }

            public static float Get4ThShootDamage(Obj_AI_Base unit)
            {
                if (MenuManager.IsCacheEnabled && CritDamage.Exist(unit.NetworkId))
                {
                    CritDamage.RefreshRate = 1000;

                    return CritDamage.Get(unit.NetworkId);
                }

                var bonusDamage = 0f;
                if (Player.Instance.Level < 6)
                    bonusDamage = 0.15f;
                else if ((Player.Instance.Level < 11) && (Player.Instance.Level >= 6))
                    bonusDamage = 0.20f;
                else if (Player.Instance.Level >= 11)
                    bonusDamage = 0.25f;

                var damage = GetRealAttackDamage() * 1.75f + (unit.MaxHealth - unit.Health) * bonusDamage;

                var finalDamage = Player.Instance.CalculateDamageOnUnit(unit, DamageType.Physical,
                    Player.Instance.HasItem(ItemId.Infinity_Edge) ? damage*1.5f : damage, false, true);

                if (MenuManager.IsCacheEnabled)
                    CritDamage.Add(unit.NetworkId, finalDamage);

                return finalDamage;
            }

            public static float GetQDamage(Obj_AI_Base unit)
            {
                if (MenuManager.IsCacheEnabled && QDamages.Exist(unit.NetworkId))
                {
                    QDamages.RefreshRate = 1000;

                    return QDamages.Get(unit.NetworkId);
                }

                var damage = Player.Instance.CalculateDamageOnUnit(unit, DamageType.Physical,
                    QDamage[Q.Level] + GetRealAttackDamage() * QDamageTotalAdMod[Q.Level] +
                    Player.Instance.FlatMagicDamageMod * QDamageBonusApMod, false, true);

                if (MenuManager.IsCacheEnabled)
                    QDamages.Add(unit.NetworkId, damage);

                return damage;
            }

            public static float GetWDamage(Obj_AI_Base unit)
            {
                if (MenuManager.IsCacheEnabled && WDamages.Exist(unit.NetworkId))
                {
                    WDamages.RefreshRate = 1000;

                    return WDamages.Get(unit.NetworkId);
                }

                var damage = WDamageOnChampions[W.Level] +
                             GetRealAttackDamage() * WDamageOnChampionsTotalAdMod;

                if (unit.GetType() != typeof(AIHeroClient))
                {
                    damage *= 0.75f;
                }

                var finalDamage = Player.Instance.CalculateDamageOnUnit(unit, DamageType.Physical, damage, false, true);

                if (MenuManager.IsCacheEnabled)
                    WDamages.Add(unit.NetworkId, damage);

                return finalDamage;
            }

            public static bool IsTargetKillableFromW(Obj_AI_Base unit)
            {
                if (MenuManager.IsCacheEnabled && IsKillable.Exist(unit.NetworkId))
                {
                    IsKillable.RefreshRate = 100;

                    return IsKillable.Get(unit.NetworkId);
                }

                bool output;

                if (unit.GetType() != typeof(AIHeroClient))
                {
                    output = unit.TotalHealthWithShields() <= GetWDamage(unit);

                    if(MenuManager.IsCacheEnabled)
                        IsKillable.Add(unit.NetworkId, output);

                    return output;
                }

                var enemy = (AIHeroClient) unit;

                if (enemy.HasSpellShield() || enemy.HasUndyingBuffA())
                    return false;

                if (enemy.ChampionName != "Blitzcrank")
                {
                    output = enemy.TotalHealthWithShields() < GetWDamage(enemy);

                    if (MenuManager.IsCacheEnabled)
                        IsKillable.Add(unit.NetworkId, output);

                    return output;
                }

                if (!enemy.HasBuff("BlitzcrankManaBarrierCD") && !enemy.HasBuff("ManaBarrier"))
                {
                    output = enemy.TotalHealthWithShields() + enemy.Mana/2 < GetWDamage(enemy);

                    if (MenuManager.IsCacheEnabled)
                        IsKillable.Add(unit.NetworkId, output);

                    return output;
                }

                output = enemy.TotalHealthWithShields() < GetWDamage(enemy);

                if (MenuManager.IsCacheEnabled)
                    IsKillable.Add(unit.NetworkId, output);

                return output;
            }
        }
    }
}