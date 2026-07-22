using UnityEngine;

public class UIPanelToggler : MonoBehaviour
{
    [Header("Настройки клавиши")]
    public KeyCode toggleKey = KeyCode.Tab;

    [Header("Целевая панель")]
    public GameObject targetPanel;

    private void Update()
    {
        if (targetPanel == null) return;

        if (Input.GetKeyDown(toggleKey))
        {
            bool isActive = targetPanel.activeSelf;
            targetPanel.SetActive(!isActive);
            Debug.Log($"[UIPanelToggler] Панель {targetPanel.name} переключена на {!isActive}");
        }
    }
}