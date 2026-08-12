using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// CSV 한 줄 = 장비 하나. 아래 컬럼 순서를 그대로 지켜서 작성한다(헤더 줄은 그대로 두거나 지워도 무방, 첫 줄이 "이름"으로 시작하면 헤더로 보고 건너뜀).
//
// 컬럼: 이름, 가로, 세로, 설명, 아이콘이름(선택), 위치조건, 탐색조건, 효과
//
// - 위치조건: TopRow / BottomRow / LeftColumn / RightColumn 중 여러 개를 ";"로 구분.
//     예) "TopRow;LeftColumn"  (없으면 빈칸)
//
// - 탐색조건: "조건타입:대상:범위:필요개수" 형식을 ";"로 여러 개 구분.
//     조건타입 = Around / SameRow / SameColumn
//     대상     = Weapon / Equipment / AnyItem
//     예) "Around:Weapon:2:1;SameRow:AnyItem:1:2"  (없으면 빈칸)
//
// - 효과: "효과타입:수치"를 ";"로 여러 개 구분.
//     효과타입 = Damage / AttackSpeed / Range / ProjectileSpeed / ProjectileCount / WeaponScale
//                / MaxHp / HpRegen / MoveSpeed / ExpAttractRange / CameraSize
//     예) "Damage:5;MoveSpeed:0.5"
//
// - 필드 안에 쉼표가 들어가야 하면(설명 등) 큰따옴표로 감싸면 된다: "설명, 쉼표 포함"
//
// 아이콘이름은 IconSearchFolders에 지정된 폴더들에서 같은 이름의 Sprite를 찾아 자동으로 연결한다.
// 못 찾으면 비워두고 넘어가며(수동 연결 필요), 에러는 아니다.
public static class EquipmentItemDataGenerator
{
    private const string OutputFolder = "Assets/Resources/ItemData/장비";

    // 아이콘 이름으로 스프라이트를 찾을 때 검색할 폴더들.
    private static readonly string[] IconSearchFolders =
    {
        "Assets/Sprites/Icons",
        "Assets/Resources/Icons"
    };

    [MenuItem("툴/인벤토리/장비 데이터 CSV로 생성")]
    public static void GenerateFromCsv()
    {
        string csvPath = EditorUtility.OpenFilePanel("장비 목록 CSV 선택", "Assets", "csv");

        if (string.IsNullOrEmpty(csvPath))
            return;

        if (!Directory.Exists(OutputFolder))
            Directory.CreateDirectory(OutputFolder);

        string[] lines = File.ReadAllLines(csvPath);

        int createdCount = 0;
        int skippedCount = 0;
        int errorCount = 0;

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex];

            if (string.IsNullOrWhiteSpace(line))
                continue;

            List<string> columns = ParseCsvLine(line);

            if (columns.Count == 0)
                continue;

            // 첫 줄이 헤더인 경우("이름"으로 시작) 건너뜀
            if (lineIndex == 0 && columns[0].Trim() == "이름")
                continue;

            if (columns.Count < 4)
            {
                Debug.LogError($"[{lineIndex + 1}번째 줄] 컬럼이 부족합니다(최소 4개: 이름/가로/세로/설명 필요): {line}");
                errorCount++;
                continue;
            }

