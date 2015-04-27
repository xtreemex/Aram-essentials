using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.GameFiles.AirClient;
using LeagueSharp.GameFiles.GameClient;
using LeagueSharp.GameFiles.Tools;
using LeagueSharp.Sandbox;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;
using Config = LeagueSharp.Common.Config;
using Font = SharpDX.Direct3D9.Font;
using Rectangle = SharpDX.Rectangle;
using ResourceManager = System.Resources.ResourceManager;

//Erstelle ein Thread, rufe darin die sich im event eingeschrieben methoden auf

namespace SAssemblies
{
    class Menu
    {
        public static MenuItemSettings GlobalSettings = new MenuItemSettings();

        public class MenuItemSettings
        {
            public bool ForceDisable;
            public dynamic Item;
            public LeagueSharp.Common.Menu Menu;
            public List<MenuItem> MenuItems = new List<MenuItem>();
            public String Name;
            public List<MenuItemSettings> SubMenus = new List<MenuItemSettings>();
            public Type Type;

            public MenuItemSettings(Type type, dynamic item)
            {
                Type = type;
                Item = item;
            }

            public MenuItemSettings(dynamic item)
            {
                Item = item;
            }

            public MenuItemSettings(Type type)
            {
                Type = type;
            }

            public MenuItemSettings(String name)
            {
                Name = name;
            }

            public MenuItemSettings()
            {
            }

            public MenuItemSettings AddMenuItemSettings(String displayName, String name)
            {
                SubMenus.Add(new MenuItemSettings(name));
                MenuItemSettings tempSettings = GetMenuSettings(name);
                if (tempSettings == null)
                {
                    throw new NullReferenceException(name + " not found");
                }
                tempSettings.Menu = Menu.AddSubMenu(new LeagueSharp.Common.Menu(displayName, name));
                return tempSettings;
            }

            public bool GetActive()
            {
                if (Menu == null)
                    return false;
                foreach (MenuItem item in Menu.Items)
                {
                    if (item.DisplayName == Language.GetString("GLOBAL_ACTIVE"))
                    {
                        if (item.GetValue<bool>())
                        {
                            return true;
                        }
                        return false;
                    }
                }
                return false;
            }

            public void SetActive(bool active)
            {
                if (Menu == null)
                    return;
                foreach (MenuItem item in Menu.Items)
                {
                    if (item.DisplayName == Language.GetString("GLOBAL_ACTIVE"))
                    {
                        item.SetValue(active);
                        return;
                    }
                }
            }

            public MenuItem GetMenuItem(String menuName)
            {
                if (Menu == null)
                    return null;
                foreach (MenuItem item in Menu.Items)
                {
                    if (item.Name == menuName)
                    {
                        return item;
                    }
                }
                return null;
            }

            public LeagueSharp.Common.Menu GetSubMenu(String menuName)
            {
                if (Menu == null)
                    return null;
                return Menu.SubMenu(menuName);
            }

            public MenuItemSettings GetMenuSettings(String name)
            {
                foreach (MenuItemSettings menu in SubMenus)
                {
                    if (menu.Name.Contains(name))
                        return menu;
                }
                return null;
            }
        }

        //public static MenuItemSettings  = new MenuItemSettings();
    }

    internal static class Log
    {
        public static String File = "C:\\SAssemblies.log";
        public static String Prefix = "Packet";

        public static void LogString(String text, String file = null, String prefix = null)
        {
            switch (text)
            {
                case "missile":
                case "DrawFX":
                case "Mfx_pcm_mis.troy":
                case "Mfx_bcm_tar.troy":
                case "Mfx_bcm_mis.troy":
                case "Mfx_pcm_tar.troy":
                    return;
            }
            LogWrite(text, file, prefix);
        }

        private static void LogGamePacket(GamePacket result, String file = null, String prefix = null)
        {
            byte[] b = new byte[result.Size()];
            long size = result.Size();
            int cur = 0;
            while (cur < size - 1)
            {
                b[cur] = result.ReadByte(cur);
                cur++;
            }
            LogPacket(b, file, prefix);
        }

        public static void LogPacket(byte[] data, String file = null, String prefix = null)
        {
            if (!(data[0].ToHexString().Equals("AE") || data[0].ToHexString().Equals("29") || data[0].ToHexString().Equals("1A") || data[0].ToHexString().Equals("34") || data[0].ToHexString().Equals("6E") || data[0].ToHexString().Equals("85") || data[0].ToHexString().Equals("C4") || data[0].ToHexString().Equals("61") || data[0].ToHexString().Equals("38") || data[0].ToHexString().Equals("FE")))
            LogWrite(BitConverter.ToString(data), file, prefix);
        }

        private static void LogWrite(String text, String file = null, String prefix = null)
        {
            if (text == null)
                return;
            if (file == null)
                file = File;
            if (prefix == null)
                prefix = Prefix;
            using (var stream = new StreamWriter(file, true))
            {
                stream.WriteLine(prefix + "@" + Game.ClockTime + ": " + text);
            }
        }
    }

    internal static class Common
    {
        public static bool IsOnScreen(Vector3 vector)
        {
            Vector2 screen = Drawing.WorldToScreen(vector);
            if (screen[0] < 0 || screen[0] > Drawing.Width || screen[1] < 0 || screen[1] > Drawing.Height)
                return false;
            return true;
        }

        public static bool IsOnScreen(Vector2 vector)
        {
            Vector2 screen = vector;
            if (screen[0] < 0 || screen[0] > Drawing.Width || screen[1] < 0 || screen[1] > Drawing.Height)
                return false;
            return true;
        }

        public static Size ScaleSize(this Size size, float scale, Vector2 mainPos = default(Vector2))
        {
            size.Height = (int) (((size.Height - mainPos.Y)*scale) + mainPos.Y);
            size.Width = (int) (((size.Width - mainPos.X)*scale) + mainPos.X);
            return size;
        }

        public static bool IsInside(Vector2 mousePos, Size windowPos, int width, int height)
        {
            return Utils.IsUnderRectangle(mousePos, windowPos.Width, windowPos.Height, width, height);
        }

        public static bool IsInside(Vector2 mousePos, Vector2 windowPos, int width, int height)
        {
            return Utils.IsUnderRectangle(mousePos, windowPos.X, windowPos.Y, width, height);
        }
    }

    internal static class SummonerSpells
    {
        public static SpellSlot GetIgniteSlot()
        {
            foreach (SpellDataInst spell in ObjectManager.Player.Spellbook.Spells)
            {
                if (spell.Name.ToLower().Contains("dot") && spell.State == SpellState.Ready)
                    return spell.Slot;
            }
            return SpellSlot.Unknown;
        }

        public static SpellSlot GetSmiteSlot()
        {
            foreach (SpellDataInst spell in ObjectManager.Player.Spellbook.Spells)
            {
                if (spell.Name.ToLower().Contains("smite") && spell.State == SpellState.Ready)
                    return spell.Slot;
            }
            return SpellSlot.Unknown;
        }

        public static SpellSlot GetHealSlot()
        {
            foreach (SpellDataInst spell in ObjectManager.Player.Spellbook.Spells)
            {
                if (spell.Name.ToLower().Contains("heal") && spell.State == SpellState.Ready)
                    return spell.Slot;
            }
            return SpellSlot.Unknown;
        }

        public static SpellSlot GetBarrierSlot()
        {
            foreach (SpellDataInst spell in ObjectManager.Player.Spellbook.Spells)
            {
                if (spell.Name.ToLower().Contains("barrier") && spell.State == SpellState.Ready)
                    return spell.Slot;
            }
            return SpellSlot.Unknown;
        }

        public static SpellSlot GetExhaustSlot()
        {
            foreach (SpellDataInst spell in ObjectManager.Player.Spellbook.Spells)
            {
                if (spell.Name.ToLower().Contains("exhaust") && spell.State == SpellState.Ready)
                    return spell.Slot;
            }
            return SpellSlot.Unknown;
        }

        public static SpellSlot GetCleanseSlot()
        {
            foreach (SpellDataInst spell in ObjectManager.Player.Spellbook.Spells)
            {
                if (spell.Name.ToLower().Contains("boost") && spell.State == SpellState.Ready)
                    return spell.Slot;
            }
            return SpellSlot.Unknown;
        }

        public static SpellSlot GetClairvoyanceSlot()
        {
            foreach (SpellDataInst spell in ObjectManager.Player.Spellbook.Spells)
            {
                if (spell.Name.ToLower().Contains("clairvoyance") && spell.State == SpellState.Ready)
                    return spell.Slot;
            }
            return SpellSlot.Unknown;
        }

