using System.Collections.Generic;
using UnityEngine;

public static class ObjectPool
{
    // 각 오브젝트 종류에 대한 풀. key로 식별하고 큐에 비활성화된 오브젝트를 보관함.
    private static Dictionary<string, Queue<GameObject>> pool = new();

    // key에 대응하는 프리팹 정보. Resources.Load()로 불러온 프리팹을 저장함.
    private static Dictionary<string, GameObject> prefabMap = new();

    // key별 부모 Transform. Hierarchy 창에서 풀링 오브젝트를 보기 좋게 정리하기 위해 사용함.
    private static Dictionary<string, Transform> parentMap = new();

    // Initialize()가 한 번만 실행되도록 막기 위한 플래그
    private static bool initialized = false;

    // 전체 풀링 오브젝트를 묶어둘 최상위 부모 오브젝트
    private static Transform rootParent;

    // 오브젝트 풀에 등록할 항목들을 정의하는 내부 클래스
    private class PoolSetting
    {
        public string key;       // 키 이름
        public string path;      // Resources 내 프리팹 경로
        public int count;        // 초기 생성 수량

        public PoolSetting(string key, string path, int count)
        {
            this.key = key;
            this.path = path;
            this.count = count;
        }
    }

    //=================================================================================
    // 등록할 오브젝트 풀 리스트.
    // 키 이름, Resources 폴더 내 프리팹 경로, 초기 생성 수량을 설정함.
    private static List<PoolSetting> settings = new()
    {
        new PoolSetting("LineWarning", "Warnings/LineWarning", 50),
        new PoolSetting("CircleWarning", "Warnings/CircleWarning", 50),
        new PoolSetting("BoxWarning", "Warnings/BoxWarning", 50),
        new PoolSetting("DamageText", "DamageText/DamageText", 50),
        
        new PoolSetting("Shop", "Shop/Shop", 10),
        new PoolSetting("SupplyCrate", "Shop/SupplyCrate", 20),
        
        new PoolSetting("Bullet", "Projectiles/Bullet", 300),
        new PoolSetting("Arrow", "Projectiles/Arrow", 200),
        new PoolSetting("FlametHrower", "Projectiles/FlametHrower", 200),
        new PoolSetting("ElectricBall", "Projectiles/ElectricBall", 150),
        new PoolSetting("Esper", "Projectiles/Esper", 150),
        new PoolSetting("Bomb", "Projectiles/Bomb", 150),
        new PoolSetting("Rocket", "Projectiles/Rocket", 150),
        new PoolSetting("Dagger", "Projectiles/Dagger", 150),
        
        new PoolSetting("Enemy_Stone", "Projectiles/Enemy_Stone", 150),
        
        new PoolSetting("Enemy", "Enemys/Enemy", 100),
        new PoolSetting("Enemy1", "Enemys/Enemy1", 100),
        new PoolSetting("Enemy2", "Enemys/Enemy2", 100),
        new PoolSetting("ExpOrb", "Exp/ExpOrb", 300),
        
        new PoolSetting("Hit", "Effect/Hit/Hit", 300),
        new PoolSetting("Explosion1", "Effect/Explosion/CFXR3 Fire Explosion B", 150),
        new PoolSetting("slash", "Effect/Weapon/slash", 50),
        new PoolSetting("slash2", "Effect/Weapon/slash2", 50),
        new PoolSetting("FuryCutter", "Effect/Weapon/FuryCutter", 100),
        new PoolSetting("ShinySlash", "Effect/Weapon/ShinySlash", 50),
        
        new PoolSetting("StatLine", "Prefab/인벤토리/StatLine", 10)
    };
    
    //=================================================================================

