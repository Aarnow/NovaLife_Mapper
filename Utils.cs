﻿using Life.AreaSystem;
using Life;
using Mapper.Entities;
using ModKit.Utils;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Life.Network;
using Newtonsoft.Json;

namespace Mapper
{
    static class Utils
    {
        public static Quaternion EulerToQuaternion(float x, float y, float z)
        {
            // Convertir les angles de degrés en radians
            float radX = x * Mathf.Deg2Rad * 0.5f;
            float radY = y * Mathf.Deg2Rad * 0.5f;
            float radZ = z * Mathf.Deg2Rad * 0.5f;

            float cx = Mathf.Cos(radX);
            float cy = Mathf.Cos(radY);
            float cz = Mathf.Cos(radZ);
            float sx = Mathf.Sin(radX);
            float sy = Mathf.Sin(radY);
            float sz = Mathf.Sin(radZ);

            float w = cx * cy * cz + sx * sy * sz;
            float qx = sx * cy * cz + cx * sy * sz;
            float qy = cx * sy * cz - sx * cy * sz;
            float qz = cx * cy * sz - sx * sy * cz;

            return new Quaternion(qx, qy, qz, w);
        }

        public static MapConfig InitMapConfig(Player player)
        {
            LifeArea lifeArea = Nova.a.GetAreaById(player.setup.areaId);
            MapConfig mapConfig = new MapConfig();

            mapConfig.CreatedAt = DateUtils.GetNumericalDateOfTheDay();
            mapConfig.AreaId = (int)player.setup.areaId;
            mapConfig.ObjectCount = lifeArea.instance.objects.Count;
            mapConfig.Source = "local";

            foreach (LifeObject i in lifeArea.instance.objects.Values)
            {
                mapConfig.ListOfLifeObject.Add(i);
            }

            mapConfig.SerializeObjects();

            return mapConfig;
        }

        public static bool ClearArea(uint areaId, ModKit.ModKit context)
        {
            LifeArea lifeArea = Nova.a.GetAreaById(areaId);

            foreach (LifeObject i in lifeArea.instance.objects.Values.ToList())
            {
                context.NetworkAreaHelper.RemoveObject(i.areaId, i.id);
            }

            if (lifeArea.instance.objects.Count == 0) return true;
            else return false;
        }

        public static bool IsValidMapName(string mapName)
        {
            return Regex.IsMatch(mapName, @"^[a-zA-Z0-9\s]+$");
        }

        public static string FormatMapName(string mapName)
        {
            return mapName.ToLower().Replace(" ", "-");
        }

        public static bool IsValidMapConfigJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;

            try
            {
                JsonConvert.DeserializeObject<MapConfig>(json);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        public static int GetAreaObjectsCount(LifeArea lifeArea)
        {
            return lifeArea.instance.objects.Count + lifeArea.instance.spawnedObjects.Count;
        }
    }
}