        public static SpellSlot GetFlashSlot()
        {
            foreach (SpellDataInst spell in ObjectManager.Player.Spellbook.Spells)
            {
                if (spell.Name.ToLower().Contains("flash") && spell.State == SpellState.Ready)
                    return spell.Slot;
            }
            return SpellSlot.Unknown;
        }
    }

    internal class Downloader
    {
        public delegate void DownloadFinished(object sender, DlEventArgs args);

        public static String Host = "https://github.com/Screeder/SAwareness/raw/master/Sprites/SAwareness/";
        public static String Path = "CHAMP/";

        private readonly List<Files> _downloadQueue = new List<Files>();
        public event DownloadFinished DownloadFileFinished;

        public void AddDownload(String hostFile, String localFile)
        {
            _downloadQueue.Add(new Files(hostFile, localFile));
        }

        public void StartDownload()
        {
            StartDownloadInternal();
        }

        private async Task StartDownloadInternal()
        {
            var webClient = new WebClient();
            var tasks = new List<DlTask>();
            foreach (Files files in _downloadQueue)
            {
                Task t = webClient.DownloadFileTaskAsync(new Uri(Host + Path + files.OnlineFile), files.OfflineFile);
                tasks.Add(new DlTask(files, t));
            }
            foreach (DlTask task in tasks)
            {
                await task.Task;
                tasks.Remove(task);
                OnFinished(new DlEventArgs(task.Files));
            }
        }

        protected virtual void OnFinished(DlEventArgs args)
        {
            if (DownloadFileFinished != null)
                DownloadFileFinished(this, args);
        }

        public static void DownloadFile(String hostfile, String localfile)
        {
            var webClient = new WebClient();
            webClient.DownloadFile(Host + Path + hostfile, localfile);
        }

        public class DlEventArgs : EventArgs
        {
            public Files DlFiles;

            public DlEventArgs(Files files)
            {
                DlFiles = files;
            }
        }

        private struct DlTask
        {
            public readonly Files Files;
            public readonly Task Task;

            public DlTask(Files files, Task task)
            {
                Files = files;
                Task = task;
            }
        }

        public struct Files
        {
            public String OfflineFile;
            public String OnlineFile;

            public Files(String onlineFile, String offlineFile)
            {
                OnlineFile = onlineFile;
                OfflineFile = offlineFile;
            }
        }
    }

    public class RafLoader
    {
        private static bool loading = false;
        private static bool loaded = false;
        public static float LastLoadTime = 0;

        public enum ImageList
        {
            None,
            Champion,
            ChampionCircle,
            SpellPassive,
            SpellQ,
            SpellW,
            SpellE,
            SpellR,
            SpellSummoner1,
            SpellSummoner2
        }

        static RafLoader()
        {
            //new Thread(InitLoader).Start();
        }

        public static void InitLoader()
        {
            if (!loading && !loaded)
            {
                loading = true;
                AirGeneratedContent.Init();
            }
        }

        private static bool IsLoaded()
        {
            if (AirGeneratedContent.Items == null || AirGeneratedContent.Items.Count == 0)
            {
                loading = true;
                AirGeneratedContent.Init();
            }
            if (AirGeneratedContent.Items != null)
            {
                loading = false;
                loaded = true;
                return true;
            }
            return false;
        }

        private static string SmiteType(String name)
        {
            switch (name)
            {
                case "s5_summonersmiteplayerganker":
                    return "smite_blue.dds";
                    break;

                case "s5_summonersmiteduel":
                    return "smite_red.dds";
                    break;

                case "s5_summonersmitequick":
                    return "smite_silver.dds";
                    break;

                case "itemsmiteaoe":
                    return "smite_purple.dds";
                    break;

                case "summonersmite":
                    return "summoner_smite.dds";
                    break;

                default:
                    return null;
            }
        }

        private static String TemporaryIniBinFix(String name)
        {
            switch (name.ToLower())
            {
                //case "chogath":
                //    return "greenterror";
                //    break;

                //case "orianna":
                //    return "oriana";
                //    break;

                default:
                    return name;
            }
        }

        private static byte[] GetFileContent(String fileName, String optionalDirName = "")
        {
            if (fileName == null)
                return null;
            fileName = fileName.ToLower();
            foreach (var file in Archives.Files)
            {
                if (file.Key.Contains(fileName) && file.Key.Contains(optionalDirName))
                {
                    return file.Value.GetLastContent();
                }
            }
            return null;
        }

        private static bool FileExists(String fileName, String optionalDirName = "")
        {
            if (fileName == null)
                return false;
            fileName = fileName.ToLower();
            foreach (var file in Archives.Files)
            {
                if (file.Key.Contains(fileName) && file.Key.Contains(optionalDirName))
                {
                    return true;
                }
            }
            return false;
        }

        public static byte[] GetImage(String baseSkinName, ImageList list, String optionalName = "")
        {
            if (!IsLoaded())
                return null;
            var champion = AirGeneratedContent.Champions[baseSkinName];
            if (champion == null)
            {
                Console.Write("SAssemblies: Can not get champion: " + baseSkinName);
                return null;
            }
            String imageStr = null;
            String category = "";
            Dictionary<uint, object> inibin = null;
            switch (list)
            {
                case ImageList.None:
                    imageStr = optionalName;
                    break;

                case ImageList.Champion:
                    category = "character";
                    if (FileExists(TemporaryIniBinFix(champion.skinName) + "_square", category))
                    {
                        imageStr = TemporaryIniBinFix(champion.skinName) + "_square";
                    }
                    if (imageStr != null)
                    {
                        break;
                    }
                    inibin = GetIniBin(champion.skinName);
                    if (inibin != null)
                    {
                        imageStr = GetIniBinOptions(inibin, "iconsquare");
                    }
                    break;

                case ImageList.ChampionCircle:
                    category = "character";
                    if (FileExists(TemporaryIniBinFix(champion.skinName) + "_circle", category))
                    {
                        imageStr = TemporaryIniBinFix(champion.skinName) + "_circle";
                    }
                    if (imageStr != null)
                    {
                        break;
                    }
                    inibin = GetIniBin(champion.skinName);
                    if (inibin != null)
                    {
                        imageStr = GetIniBinOptions(inibin, "iconcircle");
                    }
                    break;

                case ImageList.SpellPassive:
                    category = "character";
                    if (FileExists(champion.passiveIcon, category))
                    {
                        imageStr = champion.passiveIcon;
                    }
                    if (imageStr != null)
                    {
                        break;
                    }
                    inibin = GetIniBin(champion.skinName);
                    if (inibin != null)
                    {
                        imageStr = GetIniBinOptions(inibin, "iconpassive");
                    }
                    break;

                case ImageList.SpellQ:
                    category = "character";
                    if (FileExists(champion.abilityIcon1, category))
                    {
                        imageStr = champion.abilityIcon1;
                    }
                    if (imageStr != null)
                    {
                        break;
                    }
                    else
                    {
                        category = "spell";
                        if (FileExists(champion.abilityIcon1, category))
                        {
                            imageStr = champion.abilityIcon1;
                        }
                        if (imageStr != null)
                        {
                            break;
                        }
                    }
                    inibin = GetIniBin(champion.skinName);
                    if (inibin != null)
                    {
                        inibin = GetIniBin(GetIniBinOptions(inibin, "spell1"));
                        if (inibin != null)
                        {
                            category = "";
                            imageStr = GetIniBinOptions(inibin, "inventoryicon");
                        }
                    }
                    break;

                case ImageList.SpellW:
                    category = "character";
                    if (FileExists(champion.abilityIcon2, category))
                    {
                        imageStr = champion.abilityIcon2;
                    }
                    if (imageStr != null)
                    {
                        break;
                    }
                    else
                    {
                        category = "spell";
                        if (FileExists(champion.abilityIcon2, category))
                        {
                            imageStr = champion.abilityIcon2;
                        }
                        if (imageStr != null)
                        {
                            break;
                        }
                    }
                    inibin = GetIniBin(champion.skinName);
                    if (inibin != null)
                    {
                        inibin = GetIniBin(GetIniBinOptions(inibin, "spell2"));
                        if (inibin != null)
                        {
                            category = "";
                            imageStr = GetIniBinOptions(inibin, "inventoryicon");
                        }
                    }
                    break;

                case ImageList.SpellE:
                    category = "character";
                    if (FileExists(champion.abilityIcon3, category))
                    {
                        imageStr = champion.abilityIcon3;
                    }
                    if (imageStr != null)
                    {
                        break;
                    }
                    else
                    {
                        category = "spell";
                        if (FileExists(champion.abilityIcon3, category))
                        {
                            imageStr = champion.abilityIcon3;
                        }
                        if (imageStr != null)
                        {
                            break;
                        }
                    }
                    inibin = GetIniBin(champion.skinName);
                    if (inibin != null)
                    {
                        inibin = GetIniBin(GetIniBinOptions(inibin, "spell3"));
                        if (inibin != null)
                        {
                            category = "";
                            imageStr = GetIniBinOptions(inibin, "inventoryicon");
                        }
                    }
                    break;

                case ImageList.SpellR:
                    category = "character";
                    if (FileExists(champion.abilityIcon4, category))
                    {
                        imageStr = champion.abilityIcon4;
                    }
                    if (imageStr != null)
                    {
                        break;
                    }
                    else
                    {
                        category = "spell";
                        if (FileExists(champion.abilityIcon4, category))
                        {
                            imageStr = champion.abilityIcon4;
                        }
                        if (imageStr != null)
                        {
                            break;
                        }
                    }
                    inibin = GetIniBin(champion.skinName);
                    if (inibin != null)
                    {
                        inibin = GetIniBin(GetIniBinOptions(inibin, "spell4"));
                        if (inibin != null)
                        {
                            category = "";
                            imageStr = GetIniBinOptions(inibin, "inventoryicon");
                        }
                    }
                    break;

                case ImageList.SpellSummoner1:
                    category = "spells";
                    if (SmiteType(optionalName) == null)
                    {
                        inibin = GetIniBin(AirGeneratedContent.Spells[optionalName].name);
                        if (inibin != null)
                        {
                            imageStr = GetIniBinOptions(inibin, "inventoryicon");
                        }
                    }
                    else
                    {
                        imageStr = SmiteType(optionalName);
                    }
                    break;

                case ImageList.SpellSummoner2:
                    category = "spells";
                    if (SmiteType(optionalName) == null)
                    {
                        inibin = GetIniBin(AirGeneratedContent.Spells[optionalName].name);
                        if (inibin != null)
                        {
                            imageStr = GetIniBinOptions(inibin, "inventoryicon");
                        }
                    }
                    else
                    {
                        imageStr = SmiteType(optionalName);
                    }
                    break;

                default:
                    imageStr = "";
                    break;
            }
            LastLoadTime = Environment.TickCount;
            return GetFileContent(imageStr, category);
        }

