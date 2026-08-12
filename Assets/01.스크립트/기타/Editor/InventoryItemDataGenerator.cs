using System.IO;
using UnityEditor;
using UnityEngine;

public static class InventoryItemDataGenerator
{
    private const string WeaponPrefabFolder = "Assets/Resources/Weapons";
    private const string OutputFolder = "Assets/Resources/ItemData/임시";

    [MenuItem("툴/인벤토리/무기 데이터 자동 생성")]
    public static void GenerateWeaponItemData()
    {
        if (!Directory.Exists(WeaponPrefabFolder))
        {
            Debug.LogError($"무기 프리팹 폴더가 없습니다 : {WeaponPrefabFolder}");
            return;
        }

        if (!Directory.Exists(OutputFolder))
            Directory.CreateDirectory(OutputFolder);

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { WeaponPrefabFolder });

        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
                continue;

            WeaponType weaponType = GetWeaponType(prefab);
            string prefabName = prefab.name;
            string dataPath = $"{OutputFolder}/{prefabName}.asset";

            InventoryItemData data = AssetDatabase.LoadAssetAtPath<InventoryItemData>(dataPath);

            if (data != null)
            {
                Debug.Log($"이미 존재해서 건너뜀 : {dataPath}");
                continue;
            }

            data = ScriptableObject.CreateInstance<InventoryItemData>();
            AssetDatabase.CreateAsset(data, dataPath);

            SerializedObject serializedObject = new SerializedObject(data);

            SetString(serializedObject, "itemName", prefabName);
            SetEnum(serializedObject, "itemType", InventoryItemType.Weapon);
            SetEnum(serializedObject, "weaponType", weaponType);
            SetString(serializedObject, "weaponPrefabPath", GetResourcesPath(prefabPath));

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(data);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("무기 데이터 자동 생성 완료");
    }

    private static WeaponType GetWeaponType(GameObject prefab)
    {
        if (prefab.GetComponentInChildren<ProjectileWeapon>(true) != null)
            return WeaponType.ProjectileWeapon;

        if (prefab.GetComponentInChildren<OrbitWeapon>(true) != null)
            return WeaponType.OrbitWeapon;

        if (prefab.GetComponentInChildren<AuraWeapon>(true) != null)
            return WeaponType.AuraWeapon;

        if (prefab.GetComponentInChildren<PulseWeapon>(true) != null)
            return WeaponType.PulseWeapon;

        return WeaponType.ProjectileWeapon;
    }

    private static string GetResourcesPath(string assetPath)
    {
        const string resources = "Resources/";
        int index = assetPath.IndexOf(resources);

        if (index < 0)
            return string.Empty;

        string path = assetPath.Substring(index + resources.Length);
        return Path.ChangeExtension(path, null).Replace("\\", "/");
    }

    private static void SetString(SerializedObject serializedObject, string propertyName, string value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
            property.stringValue = value;
    }

    private static void SetEnum<T>(SerializedObject serializedObject, string propertyName, T value) where T : System.Enum
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
            property.enumValueIndex = System.Convert.ToInt32(value);
    }
}