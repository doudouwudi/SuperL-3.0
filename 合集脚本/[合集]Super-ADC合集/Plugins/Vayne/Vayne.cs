#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Vayne.cs" company="EloBuddy">
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

namespace Marksman_Master.Plugins.Vayne
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    using EloBuddy;
    using EloBuddy.SDK;
    using EloBuddy.SDK.Enumerations;
    using EloBuddy.SDK.Menu;
    using EloBuddy.SDK.Menu.Values;
    using EloBuddy.SDK.Rendering;

    using SharpDX;

    using Cache.Modules;
    using PermaShow.Values;
    using Utils;

    using Color = SharpDX.Color;
    using Text = EloBuddy.SDK.Rendering.Text;

    internal class Vayne : ChampionPlugin
    {
        protected static Spell.Skillshot Q { get; }
        protected static Spell.Active W { get; }
        protected static Spell.Targeted E { get; }
        protected static Spell.Active R { get; }

        internal static Menu ComboMenu { get; set; }
        internal static Menu HarassMenu { get; set; }
        internal static Menu LaneClearMenu { get; set; }
        internal static Menu MiscMenu { get; set; }
        internal static Menu DrawingsMenu { get; set; }

        private static readonly ColorPicker[] ColorPicker;

        private BoolItem DontAa { get; set; }
        private BoolItem SafetyChecks { get; set; }

        protected static BuffInstance GetTumbleBuff => Player.Instance.Buffs.FirstOrDefault(b => b.IsActive && (b.DisplayName.ToLowerInvariant() == "vaynetumble"));

        protected static bool HasTumbleBuff => Player.Instance.Buffs.Any(b => b.IsActive && (b.DisplayName.ToLowerInvariant() == "vaynetumble"));

        protected static bool HasSilverDebuff(Obj_AI_Base unit) => unit.Buffs.Any(b => b.IsActive && (b.DisplayName.ToLowerInvariant() == "vaynesilverdebuff"));

        protected static BuffInstance GetSilverDebuff(Obj_AI_Base unit) => unit.Buffs.FirstOrDefault(b => b.IsActive && (b.DisplayName.ToLowerInvariant() == "vaynesilverdebuff"));

        protected static bool HasInquisitionBuff => Player.Instance.Buffs.Any(b => b.IsActive && (b.DisplayName.ToLowerInvariant() == "vayneinquisition"));

        protected static BuffInstance GetInquisitionBuff => Player.Instance.Buffs.FirstOrDefault(b => b.IsActive && (b.DisplayName.ToLowerInvariant() == "vayneinquisition"));

        private static bool _changingRangeScan;
        private static bool _changingQDirection;
        private static float _lastQCastTime;
        private static readonly Text Text;
        private static readonly Text FlashCondemnText;

        protected static bool IsPostAttack { get; private set; }
        protected static bool IsPostAttackB { get; private set; }
        protected static bool IsPreAttack { get; private set; }

        protected static KeyBind FlashCondemnKeybind { get; set; }

        protected static Spell.Skillshot Flash { get; }

        protected static Vector3 FlashPosition { get; set; }

        protected static float LastTick { get; set; }

        protected static bool HasAnyOrbwalkerFlags => (Orbwalker.ActiveModesFlags & (Orbwalker.ActiveModes.Combo | Orbwalker.ActiveModes.Harass | Orbwalker.ActiveModes.LaneClear | Orbwalker.ActiveModes.LastHit | Orbwalker.ActiveModes.JungleClear | Orbwalker.ActiveModes.Flee)) != 0;
        
        protected static bool IsOnSideToPlayer(Vector3 point) => 
            Math.Abs((Player.Instance.GetPathingDirection() - Player.Instance.Position).Normalized().To2D().DotProduct((point - Player.Instance.Position).Normalized().To2D())) < 0.55;

        protected static bool IsAheadOfPlayer(Vector3 point) =>
            (Player.Instance.GetPathingDirection() - Player.Instance.Position).Normalized().To2D().DotProduct((point - Player.Instance.Position).Normalized().To2D()) > 0.55;

        protected static bool IsBehindPlayer(Vector3 point) =>
            (Player.Instance.GetPathingDirection() - Player.Instance.Position).Normalized().To2D().DotProduct((point - Player.Instance.Position).Normalized().To2D()) < -0.55;

        protected static int CondemnCastTime => 250;
        protected static int CondemnMissileSpeed => 2200;

        protected static float Condemn_ETA(Obj_AI_Base unit, Vector3 checkFrom) => unit.DistanceCached(checkFrom) / CondemnMissileSpeed * 1000 + CondemnCastTime;

        static Vayne()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 320, SkillShotType.Linear);
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Targeted(SpellSlot.E, 765);
            R = new Spell.Active(SpellSlot.R);
            
            ColorPicker = new ColorPicker[3];

            ColorPicker[0] = new ColorPicker("VayneBoundingRadius", new ColorBGRA(10, 255, 129, 74));
            ColorPicker[1] = new ColorPicker("VayneCurrentTarget", new ColorBGRA(255, 0, 134, 255));
            ColorPicker[2] = new ColorPicker("VayneCursorPosition", new ColorBGRA(10, 255, 129, 74));
            
            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
            Orbwalker.OnPostAttack += (target, args) => IsPostAttack = true;
            Game.OnPostTick += args => { IsPostAttack = false; IsPostAttackB = false; };

            ChampionTracker.Initialize(ChampionTrackerFlags.PostBasicAttackTracker);
            ChampionTracker.OnPostBasicAttack += ChampionTracker_OnPostBasicAttack;

            GameObject.OnCreate += Obj_AI_Base_OnCreate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Messages.OnMessage += Messages_OnMessage;
            
            var flashSlot = Player.Instance.GetSpellSlotFromName("summonerflash");

            if ((flashSlot == SpellSlot.Summoner1) || (flashSlot == SpellSlot.Summoner2))
            {
                Flash = new Spell.Skillshot(flashSlot, 475, SkillShotType.Linear);
            }
            
            Text = new Text("", new Font("calibri", 15, FontStyle.Regular));
            FlashCondemnText = new Text("", new Font("calibri", 25, FontStyle.Regular));

            CondemnEvade.Initialize();
            
            Spellbook.OnStopCast += (sender, args) =>
            {
                if(!sender.IsMe || !args.DestroyMissile)
                    return;

                IsPreAttack = false;
            };

            Spellbook.OnCastSpell += (sender, args) =>
            {
                if (!Settings.Misc.NoAaWhileStealth || !HasInquisitionBuff)
                    return;

                if ((args.Slot == SpellSlot.Q) && HasAnyOrbwalkerFlags)
                {
                    _lastQCastTime = Core.GameTickCount;
                }
            };
        }

        protected static bool IsValidDashDirection(Vector3 dashPosition)
        {
            if (Settings.Misc.QDirection == 0)
                return true;

            if ((Settings.Misc.QDirection == 1) && IsOnSideToPlayer(dashPosition))
                return true;

            return (Settings.Misc.QDirection == 2) && (IsAheadOfPlayer(dashPosition) || IsBehindPlayer(dashPosition));
        }

        private static void CastWardingItem(Vector3 position)
        {
            var trinket =
                Player.Instance.InventoryItems.FirstOrDefault(
                    x => (x.Name == "TrinketTotemLvl1") || (x.Name == "TrinketOrbLvl3"));

            if ((trinket != null) && trinket.CanUseItem() && Player.Instance.IsInRange(position, trinket.Name == "TrinketTotemLvl1" ? 620 : 4000))
            {
                trinket.Cast(position);
                return;
            }
            
            var pinkWard = Player.Instance.InventoryItems.FirstOrDefault(x => x.Name == "VisionWard");

            if ((pinkWard != null) && pinkWard.CanUseItem() && Player.Instance.IsInRange(position, 620))
            {
                pinkWard.Cast(position);
            }
        }
        
        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
