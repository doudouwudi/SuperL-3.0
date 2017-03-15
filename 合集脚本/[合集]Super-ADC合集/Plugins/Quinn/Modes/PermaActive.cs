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
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Quinn.Modes
{
    internal class PermaActive : Quinn
    {
        public static void Execute()
        {
            if(!Settings.Misc.EnableKillsteal)
                return;

            if (!Q.IsReady())
                return;

            var target =
                EntityManager.Heroes.Enemies.Where(
                    x =>
                        x.IsValidTarget(Q.Range) && !x.HasUndyingBuffA() && !x.HasSpellShield() &&
                        x.TotalHealthWithShields() < Player.Instance.GetSpellDamage(x, SpellSlot.Q))
                    .OrderByDescending(TargetSelector.GetPriority)
                    .FirstOrDefault();

            if (target != null)
            {
                Q.CastMinimumHitchance(target, HitChance.Medium);
            }
        }
    }
}