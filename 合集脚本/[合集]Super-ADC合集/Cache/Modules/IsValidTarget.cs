#region Licensing
// ---------------------------------------------------------------------
// <copyright file="IsValidTarget.cs" company="EloBuddy">
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
using EloBuddy;
using EloBuddy.SDK;
using Marksman_Master.Cache.Interfaces;

namespace Marksman_Master.Cache.Modules
{
    internal sealed class IsValidTarget : CacheModuleBase, IDisposable, ICachable
    {
        internal override string Name { get; } = "IsValidTarget";

        public bool IsDisposing { get; private set; }

        private Dictionary<KeyValuePair<int, float>, Tuple<float, bool>> CachedValues { get; set; }

        internal override void Load()
        {
            CachedValues = new Dictionary<KeyValuePair<int, float>, Tuple<float, bool>>();
        }

        ~IsValidTarget()
        {
            Dispose(true);
        }

        internal bool Get(AttackableUnit from, float range)
        {
            if (IsDisposing)
                return false;

            Tuple<float, bool> output;

            if (CachedValues.TryGetValue(new KeyValuePair<int, float>(from.NetworkId, range), out output))
            {
                if (Game.Time * 1000 - output.Item1 < RefreshRate)
                {
                    return output.Item2;
                }

                CachedValues[new KeyValuePair<int, float>(from.NetworkId, range)] =
                    new Tuple<float, bool>(Game.Time*1000, from.IsValidTarget(range));
            }
            else
            {
                CachedValues[new KeyValuePair<int, float>(from.NetworkId, range)] =
                    new Tuple<float, bool>(Game.Time * 1000, from.IsValidTarget(range));
            }
            return CachedValues[new KeyValuePair<int, float>(from.NetworkId, range)].Item2;
        }

        public void DeleteUnnecessaryData()
        {
            CachedValues?.Clear();
        }

        internal bool Exist(AttackableUnit from, float range)
        {
            return !IsDisposing && CachedValues.ContainsKey(new KeyValuePair<int, float>(@from.NetworkId, range));
        }

        private void Finallize()
        {
            CachedValues?.Clear();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool dispose)
        {
            if (IsDisposing || !dispose)
                return;

            IsDisposing = true;

            Finallize();
        }
    }
}