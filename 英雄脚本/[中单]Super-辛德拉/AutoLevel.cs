using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using static Dark_Syndra.Menus;

namespace Dark_Syndra
{
    public static class AutoLevel
    {
        //This event is triggered when a unit levels up
        public static void Obj_AI_Base_OnLevelUp(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args)
        {
            if (MiscMenu["activateAutoLVL"].Cast<CheckBox>().CurrentValue && sender.IsMe)
            {
                var delay = MiscMenu["delaySlider"].Cast<Slider>().CurrentValue;
                Core.DelayAction(LevelUpSpells, delay);
            }
        }

        //It will level up the spell using the values of the comboboxes on the menu as a priority
        private static void LevelUpSpells()
        {
            if (Player.Instance.Spellbook.CanSpellBeUpgraded(SpellSlot.R))
                Player.Instance.Spellbook.LevelSpell(SpellSlot.R);

            var firstFocusSlot = GetSlotFromComboBox(MiscMenu["firstFocus"].Cast<ComboBox>().CurrentValue);
            var secondFocusSlot = GetSlotFromComboBox(MiscMenu["secondFocus"].Cast<ComboBox>().CurrentValue);
            var thirdFocusSlot = GetSlotFromComboBox(MiscMenu["thirdFocus"].Cast<ComboBox>().CurrentValue);

            var secondSpell = Player.GetSpell(secondFocusSlot);
            var thirdSpell = Player.GetSpell(thirdFocusSlot);

            if (Player.Instance.Spellbook.CanSpellBeUpgraded(firstFocusSlot))
            {
                if (!secondSpell.IsLearned)
                    Player.Instance.Spellbook.LevelSpell(secondFocusSlot);
                if (!thirdSpell.IsLearned)
                    Player.Instance.Spellbook.LevelSpell(thirdFocusSlot);
                Player.Instance.Spellbook.LevelSpell(firstFocusSlot);
            }

            if (Player.Instance.Spellbook.CanSpellBeUpgraded(secondFocusSlot))
            {
                if (!thirdSpell.IsLearned)
                    Player.Instance.Spellbook.LevelSpell(thirdFocusSlot);
                Player.Instance.Spellbook.LevelSpell(firstFocusSlot);
                Player.Instance.Spellbook.LevelSpell(secondFocusSlot);
            }

            if (Player.Instance.Spellbook.CanSpellBeUpgraded(thirdFocusSlot))
                Player.Instance.Spellbook.LevelSpell(thirdFocusSlot);
        }

        /// It will transform the value of the combobox into a SpellSlot
        private static SpellSlot GetSlotFromComboBox(this int value)
        {
            switch (value)
            {
                case 0:
                    return SpellSlot.Q;
                case 1:
                    return SpellSlot.W;
                case 2:
                    return SpellSlot.E;
            }
            Chat.Print("Failed getting slot");
            return SpellSlot.Unknown;
        }
    }
}