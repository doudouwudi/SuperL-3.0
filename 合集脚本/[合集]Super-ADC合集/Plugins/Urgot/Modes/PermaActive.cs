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

namespace Marksman_Master.Plugins.Urgot.Modes
{
    internal class PermaActive : Urgot
    {
        public static void Execute()
        {
            if (W.IsReady())
            {
                var incomingDamage = IncomingDamage.GetIncomingDamage(Player.Instance);

                if ((incomingDamage / Player.Instance.TotalHealthWithShields() * 100 >= Settings.Misc.MinDamage) ||
                    (incomingDamage > Player.Instance.Health))
                {
                    W.Cast();
                }
            }

            if (Settings.Misc.EnableKillsteal && !Player.Instance.IsRecalling())
            {
                foreach (
                    var target in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                        x =>
                            x.IsValidTargetCached() && IsInQRange(x) && !x.HasUndyingBuffA() &&
                            (x.TotalHealthWithShields() < Player.Instance.GetSpellDamageCached(x, SpellSlot.Q))))
                {
                    if (HasEDebuff(target))
                    {
                        Player.Instance.Spellbook.CastSpell(SpellSlot.Q, target.Position);
                        return;
                    }

                    var qPrediction = Q.GetPrediction(target);

                    if (qPrediction.HitChance != HitChance.High)
                        continue;

                    Q.Cast(qPrediction.CastPosition);
                    return;
                }
            }

            if (Settings.Misc.AutoHarass && HasAnyOrbwalkerFlags)
            {
                Combo.ELogics();
            }

            if (!Settings.Misc.AutoHarass || !Settings.Combo.UseQ || !HasAnyOrbwalkerFlags || 
                Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) ||
                Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                return;

            foreach (
                var corrosiveDebufTarget in
                    CorrosiveDebufTargets.Where(
                        unit => (unit.Type == GameObjectType.AIHeroClient) && unit.IsValidTargetCached(1300)))
            {
                Player.Instance.Spellbook.CastSpell(SpellSlot.Q, corrosiveDebufTarget.Position);
                return;
            }
        }
    }
}
