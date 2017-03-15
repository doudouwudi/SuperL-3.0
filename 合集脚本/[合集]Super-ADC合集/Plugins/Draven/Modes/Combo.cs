#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Combo.cs" company="EloBuddy">
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

using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Spells;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Draven.Modes
{
    internal class Combo : Draven
    {
        public static void Execute()
        {
            if (DravenRMissile != null &&
                Player.Instance.Spellbook.GetSpell(SpellSlot.R).Name.ToLowerInvariant() == "dravenrdoublecast" &&
                R.IsReady())
            {
                var pos = Player.Instance.Position.Extend(DravenRMissile.Position, DravenRMissile.DistanceCached(Player.Instance));
                var rectangle = new Geometry.Polygon.Rectangle(Player.Instance.Position.To2D(), pos, 160);
                var entitiesInside = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x => x.IsValidTargetCached() && rectangle.IsInside(x)).ToList();

                if (entitiesInside.Count == 0)
                    return;

                var cloesestEnemy = entitiesInside.OrderBy(x => x.Distance(DravenRMissile.Position)).First();

                var posAfter = Prediction.Position.PredictUnitPosition(cloesestEnemy, 
                    (int) (cloesestEnemy.DistanceCached(DravenRMissile.Position)/1900)*1000); // r return delay

                if (rectangle.IsInside(posAfter))
                {
                    R.Cast();
                    Misc.PrintDebugMessage("hehe xd");
                }
            }

            if (E.IsReady() && Settings.Combo.UseE && !IsPreAttack && (Player.Instance.Mana - 70 > 45 + (R.IsReady() ? 100 : 0)))
            {
                var possibleTargets = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x => x.IsValidTargetCached(E.Range) && !x.HasUndyingBuffA() && !x.HasSpellShield());

                var target = TargetSelector.GetTarget(possibleTargets, DamageType.Physical);

                if (target != null && (IsAfterAttack || Player.Instance.IsInRangeCached(target, Player.Instance.GetAutoAttackRange() + 100)))
                {
                    var ePrediction = E.GetPrediction(target);

                    if (ePrediction.HitChance == HitChance.High)
                    {
                        E.Cast(ePrediction.CastPosition);
                        return;
                    }
                }
            }

            if (!R.IsReady() || !Settings.Combo.UseR || IsPreAttack)
                return;
            
            if (Player.Instance.CountEnemiesInRangeCached(1200) <= 2 && Player.Instance.CountEnemiesInRangeCached(1200) > 0)
            {
                var target = TargetSelector.GetTarget(1200, DamageType.Physical);

                if (target != null && !target.HasUndyingBuffA() && (target.TotalHealthWithShields() <
                    Player.Instance.GetSpellDamageCached(target, SpellSlot.R) + (Player.Instance.IsInAutoAttackRange(target) ? Player.Instance.GetAutoAttackDamageCached(target, true) * 3 : 0)))
                {
                    if(Player.Instance.IsInAutoAttackRange(target) && (target.TotalHealthWithShields() < Player.Instance.GetAutoAttackDamageCached(target, true) * 3))
                        return;

                    var rPrediction = R.GetPrediction(target);

                    if (rPrediction.HitChance == HitChance.High)
                    {
                        R.Cast(rPrediction.CastPosition);
                        return;
                    }
                }
            }

            if (Player.Instance.CountEnemiesInRange(1500) > Player.Instance.CountAlliesInRange(1500))
                return;
            
            foreach (var rPrediction in EntityManager.Heroes.Enemies.Where(unit => unit.IsValidTarget(2200)).Select(enemy => Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput
            {
                CollisionTypes = new HashSet<CollisionType> { Prediction.Manager.PredictionSelected == "ICPrediction" ? CollisionType.AiHeroClient : CollisionType.ObjAiMinion },
                Delay = .5f,
                From = Player.Instance.Position,
                Range = 2200,
                Radius = 160,
                RangeCheckFrom = Player.Instance.Position,
                Speed = 2000,
                Target = enemy,
                Type = SkillShotType.Linear
            })).Where(rPrediction => rPrediction.RealHitChancePercent >= 60).Where(rPrediction => rPrediction.GetCollisionObjects<AIHeroClient>().Count(x => x.IsValidTargetCached()) >= 3))
            {
                R.Cast(rPrediction.CastPosition);
                return;
            }
        }
    }
}
