using UnityEngine;
using FishNet.Object;

public class BasicPlayerController : NetworkBehaviour
{
    [SerializeField] private CharacterController controller;
    [SerializeField] private Camera playerCamera; // 拖拽摄像机引用，或自动获取

    private Vector3 playerVelocity;
    private bool groundedPlayer;
    private float playerSpeed = 2.0f;
    private float jumpHeight = 1.0f;
    private float gravityValue = -9.81f;

    private void Awake()
    {
        // 自动获取组件（如果未手动赋值）
        if (controller == null)
            controller = GetComponent<CharacterController>();
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>(); // 如果摄像机在子物体上

        // 初始禁用摄像机和控制器（由 OnStartClient 决定是否启用）
        if (controller != null)
            controller.enabled = false;
        if (playerCamera != null)
            playerCamera.enabled = false;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // 只有本地玩家（Owner）启用控制组件和摄像机
        bool isOwner = IsOwner;

        if (controller != null)
            controller.enabled = isOwner;
        if (playerCamera != null)
            playerCamera.enabled = isOwner;

        // 调试日志（可选）
        Debug.Log($"[BasicPlayerController] {gameObject.name} IsOwner={isOwner}, 控制器启用={controller?.enabled}, 摄像机启用={playerCamera?.enabled}");
    }

    void Update()
    {
        // 非 Owner 不处理移动
        if (!IsOwner || controller == null)
            return;

        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        move = Vector3.ClampMagnitude(move, 1f);
        if (move != Vector3.zero)
        {
            transform.forward = move;
        }

        if (Input.GetButtonDown("Jump") && groundedPlayer)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravityValue);
        }

        playerVelocity.y += gravityValue * Time.deltaTime;
        Vector3 finalMove = (move * playerSpeed) + (playerVelocity.y * Vector3.up);
        controller.Move(finalMove * Time.deltaTime);
    }
}