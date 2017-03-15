#region Licensing
// ---------------------------------------------------------------------
// <copyright file="IsInRange.cs" company="EloBuddy">
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
using SharpDX;

namespace Marksman_Master.Cache.Modules
{
    internal class IsInRange : CacheModuleBase, IDisposable, ICachable
    {
        internal override string Name { get; } = "IsInRange";

        public bool IsDisposing { get; private set; }

        private Dictionary<Tuple<int, int, float>, Tuple<float, bool>> CachedValues { get; set; }
        private Dictionary<Tuple<Vector3, Vector3, float>, Tuple<float, bool>> CachedValues2 { get; set; }

        internal override void Load()
        {
            CachedValues = new Dictionary<Tuple<int, int, float>, Tuple<float, bool>>();
            CachedValues2 = new Dictionary<Tuple<Vector3, Vector3, float>, Tuple<float, bool>>();
        }

        ~IsInRange()
        {
            Dispose(true);
        }

        internal bool Get(Vector3 from, Vector3 target, float range)
        {
            if (IsDisposing)
                return false;

            Tuple<float, bool> output;

            if (CachedValues2.TryGetValue(new Tuple<Vector3, Vector3, float>(from, target, range), out output))
            {
                if (Game.Time * 1000 - output.Item1 < RefreshRate)
                {
                    return output.Item2;
                }

                CachedValues2[new Tuple<Vector3, Vector3, float>(from, target, range)] = new Tuple<float, bool>(
                    Game.Time * 1000, from.IsInRange(target, range));
            }
            else
            {
                CachedValues2[new Tuple<Vector3, Vector3, float>(from, target, range)] = new Tuple<float, bool>(
                    Game.Time * 1000, from.IsInRange(target, range));
            }
            return CachedValues2[new Tuple<Vector3, Vector3, float>(from, target, range)].Item2;
        }

        internal bool Get(GameObject from, GameObject target, float range)
        {
            if (IsDisposing)
                return false;

            Tuple<float, bool> output;

            if (CachedValues.TryGetValue(new Tuple<int, int, float>(from.NetworkId, target.NetworkId, range), out output))
            {
                if (Game.Time * 1000 - output.Item1 < RefreshRate)
                {
                    return output.Item2;
                }

                CachedValues[new Tuple<int, int, float>(from.NetworkId, target.NetworkId, range)] = new Tuple<float, bool>(
                    Game.Time * 1000, from.IsInRange(target, range));
            }
            else
            {
                CachedValues[new Tuple<int, int, float>(from.NetworkId, target.NetworkId, range)] = new Tuple<float, bool>(
                    Game.Time * 1000, from.IsInRange(target, range));
            }
            return CachedValues[new Tuple<int, int, float>(from.NetworkId, target.NetworkId, range)].Item2;
        }


        public void DeleteUnnecessaryData()
        {
            CachedValues?.Clear();
            CachedValues2?.Clear();
        }

        internal bool Exist(Vector3 from, Vector3 target, float range)
        {
            return !IsDisposing && CachedValues2.ContainsKey(new Tuple<Vector3, Vector3, float>(@from, target, range));
        }

        internal bool Exist(GameObject from, GameObject target, float range)
        {
            return !IsDisposing && CachedValues.ContainsKey(new Tuple<int, int, float>(@from.NetworkId, target.NetworkId, range));
        }

        private void Finallize()
        {
            CachedValues?.Clear();
            CachedValues2?.Clear();
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