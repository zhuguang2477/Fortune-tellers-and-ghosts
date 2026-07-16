using FishNet.Object;
using UnityEngine;

public class VisibilitySetter : NetworkBehaviour
{
    public LayerMask gadalkaCullingMask;
    public LayerMask prizrakCullingMask;

    private Camera playerCam;
    private bool visibilitySet = false;

    public override void OnStartClient()
    {
        base.OnStartClient();

        playerCam = GetComponentInChildren<Camera>();
        if (playerCam == null)
        {
            Debug.LogWarning("玩家预制体上没有Camera组件！");
            return;
        }

        // 只有本地玩家才需要调整，非本地玩家摄像机已被禁用
        // 我们仍然处理，但只有 IsOwner 才会真正调整掩码
        if (!IsOwner)
        {
            // 非本地玩家不处理，直接返回
            return;
        }

        // 等待游戏开始后检测Tag变化
        visibilitySet = false;
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (visibilitySet) return;

        // 检测 Tag 是否已被服务器设置为角色名
        if (gameObject.CompareTag("Gadalka") || gameObject.CompareTag("Prizrak"))
        {
            ApplyVisibility();
        }
    }

    private void ApplyVisibility()
    {
        if (gameObject.CompareTag("Gadalka"))
        {
            playerCam.cullingMask = gadalkaCullingMask;
        }
        else if (gameObject.CompareTag("Prizrak"))
        {
            playerCam.cullingMask = prizrakCullingMask;
        }
        visibilitySet = true;
        enabled = false; // 不再需要Update
    }

    // 可选重置方法
    public void ResetVisibility()
    {
        visibilitySet = false;
        enabled = true;
    }
}