            try
            {
                if (CreateEquipmentAsset(columns))
                    createdCount++;
                else
                    skippedCount++;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{lineIndex + 1}번째 줄] 생성 중 오류: {e.Message}\n{line}");
                errorCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"장비 데이터 생성 완료 - 생성 {createdCount} / 스킵(이미 존재) {skippedCount} / 오류 {errorCount}");
    }

    private static bool CreateEquipmentAsset(List<string> columns)
    {
        string itemName = columns[0].Trim();

        if (string.IsNullOrEmpty(itemName))
            return false;

        int width = ParseIntOrDefault(columns, 1, 1);
        int height = ParseIntOrDefault(columns, 2, 1);
        string description = columns.Count > 3 ? columns[3] : string.Empty;
        string iconName = columns.Count > 4 ? columns[4].Trim() : string.Empty;
        string positionConditionsRaw = columns.Count > 5 ? columns[5].Trim() : string.Empty;
        string searchConditionsRaw = columns.Count > 6 ? columns[6].Trim() : string.Empty;
        string effectsRaw = columns.Count > 7 ? columns[7].Trim() : string.Empty;

        string dataPath = $"{OutputFolder}/{itemName}.asset";

        if (AssetDatabase.LoadAssetAtPath<InventoryItemData>(dataPath) != null)
        {
            Debug.Log($"이미 존재해서 건너뜀 : {dataPath}");
            return false;
        }

        InventoryItemData data = ScriptableObject.CreateInstance<InventoryItemData>();
        AssetDatabase.CreateAsset(data, dataPath);

        SerializedObject so = new SerializedObject(data);

        so.FindProperty("itemName").stringValue = itemName;
        so.FindProperty("width").intValue = Mathf.Max(1, width);
        so.FindProperty("height").intValue = Mathf.Max(1, height);
        so.FindProperty("description").stringValue = description;
        so.FindProperty("itemType").enumValueIndex = (int)InventoryItemType.Equipment;

        Sprite icon = FindIconByName(iconName);
        if (icon != null)
            so.FindProperty("icon").objectReferenceValue = icon;

        SerializedProperty equipmentDataProp = so.FindProperty("equipmentData");

        ApplyPositionConditions(equipmentDataProp.FindPropertyRelative("positionConditions"), positionConditionsRaw);
        ApplySearchConditions(equipmentDataProp.FindPropertyRelative("searchConditions"), searchConditionsRaw);
        ApplyEffects(equipmentDataProp.FindPropertyRelative("effects"), effectsRaw);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(data);

        return true;
    }

    private static void ApplyPositionConditions(SerializedProperty arrayProp, string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            arrayProp.arraySize = 0;
            return;
        }

        string[] entries = raw.Split(';', StringSplitOptions.RemoveEmptyEntries);
        arrayProp.arraySize = entries.Length;

        for (int i = 0; i < entries.Length; i++)
        {
            EquipmentPositionConditionType conditionType =
                (EquipmentPositionConditionType)Enum.Parse(typeof(EquipmentPositionConditionType), entries[i].Trim());

            SerializedProperty element = arrayProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("conditionType").enumValueIndex = (int)conditionType;
        }
    }

    private static void ApplySearchConditions(SerializedProperty arrayProp, string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            arrayProp.arraySize = 0;
            return;
        }

        string[] entries = raw.Split(';', StringSplitOptions.RemoveEmptyEntries);
        arrayProp.arraySize = entries.Length;

        for (int i = 0; i < entries.Length; i++)
        {
            // 형식: 조건타입:대상:범위:필요개수
            string[] parts = entries[i].Split(':');

            if (parts.Length != 4)
                throw new FormatException($"탐색조건 형식이 잘못됨(조건타입:대상:범위:필요개수) : {entries[i]}");

            EquipmentSearchConditionType conditionType =
                (EquipmentSearchConditionType)Enum.Parse(typeof(EquipmentSearchConditionType), parts[0].Trim());

            EquipmentConditionTarget target =
                (EquipmentConditionTarget)Enum.Parse(typeof(EquipmentConditionTarget), parts[1].Trim());

            int range = int.Parse(parts[2].Trim());
            int requiredCount = int.Parse(parts[3].Trim());

            SerializedProperty element = arrayProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("conditionType").enumValueIndex = (int)conditionType;
            element.FindPropertyRelative("target").enumValueIndex = (int)target;
            element.FindPropertyRelative("range").intValue = range;
            element.FindPropertyRelative("requiredCount").intValue = requiredCount;
        }
    }

    private static void ApplyEffects(SerializedProperty arrayProp, string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            arrayProp.arraySize = 0;
            return;
        }

        string[] entries = raw.Split(';', StringSplitOptions.RemoveEmptyEntries);
        arrayProp.arraySize = entries.Length;

        for (int i = 0; i < entries.Length; i++)
        {
            // 형식: 효과타입:수치
            string[] parts = entries[i].Split(':');

            if (parts.Length != 2)
                throw new FormatException($"효과 형식이 잘못됨(효과타입:수치) : {entries[i]}");

            EquipmentEffectType effectType =
                (EquipmentEffectType)Enum.Parse(typeof(EquipmentEffectType), parts[0].Trim());

            float value = float.Parse(parts[1].Trim());

            SerializedProperty element = arrayProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("effectType").enumValueIndex = (int)effectType;
            element.FindPropertyRelative("effectValue").floatValue = value;
        }
    }

    private static Sprite FindIconByName(string iconName)
    {
        if (string.IsNullOrEmpty(iconName))
            return null;

        string[] guids = AssetDatabase.FindAssets($"t:Sprite {iconName}", IconSearchFolders);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (Path.GetFileNameWithoutExtension(path) == iconName)
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        return null;
    }

    private static int ParseIntOrDefault(List<string> columns, int index, int defaultValue)
    {
        if (index >= columns.Count)
            return defaultValue;

        if (int.TryParse(columns[index].Trim(), out int result))
            return result;

        return defaultValue;
    }

    // 큰따옴표로 감싼 필드 안의 쉼표를 지원하는 간단한 CSV 파서
    private static List<string> ParseCsvLine(string line)
    {
        List<string> result = new List<string>();
        System.Text.StringBuilder current = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
        }

        result.Add(current.ToString());
        return result;
    }
}