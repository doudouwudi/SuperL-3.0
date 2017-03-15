#region Licensing
// ---------------------------------------------------------------------
// <copyright file="JungleClear.cs" company="EloBuddy">
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
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;

namespace Marksman_Master.Plugins.Corki.Modes
{
    using Utils;

    internal class JungleClear : Corki
    {
        public static void Execute()
        {
            var jungleMinions = StaticCacheProvider.GetMinions(CachedEntityType.Monsters, x => x.IsValidTargetCached(Player.Instance.GetAutoAttackRange() + 250)).ToList();

            if (!jungleMinions.Any())
                return;

            var target = Orbwalker.GetTarget() as Obj_AI_Base;

            if (Q.IsReady() && Settings.JungleClear.UseQ &&
                (Player.Instance.ManaPercent >= Settings.JungleClear.MinManaToUseQ))
            {
                if (target == null)
                    return;

                var qPrediction = Q.GetPrediction(target);

                if (qPrediction.HitChance >= EloBuddy.SDK.Enumerations.HitChance.High)
                {
                    Q.Cast(qPrediction.CastPosition);
                }
            }

            if (E.IsReady() && Settings.JungleClear.UseE &&
                (Player.Instance.ManaPercent >= Settings.JungleClear.MinManaToUseE))
            {
                if (jungleMinions.Any(x => Player.Instance.IsInRangeCached(x, 500)))
                {
                    E.Cast();
                    return;
                }
            }

            if (!R.IsReady() || !Settings.JungleClear.UseR ||
                (Player.Instance.ManaPercent < Settings.JungleClear.MinManaToUseR) ||
                (Player.Instance.Spellbook.GetSpell(SpellSlot.R).Ammo < Settings.JungleClear.MinStacksToUseR))
                return;

            if (target == null)
                return;

            var rPrediction = R.GetPrediction(target);

            if (rPrediction.HitChance >= EloBuddy.SDK.Enumerations.HitChance.High)
            {
                R.Cast(rPrediction.CastPosition);
            }
        }
    }
}