        private static String GetIniBinOptions(Dictionary<uint, object> inibinValues, String inibinFile)
        {
            foreach (var value in inibinValues)
            {
                if (IniBinKeys.GetKey(value.Key).ToLower().Contains(inibinFile))
                {
                    if (GetFileContent(value.Value.ToString()) != null)
                    {
                        return value.Value.ToString();
                    }
                }
            }
            return null;
        }

        public static Dictionary<uint, object> GetIniBin(String baseSkinName)
        {
            byte[] bInibin = GetFileContent("/" + baseSkinName + ".inibin");
            return IniBinReader.GetValues(bInibin);
        }
    }

    public static class SpriteHelperNew
    {
        public enum TextureType
        {
            Default,
            Summoner,
            Item
        }

        public enum DownloadType
        {
            Champion,
            Spell,
            Summoner,
            Item
        }

        private static Downloader _downloader = new Downloader();
        public static readonly Dictionary<String, byte[]> MyResources = new Dictionary<String, byte[]>();

        //private static List<SpriteRef> Sprites = new List<SpriteRef>();

        static SpriteHelperNew()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            ResourceManager resourceManager = new ResourceManager("Resources", assembly);
            ResourceSet resourceSet = resourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);//Resources.ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
            foreach (DictionaryEntry entry in resourceSet)
            {
                var conv = entry.Value as Bitmap;
                if (conv != null)
                {
                    MyResources.Add(entry.Key.ToString().ToLower(), (byte[])new ImageConverter().ConvertTo((Bitmap)entry.Value, typeof(byte[])));
                }
                else
                {
                    MyResources.Add(entry.Key.ToString().ToLower(), (byte[])entry.Value);
                }
            }
        }

        private static Dictionary<String, Bitmap> cachedMaps = new Dictionary<string, Bitmap>();

        public static String ConvertNames(String name)
        {
            if (name.ToLower().Contains("smite"))
            {
                return "SummonerSmite";
            }
            switch (name)
            {
                case "viw":
                    return "ViW";

                case "zedult":
                    return "ZedUlt";

                case "vayneinquisition":
                    return "VayneInquisition";

                case "reksaie":
                    return "RekSaiE";

                default:
                    return name;
            }
        }

        public static void DownloadImageOpGg(string name, String subFolder)
        {
            WebRequest request = null;
            WebRequest requestSize = null;
            request =
            WebRequest.Create("http://ss.op.gg/images/profile_icons/" + name);
            requestSize =
            WebRequest.Create("http://ss.op.gg/images/profile_icons/" + name);
            requestSize.Method = "HEAD";
            if (request == null || requestSize == null)
                return;
            try
            {
                long fileSize = 0;
                using (var resp = (HttpWebResponse)requestSize.GetResponse())
                {
                    if (resp.StatusCode == HttpStatusCode.OK)
                    {
                        fileSize = resp.ContentLength;
                    }
                }
                if (fileSize == GetFileSize(name, subFolder))
                    return;
                Stream responseStream;
                using (var response = (HttpWebResponse)request.GetResponse())
                    if (response.StatusCode == HttpStatusCode.OK)
                        using (responseStream = response.GetResponseStream())
                        {
                            if (responseStream != null)
                            {
                                using (var memoryStream = new MemoryStream())
                                {
                                    responseStream.CopyTo(memoryStream);
                                    SaveImage(name, memoryStream.ToArray(), subFolder);
                                }
                            }
                        }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot download file: {0}, Exception: {1}", name, ex);
            }
        }

        public static void DownloadImageRiot(string name, DownloadType type, String subFolder)
        {
            String version = "";
            try
            {
                String json = new WebClient().DownloadString("http://ddragon.leagueoflegends.com/realms/euw.json");
                version = (string)new JavaScriptSerializer().Deserialize<Dictionary<String, Object>>(json)["v"];
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot download file: {0}, Exception: {1}", name, ex);
                return;
            }
            WebRequest request = null;
            WebRequest requestSize = null;
            name = ConvertNames(name);
            if (type == DownloadType.Champion)
            {
                request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/champion/" + name + ".png");
                requestSize =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/champion/" + name + ".png");
                requestSize.Method = "HEAD";
            }
            else if (type == DownloadType.Spell)
            {
                //http://ddragon.leagueoflegends.com/cdn/4.20.1/img/spell/AhriFoxFire.png
                request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
                requestSize =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
                requestSize.Method = "HEAD";
            }
            else if (type == DownloadType.Summoner)
            {
                //summonerexhaust
                if (name.Contains("summonerodingarrison"))
                    name = "SummonerOdinGarrison";
                else
                    name = name[0].ToString().ToUpper() + name.Substring(1, 7) + name[8].ToString().ToUpper() + name.Substring(9, name.Length - 9);
                request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
                requestSize =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
                requestSize.Method = "HEAD";
            }
            else if (type == DownloadType.Item)
            {
                //http://ddragon.leagueoflegends.com/cdn/4.20.1/img/spell/AhriFoxFire.png
                request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
                requestSize =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
                requestSize.Method = "HEAD";
            }
            if (request == null || requestSize == null)
                return;
            try
            {
                long fileSize = 0;
                using (WebResponse resp = requestSize.GetResponse())
                {
                    fileSize = resp.ContentLength;
                }
                if (fileSize == GetFileSize(name, subFolder))
                    return;
                Stream responseStream;
                using (WebResponse response = request.GetResponse())
                using (responseStream = response.GetResponseStream())
                {
                    if (responseStream != null)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            responseStream.CopyTo(memoryStream);
                            SaveImage(name, memoryStream.ToArray(), subFolder);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot download file: {0}, Exception: {1}", name, ex);
            }
        }

        private static int GetFileSize(String name, String subFolder)
        {
            int size = 0;
            string loc = Path.Combine(new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeagueSharp", "Assemblies", "cache",
                "SAssemblies", subFolder, name  + ".png"
            });
            try
            {
                byte[] bitmap = File.ReadAllBytes(loc);
                size = bitmap.Length;
            }
            catch (Exception)
            {

            }
            return size;
        }

        public static void SaveImage(String name, /*Bitmap*/byte[] bitmap, String subFolder)
        {
            string loc = Path.Combine(new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeagueSharp", "Assemblies", "cache",
                "SAssemblies", subFolder, name  + ".png"
            });
            Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeagueSharp",
                        "Assemblies", "cache", "SAssemblies", subFolder));
            File.WriteAllBytes(loc, bitmap/*(byte[])new ImageConverter().ConvertTo(bitmap, typeof(byte[]))*/);
        }

        public static void LoadTexture(String name, ref SpriteInfoNew spriteInfo, String subFolder)
        {
            if (spriteInfo == null)
                spriteInfo = new SpriteInfoNew();
            Byte[] bitmap = null;
            name = ConvertNames(name);
            string loc = Path.Combine(new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeagueSharp", "Assemblies", "cache",
                "SAssemblies", subFolder, name  + ".png"
            });
            try
            {
                bitmap = File.ReadAllBytes(loc);
                spriteInfo.Bitmap = (Bitmap)new ImageConverter().ConvertFrom(bitmap);
                spriteInfo.Sprite = new Render.Sprite(bitmap, new Vector2(0, 0));
                spriteInfo.DownloadFinished = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot load file: {0}, Exception: {1}", name, ex);
            }
        }

        public static void LoadTexture(String name, ref Texture texture, DownloadType type)
        {
            //_rafList.SearchFileEntries();
        }

        public static void LoadTexture(String name, ref Texture texture, TextureType type)
        {
            if ((type == TextureType.Default || type == TextureType.Summoner) && MyResources.ContainsKey(name.ToLower()))
            {
                try
                {
                    texture = Texture.FromMemory(Drawing.Direct3DDevice, MyResources[name.ToLower()]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
            else if (type == TextureType.Summoner && MyResources.ContainsKey(name.ToLower().Remove(name.Length - 1)))
            {
                try
                {
                    texture = Texture.FromMemory(Drawing.Direct3DDevice,
                        MyResources[name.ToLower().Remove(name.Length - 1)]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
            else if (type == TextureType.Item && MyResources.ContainsKey(name.ToLower().Insert(0, "_")))
            {
                try
                {
                    texture = Texture.FromMemory(Drawing.Direct3DDevice, MyResources[name.ToLower().Insert(0, "_")]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
            else
            {
                Console.WriteLine("SAwarness: " + name + " is missing. Please inform Screeder!");
            }
        }

        public static void LoadTexture(String name, ref SpriteInfoNew texture, TextureType type)
        {
            if (texture == null)
                texture = new SpriteInfoNew();
            Bitmap bmp;
            if ((type == TextureType.Default || type == TextureType.Summoner) && MyResources.ContainsKey(name.ToLower()))
            {
                try
                {
                    using (var ms = new MemoryStream(MyResources[name.ToLower()]))
                    {
                        bmp = new Bitmap(ms);
                    }
                    texture.Bitmap = (Bitmap)bmp.Clone();
                    texture.Sprite = new Render.Sprite(bmp, new Vector2(0, 0));
                    texture.DownloadFinished = true;
                    //texture.Sprite.UpdateTextureBitmap(bmp);
                    //texture = new Render.Sprite(bmp, new Vector2(0, 0));
                }
                catch (Exception ex)
                {
                    if (texture == null)
                    {
                        texture = new SpriteInfoNew();
                        texture.Sprite = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
                    }
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
            else if (type == TextureType.Summoner && MyResources.ContainsKey(name.ToLower().Remove(name.Length - 1)))
            {
                try
                {
                    //texture = new Render.Sprite((Bitmap)Resources.ResourceManager.GetObject(name.ToLower().Remove(name.Length - 1)), new Vector2(0, 0));
                }
                catch (Exception ex)
                {
                    if (texture == null)
                    {
                        texture = new SpriteInfoNew();
                        texture.Sprite = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
                    }
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
            else if (type == TextureType.Item && MyResources.ContainsKey(name.ToLower().Insert(0, "_")))
            {
                try
                {
                    //texture = new Render.Sprite((Bitmap)Resources.ResourceManager.GetObject(name.ToLower().Insert(0, "_")), new Vector2(0, 0));
                }
                catch (Exception ex)
                {
                    if (texture == null)
                    {
                        texture = new SpriteInfoNew();
                        texture.Sprite = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
                    }
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
            else
            {
                if (texture == null)
                {
                    texture = new SpriteInfoNew();
                    texture.Sprite = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
                }
                Console.WriteLine("SAwarness: " + name + " is missing. Please inform Screeder!");
            }
        }

        public static void LoadTexture(Bitmap map, ref Render.Sprite texture)
        {
            if (texture == null)
                texture = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
            texture.UpdateTextureBitmap(map);
        }

        public static bool LoadTexture(String name, ref Render.Sprite texture, DownloadType type)
        {
            try
            {
                Bitmap map = null;
                if (!cachedMaps.ContainsKey(name))
                {
                    //map = DownloadImageRiot(name, type);
                    cachedMaps.Add(name, (Bitmap)map.Clone());
                }
                else
                {
                    map = new Bitmap((Bitmap)cachedMaps[name].Clone());
                }
                if (map == null)
                {
                    texture = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
                    Console.WriteLine("SAwarness: " + name + " is missing. Please inform Screeder!");
                    return false;
                }
                texture = new Render.Sprite(map, new Vector2(0, 0));
                //texture.UpdateTextureBitmap(map);
                return true;
                //texture = new Render.Sprite(map, new Vector2(0, 0));
            }
            catch (Exception ex)
            {
                Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                return false;
            }
        }

        public class SpriteInfoNew : IDisposable
        {
            public enum OVD
            {
                Small,
                Big
            }

            public Render.Sprite Sprite;
            public Bitmap Bitmap;
            public bool DownloadFinished = false;
            public bool LoadingFinished = false;
            public OVD Mode = OVD.Small;

            public void Dispose()
            {
                if (Sprite != null)
                    Sprite.Dispose();

                if (Bitmap != null)
                    Bitmap.Dispose();

            }

            ~SpriteInfoNew()
            {
                Dispose();
            }
        }
    }

    public static class SpriteHelper
    {
        public enum TextureType
        {
            Default,
            Summoner,
            Item
        }

        public enum DownloadType
        {
            Champion,
            Spell,
            Summoner,
            Item
        }

        private static Downloader _downloader = new Downloader();
        public static readonly Dictionary<String, byte[]> MyResources = new Dictionary<String, byte[]>();

        //private static List<SpriteRef> Sprites = new List<SpriteRef>();

        static SpriteHelper()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            ResourceManager resourceManager = new ResourceManager(assembly.GetName().Name + ".Properties.Resources", assembly);
            ResourceSet resourceSet = resourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
            foreach (DictionaryEntry entry in resourceSet)
            {
                var conv = entry.Value as Bitmap;
                if (conv != null)
                {
                    MyResources.Add(entry.Key.ToString().ToLower(), (byte[])new ImageConverter().ConvertTo((Bitmap)entry.Value, typeof(byte[])));
                }
                else
                {
                    MyResources.Add(entry.Key.ToString().ToLower(), (byte[])entry.Value);
                }
            }
        }

        private static Dictionary<String, Bitmap> cachedMaps = new Dictionary<string, Bitmap>();

        public static String ConvertNames(String name)
        {
            if (name.ToLower().Contains("smite"))
            {
                return "SummonerSmite";
            }
            switch (name)
            {
                case "viw":
                    return "ViW";
                    
                case "zedult":
                    return "ZedUlt";

                case "vayneinquisition":
                    return "VayneInquisition";

                case "reksaie":
                    return "RekSaiE";

                case "dravenspinning":
                    return "DravenSpinning";

                default:
                    return name;
            }
        }

        public static void DownloadImageOpGg(string name, String subFolder)
        {
            WebRequest request = null;
            WebRequest requestSize = null;
            request =
            WebRequest.Create("http://ss.op.gg/images/profile_icons/" + name);
            requestSize =
            WebRequest.Create("http://ss.op.gg/images/profile_icons/" + name);
            requestSize.Method = "HEAD";
            if (request == null || requestSize == null)
                return;
            try
            {
                long fileSize = 0;
                using (var resp = (HttpWebResponse)requestSize.GetResponse())
                {
                    if (resp.StatusCode == HttpStatusCode.OK)
                    {
                        fileSize = resp.ContentLength;
                    }
                }
                if (fileSize == GetFileSize(name, subFolder))
                    return;
                Stream responseStream;
                using (var response = (HttpWebResponse)request.GetResponse())
                if(response.StatusCode == HttpStatusCode.OK)
                using (responseStream = response.GetResponseStream())
                {
                    if (responseStream != null)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            responseStream.CopyTo(memoryStream);
                            SaveImage(name, memoryStream.ToArray(), subFolder);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot download file: {0}, Exception: {1}", name, ex);
            }
        }

        public static void DownloadImageRiot(string name, DownloadType type, String subFolder)
        {
            String version = "";
            try
            {
                String json = new WebClient().DownloadString("http://ddragon.leagueoflegends.com/realms/euw.json");
                version = (string)new JavaScriptSerializer().Deserialize<Dictionary<String, Object>>(json)["v"];
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot download file: {0}, Exception: {1}", name, ex);
                return;
            }
            WebRequest request = null;
            WebRequest requestSize = null;
            name = ConvertNames(name);
            if (type == DownloadType.Champion)
            {
                request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/champion/" + name + ".png");
                requestSize =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/champion/" + name + ".png");
                requestSize.Method = "HEAD";
            }
            else if (type == DownloadType.Spell)
            {
                //http://ddragon.leagueoflegends.com/cdn/4.20.1/img/spell/AhriFoxFire.png
                request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
                requestSize =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
                requestSize.Method = "HEAD";
            }
            else if (type == DownloadType.Summoner)
            {
                //summonerexhaust
                if (name.Contains("summonerodingarrison"))
                    name = "SummonerOdinGarrison";
                else
                    name = name[0].ToString().ToUpper() + name.Substring(1, 7) + name[8].ToString().ToUpper() + name.Substring(9, name.Length - 9);
                request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
                requestSize =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
                requestSize.Method = "HEAD";
            }
            else if (type == DownloadType.Item)
            {
                //http://ddragon.leagueoflegends.com/cdn/4.20.1/img/spell/AhriFoxFire.png
                request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
                requestSize =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
                requestSize.Method = "HEAD";
            }
            if (request == null || requestSize == null)
                return;
            try
            {
                long fileSize = 0;
                using (WebResponse resp = requestSize.GetResponse())
                {
                    fileSize = resp.ContentLength;
                }
                if (fileSize == GetFileSize(name, subFolder))
                    return;
                Stream responseStream;
                using (WebResponse response = request.GetResponse())
                using (responseStream = response.GetResponseStream())
                {
                    if (responseStream != null)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            responseStream.CopyTo(memoryStream);
                            SaveImage(name, memoryStream.ToArray(), subFolder);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot download file: {0}, Exception: {1}", name, ex);
            }
        }

        private static int GetFileSize(String name, String subFolder)
        {
            int size = 0;
            string loc = Path.Combine(new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeagueSharp", "Assemblies", "cache",
                "SAssemblies", subFolder, name  + ".png"
            });
            try
            {
                byte[] bitmap = File.ReadAllBytes(loc);
                size = bitmap.Length;
            }
            catch (Exception)
            {
                
            }
            return size;
        }

        public static void SaveImage(String name, /*Bitmap*/byte[] bitmap, String subFolder)
        {
            string loc = Path.Combine(new[]
            {
                SandboxConfig.DataDirectory, "Assemblies", "cache",
                "SAssemblies", subFolder, name  + ".png"
            });
            Directory.CreateDirectory(Path.Combine(SandboxConfig.DataDirectory,
                        "Assemblies", "cache", "SAssemblies", subFolder));
            File.WriteAllBytes(loc, bitmap/*(byte[])new ImageConverter().ConvertTo(bitmap, typeof(byte[]))*/);
        }

        public static void LoadTexture(String name, ref SpriteInfo spriteInfo, String subFolder)
        {
            if (spriteInfo == null)
                spriteInfo = new SpriteInfo();
            Byte[] bitmap = null;
            name = ConvertNames(name);
            string loc = Path.Combine(new[]
            {
                SandboxConfig.DataDirectory, "Assemblies", "cache",
                "SAssemblies", subFolder, name  + ".png"
            });
            try
            {
                bitmap = File.ReadAllBytes(loc);
                spriteInfo.Bitmap = (Bitmap)new ImageConverter().ConvertFrom(bitmap);
                spriteInfo.Sprite = new Render.Sprite(bitmap, new Vector2(0, 0));
                spriteInfo.DownloadFinished = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot load file: {0}, Exception: {1}", name, ex);
            }
        }

        public static void LoadTexture(String name, ref SpriteInfo spriteInfo, String optionalName, RafLoader.ImageList list)
        {
            if (spriteInfo == null)
                spriteInfo = new SpriteInfo();
            Byte[] bitmap = null;
            bitmap = RafLoader.GetImage(name, list, optionalName);
            try
            {
                if (bitmap == null)
                    throw new Exception("Picture not available!");
                Texture tex = Texture.FromMemory(Drawing.Direct3DDevice, bitmap);
                spriteInfo.Sprite = new Render.Sprite(tex, new Vector2(0, 0));
                spriteInfo.Bitmap = spriteInfo.Sprite.Bitmap;
                spriteInfo.DownloadFinished = true;
                tex.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot load file: {0}, Exception: {1}", name, ex);
            }
        }

        public static void LoadTexture(Bitmap bitmap, ref SpriteInfo spriteInfo)
        {
            if (spriteInfo == null)
                spriteInfo = new SpriteInfo();
            try
            {
                if(bitmap == null)
                    throw new Exception("Picture not available!");
                Texture tex = Texture.FromMemory(Drawing.Direct3DDevice, (byte[])new ImageConverter().ConvertTo(bitmap, typeof(byte[])));
                spriteInfo.Sprite = new Render.Sprite(tex, new Vector2(0, 0));
                spriteInfo.Bitmap = spriteInfo.Sprite.Bitmap;
                spriteInfo.DownloadFinished = true;
                tex.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot load texture, Exception: {0}", ex);
            }
        }

        public static void LoadTexture(String name, ref Texture texture, TextureType type)
        {
            if ((type == TextureType.Default || type == TextureType.Summoner) && MyResources.ContainsKey(name.ToLower()))
            {
                try
                {
                    texture = Texture.FromMemory(Drawing.Direct3DDevice, MyResources[name.ToLower()]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
            else if (type == TextureType.Summoner && MyResources.ContainsKey(name.ToLower().Remove(name.Length - 1)))
            {
                try
                {
                    texture = Texture.FromMemory(Drawing.Direct3DDevice,
                        MyResources[name.ToLower().Remove(name.Length - 1)]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
            else if (type == TextureType.Item && MyResources.ContainsKey(name.ToLower().Insert(0, "_")))
            {
                try
                {
                    texture = Texture.FromMemory(Drawing.Direct3DDevice, MyResources[name.ToLower().Insert(0, "_")]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
            else
            {
                Console.WriteLine("SAwarness: " + name + " is missing. Please inform Screeder!");
            }
        }

        public static void LoadTexture(String name, ref SpriteInfo texture, TextureType type)
        {
            if (texture == null)
                texture = new SpriteInfo();
            Bitmap bmp;
            if ((type == TextureType.Default || type == TextureType.Summoner) && MyResources.ContainsKey(name.ToLower()))
            {
                try
                {
                    using (var ms = new MemoryStream(MyResources[name.ToLower()]))
                    {
                        bmp = new Bitmap(ms);
                    }
                    texture.Bitmap = (Bitmap)bmp.Clone();
                    texture.Sprite = new Render.Sprite(bmp, new Vector2(0, 0));
                    texture.DownloadFinished = true;
                    //texture.Sprite.UpdateTextureBitmap(bmp);
                    //texture = new Render.Sprite(bmp, new Vector2(0, 0));
                }
                catch (Exception ex)
                {
                    if (texture == null)
                    {
                        texture = new SpriteInfo();
                        texture.Sprite = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
                    }
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
            else if (type == TextureType.Summoner && MyResources.ContainsKey(name.ToLower().Remove(name.Length - 1)))
            {
                try
                {
                    //texture = new Render.Sprite((Bitmap)Resources.ResourceManager.GetObject(name.ToLower().Remove(name.Length - 1)), new Vector2(0, 0));
                }
                catch (Exception ex)
                {
                    if (texture == null)
                    {
                        texture = new SpriteInfo();
                        texture.Sprite = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
                    }
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
            else if (type == TextureType.Item && MyResources.ContainsKey(name.ToLower().Insert(0, "_")))
            {
                try
                {
                    //texture = new Render.Sprite((Bitmap)Resources.ResourceManager.GetObject(name.ToLower().Insert(0, "_")), new Vector2(0, 0));
                }
                catch (Exception ex)
                {
                    if (texture == null)
                    {
                        texture = new SpriteInfo();
                        texture.Sprite = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
                    }
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
            else
            {
                if (texture == null)
                {
                    texture = new SpriteInfo();
                    texture.Sprite = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
                }
                Console.WriteLine("SAwarness: " + name + " is missing. Please inform Screeder!");
            }
        }

        public static void LoadTexture(Bitmap map, ref Render.Sprite texture)
        {
            if (texture == null)
                texture = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
            texture.UpdateTextureBitmap(map);
        }

        public static bool LoadTexture(String name, ref Render.Sprite texture, DownloadType type)
        {
            try
            {
                Bitmap map = null;
                if (!cachedMaps.ContainsKey(name))
                {
                    //map = DownloadImageRiot(name, type);
                    cachedMaps.Add(name, (Bitmap)map.Clone());
                }
                else
                {
                    map = new Bitmap((Bitmap)cachedMaps[name].Clone());
                }
                if (map == null)
                {
                    texture = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
                    Console.WriteLine("SAwarness: " + name + " is missing. Please inform Screeder!");
                    return false;
                }
                texture = new Render.Sprite(map, new Vector2(0, 0));
                //texture.UpdateTextureBitmap(map);
                return true;
                //texture = new Render.Sprite(map, new Vector2(0, 0));
            }
            catch (Exception ex)
            {
                Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                return false;
            }
        }

        public async static Task<SpriteInfo> LoadTextureAsync(String name, SpriteInfo texture, DownloadType type)
        {
            try
            {
                if (texture == null)
                    texture = new SpriteInfo();
                Render.Sprite tex = texture.Sprite;
                LoadTextureAsyncInternal(name, () => texture, x => texture = x, type);
                //texture.Sprite = tex;
                texture.LoadingFinished = true;
                return texture;
                //texture = new Render.Sprite(map, new Vector2(0, 0));
            }
            catch (Exception ex)
            {
                Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                return new SpriteInfo();
            }
        }

        public async static Task<Bitmap> DownloadImageAsync(string name, DownloadType type)
        {
            String json = new WebClient().DownloadString("http://ddragon.leagueoflegends.com/realms/euw.json");
            String version = (string)new JavaScriptSerializer().Deserialize<Dictionary<String, Object>>(json)["v"];
            WebRequest request = null;
            if (type == DownloadType.Champion)
            {
                request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/champion/" + name + ".png");
            }
            else if (type == DownloadType.Spell)
            {
                //http://ddragon.leagueoflegends.com/cdn/4.20.1/img/spell/AhriFoxFire.png
                request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
            }
            else if (type == DownloadType.Summoner)
            {
                //summonerexhaust
                if (name.Contains("summonerodingarrison"))
                    name = "SummonerOdinGarrison";
                else
                    name = name[0].ToString().ToUpper() + name.Substring(1, 7) + name[8].ToString().ToUpper() + name.Substring(9, name.Length - 9);
                request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
            }
            else if (type == DownloadType.Item)
            {
                //http://ddragon.leagueoflegends.com/cdn/4.20.1/img/spell/AhriFoxFire.png
                request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
            }
            if (request == null)
                return null;
            try
            {
                Stream responseStream;
                Task<WebResponse> reqA = request.GetResponseAsync();
                using (WebResponse response = await reqA) //Crash with AsyncRequest
                using (responseStream = response.GetResponseStream())
                {
                    return responseStream != null ? new Bitmap(responseStream) : null;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                return null;
            }
        }

        private static void LoadTextureAsyncInternal(String name, Func<SpriteInfo> getTexture, Action<SpriteInfo> setTexture, DownloadType type)
        {
            try
            {
                SpriteInfo spriteInfo = getTexture();
                Render.Sprite texture;
                Bitmap map;
                if (!cachedMaps.ContainsKey(name))
                {
                    Task<Bitmap> bitmap = DownloadImageAsync(name, type);
                    if (bitmap == null || bitmap.Result == null || bitmap.Status == TaskStatus.Faulted)
                    {
                        texture = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
                        Console.WriteLine("SAwarness: " + name + " is missing. Please inform Screeder!");
                        spriteInfo.Sprite = texture;
                        setTexture(spriteInfo);
                        throw new Exception();
                    }
                    map = bitmap.Result; //Change to Async to make it Async, currently crashing through loading is not thread safe.
                    //Bitmap map = await bitmap;
                    cachedMaps.Add(name, (Bitmap)map.Clone());
                }
                else
                {
                    map = new Bitmap((Bitmap)cachedMaps[name].Clone());
                }
                if (map == null)
                {
                    texture = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
                    spriteInfo.Sprite = texture;
                    setTexture(spriteInfo);
                    Console.WriteLine("SAwarness: " + name + " is missing. Please inform Screeder!");
                    throw new Exception();
                }
                spriteInfo.Bitmap = (Bitmap)map.Clone();
                texture = new Render.Sprite(map, new Vector2(0, 0));
                spriteInfo.DownloadFinished = true;
                spriteInfo.Sprite = texture;

                setTexture(spriteInfo);
                //texture = new Render.Sprite(map, new Vector2(0, 0));
            }
            catch (Exception ex)
            {
                Console.WriteLine("SAwarness: Could not load async " + name + ".");
            }
        }

        public class SpriteInfo : IDisposable
        {
            public enum OVD
            {
                Small,
                Big
            }

            public Render.Sprite Sprite;
            public Bitmap Bitmap;
            public bool DownloadFinished = false;
            public bool LoadingFinished = false;
            public OVD Mode = OVD.Small;

            public void Dispose()
            {
                if (Sprite != null)
                    Sprite.Dispose();

                if (Bitmap != null)
                    Bitmap.Dispose();

            }

            ~SpriteInfo()
            {
                Dispose();
            }
        }
    }

    internal static class Speech
    {
        private static Dictionary<int, SpeechSynthesizer> tts = new Dictionary<int, SpeechSynthesizer>();

        static Speech()
        {
            return;
            for (int i = 0; i < 4; i++)
            {
                tts.Add(i, new SpeechSynthesizer());
            }

            ReadOnlyCollection<InstalledVoice> list = tts[0].GetInstalledVoices();
            String strVoice = "";
            foreach (var voice in list)
            {
                if (voice.VoiceInfo.Culture.EnglishName.Contains("English") && voice.Enabled)
                {
                    strVoice = voice.VoiceInfo.Name;
                }
            }

            foreach (KeyValuePair<int, SpeechSynthesizer> speech in tts)
            {
                if (!strVoice.Equals(""))
                {
                    speech.Value.SelectVoice(strVoice);
                }
            }
            AppDomain.CurrentDomain.DomainUnload += CurrentDomainOnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnDomainUnload;
        }

        private static void CurrentDomainOnDomainUnload(object sender, EventArgs e)
        {
            foreach (var speech in tts)
            {
                if (speech.Value.State != SynthesizerState.Ready)
                {
                    speech.Value.SpeakAsyncCancelAll();
                }
                speech.Value.Dispose();
            }
        }

        public static void Speak(String text)
        {
            return;
            bool speaking = false;
            foreach (var speech in tts)
            {
                if (speech.Value.State == SynthesizerState.Ready && !speaking)
                {
                    if (speech.Value.Volume !=
                        Menu.GlobalSettings.GetMenuItem("SAssembliesGlobalSettingsVoiceVolume")
                            .GetValue<Slider>()
                            .Value)
                    {
                        speech.Value.Volume =
                            Menu.GlobalSettings.GetMenuItem("SAssembliesGlobalSettingsVoiceVolume")
                                .GetValue<Slider>()
                                .Value;
                    }
                    speaking = true;
                    speech.Value.SpeakAsync(text);
                }
                else if (speech.Value.State != SynthesizerState.Ready)
                {
                    speech.Value.Pause();
                    speech.Value.Volume = 1;
                    speech.Value.Resume();
                }
            }
        }
    }

    internal class Ward
    {
        public enum WardType
        {
            Stealth,
            Vision,
            Temp,
            TempVision
        }

        public static readonly List<WardItem> WardItems = new List<WardItem>();
        public static Menu.MenuItemSettings Wards = new Menu.MenuItemSettings();

        static Ward()
        {
            WardItems.Add(new WardItem(3360, "Feral Flare", "", 1000, 180, WardType.Stealth));
            WardItems.Add(new WardItem(2043, "Vision Ward", "VisionWard", 600, 180, WardType.Vision));
            WardItems.Add(new WardItem(2044, "Stealth Ward", "SightWard", 600, 180, WardType.Stealth));
            WardItems.Add(new WardItem(3154, "Wriggle's Lantern", "WriggleLantern", 600, 180, WardType.Stealth));
            WardItems.Add(new WardItem(2045, "Ruby Sightstone", "ItemGhostWard", 600, 180, WardType.Stealth));
            WardItems.Add(new WardItem(2049, "Sightstone", "ItemGhostWard", 600, 180, WardType.Stealth));
            WardItems.Add(new WardItem(2050, "Explorer's Ward", "ItemMiniWard", 600, 60, WardType.Stealth));
            WardItems.Add(new WardItem(3340, "Greater Stealth Totem", "", 600, 120, WardType.Stealth));
            WardItems.Add(new WardItem(3361, "Greater Stealth Totem", "", 600, 180, WardType.Stealth));
            WardItems.Add(new WardItem(3362, "Greater Vision Totem", "", 600, 180, WardType.Vision));
            WardItems.Add(new WardItem(3366, "Bonetooth Necklace", "", 600, 120, WardType.Stealth));
            WardItems.Add(new WardItem(3367, "Bonetooth Necklace", "", 600, 120, WardType.Stealth));
            WardItems.Add(new WardItem(3368, "Bonetooth Necklace", "", 600, 120, WardType.Stealth));
            WardItems.Add(new WardItem(3369, "Bonetooth Necklace", "", 600, 120, WardType.Stealth));
            WardItems.Add(new WardItem(3371, "Bonetooth Necklace", "", 600, 180, WardType.Stealth));
            WardItems.Add(new WardItem(3375, "Head of Kha'Zix", "", 600, 180, WardType.Stealth));
            WardItems.Add(new WardItem(3205, "Quill Coat", "", 600, 180, WardType.Stealth));
            WardItems.Add(new WardItem(3207, "Spirit of the Ancient Golem", "", 600, 180, WardType.Stealth));
            WardItems.Add(new WardItem(3342, "Scrying Orb", "", 2500, 2, WardType.Temp));
            WardItems.Add(new WardItem(3363, "Farsight Orb", "", 4000, 2, WardType.Temp));
            WardItems.Add(new WardItem(3187, "Hextech Sweeper", "", 800, 5, WardType.TempVision));
            WardItems.Add(new WardItem(3159, "Grez's Spectral Lantern", "", 800, 5, WardType.Temp));
            WardItems.Add(new WardItem(3364, "Oracle's Lens", "", 600, 10, WardType.TempVision));
        }

        private static void SetupMainMenu()
        {
            var menu = new LeagueSharp.Common.Menu("SAssemblies", "SAssembliesWards", true);
            SetupMenu(menu);
            menu.AddToMainMenu();
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            Language.SetLanguage();
            Wards.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu("Wards", "SAssembliesWards"));
            Wards.MenuItems.Add(Wards.Menu.AddItem(new MenuItem("SAssembliesWardsActive", Language.GetString("GLOBAL_ACTIVE")).SetValue(false)));
            return Wards;
        }

        public static WardItem GetWardItem()
        {
            return WardItems.FirstOrDefault(x => Items.HasItem(x.Id) && Items.CanUseItem(x.Id));
        }

        public static InventorySlot GetWardSlot()
        {
            foreach (WardItem ward in WardItems)
            {
                if (Items.CanUseItem(ward.Id))
                {
                    return ObjectManager.Player.InventoryItems.FirstOrDefault(slot => slot.Id == (ItemId)ward.Id);
                }
            }
            return null;
        }

        public class WardItem
        {
            public readonly int Id;
            public int Duration;
            public String Name;
            public int Range;
            public String SpellName;
            public WardType Type;

            public WardItem(int id, string name, string spellName, int range, int duration, WardType type)
            {
                Id = id;
                Name = name;
                SpellName = spellName;
                Range = range;
                Duration = duration;
                Type = type;
            }
        }
    }

    internal static class DirectXDrawer
    {
        private static void InternalRender(Vector3 target)
        {
            //Drawing.Direct3DDevice.SetTransform(TransformState.World, Matrix.Translation(target));
            //Drawing.Direct3DDevice.SetTransform(TransformState.View, Drawing.View);
            //Drawing.Direct3DDevice.SetTransform(TransformState.Projection, Drawing.Projection);

            Drawing.Direct3DDevice.VertexShader = null;
            Drawing.Direct3DDevice.PixelShader = null;
            Drawing.Direct3DDevice.SetRenderState(RenderState.AlphaBlendEnable, true);
            Drawing.Direct3DDevice.SetRenderState(RenderState.BlendOperation, BlendOperation.Add);
            Drawing.Direct3DDevice.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceAlpha);
            Drawing.Direct3DDevice.SetRenderState(RenderState.SourceBlend, Blend.SourceAlpha);
            Drawing.Direct3DDevice.SetRenderState(RenderState.Lighting, 0);
            Drawing.Direct3DDevice.SetRenderState(RenderState.ZEnable, true);
            Drawing.Direct3DDevice.SetRenderState(RenderState.AntialiasedLineEnable, true);
            Drawing.Direct3DDevice.SetRenderState(RenderState.Clipping, true);
            Drawing.Direct3DDevice.SetRenderState(RenderState.EnableAdaptiveTessellation, true);
            Drawing.Direct3DDevice.SetRenderState(RenderState.MultisampleAntialias, true);
            Drawing.Direct3DDevice.SetRenderState(RenderState.ShadeMode, ShadeMode.Gouraud);
            Drawing.Direct3DDevice.SetTexture(0, null);
            Drawing.Direct3DDevice.SetRenderState(RenderState.CullMode, Cull.None);
        }

        public static void DrawLine(Vector3 from, Vector3 to, Color color)
        {
            var vertices = new PositionColored[2];
            vertices[0] = new PositionColored(Vector3.Zero, color.ToArgb());
            from = from.SwitchYZ();
            to = to.SwitchYZ();
            vertices[1] = new PositionColored(to - from, color.ToArgb());

            InternalRender(from);

            Drawing.Direct3DDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices.Length/2, vertices);
        }

        public static void DrawLine(Line line, Vector3 from, Vector3 to, ColorBGRA color, Size size = default(Size),
            float[] scale = null, float rotation = 0.0f)
        {
            if (line != null)
            {
                from = from.SwitchYZ();
                to = to.SwitchYZ();
                Matrix nMatrix = (scale != null ? Matrix.Scaling(scale[0], scale[1], 0) : Matrix.Scaling(1))*
                                 Matrix.RotationZ(rotation)*Matrix.Translation(from);
                Vector3[] vec = {from, to};
                line.DrawTransform(vec, nMatrix, color);
            }
        }

        public static void DrawText(Font font, String text, Size size, SharpDX.Color color)
        {
            DrawText(font, text, size.Width, size.Height, color);
        }


        //TODO: Too many drawtext for shadowtext, need another method fps issues
        public static void DrawText(Font font, String text, int posX, int posY, SharpDX.Color color)
        {
            if (font == null || font.IsDisposed)
            {
                throw new SharpDXException("");
            }
            Rectangle rec = font.MeasureText(null, text, FontDrawFlags.Center);
            //font.DrawText(null, text, posX + 1 + rec.X, posY, Color.Black);
            font.DrawText(null, text, posX + 1 + rec.X, posY + 1, SharpDX.Color.Black);
            font.DrawText(null, text, posX + rec.X, posY + 1, SharpDX.Color.Black);
            //font.DrawText(null, text, posX - 1 + rec.X, posY, Color.Black);
            font.DrawText(null, text, posX - 1 + rec.X, posY - 1, SharpDX.Color.Black);
            font.DrawText(null, text, posX + rec.X, posY - 1, SharpDX.Color.Black);
            font.DrawText(null, text, posX + rec.X, posY, color);
        }

        public static void DrawSprite(Sprite sprite, Texture texture, Size size, float[] scale = null,
            float rotation = 0.0f)
        {
            DrawSprite(sprite, texture, size, SharpDX.Color.White, scale, rotation);
        }

        public static void DrawSprite(Sprite sprite, Texture texture, Size size, SharpDX.Color color,
            float[] scale = null,
            float rotation = 0.0f)
        {
            if (sprite != null && !sprite.IsDisposed && texture != null && !texture.IsDisposed)
            {
                Matrix matrix = sprite.Transform;
                Matrix nMatrix = (scale != null ? Matrix.Scaling(scale[0], scale[1], 0) : Matrix.Scaling(1))*
                                 Matrix.RotationZ(rotation)*Matrix.Translation(size.Width, size.Height, 0);
                sprite.Transform = nMatrix;
                Matrix mT = Drawing.Direct3DDevice.GetTransform(TransformState.World);

                //InternalRender(mT.TranslationVector);
                if (Common.IsOnScreen(new Vector2(size.Width, size.Height)))
                    sprite.Draw(texture, color);
                sprite.Transform = matrix;
            }
        }

        public static void DrawTransformSprite(Sprite sprite, Texture texture, SharpDX.Color color, Size size,
            float[] scale,
            float rotation, Rectangle? spriteResize)
        {
            if (sprite != null && texture != null)
            {
                Matrix matrix = sprite.Transform;
                Matrix nMatrix = Matrix.Scaling(scale[0], scale[1], 0)*Matrix.RotationZ(rotation)*
                                 Matrix.Translation(size.Width, size.Height, 0);
                sprite.Transform = nMatrix;
                sprite.Draw(texture, color);
                sprite.Transform = matrix;
            }
        }

        public static void DrawTransformedSprite(Sprite sprite, Texture texture, Rectangle spriteResize,
            SharpDX.Color color)
        {
            if (sprite != null && texture != null)
            {
                sprite.Draw(texture, color);
            }
        }

        public static void DrawSprite(Sprite sprite, Texture texture, Size size, SharpDX.Color color,
            Rectangle? spriteResize)
        {
            if (sprite != null && texture != null)
            {
                sprite.Draw(texture, color, spriteResize, new Vector3(size.Width, size.Height, 0));
            }
        }

        public static void DrawSprite(Sprite sprite, Texture texture, Size size, SharpDX.Color color)
        {
            if (sprite != null && texture != null)
            {
                DrawSprite(sprite, texture, size, color, null);
            }
        }

        public struct PositionColored
        {
            public static readonly int Stride = Vector3.SizeInBytes + sizeof (int);

            public int Color;
            public Vector3 Position;

            public PositionColored(Vector3 pos, int col)
            {
                Position = pos;
                Color = col;
            }
        }
    }

    static class MapPositions
    {

        public enum Region
        {
            Unknown,
            TopLeftOuterJungle,
            TopLeftInnerJungle,
            TopRightOuterJungle,
            TopRightInnerJungle,
            BottomLeftOuterJungle,
            BottomLeftInnerJungle,
            BottomRightOuterJungle,
            BottomRightInnerJungle,
            TopOuterRiver,
            TopInnerRiver,
            BottomOuterRiver,
            BottomInnerRiver,
            LeftMidLane,
            CenterMidLane,
            RightMidLane,
            LeftBotLane,
            CenterBotLane,
            RightBotLane,
            LeftTopLane,
            CenterTopLane,
            RightTopLane,

            TopLane,
            BotLane,
            MidLane,
            Lane,

            BlueInnerJungle,
            BlueOuterJungle,
            BlueLeftJungle,
            BlueRightJungle,
            RedInnerJungle,
            RedOuterJungle,
            RedLeftJungle,
            RedRightJungle,
            BlueJungle,
            RedJungle,
            Jungle,

            BottomRiver,
            TopRiver,
            InnerRiver,
            OuterRiver,
            River,

            Base,
            BlueBase,
            RedBase,
        }

        static readonly Dictionary<Region, List<Geometry.Polygon>> _regions = new Dictionary<Region, List<Geometry.Polygon>>();


        static void Drawing_OnDraw(EventArgs args)
        {
            //DrawDebug();
        }

        private static void DrawDebug()
        {
            Region pos = Region.Unknown;
            foreach (var regionList in _regions)
            {
                foreach (var region in regionList.Value)
                {
                    region.Draw(Color.Aqua);
                    if (region.IsInside(ObjectManager.Player.ServerPosition))
                    {
                        pos = regionList.Key;
                    }
                }
            }
            Drawing.DrawText(Drawing.Width, Drawing.Height - 200, Color.Aqua, pos.ToString());
        }

        private static List<Geometry.Polygon> JoinPolygonLists(params List<Geometry.Polygon>[] lists)
        {
            List<Geometry.Polygon> list = new List<Geometry.Polygon>();
            foreach (var polygonList in lists)
            {
                foreach (var polygon in polygonList)
                {
                    list.Add(polygon);
                }
            }
            return list;
        }

        public static Region GetRegion(Vector2 point)
        {
            foreach (var regionList in _regions)
            {
                foreach (var region in regionList.Value)
                {
                    if (region.IsInside(point))
                    {
                        return regionList.Key;
                    }
                }
            }
            return Region.Unknown;
        }

        public static bool IsInRegion(Vector2 point, Region region)
        {
            foreach (var regionList in _regions)
            {
                foreach (var regionPos in regionList.Value)
                {
                    if (regionPos.IsInside(point))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

    }

    static class ThreadHelper
    {

        static ThreadEventHelper[] _threadHelpers = new ThreadEventHelper[10];
        static Thread[] _threads = new Thread[10];
        private static bool _cancelThread;
        private static int _lastUsed = 0;

        static ThreadHelper()
        {
            AppDomain.CurrentDomain.DomainUnload += CurrentDomainOnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnDomainUnload;
            for (int i = 0; i < _threads.Length; i++)
            {
                _threads[i] = new Thread(CallEvent);
                _threads[i].Start(i);
            }
        }

        private static void CurrentDomainOnDomainUnload(Object obj, EventArgs args)
        {
            _cancelThread = true;
        }

        private static void CallEvent(object id)
        {
            while (!_cancelThread)
            {
                Thread.Sleep(1);
                if (_threadHelpers[(int) id] != null)
                {
                    _threadHelpers[(int)id].OnCall();
                }
            }
        }

        public static ThreadEventHelper GetInstance()
        {
            if (_threadHelpers[_lastUsed] == null)
            {
                _threadHelpers[_lastUsed] = new ThreadEventHelper();
            }
            return _threadHelpers[_lastUsed];
        }

        public class ThreadEventHelper 
        {

            //For later usage maybe
            public class ThreadHelperEventArgs : EventArgs
            {

            }

            public event EventHandler<EventArgs> Called; 

            public void OnCall()
            {
                var target = Called;

                if (target != null)
                {
                    target(this, new EventArgs());
                }
            }
        }
    }

    static class AssemblyResolver
    {
        private static Assembly evadeAssembly;
        private static Assembly jsonAssembly;
        private static Assembly inibinAssembly;

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            String assembly = Assembly.GetExecutingAssembly().GetName().Name;
            string name = args.Name.Split(',')[0];
            if (name.ToLower().Contains("evade"))
            {
                if (evadeAssembly == null)
                {
                    evadeAssembly = Load(assembly + ".Resources.DLL.Evade.dll");
                }
                return evadeAssembly;
            }
            else if (name.ToLower().Contains("newtonsoft"))
            {
                if (jsonAssembly == null)
                {
                    jsonAssembly = Load(assembly + ".Resources.DLL.Newtonsoft.Json.dll");
                }
                return jsonAssembly;
            }
            else if (name.ToLower().Contains("gamefiles"))
            {
                if (inibinAssembly == null)
                {
                    inibinAssembly = Load(assembly + ".Resources.DLL.LeagueSharp.GameFiles.dll");
                }
                return inibinAssembly;
            }
            return null;
        }

        private static Assembly Load(String assemblyName)
        {
            byte[] ba = null;
            string resource = assemblyName;
            Assembly curAsm = Assembly.GetExecutingAssembly();
            using (Stream stm = curAsm.GetManifestResourceStream(resource))
            {
                ba = new byte[(int)stm.Length];
                stm.Read(ba, 0, (int)stm.Length);
                return Assembly.Load(ba);
            }
        }

        public static void Init()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }
    }

    static class Language
    {

        private static System.Resources.ResourceManager resMgr;
        private static System.Resources.ResourceManager resMgrAlt;

        public static void UpdateLanguage(string langID)
        {
            String assembly = Assembly.GetExecutingAssembly().GetName().Name;
            try
            {
                resMgr = new System.Resources.ResourceManager(assembly + ".Resources.TRANSLATIONS.Translation-" + langID, Assembly.GetExecutingAssembly());
                resMgrAlt = new System.Resources.ResourceManager(assembly + ".Resources.TRANSLATIONS.Translation-en-US", Assembly.GetExecutingAssembly());
            }
            catch (Exception)
            {
                resMgr = new System.Resources.ResourceManager(assembly + ".Resources.TRANSLATIONS.Translation-en-US", Assembly.GetExecutingAssembly());
                resMgrAlt = resMgr;
            }
        }
 
        public static string GetString(String pattern)
        {
            try
            {
                if (resMgr == null || resMgr.GetString(pattern) == null || resMgr.GetString(pattern).Equals(""))
                {
                    return resMgrAlt.GetString(pattern) ?? "";
                }
                return resMgr.GetString(pattern);
            }
            catch (Exception)
            {
                try
                {
                    return resMgrAlt.GetString(pattern);
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        public static void SetLanguage()
        {
            switch (Config.SelectedLanguage)
            {
                case "Arabic":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("ar-SA");
                    break;

                case "Chinese":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-CN");
                    break;

                case "Dutch":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("nl-NL");
                    break;

                case "English":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
                    break;

                case "French":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("fr-FR");
                    break;

                case "German":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("de-DE");
                    break;

                case "Greek":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("el-GR");
                    break;

                case "Italian":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("it-IT");
                    break;

                case "Korean":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("ko-KR");
                    break;

                case "Polish":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("pl-PL");
                    break;

                case "Portuguese":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("pt-PT");
                    break;

                case "Romanian":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("ro-RO");
                    break;

                case "Russian":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("ru-RU");
                    break;

                case "Spanish":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("es-ES");
                    break;

                case "Swedish":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("sv-SE");
                    break;

                case "Thai":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("th-TH");
                    break;

                case "Turkish":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("tr-TR");
                    break;

                case "Vietnamese":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("vi-VN");
                    break;

                default:
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
                    break;
            }
            UpdateLanguage(Thread.CurrentThread.CurrentUICulture.Name);
        }
    }
}