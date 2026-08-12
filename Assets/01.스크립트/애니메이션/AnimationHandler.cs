using System.Collections;
using UnityEngine;

public class AnimationHandler : MonoBehaviour
{
    [Header("애니메이션 이름")]
    [InspectorLabel("대기")]
    [SerializeField] private string idleStateName = "Idle";

    [InspectorLabel("이동")]
    [SerializeField] private string moveStateName = "Run";

    [InspectorLabel("피격")]
    [SerializeField] private string hitStateName = "Hit";

    [InspectorLabel("사망")]
    [SerializeField] private string deathStateName = "Death";

    [Header("설정")]
    [InspectorLabel("전환 시간")]
    [SerializeField] private float fadeTime;

    [InspectorLabel("피격 유지 시간")]
    [SerializeField] private float hitDuration;

    private Animator animator;
    private Coroutine hitRoutine;
    private string currentState;
    private string returnState;
    private bool isDead;

    private void Reset()
    {
        RefreshAnimator();
    }

    private void Awake()
    {
        RefreshAnimator();
    }

    private void OnDisable()
    {
        hitRoutine = null;
        currentState = null;
        isDead = false;
    }

    public void RefreshAnimator()
    {
        animator = null;

        Animator[] animators = GetComponentsInChildren<Animator>(true);

        for (int i = 0; i < animators.Length; i++)
        {
            if (!animators[i].gameObject.activeInHierarchy)
                continue;

            animator = animators[i];
            currentState = null;
            return;
        }
    }

    public void PlayIdle()
    {
        if (isDead)
            return;

        returnState = idleStateName;

        if (IsPlayingHit())
            return;

        Play(idleStateName);
    }

    public void PlayMove()
    {
        if (isDead)
            return;

        returnState = moveStateName;

        if (IsPlayingHit())
            return;

        Play(moveStateName);
    }

    public void PlayHit()
    {
        if (isDead)
            return;

        if (hitRoutine != null)
            StopCoroutine(hitRoutine);

        Play(hitStateName);
        hitRoutine = StartCoroutine(HitRoutine());
    }

    public void PlayDeath()
    {
        if (isDead)
            return;

        isDead = true;

        if (hitRoutine != null)
        {
            StopCoroutine(hitRoutine);
            hitRoutine = null;
        }

        Play(deathStateName);
    }

    public void ResetState()
    {
        isDead = false;
        currentState = null;
        returnState = idleStateName;

        if (hitRoutine != null)
        {
            StopCoroutine(hitRoutine);
            hitRoutine = null;
        }

        RefreshAnimator();
        PlayIdle();
    }

    public void Play(string stateName)
    {
        if (animator == null || !animator.gameObject.activeInHierarchy)
            RefreshAnimator();

        if (animator == null)
            return;

        if (string.IsNullOrEmpty(stateName))
            return;

        int stateHash = Animator.StringToHash(stateName);

        if (!animator.HasState(0, stateHash))
        {
            if (stateName == idleStateName)
            {
                stateName = moveStateName;
                stateHash = Animator.StringToHash(stateName);

                if (!animator.HasState(0, stateHash))
                    return;
            }
            else
            {
                return;
            }
        }

        if (currentState == stateName)
            return;

        currentState = stateName;
        animator.CrossFade(stateHash, fadeTime, 0);
    }

    private IEnumerator HitRoutine()
    {
        yield return new WaitForSeconds(hitDuration);

        hitRoutine = null;

        if (isDead)
            yield break;

        if (string.IsNullOrEmpty(returnState))
            returnState = idleStateName;

        Play(returnState);
    }

    private bool IsPlayingHit()
    {
        return hitRoutine != null;
    }
    
    public void SetMoveDirection(Vector3 direction)
    {
        if (direction.x > 0.01f)
            SetFlip(false);
        else if (direction.x < -0.01f)
            SetFlip(true);
    }

    private void SetFlip(bool flip)
    {
        Vector3 scale = transform.localScale;

        scale.x = flip
            ? -Mathf.Abs(scale.x)
            : Mathf.Abs(scale.x);

        transform.localScale = scale;
    }
}