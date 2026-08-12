using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    [InspectorLabel("플레이어 스탯")]
    [SerializeField] private PlayerStat playerStat;
    
    [InspectorLabel("애니메이션 핸들러")]
    [SerializeField] private AnimationHandler animationHandler;
    
    CharacterController controller;
    Vector2 moveInput;
    
    public Vector3 MoveDirection { get; private set; }
    
    public void SetMoveInput(Vector2 input)
    {
        moveInput = input;

        if (moveInput.sqrMagnitude > 1f)
            moveInput = moveInput.normalized;
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (playerStat == null)
            playerStat = GetComponent<PlayerStat>();

        if (animationHandler == null)
            animationHandler = GetComponentInChildren<AnimationHandler>(true);
    }
    
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        
        if (moveInput.sqrMagnitude > 1f)
            moveInput = moveInput.normalized;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            MoveDirection = Vector3.zero;
            return;
        }

        if (controller == null || playerStat == null)
            return;

        Vector3 dir = new Vector3(moveInput.x, 0f, moveInput.y);

        MoveDirection = dir;

        controller.Move(dir * (playerStat.MoveSpeed * Time.deltaTime));

        if (animationHandler == null)
            return;

        animationHandler.SetMoveDirection(dir);

        if (dir.sqrMagnitude > 0.001f)
            animationHandler.PlayMove();
        else
            animationHandler.PlayIdle();
    }
}