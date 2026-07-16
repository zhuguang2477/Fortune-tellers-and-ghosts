using FishNet.Object;
using UnityEngine;

public class PlayerCameraController : NetworkBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;

    [Header("剔除层配置（根据角色）")]
    [SerializeField] private LayerMask gadalkaCullingMask = -1; // 占卜师摄像机剔除掩码（例如：除Prizrak层外全部）
    [SerializeField] private LayerMask prizrakCullingMask = -1; // 鬼魂摄像机剔除掩码（例如：全部可见）

    private void Awake()
    {
        if (playerCamera == null) playerCamera = GetComponent<Camera>();
        if (audioListener == null) audioListener = GetComponent<AudioListener>();

        // 强制初始禁用（确保非Owner不会意外启用）
        if (playerCamera != null) playerCamera.enabled = false;
        if (audioListener != null) audioListener.enabled = false;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // 只有 Owner 拥有渲染权利
        if (!IsOwner)
        {
            if (playerCamera != null) playerCamera.enabled = false;
            if (audioListener != null) audioListener.enabled = false;
            return;
        }

        // Owner 启用摄像机
        if (playerCamera != null)
        {
            playerCamera.enabled = true;
            ApplyCullingByTag(); // 根据当前Tag设置剔除
        }
        if (audioListener != null) audioListener.enabled = true;
    }

    /// <summary>
    /// 根据当前对象的Tag更新剔除层（外部可调用刷新）
    /// </summary>
    public void RefreshCulling()
    {
        if (IsOwner && playerCamera != null)
        {
            ApplyCullingByTag();
            Debug.Log($"[PlayerCameraController] 刷新剔除层，cullingMask={playerCamera.cullingMask}");
        }
    }

    private void ApplyCullingByTag()
    {
        if (playerCamera == null) return;

        string tag = gameObject.tag;
        if (tag == "Gadalka")
            playerCamera.cullingMask = gadalkaCullingMask;
        else if (tag == "Prizrak")
            playerCamera.cullingMask = prizrakCullingMask;
        else
            playerCamera.cullingMask = -1; // 默认全部可见
    }
}