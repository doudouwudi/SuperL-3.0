﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy.SDK;
using EloBuddy;

namespace Auto_Carry_Vayne.Features.Module.Items
{
    class Bilge : IModule
    {
        public void OnLoad()
        {

        }

        public ModuleType GetModuleType()
        {
            return ModuleType.OnUpdate;
        }

        public bool ShouldGetExecuted()
        {
            return Manager.MenuManager.Botrk && Item.HasItem(3153) && Item.CanUseItem(3153);
        }

        public void OnExecute()
        {
            var unit = TargetSelector.GetTarget(500, DamageType.Magical);

            if (unit != null && Variables.AfterAttack && (ObjectManager.Player.Health / ObjectManager.Player.MaxHealth) * 100 <= 95)
            {
                Item.UseItem(3153, unit);
            }
        }
    }
}
