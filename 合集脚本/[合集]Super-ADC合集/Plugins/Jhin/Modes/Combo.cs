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

namespace Marksman_Master.Plugins.Jhin.Modes
{
    internal class Combo : Jhin
    {
        public static void Execute()
        {
            if (Q.IsReady() && Settings.Combo.UseQ && (Player.Instance.Mana - (30 + (Q.Level - 1)*5) > (R.IsReady() ? 100 : 0)))
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

                if (target != null && !target.HasSpellShield() && !target.HasUndyingBuffA())
                {
                    Q.Cast(target);
                }
            }

            if (W.IsReady() && Settings.Combo.UseW && (Player.Instance.Mana - (50 + (Q.Level - 1)*10) > (R.IsReady() ? 100 : 0)))
            {
                var enemiesInAaRange = Player.Instance.CountEnemiesInRangeCached(Player.Instance.GetAutoAttackRange());

                if(enemiesInAaRange > 2)
                    return;

                var possibleTargets = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                    x => x.IsValidTargetCached(W.Range) && HasSpottedBuff(x) && !x.HasUndyingBuffA());

                var target = TargetSelector.GetTarget(possibleTargets, DamageType.Physical);

                if (target != null)
                {
                    var wPrediction = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput
                    {
                        Range = W.Range,
                        Target = target,
                        Speed = int.MaxValue,
                        RangeCheckFrom = Player.Instance.Position,
                        Delay = 1,
                        From = Player.Instance.Position,
                        Radius = W.Width,
                        Type = SkillShotType.Linear,
                        CollisionTypes = new HashSet<CollisionType> { Prediction.Manager.PredictionSelected == "ICPrediction" ? CollisionType.AiHeroClient : CollisionType.ObjAiMinion }
                    });

                    if (wPrediction.HitChance >= HitChance.High)
                    {
                        Player.CastSpell(SpellSlot.W, wPrediction.CastPosition);
                        return;
                    }
                }
            }

            if (!E.IsReady() || !Settings.Combo.UseE || IsPreAttack)
                return;

            if (StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x => x.IsValidTarget() && x.Distance(Player.Instance.ServerPosition) < 300 && x.IsMelee && x.Path.Last().Distance(Player.Instance) < 400).Any())
            {
                E.Cast(Player.Instance.ServerPosition);
                return;
            }

            var t = E.GetTarget();

            if (t != null)
            {
                var ePrediction = E.GetPrediction(t);

                if (ePrediction.HitChancePercent >= 75 && ePrediction.CastPosition.Distance(t) > 250)
                {
                    E.Cast(ePrediction.CastPosition);
                    return;
                }
            }

            foreach (var target in from target in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x => x.IsValidTargetCached(E.Range))
                let duration = target.GetMovementBlockedDebuffDuration() * 1000
                where
                    !(duration <= 0)
                where duration > 400
                select target)
            {
                E.Cast(target.Position);
            }
        }
    }
}