    // 실제 오브젝트 풀을 초기화
    private static void Initialize()
    {
        // 이미 초기화된 경우 중복 실행 방지
        if (initialized) return;
        initialized = true;

        // 풀링 오브젝트들을 Hierarchy에서 정리하기 위한 부모 오브젝트 생성
        GameObject rootObj = new GameObject("[ObjectPool]");
        rootParent = rootObj.transform;

        // 씬이 바뀌어도 풀을 유지하고 싶으면 사용
        // 씬마다 풀을 새로 만들고 싶으면 이 줄을 지워도 됨
        Object.DontDestroyOnLoad(rootObj);

        // 사전 정의된 오브젝트 풀 리스트(settings)에 따라 각 오브젝트 풀 생성
        foreach (var setting in settings)
        {
            // key가 비어 있으면 등록할 수 없으므로 건너뜀
            if (string.IsNullOrEmpty(setting.key))
            {
                continue;
            }

            // 같은 key가 중복 등록되면 건너뜀
            if (pool.ContainsKey(setting.key))
            {
                continue;
            }

            // Resources 폴더에서 프리팹 로드 (경로 예: "Zombie/Zombie")
            GameObject prefab = Resources.Load<GameObject>(setting.path);

            // 프리팹이 없으면 해당 항목 건너뜀
            if (prefab == null)
            {
                continue;
            }

            // key에 해당하는 프리팹과 큐를 딕셔너리에 저장
            prefabMap[setting.key] = prefab;
            pool[setting.key] = new Queue<GameObject>();

            // key별 부모 오브젝트 생성
            Transform parent = CreateParent(setting.key);
            parentMap[setting.key] = parent;

            // 초기 생성 수량이 음수가 되지 않도록 보정
            int createCount = Mathf.Max(0, setting.count);

            // 설정된 수량만큼 오브젝트를 미리 생성하여 비활성화 상태로 큐에 삽입
            for (int i = 0; i < createCount; i++)
            {
                GameObject obj = CreateNewObject(setting.key);

                // 생성에 실패했으면 건너뜀
                if (obj == null)
                {
                    continue;
                }

                // 풀에 들어가 있는 상태로 표시
                PoolObject poolObject = obj.GetComponent<PoolObject>();
                if (poolObject != null)
                {
                    poolObject.SetPooled(true);
                }

                obj.SetActive(false);
                pool[setting.key].Enqueue(obj);
            }
        }
    }

    //=================================================================================

    // key에 해당하는 부모 오브젝트 생성
    private static Transform CreateParent(string key)
    {
        // Hierarchy에서 보기 쉽게 key 이름으로 부모 오브젝트 생성
        GameObject parentObj = new GameObject($"{key} Pool");

        // 전체 풀 부모 아래에 배치
        parentObj.transform.SetParent(rootParent);

        return parentObj.transform;
    }

    // key에 해당하는 새 오브젝트 생성
    private static GameObject CreateNewObject(string key)
    {
        // key에 해당하는 프리팹이 없으면 생성 불가
        if (!prefabMap.ContainsKey(key))
        {
            return null;
        }

        // 프리팹 복제 생성
        GameObject obj = Object.Instantiate(prefabMap[key]);

        // Hierarchy 정리를 위해 key별 부모 아래로 이동
        if (parentMap.ContainsKey(key))
        {
            obj.transform.SetParent(parentMap[key]);
        }

        // 어떤 풀에서 생성된 오브젝트인지 기록하는 컴포넌트 추가
        PoolObject poolObject = obj.GetComponent<PoolObject>();
        if (poolObject == null)
        {
            poolObject = obj.AddComponent<PoolObject>();
        }

        // Return(GameObject obj)로 반환할 때 key를 알 수 있도록 저장
        poolObject.SetKey(key);

        // 새로 생성된 직후에는 아직 풀에 들어간 상태가 아님
        poolObject.SetPooled(false);

        return obj;
    }

    //=================================================================================

    // key에 해당하는 오브젝트를 풀에서 꺼냄
    public static GameObject Get(string key)
    {
        // 초기화가 안 되어 있으면 자동으로 Initialize 실행 (최초 1회)
        if (!initialized) Initialize();

        // key가 비어 있으면 사용할 수 없으므로 null 반환
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        // 등록되지 않은 key면 null 반환
        if (!pool.ContainsKey(key))
        {
            return null;
        }

        GameObject obj = null;

        // 큐에 남아 있는 오브젝트를 하나씩 꺼내서 유효한지 확인
        while (pool[key].Count > 0)
        {
            obj = pool[key].Dequeue();

            // Destroy된 오브젝트가 아니면 사용
            if (obj != null)
            {
                break;
            }
        }

        // 사용할 수 있는 오브젝트가 없으면 새로 생성
        if (obj == null)
        {
            obj = CreateNewObject(key);
        }

        // 생성에 실패했으면 null 반환
        if (obj == null)
        {
            return null;
        }

        // 풀에서 꺼냈으므로 현재는 사용 중인 상태
        PoolObject poolObject = obj.GetComponent<PoolObject>();
        if (poolObject != null)
        {
            poolObject.SetPooled(false);
        }

        // 꺼낸 오브젝트를 활성화해서 씬에 등장시킴
        obj.SetActive(true);

        // 호출한 쪽으로 반환
        return obj;
    }

