using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class PlayerSaveData
{
    public List<WeaponType> unlockedWeapons = new();

    public float bestDistance;
    public int totalPlayCount;
}

public static class SaveManager
{
    private const string SaveFileName = "PlayerSaveData.json";

    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static PlayerSaveData Load()
    {
        try
        {
            if (!File.Exists(SavePath))
                return new PlayerSaveData();

            string json = File.ReadAllText(SavePath);

            if (string.IsNullOrEmpty(json))
                return new PlayerSaveData();

            return JsonUtility.FromJson<PlayerSaveData>(json) ?? new PlayerSaveData();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"세이브 파일을 불러오지 못해 기본값으로 시작합니다: {e.Message}");
            return new PlayerSaveData();
        }
    }

    public static void Save(PlayerSaveData data)
    {
        if (data == null)
            return;

        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"세이브 파일 저장에 실패했습니다: {e.Message}");
        }
    }

    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }

    public static void SaveBestDistance(float distance)
    {
        PlayerSaveData data = Load();

        if (distance > data.bestDistance)
        {
            data.bestDistance = distance;
            Save(data);
        }
    }

    public static void AddPlayCount()
    {
        PlayerSaveData data = Load();
        data.totalPlayCount++;
        Save(data);
    }

    public static bool IsWeaponUnlocked(WeaponType weaponType)
    {
        PlayerSaveData data = Load();
        return data.unlockedWeapons.Contains(weaponType);
    }

    public static void UnlockWeapon(WeaponType weaponType)
    {
        PlayerSaveData data = Load();

        if (!data.unlockedWeapons.Contains(weaponType))
            data.unlockedWeapons.Add(weaponType);

        Save(data);
    }
}