#region Q
            if (Q.IsReady() && sender.IsEnemy && (sender.Type == GameObjectType.AIHeroClient))
            {
                var enemy = sender as AIHeroClient;
                
                if (enemy != null )
                {
                    var positions = new Geometry.Polygon.Circle(Player.Instance.Position, 300, 50).Points;

                    switch (enemy.Hero)
                    {
                        case Champion.Alistar:
                            {
                                if (args.Slot == SpellSlot.Q)
                                {
                                    var polygon = new Geometry.Polygon.Circle(enemy.Position, 365);
                                    if (polygon.IsInside(Player.Instance) && positions.Any(x => polygon.IsOutside(x)))
                                    {
                                        Q.Cast(
                                            positions.FirstOrDefault(
                                                x =>
                                                    new Geometry.Polygon.Circle(x,
                                                        Player.Instance.BoundingRadius).Points.All(
                                                            p => polygon.IsOutside(p)) &&
                                                    (x.DistanceCached(enemy) - 50 >
                                                     Player.Instance.DistanceCached(enemy)))
                                                .To3D());
                                    }
                                }
                                break;
                            }
                        case Champion.Leona:
                        {
                            if (args.Slot == SpellSlot.R)
                            {
                                var polygon = new Geometry.Polygon.Circle(args.End, 150);
                                if (polygon.IsInside(Player.Instance) && positions.Any(x => polygon.IsOutside(x)))
                                    {
                                        Q.Cast(
                                            positions.FirstOrDefault(
                                                x =>
                                                    new Geometry.Polygon.Circle(x,
                                                        Player.Instance.BoundingRadius).Points.All(
                                                            p => polygon.IsOutside(p)) &&
                                                    (x.DistanceCached(enemy) - 50 >
                                                     Player.Instance.DistanceCached(enemy)))
                                                .To3D());
                                    }
                            }
                            break;
                        }
                        case Champion.Chogath:
                        {
                            if (args.Slot == SpellSlot.Q)
                            {
                                var polygon = new Geometry.Polygon.Circle(args.End, 180);

                                if (polygon.IsInside(Player.Instance))
                                {
                                    var qPos =
                                        positions.FirstOrDefault(
                                            x =>
                                                new Geometry.Polygon.Circle(x,
                                                    Player.Instance.BoundingRadius, 10).Points.All(
                                                        p => polygon.IsOutside(p)) &&
                                                (x.DistanceCached(enemy) >
                                                 Player.Instance.DistanceCached(enemy)))
                                            .To3D();

                                    Q.Cast(qPos);
                                }
                            }
                            break;
                        }
                        case Champion.Thresh:
                        {
                            if (args.Slot == SpellSlot.E)
                            {
                                var endPosition = enemy.Position.Extend(args.End, 400);
                                var startPosition = enemy.Position.Extend(endPosition, -400);
                                var polygon = new Geometry.Polygon.Rectangle(startPosition, endPosition, 90);

                                if (polygon.IsInside(Player.Instance) && positions.Any(x => polygon.IsOutside(x)))
                                {
                                    Q.Cast(
                                        positions.FirstOrDefault(
                                            x =>
                                                new Geometry.Polygon.Circle(x,
                                                    Player.Instance.BoundingRadius).Points.All(
                                                        p => polygon.IsOutside(p)) &&
                                                (x.DistanceCached(enemy) - 50 >
                                                 Player.Instance.DistanceCached(enemy)))
                                            .To3D());
                                }
                            }
                            break;
                            }
                        case Champion.Braum:
                        {
                            if (args.Slot == SpellSlot.R)
                            {
                                var polygon = new Geometry.Polygon.Rectangle(enemy.Position,
                                    enemy.Position.Extend(args.End, 1250).To3D(), 120);

                                if (polygon.IsInside(Player.Instance) && positions.Any(x => polygon.IsOutside(x)))
                                {
                                    Q.Cast(
                                        positions.FirstOrDefault(
                                            x =>
                                                new Geometry.Polygon.Circle(x,
                                                    Player.Instance.BoundingRadius).Points.All(
                                                        p => polygon.IsOutside(p)) &&
                                                (x.DistanceCached(enemy) - 50 >
                                                 Player.Instance.DistanceCached(enemy)))
                                            .To3D());
                                }
                            }
                            break;
                        }
                        case Champion.Sona:
                        {
                            if (args.Slot == SpellSlot.R)
                            {
                                var polygon = new Geometry.Polygon.Rectangle(enemy.Position,
                                    enemy.Position.Extend(args.End, 900).To3D(), 120);

                                if (polygon.IsInside(Player.Instance) && positions.Any(x => polygon.IsOutside(x)))
                                {
                                    Q.Cast(
                                        positions.FirstOrDefault(
                                            x =>
                                                new Geometry.Polygon.Circle(x,
                                                    Player.Instance.BoundingRadius).Points.All(
                                                        p => polygon.IsOutside(p)) &&
                                                (x.DistanceCached(enemy) - 50 >
                                                 Player.Instance.DistanceCached(enemy)))
                                            .To3D());
                                }
                            }
                            break;
                        }
                        case Champion.Ezreal:
                        {
                            if (args.Slot == SpellSlot.R)
                            {
                                var polygon = new Geometry.Polygon.Rectangle(enemy.Position,
                                    enemy.Position.Extend(args.End, 3000).To3D(), 120);

                                if (polygon.IsInside(Player.Instance) && positions.Any(x => polygon.IsOutside(x)))
                                {
                                    Q.Cast(
                                        positions.FirstOrDefault(
                                            x =>
                                                new Geometry.Polygon.Circle(x,
                                                    Player.Instance.BoundingRadius).Points.All(
                                                        p => polygon.IsOutside(p)) &&
                                                (x.DistanceCached(enemy) - 50 >
                                                 Player.Instance.DistanceCached(enemy)))
                                            .To3D());
                                }
                            }
                            break;
                        }
                        case Champion.Jinx:
                        case Champion.Ashe:
                        {
                            if (args.Slot == SpellSlot.R)
                            {
                                var polygon = new Geometry.Polygon.Rectangle(enemy.Position,
                                    enemy.Position.Extend(args.End, 3000).To3D(), 90);

                                if (polygon.IsInside(Player.Instance) && positions.Any(x => polygon.IsOutside(x)))
                                {
                                    Q.Cast(
                                        positions.FirstOrDefault(
                                            x =>
                                                new Geometry.Polygon.Circle(x,
                                                    Player.Instance.BoundingRadius).Points.All(
                                                        p => polygon.IsOutside(p)) &&
                                                (x.DistanceCached(enemy) - 50 >
                                                 Player.Instance.DistanceCached(enemy)))
                                            .To3D());
                                }
                            }
                            break;
                        }
                        case Champion.Draven:
                        {
                            if (args.Slot == SpellSlot.R)
                            {
                                var polygon = new Geometry.Polygon.Rectangle(enemy.Position,
                                    enemy.Position.Extend(args.End, 3000).To3D(), 120);

                                if (polygon.IsInside(Player.Instance) && positions.Any(x => polygon.IsOutside(x)))
                                {
                                    Q.Cast(
                                        positions.FirstOrDefault(
                                            x =>
                                                new Geometry.Polygon.Circle(x,
                                                    Player.Instance.BoundingRadius).Points.All(
                                                        p => polygon.IsOutside(p)) &&
                                                (x.DistanceCached(enemy) - 50 >
                                                 Player.Instance.DistanceCached(enemy)))
                                            .To3D());
                                }
                            }
                            break;
                        }
                        case Champion.Graves:
                        {
                            if (args.Slot == SpellSlot.R)
                            {
                                var polygon = new Geometry.Polygon.Rectangle(enemy.Position,
                                    enemy.Position.Extend(args.End, 1000).To3D(), 120);

                                if (polygon.IsInside(Player.Instance) && positions.Any(x => polygon.IsOutside(x)))
                                {
                                    Q.Cast(
                                        positions.FirstOrDefault(
                                            x =>
                                                new Geometry.Polygon.Circle(x,
                                                    Player.Instance.BoundingRadius).Points.All(
                                                        p => polygon.IsOutside(p)) &&
                                                (x.DistanceCached(enemy) - 50 >
                                                 Player.Instance.DistanceCached(enemy)))
                                            .To3D());
                                }
                            }
                            break;
                        }
                    }
                }
            }
