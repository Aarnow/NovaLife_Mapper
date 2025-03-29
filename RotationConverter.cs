using Newtonsoft.Json;
using UnityEngine;

// Classe à rajouter dans ModKit
public class RotationConverter
{
    // Convertit un quaternion en une chaîne JSON
    public static string WriteJson(Quaternion quaternion)
    {
        Vector3 eulerAngles = quaternion.eulerAngles;
        return JsonConvert.SerializeObject(eulerAngles);
    }

    // Convertit une chaîne JSON contenant les angles d'Euler en quaternion
    public static Quaternion ReadJson(string json)
    {
        Vector3 eulerAngles = JsonConvert.DeserializeObject<Vector3>(json);
        return EulerToQuaternion(eulerAngles.x, eulerAngles.y, eulerAngles.z);
    }

    // Convertit les angles d'Euler en quaternion
    public static Quaternion EulerToQuaternion(float x, float y, float z)
    {
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
}