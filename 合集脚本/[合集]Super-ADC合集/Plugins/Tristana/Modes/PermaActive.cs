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
namespace Marksman_Master.Plugins.Tristana.Modes
{
    using System.Linq;
    using EloBuddy;
    using EloBuddy.SDK;
    using Utils;

    internal class PermaActive : Tristana
    {
        public static void Execute()
        {
            if (!R.IsReady() || !Settings.Combo.UseR)
                return;

            foreach (var target in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x =>
            {
                if (x.IsValidTarget(Player.Instance.GetAutoAttackRange()) && (x.TotalHealthWithShields() < Player.Instance.GetAutoAttackDamageCached(x, true)))
                    return false;

                return x.IsValidTarget(R.Range);

            }).OrderBy(TargetSelector.GetPriority))
            {
                if (Damage.IsTargetKillableFromR(target))
                {
                    R.Cast(target);
                    break;
                }

                var damage = Damage.GetRDamage(target);

                if (HasExplosiveChargeBuff(target))
                    damage += Damage.GetEPhysicalDamage(target);

                if ((target.Hero == Champion.Blitzcrank) && !target.HasBuff("BlitzcrankManaBarrierCD") && !target.HasBuff("ManaBarrier"))
                {
                    damage -= target.Mana / 2;
                }

                if (target.TotalHealthWithShields(true) < damage)
                {
                    R.Cast(target);
                }
            }
        }
    }
}