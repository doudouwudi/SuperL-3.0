#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Harass.cs" company="EloBuddy">
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

namespace Marksman_Master.Plugins.Sivir.Modes
{
    using EloBuddy;
    using EloBuddy.SDK;
    using EloBuddy.SDK.Enumerations;
    using Utils;

    internal class Harass : Sivir
    {
        public static void Execute()
        {
            if (Q.IsReady() && Settings.Harass.UseQ && !IsPreAttack && (Player.Instance.ManaPercent >= Settings.Harass.MinManaQ))
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

                if (target != null && !target.HasUndyingBuffA() && !target.HasSpellShield())
                {
                    var qPrediction = Q.GetPrediction(target);

                    if ((qPrediction.HitChance >= HitChance.Medium) &&
                        (target.TotalHealthWithShields() <
                         Player.Instance.GetAutoAttackDamageCached(target, true) * 2 +
                         Player.Instance.GetSpellDamageCached(target, SpellSlot.Q)))
                    {
                        Q.Cast(qPrediction.CastPosition);
                    }
                    else if ((qPrediction.HitChancePercent >= 65) &&
                             (Player.Instance.Mana - 60 > (R.IsReady() ? 100 : 0)))
                    {
                        Q.Cast(qPrediction.CastPosition);
                    }
                }
            }

            if (!W.IsReady() || !Settings.Harass.UseW || !IsPostAttack || Player.Instance.ManaPercent < Settings.Harass.MinManaQ)
                return;

            {
                var target = TargetSelector.GetTarget(Player.Instance.GetAutoAttackRange(), DamageType.Physical);

                if (target != null && (target.TotalHealthWithShields() - IncomingDamage.GetIncomingDamage(target) <
                    Player.Instance.GetAutoAttackDamageCached(target, true) * 2))
                {
                    Misc.PrintDebugMessage($"Casting W on {target.Hero} variant 1");
                    W.Cast();
                }
                else if (target != null && Player.Instance.IsInRangeCached(target, Player.Instance.GetAutoAttackRange() - 50))
                {
                    Misc.PrintDebugMessage($"Casting W on {target.Hero} variant 2");
                    W.Cast();
                }
            }
        }
    }
}
