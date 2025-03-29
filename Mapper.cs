using Life;
using Life.AreaSystem;
using Life.DB;
using Life.Network;
using Life.UI;
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

        public Mapper(IGameAPI api) : base(api)
        {
            PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "1.0.0", "Aarnow");
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();

            GenerateDirectory();
            GenerateCommands();
            InsertMenu();

            Orm.RegisterTable<MapConfig>();

            ModKit.Internal.Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "initialisé");
        }

        public void InsertMenu()
        {
            _menu.AddAdminPluginTabLine(PluginInformations, 1, "Mapper", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                MapperPanel(player, false);
            }, 0);
        }
        private void GenerateDirectory()
        {
            try
            {
                ConfigDirectoryPath = DirectoryPath + "/Mapper";

                if (!Directory.Exists(ConfigDirectoryPath)) Directory.CreateDirectory(ConfigDirectoryPath);
            }
            catch (IOException ex)
            {
                ModKit.Internal.Logger.LogError("InitDirectory", ex.Message);
            }
        }
        public void GenerateCommands()
        {
            new SChatCommand("/mapper", new string[] { "/map" }, "Permet d'ouvrir le panel du plugin \"Mapper\"", "/mapper", (player, arg) =>
            {
                if(player.IsAdmin) MapperPanel(player, true);
                else player.Notify("Mapper", "Vous n'avez pas la permission requise.", NotificationManager.Type.Warning);
            }).Register();      
        }

        public void MapperPanel(Player player, bool isCmd)
        {
            //Déclaration
            Panel panel = PanelHelper.Create("Mapper", UIPanel.PanelType.TabPrice, player, () => MapperPanel(player, isCmd));

            //Corps
            panel.AddTabLine($"{mk.Color("Terrain actuel", mk.Colors.Info)}", _ => CheckAreaPanel(player));
            panel.AddTabLine($"{mk.Color("Terrain enregistrés", mk.Colors.Info)}", _ => ShowLoadableAreasPanel(player));
            panel.AddTabLine($"{mk.Color("Importer", mk.Colors.Info)}", _ => ImportAreaPanel(player));
            panel.AddTabLine($"{mk.Color("Sauvegarder", mk.Colors.Info)}", _ => InitSaveAreaPanel(player));

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

            panel.NextButton("Vider", () => ClearAreaPanel(player, lifeArea.areaId));
            panel.NextButton("Voir les sauvegardes", () => ShowLoadableAreasPanel(player, lifeArea.areaId));
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
            panel.TextLines.Add("Êtes-vous sûr de vouloir vider ce terrain ?");
            panel.TextLines.Add("L'ensemble des objets seront supprimés.");

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
                    panel.AddTabLine($"{mapConfig.Name} - {mapConfig.Author}", _ => { });
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
            panel.TextLines.Add($"Voulez-vous charger ce terrain ?");
            panel.TextLines.Add($"Attention, la décoration actuelle du terrain n°{mapConfig.AreaId} sera remplacée.");
            panel.TextLines.Add($"Nombre d'objets: {mapConfig.ObjectCount}");

            panel.AddButton("Téléportation", _ =>
            {
                player.setup.TargetSetPosition(lifeArea.instance.spawn);
                panel.Refresh();
            });
            panel.AddButton("Supprimer", _ => DeleteAreaPanel(player, mapConfig));
            panel.PreviousButtonWithAction("Charger", async () =>
            {
                if(!Utils.ClearArea(lifeArea, this))
                {
                    player.Notify("Mapper", $"Erreur lors du nettoyage du terrain !", NotificationManager.Type.Error);
                    return await Task.FromResult(false);
                }

                mapConfig.DeserializeObjects();

                if (mapConfig.ListOfLifeObject != null && mapConfig.ListOfLifeObject.Count > 0)
                {
                    foreach (LifeObject i in mapConfig.ListOfLifeObject)
                    {
                        var position = new Vector3(i.x, i.y, i.z);
                        Quaternion rotation = Utils.EulerToQuaternion(i.rotX, i.rotY, i.rotZ);
                        NetworkAreaHelper.PlaceObject(i.areaId, i.objectId, i.objectId, position, rotation, i.isInterior, i.steamId, i.data);
                    }
                }

                player.Notify("Mapper", $"La décoration \"{mapConfig.Name}\" est chargée !", NotificationManager.Type.Success);
                return await Task.FromResult(true);
            });

            panel.PreviousButton();
            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        public void DeleteAreaPanel(Player player, MapConfig mapConfig)
        {
            //Déclaration
            Panel panel = PanelHelper.Create($"Mapper - Supprimer le terrain \"{mapConfig.Name}\"", UIPanel.PanelType.Text, player, () => DeleteAreaPanel(player, mapConfig));

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
            string NewMapPath = Path.Combine(ConfigDirectoryPath, fileName);

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
            string[] jsonFiles = Directory.GetFiles(ConfigDirectoryPath, "*.json").Select(Path.GetFileNameWithoutExtension).ToArray();
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
                    string path = ConfigDirectoryPath + "/" + filesToImport[panel.selectedTab] + ".json";
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
