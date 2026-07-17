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
            Debug.LogWarning("На префабе игрока отсутствует компонент Camera!");
            return;
        }

        if (!IsOwner)
        {
            return;
        }

        visibilitySet = false;
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (visibilitySet) return;

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
        enabled = false;
    }

    public void ResetVisibility()
    {
        visibilitySet = false;
        enabled = true;
    }
}