    // 위치와 회전을 지정해서 오브젝트를 풀에서 꺼냄
    public static GameObject Get(string key, Vector3 position, Quaternion rotation)
    {
        // 기본 Get으로 오브젝트 가져오기
        GameObject obj = Get(key);

        // 가져오지 못했으면 null 반환
        if (obj == null)
        {
            return null;
        }

        // 위치와 회전값 적용
        obj.transform.SetPositionAndRotation(position, rotation);

        return obj;
    }

    //=================================================================================

    // key에 해당하는 오브젝트를 다시 풀에 반환
    public static void Return(string key, GameObject obj)
    {
        // 초기화가 안 되어 있으면 자동으로 Initialize 실행
        if (!initialized) Initialize();

        // 반환할 오브젝트가 없으면 실행 중단
        if (obj == null)
        {
            return;
        }

        // key가 비어 있으면 반환할 수 없으므로 실행 중단
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        // 등록되지 않은 key면 잘못 섞이지 않도록 반환하지 않음
        if (!pool.ContainsKey(key))
        {
            return;
        }

        // PoolObject가 없으면 중복 반환 상태를 확인할 수 없으므로 반환하지 않음
        PoolObject poolObject = obj.GetComponent<PoolObject>();
        if (poolObject == null)
        {
            return;
        }

        // 이미 풀에 들어가 있는 오브젝트면 중복 반환 방지를 위해 실행 중단
        if (poolObject.IsPooled)
        {
            return;
        }

        // 풀에 들어간 상태로 표시
        poolObject.SetPooled(true);

        // 오브젝트를 비활성화
        obj.SetActive(false);

        // Hierarchy 정리를 위해 다시 부모 아래로 이동
        if (parentMap.ContainsKey(key))
        {
            obj.transform.SetParent(parentMap[key]);
        }

        // 비활성화된 오브젝트를 다시 큐에 넣음
        pool[key].Enqueue(obj);
    }

    // 오브젝트가 가진 PoolObject 컴포넌트의 key를 이용해서 반환
    public static void Return(GameObject obj)
    {
        // 반환할 오브젝트가 없으면 실행 중단
        if (obj == null)
        {
            return;
        }

        // PoolObject 컴포넌트가 없으면 어떤 풀로 돌려보내야 할지 알 수 없음
        PoolObject poolObject = obj.GetComponent<PoolObject>();
        if (poolObject == null)
        {
            return;
        }

        // 저장된 key가 없으면 반환하지 않음
        if (string.IsNullOrEmpty(poolObject.Key))
        {
            return;
        }

        // 저장된 key를 사용해서 반환
        Return(poolObject.Key, obj);
    }

    //=================================================================================

    // 특정 key의 풀에 남아 있는 오브젝트 개수 확인
    public static int GetRemainCount(string key)
    {
        // 초기화가 안 되어 있으면 자동으로 Initialize 실행
        if (!initialized) Initialize();

        // 등록되지 않은 key면 0 반환
        if (!pool.ContainsKey(key))
        {
            return 0;
        }

        return pool[key].Count;
    }

    // 특정 key가 풀에 등록되어 있는지 확인
    public static bool ContainsKey(string key)
    {
        // 초기화가 안 되어 있으면 자동으로 Initialize 실행
        if (!initialized) Initialize();

        return pool.ContainsKey(key);
    }

    // 모든 풀을 비우고 다시 초기화할 수 있도록 상태 초기화
    // 씬 전환 후 풀을 새로 구성하고 싶을 때 사용할 수 있음
    public static void Clear()
    {
        // 생성된 풀 부모 오브젝트 제거
        if (rootParent != null)
        {
            Object.Destroy(rootParent.gameObject);
        }

        // 딕셔너리 초기화
        pool.Clear();
        prefabMap.Clear();
        parentMap.Clear();

        // 초기화 상태 되돌리기
        initialized = false;
        rootParent = null;
    }
}

//=================================================================================

// 풀에서 생성된 오브젝트가 자기 key와 풀 상태를 기억하기 위한 컴포넌트
public class PoolObject : MonoBehaviour
{
    // 이 오브젝트가 어느 풀에서 나왔는지 저장
    public string Key { get; private set; }

    // 현재 이 오브젝트가 풀 안에 들어가 있는 상태인지 확인
    public bool IsPooled { get; private set; }

    // ObjectPool에서 생성할 때 key를 저장
    public void SetKey(string key)
    {
        Key = key;
    }

    // ObjectPool에서 Get / Return 할 때 풀 상태를 저장
    public void SetPooled(bool isPooled)
    {
        IsPooled = isPooled;
    }

    // 자기 자신을 풀에 반환
    public void ReturnToPool()
    {
        ObjectPool.Return(gameObject);
    }
}
