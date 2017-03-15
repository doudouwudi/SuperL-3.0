using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;
using EloBuddy.SDK.Menu.Values;

namespace Dark_Syndra
{
    internal class Functions
    {

        public static Vector3 GrabWPost(bool onlyQ)
        {
            var sphere =
                ObjectManager.Get<Obj_AI_Base>().FirstOrDefault(a => a.Name == "Seed" && a.IsValid);
            if (sphere != null)
            {
                return sphere.Position;
            }
            if (Menus.ComboMenu["W"].Cast<CheckBox>().CurrentValue)
            { 
                var minion = EntityManager.MinionsAndMonsters.GetLaneMinions()
                    .OrderByDescending(m => m.Health)
                    .FirstOrDefault(m => m.IsValidTarget(SpellsManager.W.Range) && m.IsEnemy);
                if (minion != null)
                {
                    return minion.Position;
                }
            }
            return new Vector3();
        }

        public static void QE(Vector2 position)
        {
            if (SpellsManager.Q.IsReady() && SpellsManager.E.IsReady())
            {
                var target = TargetSelector.GetTarget(SpellsManager.W.Range, DamageType.Magical);
                var pred = SpellsManager.Q.GetPrediction(target);
                SpellsManager.Q.Cast(Player.Instance.Position.Extend(pred.CastPosition, SpellsManager.E.Range - 10).To3D());
                SpellsManager.E.Cast(Player.Instance.Position.Extend(pred.CastPosition, SpellsManager.E.Range - 10).To3D());
            }
        }

        public static Vector3 GrabWPostt(bool onlyQ)
        {
            if (Menus.LaneClearMenu["W"].Cast<CheckBox>().CurrentValue)
            {
                var minion = EntityManager.MinionsAndMonsters.GetLaneMinions()
                    .FirstOrDefault(m => m.IsValidTarget(SpellsManager.W.Range) && m.IsEnemy);
                if (minion != null)
                {
                    return minion.Position;
                }
            }
            return new Vector3();
        }
    }
}