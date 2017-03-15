#region Licensing
// ---------------------------------------------------------------------
// <copyright file="IncomingDamage.cs" company="EloBuddy">
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

namespace Marksman_Master.Utils
{
    internal static class IncomingDamage
    {
        private static readonly List<IncomingDamageArgs> IncomingDamages = new List<IncomingDamageArgs>();
        private static readonly List<int> Champions = new List<int>();

        static IncomingDamage()
        {
            Game.OnTick += Game_OnTick;

            Obj_AI_Base.OnBasicAttack += Obj_AI_Base_OnBasicAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            AttackableUnit.OnDamage += Obj_AI_Base_OnDamage;

            ChampionTracker.Initialize(ChampionTrackerFlags.PostBasicAttackTracker);
            ChampionTracker.OnPostBasicAttack += ChampionTracker_OnPostBasicAttack;
        }

        private static void ChampionTracker_OnPostBasicAttack(object sender, PostBasicAttackArgs e)
        {
            if (Player.Instance.IsDead)
                return;

            if ((e.Sender == null) || (e.Target == null))
                return;

            var heroSender = e.Sender;
            var target = e.Target as AIHeroClient;

            if ((heroSender == null) || (target == null) || Champions.All(x => target.NetworkId != x) || (target.Team == heroSender.Team))
                return;

            IncomingDamages.Add(new IncomingDamageArgs
            {
                Sender = heroSender,
                Target = target,
                Tick = Core.GameTickCount,
                Damage = heroSender.GetAutoAttackDamageCached(target, true),
                IsTargetted = true
            });
        }

        private static void Obj_AI_Base_OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            IncomingDamages.RemoveAll(x => (x.Sender.NetworkId == args.Source.NetworkId) &&
                                           (x.Target.NetworkId == args.Target.NetworkId));
        }

        private static void Obj_AI_Base_OnBasicAttack(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (Player.Instance.IsDead)
                return;

            var turret = sender as Obj_AI_Turret;
            var target = args.Target as AIHeroClient;

            if ((turret == null) || (target == null) || Champions.All(x => target.NetworkId != x) || (target.Team == turret.Team))
                return;

            var damage = IsInhibitorOrNexusTurret(turret.BaseSkinName)
                ? turret.GetAutoAttackDamageCached(target)*0.275f
                : turret.GetAutoAttackDamageCached(target);

            IncomingDamages.Add(new IncomingDamageArgs
            {
                Sender = turret,
                Target = target,
                IsTurretShot = true,
                Tick = Core.GameTickCount,
                IsTargetted = false,
                IsSkillShot = false,
                Damage = damage
            });
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (Player.Instance.IsDead)
                return;

            var heroSender = sender as AIHeroClient;
            var target = args.Target as AIHeroClient;

            if ((heroSender != null) && (target != null) && Champions.Exists(x => target.NetworkId == x) &&
                !Equals(target.Team, heroSender.Team))
            {
                IncomingDamages.Add(new IncomingDamageArgs
                {
                    Sender = heroSender,
                    Target = target,
                    Tick = Core.GameTickCount,
                    Damage = heroSender.GetSpellDamageCached(target, args.Slot),
                    IsTargetted = true
                });
            }
            if ((heroSender == null) || (target != null))
                return;

            if (args.SData.TargettingType != SpellDataTargetType.LocationAoe)
                return;

            var polygon = new Geometry.Polygon.Circle(args.End, args.SData.CastRadius);
            var polygon2 = new Geometry.Polygon.Circle(args.End, args.SData.CastRadiusSecondary);

            foreach (
                var hero in
                EntityManager.Heroes.AllHeroes.Where(
                    ally =>
                        (Champions.Exists(x => ally.NetworkId == x) && polygon.IsInside(ally)) ||
                        (polygon2.IsInside(ally) && (ally.Team != heroSender.Team))))
            {
                IncomingDamages.Add(new IncomingDamageArgs
                {
                    Sender = heroSender,
                    Target = hero,
                    IsSkillShot = true,
                    Damage = heroSender.GetSpellDamageCached(hero, heroSender.GetSpellSlotFromName(args.SData.Name)),
                    Tick = Core.GameTickCount,
                    IsTargetted = false,
                    IsTurretShot = false
                });
            }
        }

        public static float GetIncomingDamage(AIHeroClient hero)
        {
            if (Champions.Contains(hero.NetworkId))
                return
                    IncomingDamages.Where(x => x.Target.NetworkId.Equals(hero.NetworkId))
                        .Sum(incomingDamageArgse => incomingDamageArgse.Damage);
            {
                Champions.Add(hero.NetworkId);
                Core.DelayAction(() => Champions.RemoveAll(x=> x == hero.NetworkId), 15000);
            }

            return IncomingDamages.Where(x => x.Target.NetworkId.Equals(hero.NetworkId)).Sum(incomingDamageArgse => incomingDamageArgse.Damage);
        }

        private static void Game_OnTick(EventArgs args)
        {
            IncomingDamages.RemoveAll(x => Core.GameTickCount - x.Tick > 1250);
        }

        private class IncomingDamageArgs
        {
            public Obj_AI_Base Sender { get; set; }
            public AIHeroClient Target { get; set; }
            public int Tick { get; set; }
            public float Damage { get; set; }
            public bool IsTurretShot { get; set; }
            public bool IsTargetted { get; set; }
            public bool IsSkillShot { get; set; }
        }

        private static bool IsInhibitorOrNexusTurret(string name)
        {
            return name.Contains("Chaos3") || name.Contains("Chaos4") || name.Contains("Order3") || name.Contains("Order4");
        }
    }
}