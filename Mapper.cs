using EVP;
using Life;
using Life.AreaSystem;
using Life.Network;
using Life.UI;
using Mapper.Classes;
using Mapper.Entities;
using ModKit.Helper;
using ModKit.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using _menu = AAMenu.Menu;
using mk = ModKit.Helper.TextFormattingHelper;

namespace Mapper
{
    public class Mapper : ModKit.ModKit
    {
        public static string ConfigDirectoryPath;
        public static string JsonDirectoryPath;
        public static string ConfigMapperPath;
        public static MapperConfig _mapperConfig;

        public Mapper(IGameAPI api) : base(api)
        {
            PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "1.0.0", "Aarnow");
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();

            InitConfigAndDirectory();
            _mapperConfig = LoadConfigFile(ConfigMapperPath);

            GenerateCommands();
            InsertMenu();

            Orm.RegisterTable<MapConfig>();

            ModKit.Internal.Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "initialisé");
        }
        #region Config
        private void InitConfigAndDirectory()
        {
            try
            {
                ConfigDirectoryPath = DirectoryPath + "/Mapper";
                ConfigMapperPath = Path.Combine(ConfigDirectoryPath, "MapperConfig.json");
                JsonDirectoryPath = ConfigDirectoryPath + "/Partage";

                if (!Directory.Exists(ConfigDirectoryPath)) Directory.CreateDirectory(ConfigDirectoryPath);
                if (!Directory.Exists(JsonDirectoryPath)) Directory.CreateDirectory(JsonDirectoryPath);
                if (!File.Exists(ConfigMapperPath)) InitMapperConfig();
            }
            catch (IOException ex)
            {
                ModKit.Internal.Logger.LogError("InitDirectory", ex.Message);
            }
        }

        private void InitMapperConfig()
        {
            MapperConfig mapperConfig = new MapperConfig();
            string json = JsonConvert.SerializeObject(mapperConfig);
            File.WriteAllText(ConfigMapperPath, json);
        }

        private MapperConfig LoadConfigFile(string path)
        {
            if (File.Exists(path))
            {
                string jsonContent = File.ReadAllText(path);
                MapperConfig mapperConfig = JsonConvert.DeserializeObject<MapperConfig>(jsonContent);

                return mapperConfig;
            }
            else return null;
        }
        #endregion
        public void InsertMenu()
        {
            _menu.AddAdminPluginTabLine(PluginInformations, 1, "Mapper", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                MapperPanel(player, true);
            }, 0);
        }
        public void GenerateCommands()
        {
            new SChatCommand("/mapper", new string[] { "/map" }, "Permet d'ouvrir le panel du plugin \"Mapper\"", "/mapper", (player, arg) =>
            {
                if(player.IsAdmin) MapperPanel(player);
                else player.Notify("Mapper", "Vous n'avez pas la permission requise.", NotificationManager.Type.Warning);
            }).Register();      
        }

        public void MapperPanel(Player player, bool isCmd = false)
        {
            //Déclaration
            Panel panel = PanelHelper.Create("Mapper", UIPanel.PanelType.TabPrice, player, () => MapperPanel(player, isCmd));

            //Corps
            panel.AddTabLine($"{mk.Color("Terrain actuel", mk.Colors.Verbose)}", _ => CheckAreaPanel(player));
            panel.AddTabLine($"{mk.Color("Vos sauvegardes", mk.Colors.Verbose)}", _ => ShowLoadableAreasPanel(player));
            panel.AddTabLine($"{mk.Color("Importer une sauvegarde", mk.Colors.Warning)}", _ => ImportAreaPanel(player));
            panel.AddTabLine($"{mk.Color("Appliquer la configuration", mk.Colors.Info)}", _ =>
            {
                _mapperConfig = LoadConfigFile(ConfigMapperPath);
                panel.Refresh();
            });

            if (isCmd) panel.AddButton("Retour", _ => AAMenu.AAMenu.menu.AdminPluginPanel(player));
            panel.NextButton("Sélectionner", () => panel.SelectTab());

            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        #region PANELS
        public void CheckAreaPanel(Player player)
        {
            LifeArea lifeArea = Nova.a.GetAreaById(player.setup.areaId);

            //Déclaration
            Panel panel = PanelHelper.Create($"Mapper - Terrain n°{player.setup.areaId}", UIPanel.PanelType.TabPrice, player, () => CheckAreaPanel(player));

            //Corps
            panel.AddTabLine($"{mk.Color("Numéro du terrain", mk.Colors.Warning)}: {lifeArea.areaId}", _ => {});
            panel.AddTabLine($"{mk.Color("Nombre d'objets", mk.Colors.Warning)}: {Utils.GetAreaObjectsCount(lifeArea)}", _ => {});

            panel.NextButton($"{mk.Size($"{mk.Color("sauvegarder", mk.Colors.Info)}", 16)}", () => InitSaveAreaPanel(player));
            panel.NextButton($"{mk.Size($"{mk.Color("Vos sauvegardes", mk.Colors.Success)}", 16)}", () => ShowLoadableAreasPanel(player, lifeArea.areaId));
            
            panel.NextButton($"{mk.Size($"{mk.Color("Vider", mk.Colors.Orange)}", 16)}", () => ClearAreaPanel(player, lifeArea.areaId));
            panel.AddButton($"{mk.Size($"{mk.Color("prochaine fonctionnalité...", mk.Colors.Grey)}", 12)}", _ => {});

            panel.PreviousButton();
            panel.CloseButton();

            //Affichage
            panel.Display();
        }
        public void ClearAreaPanel(Player player, uint areaId)
        {
            LifeArea lifeArea = Nova.a.GetAreaById(areaId);

            //Déclaration
            Panel panel = PanelHelper.Create($"Mapper - Vider le terrain n°{player.setup.areaId}", UIPanel.PanelType.Text, player, () => ClearAreaPanel(player, areaId));

            //Corps
            panel.TextLines.Add($"{mk.Size("Êtes-vous sûr de vouloir vider ce terrain ?", 18)}");
            panel.TextLines.Add("");
            panel.TextLines.Add($"{mk.Size(mk.Color(mk.Bold("ATTENTION"), mk.Colors.Error), 18)}");
            panel.TextLines.Add($"{mk.Size(mk.Color("L'ensemble des objets seront supprimés.", mk.Colors.Error), 18)}");

            panel.PreviousButton();
            panel.PreviousButtonWithAction("Confirmer", async () =>
            {
                if (Utils.ClearArea(lifeArea, this))
                {
                    player.Notify("Mapper", $"Terrain n°{lifeArea.areaId} vidé !", NotificationManager.Type.Success);
                    return await Task.FromResult(true);
                }
                else
                {
                    player.Notify("Mapper", $"Échec lors du nettoyage du terrain n°{lifeArea.areaId}", NotificationManager.Type.Error);
                    return await Task.FromResult(false);
                }
            });

            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        public void InitSaveAreaPanel(Player player)
        {
            MapConfig mapConfig = Utils.InitMapConfig(player);

            //Déclaration
            Panel panel = PanelHelper.Create($"Mapper - Sauvegarder le terrain n°{player.setup.areaId}", UIPanel.PanelType.Input, player, () => InitSaveAreaPanel(player));

            //Corps
            panel.TextLines.Add($"Veuillez renseigner l'auteur de cette décoration");
            panel.inputPlaceholder = "Ada Lovelace";
            
            panel.PreviousButton();
            panel.NextButton("Suivant", () =>
            {
                if(panel.inputText.Length >= 3)
                {
                    mapConfig.Author = panel.inputText;
                    ConfirmSaveAreaPanel(player, mapConfig);
                } else
                {
                    player.Notify("Mapper", "Veuillez renseigner l'auteur de cette décoration (3 caractères minimum)", NotificationManager.Type.Warning);
                    panel.Refresh();
                }
            });

            panel.CloseButton();

            //Affichage
            panel.Display();
        }
        public void ConfirmSaveAreaPanel(Player player, MapConfig mapConfig)
        {
            //Déclaration
            Panel panel = PanelHelper.Create($"Mapper - Confirmer la sauvegarde du terrain n°{player.setup.areaId}", UIPanel.PanelType.Input, player, () => ConfirmSaveAreaPanel(player, mapConfig));

            //Corps
            panel.TextLines.Add($"Veuillez donner un nom à cette décoration");
            panel.TextLines.Add($"3 caractères minimum");
            panel.TextLines.Add($"Les caractères spéciaux ne sont pas acceptés");
            panel.inputPlaceholder = "Circuit de course";


            panel.PreviousButton();
            panel.CloseButtonWithAction("Suivant", async () =>
            {
                if (panel.inputText.Length >= 3 && Utils.IsValidMapName(panel.inputText))
                {
                    mapConfig.Name = panel.inputText;
                    if(await mapConfig.Save())
                    {
                        player.Notify("Mapper", "Décoration sauvegardée !", NotificationManager.Type.Success);
                        return await Task.FromResult(true);
                    }
                    else
                    {
                        player.Notify("Mapper", "Échec lors de la sauvegarde", NotificationManager.Type.Error);
                        return await Task.FromResult(false);
                    }
                }
                else
                {
                    player.Notify("Mapper", "Veuillez donner un nom à cette décoration en respectant me format", NotificationManager.Type.Warning);
                    return await Task.FromResult(false);
                }
            });

            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        public async void ShowLoadableAreasPanel(Player player, uint? areaId = null)
        {
            List<MapConfig> mapConfigs = areaId != null ? await MapConfig.Query(m => m.AreaId == areaId) : await MapConfig.QueryAll();

            //Déclaration
            Panel panel = PanelHelper.Create($"Mapper - Liste des terrains enregistrés", UIPanel.PanelType.TabPrice, player, () => ShowLoadableAreasPanel(player, areaId));

            //Corps
            if(mapConfigs != null && mapConfigs.Count > 0)
            {
                foreach (MapConfig mapConfig in mapConfigs)
                {
                    panel.AddTabLine($"{(mapConfig.MapId != Nova.mapId ? $"{mk.Color($"{mk.Italic("[incompatible]")}", mk.Colors.Error)}" : $"")} {mapConfig.Name} - {mapConfig.Author}", _ => { });
                }

                panel.NextButton("Exporter", () => ExportAreaPanel(player, mapConfigs[panel.selectedTab]));
                panel.NextButton("Sélectionner", () => LoadAreaPanel(player, mapConfigs[panel.selectedTab]));
            } else panel.AddTabLine($"Aucun terrain n'est enregistré", _ => { });

            panel.PreviousButton();
            panel.CloseButton();

            //Affichage
            panel.Display();
        }
        public void LoadAreaPanel(Player player, MapConfig mapConfig)
        {
            LifeArea lifeArea = Nova.a.GetAreaById((uint)mapConfig.AreaId);

            //Déclaration
            Panel panel = PanelHelper.Create($"Mapper - Terrain \"{mapConfig.Name}\"", UIPanel.PanelType.Text, player, () => LoadAreaPanel(player, mapConfig));

            //Corps
            panel.TextLines.Add($"{mk.Size("Comment voulez-vous charger ce terrain ?", 18)}");
            panel.TextLines.Add($"{mk.Size($"{mk.Color("Nombre d'objets :", mk.Colors.Purple)} {mapConfig.ObjectCount}", 18)}");
            panel.TextLines.Add($"");
            panel.TextLines.Add($"{mk.Size($"{mk.Align("   • Ajouter la décoration au terrain sans affecter l'existant.", mk.Aligns.Left)}", 14)}");
            panel.TextLines.Add($"{mk.Size($"{mk.Align("   • Remplacer la décoration actuelle du terrain", mk.Aligns.Left)}", 14)}");

            panel.PreviousButtonWithAction("Ajouter", async () =>
            {
                float time = Utils.GetLoadTime(mapConfig);
                player.Notify("Mapper", $"La décoration \"{mapConfig.Name}\" est en cours de chargement (environ {time} secondes) !", NotificationManager.Type.Success, 5); 

                if (await Utils.LoadArea(mapConfig, this))
                {
                    player.Notify("Mapper", $"La décoration \"{mapConfig.Name}\" est chargée !", NotificationManager.Type.Success);
                    return await Task.FromResult(true);
                }
                else
                {
                    player.Notify("Mapper", $"Échec du chargement de la décoration {mapConfig.Name}", NotificationManager.Type.Error);
                    return await Task.FromResult(false);
                }
            });
            panel.PreviousButtonWithAction("Remplacer", async () =>
            {
                float time = Utils.GetLoadTime(mapConfig);

                player.Notify("Mapper", $"La décoration \"{mapConfig.Name}\" est en cours de chargement (environ {time} secondes) !", NotificationManager.Type.Success, 5);
                if (!Utils.ClearArea(lifeArea, this))
                {
                    player.Notify("Mapper", $"Erreur lors du nettoyage du terrain !", NotificationManager.Type.Error);
                    return await Task.FromResult(false);
                }

                if (await Utils.LoadArea(mapConfig, this))
                {
                    player.Notify("Mapper", $"La décoration \"{mapConfig.Name}\" est chargée !", NotificationManager.Type.Success);
                    return await Task.FromResult(true);
                }
                else
                {
                    player.Notify("Mapper", $"Échec du chargement de la décoration {mapConfig.Name}", NotificationManager.Type.Error);
                    return await Task.FromResult(false);
                }
            });

            panel.AddButton("Téléportation", _ =>
            {
                player.setup.TargetSetPosition(lifeArea.instance.spawn);
                panel.Refresh();
            });
            panel.AddButton("Supprimer", _ => DeleteAreaPanel(player, mapConfig));

            panel.PreviousButton();
            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        public void DeleteAreaPanel(Player player, MapConfig mapConfig)
        {
            //Déclaration
            Panel panel = PanelHelper.Create($"Mapper - Supprimer la sauvegarde \"{mapConfig.Name}\"", UIPanel.PanelType.Text, player, () => DeleteAreaPanel(player, mapConfig));

            //Corps
            panel.TextLines.Add($"Voulez-vous vraiment supprimer cette sauvegarde ?");
            panel.TextLines.Add("La suppression d'une sauvegarde n'affecte pas l'état actuel du terrain");


            panel.PreviousButtonWithAction("Confirmer la supression", async () =>
            {
                if (await mapConfig.Delete())
                {
                    player.Notify("Mapper", $"Sauvegarde supprimée", NotificationManager.Type.Success);
                    return await Task.FromResult(true);
                }
                else
                {
                    player.Notify("Mapper", $"Échec lors de la suppression de cette sauvegarde", NotificationManager.Type.Error);
                    return await Task.FromResult(false);
                }
            });

            panel.PreviousButton();
            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        public void ExportAreaPanel(Player player, MapConfig mapConfig)
        {
            string fileName = Utils.FormatMapName(mapConfig.Name) + $"-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.json";
            string NewMapPath = Path.Combine(JsonDirectoryPath, fileName);

            string json = JsonConvert.SerializeObject(mapConfig);
            
            //Déclaration
            Panel panel = PanelHelper.Create($"Mapper - Exporter le terrain n°{player.setup.areaId}", UIPanel.PanelType.Text, player, () => ExportAreaPanel(player, mapConfig));

            //Corps;
            panel.TextLines.Add($"Exporter la décoration {mk.Color($"\"{mapConfig.Name}\"", mk.Colors.Warning)} ?");
            panel.TextLines.Add($"Votre ficher sera disponible à cette emplacement");
            panel.TextLines.Add(mk.Color($"Plugins/ModKit/Mapper/{fileName}", mk.Colors.Info));

            panel.PreviousButton();
            panel.PreviousButtonWithAction("Confirmer", async () =>
            {
                if(json != null)
                {
                    File.WriteAllText(NewMapPath, json);

                }

                if (Utils.IsValidMapConfigJson(json))
                {
                    File.WriteAllText(NewMapPath, json);
                    player.Notify("Mapper", $"La décoration \"{mapConfig.Name}\" à bien été exportée !", NotificationManager.Type.Success);
                    return await Task.FromResult(true);
                }
                else
                {
                    player.Notify("Mapper", $"Échec lors de l'exportaton de la décoration \"{mapConfig.Name}\"", NotificationManager.Type.Error);
                    return await Task.FromResult(false);
                }

            });

            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        public async void ImportAreaPanel(Player player)
        {
            string[] jsonFiles = Directory.GetFiles(JsonDirectoryPath, "*.json").Select(Path.GetFileNameWithoutExtension).ToArray();
            List<MapConfig> mapConfigs = await MapConfig.QueryAll();
            List<string> filesToImport = jsonFiles.Where(fileName => !mapConfigs.Any(config => string.Equals(config.Source, fileName, StringComparison.OrdinalIgnoreCase))).ToList();

            //Déclaration
            Panel panel = PanelHelper.Create($"Mapper - Liste des terrains à importer", UIPanel.PanelType.TabPrice, player, () => ImportAreaPanel(player));

            //Corps
            if(filesToImport != null && filesToImport.Count > 0)
            {
                foreach (var fileName in filesToImport)
                {
                    panel.AddTabLine($"{fileName}", _ => { });
                }

                panel.AddButton("Import", async _ =>
                {
                    string path = JsonDirectoryPath + "/" + filesToImport[panel.selectedTab] + ".json";
                    if (File.Exists(path))
                    {
                        string jsonContent = File.ReadAllText(path);
                        MapConfig mapConfig = JsonConvert.DeserializeObject<MapConfig>(jsonContent);
                        mapConfig.DeserializeObjects();
                        mapConfig.Source = filesToImport[panel.selectedTab];

                        if (await mapConfig.Save()) player.Notify("Mapper", $"La décoration \"{mapConfig.Name}\" à bien été importée !", NotificationManager.Type.Success);
                        else player.Notify("Mapper", $"Échec de l'importation", NotificationManager.Type.Error);
                    }
                    else player.Notify("Mapper", $"Le fichier JSON est introuvable", NotificationManager.Type.Error);
                    panel.Refresh();
                });
            } else panel.AddTabLine($"Aucun fichier qui n'est pas déjà importé", _ => { });

            
            panel.PreviousButton();
            panel.CloseButton();

            //Affichage
            panel.Display();
        }
        #endregion
    }
}
