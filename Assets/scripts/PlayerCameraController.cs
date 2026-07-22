using FishNet.Object;
using UnityEngine;

public class PlayerCameraController : NetworkBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;

    [Header("Настройки маски отсечения (в зависимости от роли)")]
    [SerializeField] private LayerMask gadalkaCullingMask = -1;
    [SerializeField] private LayerMask prizrakCullingMask = -1;

    private void Awake()
    {
        if (playerCamera == null) playerCamera = GetComponent<Camera>();
        if (audioListener == null) audioListener = GetComponent<AudioListener>();

        if (playerCamera != null) playerCamera.enabled = false;
        if (audioListener != null) audioListener.enabled = false;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner)
        {
            if (playerCamera != null) playerCamera.enabled = false;
            if (audioListener != null) audioListener.enabled = false;
            return;
        }

        if (playerCamera != null)
        {
            playerCamera.enabled = true;
            ApplyCullingByTag();
        }
        if (audioListener != null) audioListener.enabled = true;
    }

    public void RefreshCulling()
    {
        if (IsOwner && playerCamera != null)
        {
            ApplyCullingByTag();
            Debug.Log($"[PlayerCameraController] Маска отсечения обновлена, cullingMask={playerCamera.cullingMask}");
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
            playerCamera.cullingMask = -1;
    }
}