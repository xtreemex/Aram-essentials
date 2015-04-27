using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.GameFiles.AirClient;
using LeagueSharp.GameFiles.GameClient;
using LeagueSharp.Sandbox;
using SAssemblies;
using SAwareness.Properties;
using SharpDX;
using Menu = SAssemblies.Menu;
using MenuItem = LeagueSharp.Common.MenuItem;
using SAssemblies.Timers;

namespace SAssemblies
{
    class MainMenu : Menu
    {
        private readonly Dictionary<Menu.MenuItemSettings, Func<dynamic>> MenuEntries;

       
        public static MenuItemSettings Timers = new MenuItemSettings();
        public static MenuItemSettings HealthTimer = new MenuItemSettings();
        public static MenuItemSettings InhibitorTimer = new MenuItemSettings();


        public MainMenu()
        {
            MenuEntries =
            new Dictionary<Menu.MenuItemSettings, Func<dynamic>>
            {

                { HealthTimer, () => new Timers.Health() },
                { InhibitorTimer, () => new Timers.Inhibitor() },
 
             
            };
        }

        public Tuple<Menu.MenuItemSettings, Func<dynamic>> GetDirEntry(Menu.MenuItemSettings menuItem)
        {
            return new Tuple<MenuItemSettings, Func<dynamic>>(menuItem, MenuEntries[menuItem]);
        }

        public Dictionary<Menu.MenuItemSettings, Func<dynamic>> GetDirEntries()
        {
            return MenuEntries;
        }

        public void UpdateDirEntry(ref Menu.MenuItemSettings oldMenuItem, Menu.MenuItemSettings newMenuItem)
        {
            Func<dynamic> save = MenuEntries[oldMenuItem];
            MenuEntries.Remove(oldMenuItem);
            MenuEntries.Add(newMenuItem, save);
            oldMenuItem = newMenuItem;
        }

    }

    class Program
    {
        private static float lastDebugTime = 0;
        private MainMenu mainMenu;
        private static readonly Program instance = new Program();

        public static void Main(string[] args)
        {
            AssemblyResolver.Init();
            AppDomain.CurrentDomain.DomainUnload += delegate { threadActive = false; };
            AppDomain.CurrentDomain.ProcessExit += delegate { threadActive = false; };
           
            Instance().Load();
        }

        public void Load()
        {
            mainMenu = new MainMenu();
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        public static Program Instance()
        {
            return instance;
        }

        private void CreateMenu()
        {
            //http://www.cambiaresearch.com/articles/15/javascript-char-codes-key-codes
            try
            {
                Menu.MenuItemSettings tempSettings;
                var menu = new LeagueSharp.Common.Menu("Aram essentials", "Aram essentials", true);

                
                MainMenu.Timers = Timers.Timer.SetupMenu(menu);
           
                mainMenu.UpdateDirEntry(ref MainMenu.HealthTimer, Timers.Health.SetupMenu(MainMenu.Timers.Menu));
            
                mainMenu.UpdateDirEntry(ref MainMenu.InhibitorTimer, Timers.Inhibitor.SetupMenu(MainMenu.Timers.Menu));
   

            }
            catch (Exception)
            {
                throw;
            }
        }

        private async /*static*/ void Game_OnGameLoad(EventArgs args)
        {
            CreateMenu();
            Game.PrintChat("Aram essentials loaded!");

            new Thread(GameOnOnGameUpdate).Start();
        }

        private static bool threadActive = true;

        private /*static*/ void GameOnOnGameUpdate(/*EventArgs args*/)
        {
            try
            {
                while (threadActive)
                {
                    Thread.Sleep(1000);

                    if (mainMenu == null)
                        continue;

                    foreach (var entry in mainMenu.GetDirEntries())
                    {
                        var item = entry.Key;
                        if (item == null)
                        {
                            continue;
                        }
                        try
                        {
                            if (item.GetActive() == false && item.Item != null)
                            {
                                item.Item = null;
                                //GC.Collect();
                            }
                            else if (item.GetActive() && item.Item == null && !item.ForceDisable && item.Type != null)
                            {
                                try
                                {
                                    item.Item = entry.Value.Invoke();
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                         
                        }
                    }
                   
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("SAwareness: " + e);
                threadActive = false;
            }
            
         
        }

        public static PropertyInfo[] GetPublicProperties(Type type)
        {
            if (type.IsInterface)
            {
                var propertyInfos = new List<PropertyInfo>();

                var considered = new List<Type>();
                var queue = new Queue<Type>();
                considered.Add(type);
                queue.Enqueue(type);
                while (queue.Count > 0)
                {
                    Type subType = queue.Dequeue();
                    foreach (Type subInterface in subType.GetInterfaces())
                    {
                        if (considered.Contains(subInterface)) continue;

                        considered.Add(subInterface);
                        queue.Enqueue(subInterface);
                    }

                    PropertyInfo[] typeProperties = subType.GetProperties(
                        BindingFlags.FlattenHierarchy
                        | BindingFlags.Public
                        | BindingFlags.Instance);

                    IEnumerable<PropertyInfo> newPropertyInfos = typeProperties
                        .Where(x => !propertyInfos.Contains(x));

                    propertyInfos.InsertRange(0, newPropertyInfos);
                }

                return propertyInfos.ToArray();
            }

            return type.GetProperties(BindingFlags.Static | BindingFlags.Public);
        }

    }
}