#endregion

            if (E.IsReady() && sender.IsEnemy &&
                args.SData.Name.Equals("summonerflash", StringComparison.CurrentCultureIgnoreCase) && Settings.Misc.EAntiFlash &&
                (sender.Position.Extend(args.End,
                    args.End.DistanceCached(sender) > 475 ? 475 : args.End.DistanceCached(sender))
                    .Distance(Player.Instance) <= 500) && sender.IsValidTarget(E.Range))
            {
                E.Cast(sender);
                return;
            }

            if(!sender.IsMe)
                return;
            
            if ((args.Slot == SpellSlot.E) && Settings.Misc.UseTrinket)
            {
                var heroClient = args.Target as AIHeroClient;

                if (heroClient != null)
                {
                    for (var i = 100; i < 475 + 50; i += 50)
                    {
                        var endPosition = heroClient.Position.Extend(Player.Instance, -i);

                        if (!endPosition.IsGrass())
                            continue;

                        Core.DelayAction(() =>
                        {
                            if (new Geometry.Polygon.Circle(Player.Instance.Position, 100, 50).Points.Any(x => x.IsGrass()))
                                return;
                            
                            CastWardingItem(endPosition.To3D());
                        }, (int)Condemn_ETA(heroClient, Player.Instance.Position));
                        break;
                    }

                }
            }

            if ((args.Slot != SpellSlot.E) || (args.Target == null))
                return;
            
            if (FlashPosition != Vector3.Zero)
            {
                Flash.Cast(FlashPosition);
                FlashPosition = Vector3.Zero;
            }
        }

        private static void Messages_OnMessage(Messages.WindowMessage args)
        {
            if (FlashCondemnKeybind == null)
                return;

            if (args.Message == WindowMessages.KeyDown)
            {
                if ((args.Handle.WParam == FlashCondemnKeybind.Keys.Item1) || (args.Handle.WParam == FlashCondemnKeybind.Keys.Item2))
                {
                    Orbwalker.ActiveModesFlags |= Orbwalker.ActiveModes.Combo;
                }
            }

            if (args.Message != WindowMessages.KeyUp)
                return;

            if ((args.Handle.WParam == FlashCondemnKeybind.Keys.Item1) || (args.Handle.WParam == FlashCondemnKeybind.Keys.Item2))
            {
                Orbwalker.ActiveModesFlags = Orbwalker.ActiveModes.None;
                FlashPosition = Vector3.Zero;
            }
        }

        private static void ChampionTracker_OnPostBasicAttack(object sender, PostBasicAttackArgs e)
        {
            if ((e.Sender == null) || !e.Sender.IsMe)
                return;

            IsPreAttack = false;
            IsPostAttackB = true;

            if (((e.Target.Type == GameObjectType.obj_AI_Turret) ||
                 (e.Target.Type == GameObjectType.obj_BarracksDampener) || (e.Target.Type == GameObjectType.obj_HQ)) && Q.IsReady() && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) && Settings.LaneClear.UseQToLaneClear)
            {
                if ((Player.Instance.CountEnemiesInRangeCached(1000) == 0) &&
                    (Player.Instance.ManaPercent >= Settings.LaneClear.MinMana))
                {
                    Q.Cast(Player.Instance.Position.Extend(Game.CursorPos, 285).To3D());
                }
            }

            if ((e.Target == null) || (e.Target.GetType() != typeof(AIHeroClient)) || !Settings.Misc.EKs || !e.Target.IsValid)
                return;

            var enemy = (AIHeroClient) e.Target;

            if (!enemy.IsValidTargetCached(E.Range) || !HasSilverDebuff(enemy) || (GetSilverDebuff(enemy).Count != 1))
                return;

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) && Settings.Harass.UseE &&
                (Player.Instance.ManaPercent >= Settings.Harass.MinManaToUseE))
            {
                E.Cast(enemy);
                return;
            }

            if (!Damage.IsKillableFromSilverEAndAuto(enemy) ||
                (enemy.TotalHealthWithShields() - IncomingDamage.GetIncomingDamage(enemy) <= 0))
                return;

            Misc.PrintDebugMessage("casting e to ks");

            E.Cast(enemy);

            Misc.PrintInfoMessage($"Casting <b><blue>condemn</blue></b> to execute <c>{enemy.Hero}</c>");
        }
        
        private static void Obj_AI_Base_OnCreate(GameObject sender, EventArgs args)
        {
            if ((sender == null) || !StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x => x.Hero == Champion.Rengar).Any())
                return;

            if ((sender.Name != "Rengar_LeapSound.troy") || !E.IsReady() || Player.Instance.IsDead || !Settings.Misc.EAntiRengar)
                return;

            foreach (var rengar in EntityManager.Heroes.Enemies.Where(x => x.ChampionName == "Rengar").Where(rengar => rengar.Distance(Player.Instance.Position) < 1000).Where(rengar => rengar.IsValidTarget(E.Range) && E.IsReady()))
            {
                Misc.PrintDebugMessage("casting e as anti-rengar");
                E.Cast(rengar);
            }
        }

        private static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            IsPreAttack = true;

            if (!HasInquisitionBuff || !Settings.Misc.NoAaWhileStealth || (Core.GameTickCount - _lastQCastTime > Settings.Misc.NoAaDelay))
                return;

            var client = target as AIHeroClient;

            if ((client != null) && (client.Health > Player.Instance.GetAutoAttackDamageCached(client, true)*3))
            {
                IsPreAttack = false;
                args.Process = false;
            }
        }

        protected override void OnInterruptible(AIHeroClient sender, InterrupterEventArgs args)
        {
            if (!E.IsReady() || !sender.IsValidTargetCached(E.Range))
                return;

            if (args.Delay == 0)
                E.Cast(sender);
            else Core.DelayAction(() => E.Cast(sender), args.Delay);

            Misc.PrintInfoMessage($"Interrupting <c>{sender.ChampionName}'s</c> <in>{args.SpellName}</in>");

            Misc.PrintDebugMessage($"OnInterruptible | Champion : {sender.ChampionName} | SpellSlot : {args.SpellSlot}");
        }

        protected override void OnGapcloser(AIHeroClient sender, GapCloserEventArgs args)
        {
            if (args.End.DistanceCached(Player.Instance.Position) > 300)
                return;

            if (!E.IsReady() || !sender.IsValidTargetCached(E.Range))
            {
                if (Q.IsReady())
                {
                    var list = SafeSpotFinder.PointsInRange(Player.Instance.Position.To2D(), 300, 300);
                    var closestEnemy = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x => x.IsValidTargetCached(1300))
                            .OrderBy(x => x.DistanceCached(Player.Instance))
                            .ToList()[0];

                    var positionToCast = Misc.SortVectorsByDistanceDescending(list.ToList(),
                        closestEnemy.Position.To2D())[0];

                    if (positionToCast != default(Vector2))
                    {
                        Misc.PrintDebugMessage($"OnGapcloser | Champion : {sender.ChampionName} | SpellSlot : {args.SpellSlot}");
                        Q.Cast(positionToCast.To3D());
                        return;
                    }
                }
            }

            if (args.Delay == 0)
                E.Cast(sender);
            else Core.DelayAction(() => E.Cast(sender), args.Delay);

            Misc.PrintDebugMessage($"OnGapcloser | Champion : {sender.ChampionName} | SpellSlot : {args.SpellSlot}");
        }

        protected static bool WillEStun(Obj_AI_Base target, Vector3 from = default(Vector3), int customHitchance = -1, int customPushDistance = -1, int additionalDelay = 0)
        {
            if (target == null)
                return false;

            var checkFrom = from != default(Vector3) ? from : Player.Instance.Position;

            if (!IsECastableOnEnemy(target, checkFrom))
                return false;

            var hitchance = customHitchance > 0 ? customHitchance : Settings.Misc.EHitchance;
            var pushDistance = customPushDistance > 0 ? customPushDistance : Settings.Misc.PushDistance;
            var eta = Condemn_ETA(target, checkFrom);
            var predictedPosition = Prediction.Position.PredictUnitPosition(target, (int) eta + additionalDelay);
            var unitPosition = target.Position.To2D().Shorten(checkFrom.To2D(), -(target.BoundingRadius / 2));
            var position = predictedPosition.Shorten(checkFrom.To2D(), -(target.BoundingRadius / 2));

            if (target.GetMovementBlockedDebuffDuration() > eta/1000 )
            {
                for (var i = 25; i < pushDistance + 50; i += 50)
                {
                    if (!target.Position.Extend(checkFrom, -Math.Min(i, pushDistance)).IsWall())
                        continue;

                    return true;
                }
            }

            if (Settings.Misc.EMethod == 2)
            {
                for (var i = pushDistance; i >= 100; i -= 100)
                {
                    var vec = unitPosition.Extend(Player.Instance.Position, -i);
                    var left = new Vector2[5];
                    var right = new Vector2[5];
                    var var = 18 * i / 100;

                    for (var x = 0; x < 5; x++)
                    {
                        left[x] = unitPosition.Extend(vec + (unitPosition - vec).Normalized().Rotated((float)Math.Max(0, Math.PI / 180 * var)) * Math.Abs(i < 200 ? 50 : 45 * x), i);
                        right[x] = unitPosition.Extend(vec + (unitPosition - vec).Normalized().Rotated((float)-Math.Max(0, Math.PI / 180 * var)) * Math.Abs(i < 200 ? 50 : 45 * x), i);
                    }

                    if (left.All(x => x.IsWall()) && right.All(x => x.IsWall()) && vec.IsWall())
                    {
                        return true;
                    }
                }
            }

            for (var i = 100; i < pushDistance + 50; i += 50)
            {
                var max = i > pushDistance ? pushDistance : i;
                var vec = position.Extend(checkFrom, -max);
                var tPos = unitPosition.Extend(checkFrom, -max);

                if (Settings.Misc.EMethod == 1)
                {
                    var direction = (tPos - unitPosition).Normalized().Perpendicular();

                    Vector2[] vectors =
                    {
                        tPos - direction*(float) Misc.GetNumberInRangeFromProcent(hitchance, 10, target.BoundingRadius),
                        tPos + direction*(float) Misc.GetNumberInRangeFromProcent(hitchance, 10, target.BoundingRadius)
                    };

                    if(!vectors.All(x => x.IsWall()))
                        continue;
                }

                if (vec.IsWall() && tPos.IsWall())
                {
                    return true;
                }
            }
            return false;
        }

        protected static void PerformFlashCondemn()
        {
            if ((Core.GameTickCount - LastTick < 500) || !E.IsReady() || !Flash.IsReady())
                return;

            LastTick = Core.GameTickCount;

            var target = (TargetSelector.SelectedTarget != null) && TargetSelector.SelectedTarget.IsValidTargetCached(E.Range + 475)
                ? TargetSelector.SelectedTarget
                : TargetSelector.GetTarget(E.Range + 475, DamageType.Physical);

            if ((target == null) || WillEStun(target))
                return;

            if (WillEStun(target, Player.Instance.Position.Extend(Game.CursorPos, 450).To3D(), 100, 440, 200))
            {
                E.Cast(target);
                FlashPosition = Player.Instance.Position.Extend(Game.CursorPos, 450).To3D();
                return;
            }

            var points = SafeSpotFinder.PointsInRange(target.Position.To2D(), 500, 100)
                .Where(x => !x.To3D().IsVectorUnderEnemyTower() && (x.Distance(Player.Instance) < 475) && (x.Distance(target) > 150) && WillEStun(target, x.To3D(), 100, 440, 200))
                .ToList();

            foreach (var vector2 in points)
            {
                E.Cast(target);
                FlashPosition = vector2.To3D();
                break;
            }
        }

        protected static bool IsECastableOnEnemy(Obj_AI_Base unit, Vector3 checkFrom)
        {
            return E.IsReady() && unit.IsValidTargetCached() && Player.Instance.IsInRangeCached(unit, E.Range) && !IsPreAttack && !unit.IsZombie && !unit.HasSpellShield();
        }
        
        protected static IEnumerable<AIHeroClient> EnemiesInDirectionOfTheDash(Vector3 dashEndPosition, float maxRangeToEnemy)
        {
            return
                from enemy in
                    StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x => x.IsValidTargetCached(maxRangeToEnemy))

                let dotProduct = (dashEndPosition - Player.Instance.Position).Normalized()
                    .To2D()
                    .DotProduct(enemy.Position.To2D().Normalized())

                where dotProduct >= .65
                select enemy;
        }

        protected override void OnDraw()
        {
            if (MenuManager.IsDebugEnabled)
            {
                foreach (var xd in SafeSpotFinder.PointsInRange(Player.Instance.Position.To2D(), 300, 300).Where(x => IsValidDashDirection(x.To3D())))
                {
                    Circle.Draw(Color.GreenYellow, 5, xd.To3D());

                }

                var t = ObjectManager.Get<Obj_AI_Base>().FirstOrDefault(x => !x.IsMe && x.IsValidTarget(E.Range));

                if (t != null)
                {
                    for (var i = 100; i < Settings.Misc.PushDistance + 50; i += 50)
                    {
                        var max = i > Settings.Misc.PushDistance ? Settings.Misc.PushDistance : i;
                        var unitPosition = t.Position.To2D().Shorten(Player.Instance.Position.To2D(), -(t.BoundingRadius/2));
                        var position = Prediction.Position.PredictUnitPosition(t, (int)Condemn_ETA(t, Player.Instance.Position)).Shorten(Player.Instance.Position.To2D(), -(t.BoundingRadius / 2));
                        var tPos = position.Extend(Player.Instance.Position.To2D(), -max);
                        var direction = (tPos - unitPosition).Normalized().Perpendicular();

                        Vector2[] vectors =
                        {
                            tPos -
                            direction*
                            (float) Misc.GetNumberInRangeFromProcent(Settings.Misc.EHitchance, 10, t.BoundingRadius),
                            tPos +
                            direction*
                            (float) Misc.GetNumberInRangeFromProcent(Settings.Misc.EHitchance, 10, t.BoundingRadius),

                            position.Extend(Player.Instance.Position, -max)
                        };

                        foreach (var vector2 in vectors)
                        {
                            Circle.Draw(Color.GreenYellow, 5, vector2.To3D());
                        }
                    }
                }
            }

            if (_changingRangeScan)
                Circle.Draw(Color.White,
                    LaneClearMenu["Plugins.Vayne.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue, Player.Instance);

            if (_changingQDirection)
            {
                var points = SafeSpotFinder.PointsInRange(Player.Instance.Position.To2D(), 300, 300, 200)
                        .Where(x => IsValidDashDirection(x.To3D())).ToList();

                foreach (var point in points)
                {
                    Line.DrawLine(System.Drawing.Color.FromArgb(200, 82, 5, 41), 50, Player.Instance.Position, point.To3D());
                }
            }

            if ((FlashCondemnKeybind != null) && FlashCondemnKeybind.CurrentValue)
            {
                FlashCondemnText.Position = new Vector2(Drawing.Width * 0.4f, Drawing.Height * 0.8f);
                FlashCondemnText.Color = System.Drawing.Color.Red;
                FlashCondemnText.TextValue = "FLASH CONDEMN IS ACTIVE !";
                FlashCondemnText.Draw();
            }

            if (Settings.Drawings.DrawPlayerBoundingRadius)
            {
                Circle.Draw(ColorPicker[0].Color, Player.Instance.BoundingRadius, 8, Player.Instance.Position);
            }

            if (Settings.Drawings.DrawCurrentOrbwalkingTarget)
            {
                var orbwalkingTarget = TargetSelector.GetTarget(Player.Instance.GetAutoAttackRange(), DamageType.Physical);

                if (orbwalkingTarget != null)
                {
                    Circle.Draw(ColorPicker[1].Color, orbwalkingTarget.BoundingRadius, 8, orbwalkingTarget);
                }
            }

            if (Settings.Drawings.DrawCursorEndPosition)
            {
                switch (Settings.Drawings.DrawCursorEndPositionMode)
                {
                    case 0:
                        Circle.Draw(ColorPicker[2].Color, Player.Instance.BoundingRadius, 4, Game.CursorPos);
                        break;
                    case 1:
                        var linepos = Game.CursorPos;

                        Misc.DrawArrowHead(Player.Instance.Position, linepos, 35, 80, 8, ColorPicker[2].Color.ColorFromColorBGRA());
                        Line.DrawLine(ColorPicker[2].Color.ColorFromColorBGRA(), 8, Player.Instance.Position.Extend(linepos, Player.Instance.BoundingRadius).To3DWorld(), linepos);
                        break;
                }
            }

            if (!Settings.Drawings.DrawInfo)
                return;

            foreach (var source in 
                    StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                        x => HasSilverDebuff(x) && x.IsVisible && x.IsHPBarRendered && x.Position.IsOnScreen()))
            {
                var hpPosition = source.HPBarPosition;
                hpPosition.Y = hpPosition.Y + 30; // tracker friendly.
                var debuff = GetSilverDebuff(source);
                var timeLeft = debuff.EndTime - Game.Time;
                var endPos = timeLeft*0x3e8/32;

                var degree = Misc.GetNumberInRangeFromProcent(timeLeft*1000d/3000d*100d, 3, 110);
                var color = new Misc.HsvColor(degree, 1, 1).ColorFromHsv();

                var c1 = System.Drawing.Color.FromArgb(180, 201, 201, 201);
                var c2 = System.Drawing.Color.FromArgb(180, 173, 255, 47);

                for (var i = 0; i < 3; i++)
                {
                    Drawing.DrawLine(
                        new Vector2(source.HPBarPosition.X + 6 + i * 50, source.HPBarPosition.Y - 35),
                        new Vector2(source.HPBarPosition.X + 6 + i * 50 + 15, source.HPBarPosition.Y - 35),
                       15, debuff.Count <= i ? c1 : c2);
                }

                Text.X = (int) (hpPosition.X + endPos);
                Text.Y = (int) hpPosition.Y + 15; // + text size
                Text.Color = color;
                Text.TextValue = timeLeft.ToString("F1");
                Text.Draw();

                Drawing.DrawLine(hpPosition.X + endPos, hpPosition.Y, hpPosition.X, hpPosition.Y, 1, color);
            }
        }

        protected override void CreateMenu()
        {
            ComboMenu = MenuManager.Menu.AddSubMenu("Combo");
            ComboMenu.AddGroupLabel("Combo mode settings for Vayne addon");

            ComboMenu.AddLabel("Tumble (Q) settings :");
            ComboMenu.Add("Plugins.Vayne.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.AddSeparator(5);

            ComboMenu.Add("Plugins.Vayne.ComboMenu.TryToQE", new CheckBox("Try to Q => E"));
            ComboMenu.AddLabel("Vanyne will try to use Q to set herself in position that will let you condemn the target.");
            ComboMenu.AddSeparator(5);

            ComboMenu.Add("Plugins.Vayne.ComboMenu.UseQToPoke", new CheckBox("Use Q to poke"));
            ComboMenu.AddSeparator(5);

            ComboMenu.Add("Plugins.Vayne.ComboMenu.UseQOnlyToProcW", new CheckBox("Use Q only to proc W stacks", false));
            ComboMenu.AddSeparator(5);

            ComboMenu.Add("Plugins.Vayne.ComboMenu.BlockQsOutOfAARange", new CheckBox("Don't use Q if it leaves range of target", false));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Condemn (E) settings :");
            ComboMenu.Add("Plugins.Vayne.ComboMenu.UseE", new CheckBox("Use E"));
            ComboMenu.AddSeparator(5);

            ComboMenu.Add("Plugins.Vayne.ComboMenu.UseEAgainstMelee", new CheckBox("Use E against melee champions"));
            FlashCondemnKeybind = ComboMenu.Add("Plugins.Vayne.ComboMenu.FlashCondemn", new KeyBind("Flash condemn", false, KeyBind.BindTypes.HoldActive, 'A'));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Final Hour (R) settings :");
            ComboMenu.Add("Plugins.Vayne.ComboMenu.UseR", new CheckBox("Use R", false));
            ComboMenu.AddSeparator(5);

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("Harass mode settings for Vayne addon");

            HarassMenu.AddLabel("Tumble (Q) settings :");
            HarassMenu.Add("Plugins.Vayne.HarassMenu.UseQ", new CheckBox("Use Q"));
            HarassMenu.Add("Plugins.Vayne.HarassMenu.MinManaToUseQ", new Slider("Min mana percentage ({0}%) to use Q", 50, 1));
            HarassMenu.AddSeparator(5);

            HarassMenu.AddLabel("Condemn (E) settings :");
            HarassMenu.Add("Plugins.Vayne.HarassMenu.UseE", new CheckBox("Use E to proc 3rd W stack"));
            HarassMenu.Add("Plugins.Vayne.HarassMenu.MinManaToUseE", new Slider("Min mana percentage ({0}%) to use E", 80, 1));

            LaneClearMenu = MenuManager.Menu.AddSubMenu("Clear mode");
            LaneClearMenu.AddGroupLabel("Lane clear / Jungle Clear mode settings for Vayne addon");

            LaneClearMenu.AddLabel("Basic settings :");
            LaneClearMenu.Add("Plugins.Vayne.LaneClearMenu.EnableLCIfNoEn",
                new CheckBox("Enable lane clear only if no enemies nearby"));
            var scanRange = LaneClearMenu.Add("Plugins.Vayne.LaneClearMenu.ScanRange",
                new Slider("Range to scan for enemies", 1500, 0, 2500));
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
            LaneClearMenu.Add("Plugins.Vayne.LaneClearMenu.AllowedEnemies",
                new Slider("Allowed enemies amount", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Tumble (Q) settings :");
            LaneClearMenu.Add("Plugins.Vayne.LaneClearMenu.UseQToLaneClear", new CheckBox("Use Q to lane clear"));
            LaneClearMenu.Add("Plugins.Vayne.LaneClearMenu.UseQToJungleClear", new CheckBox("Use Q to jungle clear"));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Condemn (E) settings :");
            LaneClearMenu.Add("Plugins.Vayne.LaneClearMenu.UseE", new CheckBox("Use E in jungle clear"));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Mana settings :");
            LaneClearMenu.Add("Plugins.Vayne.LaneClearMenu.MinMana",
                new Slider("Min mana percentage ({0}%) for lane clear and jungle clear", 80, 1));

            MenuManager.BuildAntiGapcloserMenu();
            MenuManager.BuildInterrupterMenu();
            CondemnEvade.BuildMenu();

            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("Misc settings for Vayne addon");

            MiscMenu.AddLabel("Basic settings :");
            MiscMenu.Add("Plugins.Vayne.MiscMenu.NoAAWhileStealth",
                new KeyBind("Dont AutoAttack while stealth", false, KeyBind.BindTypes.PressToggle, 'T')).OnValueChange
                +=
                (sender, args) =>
                {
                    DontAa.Value = args.NewValue;
                };
            MiscMenu.Add("Plugins.Vayne.MiscMenu.NoAADelay", new Slider("Delay", 1000, 0, 1000));
            MiscMenu.AddSeparator(5);

            MiscMenu.AddLabel("Additional Condemn (E) settings :");
            MiscMenu.Add("Plugins.Vayne.MiscMenu.EAntiRengar", new CheckBox("Enable Anti-Rengar"));
            MiscMenu.Add("Plugins.Vayne.MiscMenu.EAntiFlash", new CheckBox("Cast against flashes"));
            MiscMenu.Add("Plugins.Vayne.MiscMenu.Eks", new CheckBox("Use E to killsteal"));
            MiscMenu.Add("Plugins.Vayne.MiscMenu.UseTrinket", new CheckBox("Use trinket if end position is in bush"));
            MiscMenu.Add("Plugins.Vayne.MiscMenu.PushDistance", new Slider("Push distance", 420, 400, 470));
            MiscMenu.Add("Plugins.Vayne.MiscMenu.EHitchance", new Slider("Condemn hitchance : {0}%", 65));
            MiscMenu.AddLabel("More often condemn attempts the lower the Hitchance option is.");
            MiscMenu.AddSeparator(5);
            MiscMenu.Add("Plugins.Vayne.MiscMenu.EMode", new ComboBox("E Mode", 1, "Always", "Only in Combo"));
            MiscMenu.Add("Plugins.Vayne.MiscMenu.EMethod", new ComboBox("E Method", 1, "Fast", "More Accurate", "Old method"));
            MiscMenu.Add("Plugins.Vayne.MiscMenu.ETargeting", new ComboBox("E Targeting", 0, "Current Target", "All enemies"));
            MiscMenu.AddSeparator(5);

            MiscMenu.AddLabel("Additional Tumble (Q) settings :");
            MiscMenu.Add("Plugins.Vayne.MiscMenu.QMode", new ComboBox("Q Mode", 0, "CursorPos", "Auto"));
            var qDirection = MiscMenu.Add("Plugins.Vayne.MiscMenu.QDirection", new ComboBox("Q Direction", 0, "Everywhere", "Only to side", "Only forward/backward"));
            
            qDirection.OnValueChange +=
                (sender, args) =>
                {
                    _changingQDirection = true;
                    Core.DelayAction(() =>
                    {
                        if (!qDirection.IsLeftMouseDown && !qDirection.IsMouseInside)
                        {
                            _changingQDirection = false;
                        }
                    }, 2000);
                };
            MiscMenu.Add("Plugins.Vayne.MiscMenu.QSafetyChecks", new CheckBox("Enable safety checks")).OnValueChange +=
                (sender, args) =>
                {
                    SafetyChecks.Value = args.NewValue;
                };

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawing settings for Vayne addon");
            DrawingsMenu.Add("Plugins.Vayne.DrawingsMenu.DrawInfo", new CheckBox("Draw W debuff info"));
            DrawingsMenu.AddSeparator(5);
            DrawingsMenu.Add("Plugins.Vayne.DrawingsMenu.DrawPlayerBoundingRadius", new CheckBox("Draw player bounding radius"));
            DrawingsMenu.Add("Plugins.Vayne.DrawingsMenu.DrawPlayerBoundingRadiusColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[0].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.Add("Plugins.Vayne.DrawingsMenu.DrawCurrentOrbwalkingTarget",new CheckBox("Draw current orbwalking target"));
            DrawingsMenu.Add("Plugins.Vayne.DrawingsMenu.DrawCurrentOrbwalkingTargetColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
                {
                    if (!b.NewValue)
                        return;

                    ColorPicker[1].Initialize(System.Drawing.Color.Aquamarine);
                    a.CurrentValue = false;
                };
            DrawingsMenu.Add("Plugins.Vayne.DrawingsMenu.DrawCursorEndPosition", new CheckBox("Draw cursor end position", false));
            DrawingsMenu.Add("Plugins.Vayne.DrawingsMenu.DrawCursorEndPositionMode", new ComboBox("Drawing mode", 0, "Circle", "Arrow"));
            DrawingsMenu.Add("Plugins.Vayne.DrawingsMenu.DrawCursorEndPositionColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[2].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };

            DontAa = MenuManager.PermaShow.AddItem("Vanye.SafetyChecks", new BoolItem("Don't auto attack while in stealth", Settings.Misc.NoAaWhileStealth));
            SafetyChecks = MenuManager.PermaShow.AddItem("Vanye.SafetyChecks", new BoolItem("Enable safety checks", Settings.Misc.QSafetyChecks));
        }

        protected override void PermaActive()
        {
            Modes.PermaActive.Execute();
        }

        protected override void ComboMode()
        {
            if (FlashCondemnKeybind.CurrentValue)
            {
                PerformFlashCondemn();
            }

            if (Settings.Combo.UseEAgainstMelee && E.IsReady())
            {
                CondemnEvade.MeleeLogic(StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x => x.IsMelee && x.IsValidTargetCached(E.Range)).ToList());
            }

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
                public static bool UseQ => MenuManager.MenuValues["Plugins.Vayne.ComboMenu.UseQ"];

                // ReSharper disable once InconsistentNaming
                public static bool TryToQE => MenuManager.MenuValues["Plugins.Vayne.ComboMenu.TryToQE"]; 

                public static bool UseQToPoke => MenuManager.MenuValues["Plugins.Vayne.ComboMenu.UseQToPoke"];

                public static bool UseQOnlyToProcW => MenuManager.MenuValues["Plugins.Vayne.ComboMenu.UseQOnlyToProcW"];

                public static bool BlockQsOutOfAaRange => MenuManager.MenuValues["Plugins.Vayne.ComboMenu.BlockQsOutOfAARange"];

                public static bool UseE => MenuManager.MenuValues["Plugins.Vayne.ComboMenu.UseE"];

                public static bool UseEAgainstMelee => MenuManager.MenuValues["Plugins.Vayne.ComboMenu.UseEAgainstMelee"]; 

                public static bool UseR => MenuManager.MenuValues["Plugins.Vayne.ComboMenu.UseR"];
            }

            internal static class Harass
            {
                public static bool UseQ => MenuManager.MenuValues["Plugins.Vayne.HarassMenu.UseQ"];

                public static int MinManaToUseQ => MenuManager.MenuValues["Plugins.Vayne.HarassMenu.MinManaToUseQ", true];

                public static bool UseE => MenuManager.MenuValues["Plugins.Vayne.HarassMenu.UseE"];

                public static int MinManaToUseE => MenuManager.MenuValues["Plugins.Vayne.HarassMenu.MinManaToUseE", true];
            }

            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies => MenuManager.MenuValues["Plugins.Vayne.LaneClearMenu.EnableLCIfNoEn"];

                public static int ScanRange => MenuManager.MenuValues["Plugins.Vayne.LaneClearMenu.ScanRange", true];

                public static int AllowedEnemies => MenuManager.MenuValues["Plugins.Vayne.LaneClearMenu.AllowedEnemies", true];

                public static bool UseQToLaneClear => MenuManager.MenuValues["Plugins.Vayne.LaneClearMenu.UseQToLaneClear"];

                public static bool UseQToJungleClear => MenuManager.MenuValues["Plugins.Vayne.LaneClearMenu.UseQToJungleClear"];

                public static bool UseE => MenuManager.MenuValues["Plugins.Vayne.LaneClearMenu.UseE"]; 

                public static int MinMana => MenuManager.MenuValues["Plugins.Vayne.LaneClearMenu.MinMana", true];
            }

            internal static class Misc
            {
                public static bool NoAaWhileStealth => MenuManager.MenuValues["Plugins.Vayne.MiscMenu.NoAAWhileStealth"];

                public static int NoAaDelay => MenuManager.MenuValues["Plugins.Vayne.MiscMenu.NoAADelay", true];

                public static bool EAntiRengar => MenuManager.MenuValues["Plugins.Vayne.MiscMenu.EAntiRengar"];

                public static bool EAntiFlash => MenuManager.MenuValues["Plugins.Vayne.MiscMenu.EAntiFlash"];
                
                public static bool UseTrinket => MenuManager.MenuValues["Plugins.Vayne.MiscMenu.UseTrinket"]; 

                public static bool EKs => MenuManager.MenuValues["Plugins.Vayne.MiscMenu.Eks"];

                public static int PushDistance => MenuManager.MenuValues["Plugins.Vayne.MiscMenu.PushDistance", true];

                public static int EHitchance => MenuManager.MenuValues["Plugins.Vayne.MiscMenu.EHitchance", true];

                /// <summary>
                /// 0 - Always
                /// 1 - Only in combo
                /// </summary>
                public static int EMode => MenuManager.MenuValues["Plugins.Vayne.MiscMenu.EMode", true];

                /// <summary>
                /// 0 - Fast
                /// 1 - More Accurate
                /// 2 - Old method
                /// </summary>
                public static int EMethod => MenuManager.MenuValues["Plugins.Vayne.MiscMenu.EMethod", true];

                /// <summary>
                /// 0 - Current Target
                /// 1 - All enemies
                /// </summary>
                public static int ETargeting => MenuManager.MenuValues["Plugins.Vayne.MiscMenu.ETargeting", true];

                /// <summary>
                /// 0 - CursorPos
                /// 1 - Auto
                /// </summary>
                public static int QMode => MenuManager.MenuValues["Plugins.Vayne.MiscMenu.QMode", true];

                /// <summary>
                /// 0 - Everywhere
                /// 1 - Only to side
                /// 2 - Only forward/backward
                /// </summary>
                public static int QDirection => MenuManager.MenuValues["Plugins.Vayne.MiscMenu.QDirection", true];
                
                public static bool QSafetyChecks => MenuManager.MenuValues["Plugins.Vayne.MiscMenu.QSafetyChecks"];
            }

            internal static class Drawings
            {
                public static bool DrawInfo => MenuManager.MenuValues["Plugins.Vayne.DrawingsMenu.DrawInfo"];

                public static bool DrawCurrentOrbwalkingTarget => MenuManager.MenuValues["Plugins.Vayne.DrawingsMenu.DrawCurrentOrbwalkingTarget"];

                public static bool DrawPlayerBoundingRadius => MenuManager.MenuValues["Plugins.Vayne.DrawingsMenu.DrawPlayerBoundingRadius"];

                public static bool DrawCursorEndPosition => MenuManager.MenuValues["Plugins.Vayne.DrawingsMenu.DrawCursorEndPosition"];

                /// <summary>
                /// 0 - Circle
                /// 1 - Arrow
                /// </summary>
                public static int DrawCursorEndPositionMode => MenuManager.MenuValues["Plugins.Vayne.DrawingsMenu.DrawCursorEndPositionMode", true];
            }
        }

        protected static class Damage
        {
            public static CustomCache<int, bool> IsKillableFrom3SilverStacksCache { get; set; } =
                StaticCacheProvider.Cache.Resolve<CustomCache<int, bool>>();

            public static CustomCache<int, float> WDamageCache { get; set; } =
                StaticCacheProvider.Cache.Resolve<CustomCache<int, float>>();

            public static CustomCache<int, bool> IsKillableFromSilverEAndAutoCache { get; set; } =
                StaticCacheProvider.Cache.Resolve<CustomCache<int, bool>>();

            public static float[] QBonusDamage { get; } = { 0, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f };
            public static int[] WMinimumDamage { get; } = {0, 40, 60, 80, 100, 120};
            public static float[] WPercentageDamage { get; } = {0, 0.06f, 0.075f, 0.09f, 0.105f, 0.12f};
            public static int[] EDamage { get; } = {0, 45, 80, 115, 150, 185};

            public static bool IsKillableFrom3SilverStacks(Obj_AI_Base unit)
            {
                if (MenuManager.IsCacheEnabled && IsKillableFrom3SilverStacksCache.Exist(unit.NetworkId))
                {
                    return IsKillableFrom3SilverStacksCache.Get(unit.NetworkId);
                }

                var edmg = Player.Instance.CalculateDamageOnUnit(unit, DamageType.Physical,
                    EDamage[E.Level] + Player.Instance.FlatPhysicalDamageMod / 2);

                if (WillEStun(unit))
                    edmg *= 2;

                var damage = GetWDamage(unit) + edmg;

                bool output;

                if (unit.GetType() != typeof(AIHeroClient))
                {
                    output = unit.TotalHealthWithShields() <= damage;

                    if (MenuManager.IsCacheEnabled)
                        IsKillableFrom3SilverStacksCache.Add(unit.NetworkId, output);

                    return output;
                }

                var enemy = (AIHeroClient)unit;

                if (enemy.HasSpellShield() || enemy.HasUndyingBuffA())
                    return false;

                if (enemy.ChampionName != "Blitzcrank")
                {
                    output = enemy.TotalHealthWithShields() < damage;

                    if (MenuManager.IsCacheEnabled)
                        IsKillableFrom3SilverStacksCache.Add(unit.NetworkId, output);

                    return output;
                }

                if (!enemy.HasBuff("BlitzcrankManaBarrierCD") && !enemy.HasBuff("ManaBarrier"))
                {
                    output = enemy.TotalHealthWithShields() + enemy.Mana / 2 < damage;

                    if (MenuManager.IsCacheEnabled)
                        IsKillableFrom3SilverStacksCache.Add(unit.NetworkId, output);

                    return output;
                }

                output = enemy.TotalHealthWithShields() < damage;

                if (MenuManager.IsCacheEnabled)
                    IsKillableFrom3SilverStacksCache.Add(unit.NetworkId, output);

                return output;
            }

            public static bool IsKillableFromSilverEAndAuto(Obj_AI_Base unit)
            {
                if (!IsECastableOnEnemy(unit, Player.Instance.Position))
                    return false;

                if (MenuManager.IsCacheEnabled && IsKillableFromSilverEAndAutoCache.Exist(unit.NetworkId))
                {
                    return IsKillableFromSilverEAndAutoCache.Get(unit.NetworkId);
                }

                bool output;

                var edmg = Player.Instance.CalculateDamageOnUnit(unit, DamageType.Physical,
                    EDamage[E.Level] + Player.Instance.FlatPhysicalDamageMod / 2);

                if (WillEStun(unit))
                    edmg *= 2;

                var aaDamage = Player.Instance.GetAutoAttackDamageCached(unit);

                var damage = GetWDamage(unit) + edmg + aaDamage;

                if (unit.GetType() != typeof(AIHeroClient))
                {
                    output = unit.TotalHealthWithShields() <= damage;

                    if (MenuManager.IsCacheEnabled)
                        IsKillableFromSilverEAndAutoCache.Add(unit.NetworkId, output);

                    return output;
                }

                var enemy = (AIHeroClient)unit;

                if (enemy.HasSpellShield() || enemy.HasUndyingBuffA())
                    return false;

                if (enemy.ChampionName != "Blitzcrank")
                {
                    output = enemy.TotalHealthWithShields() < damage;

                    if (MenuManager.IsCacheEnabled)
                        IsKillableFromSilverEAndAutoCache.Add(unit.NetworkId, output);

                    return output;
                }

                if (!enemy.HasBuff("BlitzcrankManaBarrierCD") && !enemy.HasBuff("ManaBarrier"))
                {
                    output = enemy.TotalHealthWithShields() + enemy.Mana / 2 < damage;

                    if (MenuManager.IsCacheEnabled)
                        IsKillableFromSilverEAndAutoCache.Add(unit.NetworkId, output);

                    return output;
                }

                output = enemy.TotalHealthWithShields() < damage;

                if (MenuManager.IsCacheEnabled)
                    IsKillableFromSilverEAndAutoCache.Add(unit.NetworkId, output);

                return output;
            }

            public static float GetWDamage(Obj_AI_Base unit)
            {
                if (MenuManager.IsCacheEnabled && WDamageCache.Exist(unit.NetworkId))
                {
                    return WDamageCache.Get(unit.NetworkId);
                }

                var damage = Math.Max(WMinimumDamage[W.Level], unit.MaxHealth*WPercentageDamage[W.Level]);

                if ((damage > 200) && !(unit is AIHeroClient))
                    damage = 200;

                damage = Player.Instance.CalculateDamageOnUnit(unit, DamageType.True, damage);

                if (MenuManager.IsCacheEnabled)
                    WDamageCache.Add(unit.NetworkId, damage);

                return damage;
            }
        }

        protected static class CondemnEvade
        {
            public static Menu EEvadeMenu { get; private set; }

#region list
            public static List<TargetedSpell> SpellsList = new List<TargetedSpell>
            {
                new TargetedSpell(Champion.Brand, "Pyroclasm", SpellSlot.R),
                new TargetedSpell(Champion.Caitlyn, "Ace in the Hole", SpellSlot.R),
                new TargetedSpell(Champion.Chogath, "Feast", SpellSlot.R),
                new TargetedSpell(Champion.Darius, "Noxian Guillotine", SpellSlot.R),
                new TargetedSpell(Champion.FiddleSticks, "Terrify", SpellSlot.Q, false, 50, 20, 700),
                new TargetedSpell(Champion.Fiora, "Grand Challenge", SpellSlot.R),
                new TargetedSpell(Champion.Garen, "Demacian Justice", SpellSlot.R),
                new TargetedSpell(Champion.JarvanIV, "Cataclysm", SpellSlot.R),
                new TargetedSpell(Champion.Jayce, "To The Skies!", SpellSlot.Q, true, 100, 30),
                new TargetedSpell(Champion.Karma, "Focused Resolve", SpellSlot.W, true, 100, 30),
                new TargetedSpell(Champion.Kayle, "Reckoning", SpellSlot.Q, true, 50, 30),
                new TargetedSpell(Champion.Khazix, "Taste Their Fear", SpellSlot.Q, false),
                new TargetedSpell(Champion.Kennen, "Lightning Rush (E=>R combo)", SpellSlot.E),
                new TargetedSpell(Champion.Kindred, "Mounting Dread", SpellSlot.E, true, 100, 50),
                new TargetedSpell(Champion.Kled, "Beartrap on a Rope", SpellSlot.Q, true, 100, 50),
                new TargetedSpell(Champion.LeeSin, "Dragon's Rage", SpellSlot.R),
                new TargetedSpell(Champion.Mordekaiser, "Children of the Grave", SpellSlot.R),
                new TargetedSpell(Champion.Morgana, "Soul Shackles", SpellSlot.R),
                new TargetedSpell(Champion.Nasus, "Wither", SpellSlot.W, false, 100, 20),
                new TargetedSpell(Champion.Quinn, "Vault", SpellSlot.E, true, 50, 20),
                new TargetedSpell(Champion.Renekton, "Ruthless Predator", SpellSlot.W),
                new TargetedSpell(Champion.Rammus, "Puncturing Taunt", SpellSlot.E),
                new TargetedSpell(Champion.Riven, "Ki Burst", SpellSlot.W),
                new TargetedSpell(Champion.Ryze, "Rune Prison", SpellSlot.W, true, 100, 20, 500),
                new TargetedSpell(Champion.Shaco, "Two-Shiv Poison", SpellSlot.E, true, 70, 20, 400),
                new TargetedSpell(Champion.Singed, "Fling", SpellSlot.E),
                new TargetedSpell(Champion.TahmKench, "Devour", SpellSlot.W),
                new TargetedSpell(Champion.Teemo, "Blinding Dart", SpellSlot.Q, true, 70, 30, 400),
                new TargetedSpell(Champion.Vayne, "Condemn", SpellSlot.E, false),
                new TargetedSpell(Champion.Warwick, "Infinite Duress", SpellSlot.R)
            };
#endregion

            public static void Initialize()
            {
                Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;

                if (EntityManager.Heroes.Enemies.Any(x => (x.Hero == Champion.Kled) || (x.Hero == Champion.Riven) || (x.Hero == Champion.Kennen)))
                {
                    Game.OnTick += Game_OnTick;
                }
            }

            private static void Game_OnTick(EventArgs args)
            {
                if (!E.IsReady())
                    return;

                if (StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                    x => ((x.Hero == Champion.Riven) || (x.Hero == Champion.Kennen)) && x.IsValidTarget(E.Range)).Any())
                {
                    var rivenData = GetMenuData(Champion.Riven, SpellSlot.W);
                    var riven = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).FirstOrDefault(x => (x.Hero == Champion.Riven) && x.IsValidTarget(E.Range));
                    var kennenData = GetMenuData(Champion.Kennen, SpellSlot.E);
                    var kennen = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).FirstOrDefault(x => (x.Hero == Champion.Kennen) && x.IsValidTarget(E.Range));

                    if ((rivenData != null) && (riven != null) && rivenData.Enabled &&
                        (!rivenData.OnlyInCombo || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)) &&
                        (Player.Instance.HealthPercent <= rivenData.MyHealthMinimumPercent) && (riven.HealthPercent <= rivenData.CasterHealthMinimumPercent))
                    {
                        var hasRbuff = riven.Buffs.Any(x => x.Name.Equals("RivenFengShuiEngine", StringComparison.CurrentCultureIgnoreCase));
                        var predictedRivenPosition = Prediction.Position.PredictUnitPosition(riven, CondemnCastTime);

                        if (predictedRivenPosition.IsInRange(Player.Instance, hasRbuff ? 300 : 260))
                        {
                            E.Cast(riven);

                            Misc.PrintInfoMessage("Casting <blue>condemn</blue> against <c>Riven's</c> <in>Ki Burst</in>");
                            return;
                        }
                    }

                    if ((kennenData != null) && (kennen != null) && kennenData.Enabled &&
                        kennen.Buffs.Any(x => x.Name.Equals("KennenLightningRush", StringComparison.CurrentCultureIgnoreCase)) &&
                        kennen.Spellbook.GetSpell(SpellSlot.R).IsReady && kennen.Spellbook.GetSpell(SpellSlot.R).IsLearned && kennen.IsFacingB(Player.Instance) &&
                        (!kennenData.OnlyInCombo || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)) &&
                        (Player.Instance.HealthPercent <= kennenData.MyHealthMinimumPercent) &&
                        (kennen.HealthPercent <= kennenData.CasterHealthMinimumPercent))
                    {
                        E.Cast(kennen);

                        Misc.PrintInfoMessage("Casting <blue>condemn</blue> against <c>Kennen's</c> <in>Lightning Rush</in>");
                        return;
                    }
                }

                var buff =
                    Player.Instance.Buffs.Find(
                        x => x.IsActive && string.Equals(x.Name, "kledqmark", StringComparison.InvariantCultureIgnoreCase));

                if (buff?.Caster == null)
                    return;

                var target = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).ToList().Find(x => x.NetworkId == buff.Caster.NetworkId);

                if ((target == null) || !target.IsValidTargetCached(E.Range) || (target.DistanceCached(Player.Instance) < 150))
                    return;

                var data = GetMenuData(target.Hero, SpellSlot.Q);

                if ((data == null) || !data.Enabled)
                    return;

                if (data.OnlyInCombo && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                    return;

                if ((target.DistanceCached(Player.Instance) <= data.CasterMinimumDistanceToPlayer) && (Player.Instance.HealthPercent <= data.MyHealthMinimumPercent) &&
                    (target.HealthPercent <= data.CasterHealthMinimumPercent))
                {
                    E.Cast(target);

                    Misc.PrintInfoMessage("Casting <blue>condemn</blue> against <c>Kled's</c> <in>Beartrap on a Rope</in>");
                }
            }

            private static void OnProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
            {
                if (!E.IsReady() || sender.IsMe || Player.Instance.IsDead || (sender.GetType() != typeof (AIHeroClient)) ||
                    !sender.IsEnemy || !sender.IsValidTargetCached(E.Range))
                    return;

                var hero = sender as AIHeroClient;

                if ((hero == null) || (hero.Hero == Champion.Kled) || (hero.Hero == Champion.Rengar) || (hero.Hero == Champion.Riven) || (hero.Hero == Champion.Kennen))
                    return;

                var data = GetMenuData(hero.Hero, args.Slot);

                if ((data == null) || !data.Enabled)
                    return;

                if (data.OnlyInCombo && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                    return;

                if ((hero.DistanceCached(Player.Instance) <= data.CasterMinimumDistanceToPlayer) && (Player.Instance.HealthPercent <= data.MyHealthMinimumPercent) &&
                    (hero.HealthPercent <= data.CasterHealthMinimumPercent))
                {
                    E.Cast(hero);

                    Misc.PrintInfoMessage($"Casting <b><blue>condemn</blue></b> against <c>{hero.Hero}</c> <in>{data.SpellName}</in>");
                }
            }

            public static void BuildMenu()
            {
                if (!EntityManager.Heroes.Enemies.Any(x => SpellsList.Exists(s => s.Champion == x.Hero)))
                {
                    return;
                }

                EEvadeMenu = MenuManager.Menu.AddSubMenu("Condemn evade");

                foreach (var spellData in EntityManager.Heroes.Enemies.Where(enemy => SpellsList.Any(x => x.Champion == enemy.Hero)).Select(enemy => SpellsList.Find(x => x.Champion == enemy.Hero)).Where(spellData => spellData != null))
                {
                    EEvadeMenu.AddGroupLabel(spellData.Champion.ToString());

                    EEvadeMenu.AddLabel($"Spell : [{spellData.SpellSlot}] {spellData.SpellName}");
                    EEvadeMenu.Add($"Plugins.Vayne.EEvadeMenu.{spellData.Champion}.{spellData.SpellSlot}.MyMinHealth", new Slider("My minimum health percentage to cast condemn : {0}%", spellData.MyHealthMinimumPercent));
                    EEvadeMenu.Add($"Plugins.Vayne.EEvadeMenu.{spellData.Champion}.{spellData.SpellSlot}.CasterMinHealth", new Slider("Cast if "+spellData.Champion+" health percentage is higher than : {0}%", spellData.CasterHealthMinimumPercent));
                    EEvadeMenu.Add($"Plugins.Vayne.EEvadeMenu.{spellData.Champion}.{spellData.SpellSlot}.CasterMinDistance", new Slider(spellData.Champion + " minimum distance to player to cast condemn : {0}", spellData.CasterMinimumDistanceToPlayer, 0, 750));
                    EEvadeMenu.Add($"Plugins.Vayne.EEvadeMenu.{spellData.Champion}.{spellData.SpellSlot}.OnlyInCombo", new CheckBox("Only in combo", false));
                    EEvadeMenu.Add($"Plugins.Vayne.EEvadeMenu.{spellData.Champion}.{spellData.SpellSlot}.Enabled", new CheckBox("Enabled", spellData.EnabledByDefault));
                }
            }

            public static TargetedSpell GetMenuData(Champion champion, SpellSlot slot)
            {
                if (!SpellsList.Any(x => (x.Champion == champion) && (x.SpellSlot == slot)))
                    return null;

                if (EEvadeMenu == null)
                    return SpellsList.Find(x => (x.Champion == champion) && (x.SpellSlot == slot));

                var myMinHealth = EEvadeMenu[$"Plugins.Vayne.EEvadeMenu.{champion}.{slot}.MyMinHealth"];
                var casterMinHealth = EEvadeMenu[$"Plugins.Vayne.EEvadeMenu.{champion}.{slot}.CasterMinHealth"];
                var casterMinDistance = EEvadeMenu[$"Plugins.Vayne.EEvadeMenu.{champion}.{slot}.CasterMinDistance"];
                var onlyInCombo = EEvadeMenu[$"Plugins.Vayne.EEvadeMenu.{champion}.{slot}.OnlyInCombo"];
                var enabled = EEvadeMenu[$"Plugins.Vayne.EEvadeMenu.{champion}.{slot}.Enabled"];
                var spellData = SpellsList.Find(x => (x.Champion == champion) && (x.SpellSlot == slot));

                var output = new TargetedSpell(champion, spellData.SpellName, slot, spellData.EnabledByDefault,
                    myMinHealth?.Cast<Slider>().CurrentValue ?? spellData.MyHealthMinimumPercent,
                    casterMinHealth?.Cast<Slider>().CurrentValue ?? spellData.CasterHealthMinimumPercent,
                    casterMinDistance?.Cast<Slider>().CurrentValue ?? spellData.CasterMinimumDistanceToPlayer,
                    onlyInCombo?.Cast<CheckBox>().CurrentValue ?? spellData.EnabledByDefault,
                    enabled?.Cast<CheckBox>().CurrentValue ?? spellData.EnabledByDefault);

                return output;
            }

            public static void MeleeLogic(List<AIHeroClient> meleeChampionsInERange)
            {
                var mostDangerousEnemy = (from aiHeroClient in meleeChampionsInERange
                    orderby aiHeroClient.TotalAttackDamage
                    select aiHeroClient).LastOrDefault();

                var closestEnemy = (from aiHeroClient in meleeChampionsInERange
                    orderby aiHeroClient.DistanceCached(Player.Instance)
                    select aiHeroClient).FirstOrDefault();

                var condemnableBestTarget = (from aiHeroClient in meleeChampionsInERange
                    where WillEStun(aiHeroClient)
                    orderby aiHeroClient.TotalAttackDamage
                    select aiHeroClient).FirstOrDefault();

                if ((mostDangerousEnemy == null) || (closestEnemy == null))
                {
                    return;
                }

                if ((IncomingDamage.GetIncomingDamage(Player.Instance) - 100 > Player.Instance.TotalHealthWithShields()) || (Player.Instance.HealthPercent <= 10))
                {
                    E.Cast(closestEnemy);
                    return;
                }

                if ((meleeChampionsInERange.Count == 1) && (condemnableBestTarget != null))
                {
                    E.Cast(condemnableBestTarget);
                    return;
                }

                if ((Player.Instance.CountEnemiesInRangeCached(1000) >= 2) && (Player.Instance.CountAlliesInRangeCached(800) == 1) && !Q.IsReady() && mostDangerousEnemy.IsFacingB(Player.Instance)
                    && ((Player.Instance.HealthPercent <= 25) || ((mostDangerousEnemy.HealthPercent > Player.Instance.HealthPercent) && (mostDangerousEnemy.HealthPercent > 50))))
                {
                    E.Cast(condemnableBestTarget ?? mostDangerousEnemy);
                    return;
                }

                if ((Player.Instance.CountEnemiesInRangeCached(Player.Instance.GetAutoAttackRange()) == 1) && (Player.Instance.CountAlliesInRangeCached(800) == 1) && !Q.IsReady() && mostDangerousEnemy.IsFacingB(Player.Instance)
                    && Player.Instance.ServerPosition.IsInRange(mostDangerousEnemy.Position.Extend(Player.Instance.ServerPosition, -470), Player.Instance.GetAutoAttackRange() - 50))
                {
                    E.Cast(mostDangerousEnemy);
                }
            }

            public class TargetedSpell
            {
                public Champion Champion { get; }
                public string SpellName { get; }
                public SpellSlot SpellSlot { get; }
                public bool EnabledByDefault { get; }
                public bool Enabled { get; }
                public bool OnlyInCombo { get; }
                public int MyHealthMinimumPercent { get; } = 100;
                public int CasterHealthMinimumPercent { get; } = 100;
                public int CasterMinimumDistanceToPlayer { get; } = 750;

                public TargetedSpell(Champion champion, string spellName, SpellSlot spellSlot, bool enabledByDefault = true)
                {
                    Champion = champion;
                    SpellName = spellName;
                    SpellSlot = spellSlot;
                    EnabledByDefault = enabledByDefault;
                }

                public TargetedSpell(Champion champion, string spellName, SpellSlot spellSlot, bool enabledByDefault,
                    int myHealthMinimumPercent, int casterHealthMinimumPercent, int casterMinimumDistanceToPlayer = 750)
                    : this(champion, spellName, spellSlot, enabledByDefault)
                {
                    MyHealthMinimumPercent = myHealthMinimumPercent;
                    CasterHealthMinimumPercent = casterHealthMinimumPercent;
                    CasterMinimumDistanceToPlayer = casterMinimumDistanceToPlayer;
                }

                public TargetedSpell(Champion champion, string spellName, SpellSlot spellSlot, bool enabledByDefault,
                    int myHealthMinimumPercent, int casterHealthMinimumPercent, int casterMinimumDistanceToPlayer, bool onlyInCombo, bool enabled)
                    : this(champion, spellName, spellSlot, enabledByDefault, myHealthMinimumPercent, casterHealthMinimumPercent, casterMinimumDistanceToPlayer)
                {
                    OnlyInCombo = onlyInCombo;
                    Enabled = enabled;
                }
            }
        }
    }
}