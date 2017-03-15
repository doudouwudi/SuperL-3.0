#region Licensing
// ---------------------------------------------------------------------
// <copyright file="PermaActive.cs" company="EloBuddy">
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

namespace Marksman_Master.Plugins.Ashe.Modes
{
    internal class PermaActive : Ashe
    {
        public static void Execute()
        {
            if (R.IsReady() && Settings.Combo.UseR && StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x => x.IsValidTargetCached(Settings.Combo.RMaximumRange) && (x.HealthPercent < 50) && !x.HasSpellShield() && !x.HasUndyingBuffA()).Any())
            {
                foreach (var target in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x => x.IsValidTargetCached(Settings.Combo.RMaximumRange)).OrderBy(TargetSelector.GetPriority))
                {
                    var incomingDamage = IncomingDamage.GetIncomingDamage(target);

                    if (incomingDamage > target.TotalHealthWithShields())
                        return;

                    var damage = incomingDamage + Player.Instance.GetSpellDamageCached(target, SpellSlot.R)-10;

                    if (target.Hero == Champion.Blitzcrank && !target.HasBuff("BlitzcrankManaBarrierCD") && !target.HasBuff("ManaBarrier"))
                    {
                        damage -= target.Mana / 2;
                    }

                    if(target.TotalHealthWithShields(true) < Player.Instance.GetAutoAttackDamageCached(target, true)*2 && Player.Instance.IsInAutoAttackRange(target))
                        continue;

                    if (target.TotalHealthWithShields(true) > damage)
                        continue;

                    var rPrediction = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput
                    {
                        CollisionTypes = new HashSet<CollisionType> { Prediction.Manager.PredictionSelected == "ICPrediction" ? CollisionType.AiHeroClient : CollisionType.ObjAiMinion },
                        Delay = .25f,
                        From = Player.Instance.Position,
                        Radius = 130,
                        Range = Settings.Combo.RMaximumRange,
                        RangeCheckFrom = Player.Instance.Position,
                        Speed = R.Speed,
                        Target = target,
                        Type = SkillShotType.Linear
                    });

                    if (rPrediction.HitChance < HitChance.High)
                        continue;

                    Misc.PrintDebugMessage($"Casting R on : {target.Hero} to killsteal ! v 1");
                    R.Cast(rPrediction.CastPosition);
                    break;
                }
            }

            if (!W.IsReady() || !Settings.Combo.UseW)
                return;

            foreach (
                var wPrediction in
                    StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                        x =>
                            x.IsValidTargetCached(W.Range) && !x.HasUndyingBuffA() && !x.HasSpellShield() &&
                            x.TotalHealthWithShields() < Player.Instance.GetSpellDamageCached(x, SpellSlot.W))
                        .Select(GetWPrediction)
                        .Where(wPrediction => wPrediction != null && wPrediction.HitChance >= HitChance.Medium))
            {
                W.Cast(wPrediction.CastPosition);
                break;
            }
        }
    }
}