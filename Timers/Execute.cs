using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace SAssemblies.Timers
{
    class Execute
    {
        public static Menu.MenuItemSettings ExecuteTimers = new Menu.MenuItemSettings(typeof(Execute));

        Dictionary<Obj_AI_Hero, int> lastDmg = new Dictionary<Obj_AI_Hero, int>(); 

        public Execute() //TODO: Wait for Dmg Event or working Dmg Packet
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                lastDmg.Add(hero, 0);
            }
            Game.OnUpdate += Game_OnGameUpdate;
        }

        ~Execute()
        {
            Game.OnUpdate -= Game_OnGameUpdate;
        }

        public bool IsActive()
        {
            return Timer.Timers.GetActive() && ExecuteTimers.GetActive();
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            ExecuteTimers.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("TIMERS_EXECUTE_MAIN"), "SAssembliesTimersExecute"));
            ExecuteTimers.MenuItems.Add(
                ExecuteTimers.Menu.AddItem(new MenuItem("SAssembliesTimersExecuteActive", Language.GetString("GLOBAL_ACTIVE")).SetValue(false)));
            return ExecuteTimers;
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            throw new NotImplementedException();
        }
    }
}
