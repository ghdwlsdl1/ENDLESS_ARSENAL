using UnityEngine;

public abstract class EnemyController : MonoBehaviour
{
    [InspectorLabel("애니메이션 핸들러")]
    [SerializeField] protected AnimationHandler animationHandler;

    [InspectorLabel("적 스탯")]
    [SerializeField] protected EnemyStat enemyStat;

    [InspectorLabel("적 체력")]
    [SerializeField] protected EnemyHealth enemyHealth;

    protected Transform target;
    protected float currentMoveSpeed;

    public Transform Target => target;
    
    public bool IsDead => enemyHealth != null && enemyHealth.IsDead;

    private const float FallbackMoveSpeed = 3.5f;

    protected virtual void Reset()
    {
        RefreshReferences();
    }

    protected virtual void Awake()
    {
        RefreshReferences();
    }

    protected virtual void OnEnable()
    {
        RefreshReferences();
        RefreshStat();

        EnemyRegistry.Register(this);

        if (animationHandler != null)
            animationHandler.PlayMove();
    }

    protected virtual void OnDisable()
    {
        EnemyRegistry.Unregister(this);
    }

    public virtual void SetTarget(Transform player)
    {
        target = player;
    }

    public virtual void RefreshStat()
    {
        if (enemyStat == null)
            enemyStat = GetComponent<EnemyStat>();

        currentMoveSpeed = enemyStat != null ? enemyStat.MoveSpeed : FallbackMoveSpeed;
    }

    protected void PlayMoveAnimation(Vector3 moveDirection)
    {
        if (IsDead)
            return;

        if (animationHandler == null)
            RefreshReferences();

        if (animationHandler == null)
            return;

        animationHandler.SetMoveDirection(moveDirection);
        animationHandler.PlayMove();
    }

    protected void PlayIdleAnimation()
    {
        if (IsDead)
            return;

        if (animationHandler == null)
            RefreshReferences();

        if (animationHandler == null)
            return;

        animationHandler.PlayIdle();
    }

    protected void RefreshReferences()
    {
        if (animationHandler == null)
            animationHandler = GetComponentInChildren<AnimationHandler>(true);

        if (enemyStat == null)
            enemyStat = GetComponent<EnemyStat>();

        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();
    }
}