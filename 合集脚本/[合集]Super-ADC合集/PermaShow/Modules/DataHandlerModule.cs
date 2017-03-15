#region Licensing
// ---------------------------------------------------------------------
// <copyright file="DataHandlerModule.cs" company="EloBuddy">
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
namespace Marksman_Master.PermaShow.Modules
{
    using System.Collections.Generic;
    
    using Values;

    using Interfaces;

    internal sealed class DataHandlerModule : Wrapper
    {
        public Dictionary<ValueBase, IPermaShowItem> PermaShowItems { get; set; } = new Dictionary<ValueBase, IPermaShowItem>();
        public List<Separator> Separators { get; set; } = new List<Separator>();
        public List<Separator> Underlines { get; set; } = new List<Separator>();
        public Text HeaderText { get; set; }
        
        public override void Load()
        {
        }

        public void AddPermashowItem(KeyValuePair<ValueBase, IPermaShowItem> item)
        {
            PermaShowItems.Add(item.Key, item.Value);
        }

        public void AddSeparator(Separator separator)
        {
            Separators.Add(separator);
        }

        public void AddUnderline(Separator underline)
        {
            Underlines.Add(underline);
        }
    }
}