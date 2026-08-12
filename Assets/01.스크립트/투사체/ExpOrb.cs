using UnityEngine;

public class ExpOrb : MonoBehaviour
{
    private float attractSpeed = 80f;

    private PlayerStat playerStat;
    private PlayerLevel playerLevel;
    private Transform player;

    private Renderer[] renderers;
    private Vector3 defaultScale;
    private MaterialPropertyBlock propertyBlock;

    private int expAmount;
    private string poolKey;
    private bool isAttracting;
    private bool isInitialized;

    public int ExpAmount => expAmount;
    public bool IsAttracting => isAttracting;

    public bool CanMerge =>
        gameObject.activeInHierarchy &&
        isInitialized &&
        !isAttracting;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        defaultScale = transform.localScale;
        propertyBlock = new MaterialPropertyBlock();
    }

    private void OnDisable()
    {
        if (ExpOrbManager.Instance != null)
            ExpOrbManager.Instance.Unregister(this);

        isInitialized = false;
        isAttracting = false;

        SetVisible(true);
        transform.localScale = defaultScale;
    }

    public void Init(Transform player, int expAmount, string poolKey)
    {
        this.player = player;
        this.expAmount = Mathf.Max(1, expAmount);
        this.poolKey = poolKey;

        playerStat = null;
        playerLevel = null;

        if (player != null)
        {
            playerStat = player.GetComponent<PlayerStat>();
            playerLevel = player.GetComponent<PlayerLevel>();
        }

        isAttracting = false;
        isInitialized = true;

        SetVisible(true);
        UpdateVisual();

        if (ExpOrbManager.Instance != null)
            ExpOrbManager.Instance.Register(this);
    }

    private void Update()
    {
        if (!isInitialized)
            return;

        if (player == null)
            return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        float dist = toPlayer.magnitude;

        if (dist <= 0.6f)
        {
            if (playerLevel != null)
                playerLevel.AddExp(expAmount);

            ReturnToPool();
            return;
        }

        if (!isAttracting &&
            playerStat != null &&
            dist <= playerStat.ExpAttractRange)
        {
            isAttracting = true;
            SetVisible(true);
        }

        if (isAttracting)
        {
            Vector3 move = toPlayer.normalized * attractSpeed * Time.deltaTime;
            transform.position += move;
        }
    }

    public void AddExp(int amount)
    {
        if (amount <= 0)
            return;

        expAmount += amount;
        UpdateVisual();
    }

    public float GetDistanceToPlayerSqr()
    {
        if (player == null)
            return 0f;

        Vector3 diff = player.position - transform.position;
        diff.y = 0f;

        return diff.sqrMagnitude;
    }

    public float GetDistanceToOrbSqr(ExpOrb other)
    {
        if (other == null)
            return float.MaxValue;

        Vector3 diff = other.transform.position - transform.position;
        diff.y = 0f;

        return diff.sqrMagnitude;
    }

    public void SetVisible(bool visible)
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = visible;
        }
    }

    public void ReturnToPool()
    {
        if (ExpOrbManager.Instance != null)
            ExpOrbManager.Instance.Unregister(this);

        if (!string.IsNullOrEmpty(poolKey))
            ObjectPool.Return(poolKey, gameObject);
        else
            gameObject.SetActive(false);
    }

    private void UpdateVisual()
    {
        Color color;
        float scale;

        if (expAmount >= 500)
        {
            color = Color.red;
            scale = 2.2f;
        }
        else if (expAmount >= 100)
        {
            color = new Color(1f, 0.45f, 0f);
            scale = 1.8f;
        }
        else if (expAmount >= 50)
        {
            color = Color.magenta;
            scale = 1.5f;
        }
        else if (expAmount >= 10)
        {
            color = Color.cyan;
            scale = 1.25f;
        }
        else
        {
            color = Color.green;
            scale = 1f;
        }

        transform.localScale = defaultScale * scale;

        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;
            renderers[i].GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", color);
            propertyBlock.SetColor("_BaseColor", color);
            renderers[i].SetPropertyBlock(propertyBlock);
        }
    }
}