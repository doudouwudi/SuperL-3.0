#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Distance.cs" company="EloBuddy">
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
    internal sealed class Distance : CacheModuleBase, IDisposable, ICachable
    {
        internal override string Name { get; } = "Distance";

        public bool IsDisposing { get; private set; }

        private Dictionary<KeyValuePair<int, int>, Tuple<float, float>> CachedValues { get; set; }
        private Dictionary<KeyValuePair<Vector3,Vector3>, Tuple<float, float>> CachedValues2 { get; set; }

        internal override void Load()
        {
            CachedValues = new Dictionary<KeyValuePair<int, int>, Tuple<float, float>>();
            CachedValues2 = new Dictionary<KeyValuePair<Vector3, Vector3>, Tuple<float, float>>();
        }

        ~Distance()
        {
            Dispose(true);
        }

        internal float Get(Vector3 from, Vector3 target)
        {
            if (IsDisposing)
                return 0;

            Tuple<float, float> output;

            if (CachedValues2.TryGetValue(new KeyValuePair<Vector3, Vector3>(from, target), out output))
            {
                if (Game.Time*1000 - output.Item1 < RefreshRate)
                {
                    return output.Item2;
                }

                CachedValues2[new KeyValuePair<Vector3, Vector3>(from, target)] = new Tuple<float, float>(
                    Game.Time*1000, from.Distance(target));
            }
            else
            {
                CachedValues2[new KeyValuePair<Vector3, Vector3>(from, target)] = new Tuple<float, float>(
                    Game.Time * 1000, from.Distance(target));
            }
            return CachedValues2[new KeyValuePair<Vector3, Vector3>(from, target)].Item2;
        }

        internal float Get(GameObject from, GameObject target)
        {
            if (IsDisposing)
                return 0;

            Tuple<float, float> output;

            if (CachedValues.TryGetValue(new KeyValuePair<int, int>(from.NetworkId, target.NetworkId), out output))
            {
                if (Game.Time * 1000 - output.Item1 < RefreshRate)
                {
                    return output.Item2;
                }

                CachedValues[new KeyValuePair<int, int>(from.NetworkId, target.NetworkId)] = new Tuple<float, float>(
                    Game.Time * 1000, from.Distance(target));
            }
            else
            {
                CachedValues[new KeyValuePair<int, int>(from.NetworkId, target.NetworkId)] = new Tuple<float, float>(
                    Game.Time * 1000, from.Distance(target));
            }
            return CachedValues[new KeyValuePair<int, int>(from.NetworkId, target.NetworkId)].Item2;
        }

        public void DeleteUnnecessaryData()
        {
            CachedValues?.Clear();
            CachedValues2?.Clear();
        }

        internal bool Exist(Vector3 from, Vector3 target)
        {
            return !IsDisposing && CachedValues2.ContainsKey(new KeyValuePair<Vector3, Vector3>(@from, target));
        }

        internal bool Exist(GameObject from, GameObject target)
        {
            return !IsDisposing && CachedValues.ContainsKey(new KeyValuePair<int, int>(@from.NetworkId, target.NetworkId));
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
