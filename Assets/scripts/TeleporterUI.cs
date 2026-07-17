using UnityEngine;
using UnityEngine.UI;
using FishNet;
using FishNet.Connection;

public class TeleporterUI : MonoBehaviour
{
    [Header("Настройки UI")]
    public GameObject buttonPrefab;
    public Transform buttonContainer;
    public Teleporter teleporter;

    private void Start()
    {
        if (!InstanceFinder.IsClientStarted) return;

        if (!IsLocalPlayerPrizrak())
        {
            gameObject.SetActive(false);
            return;
        }

        if (teleporter == null)
        {
            teleporter = FindObjectOfType<Teleporter>();
            if (teleporter == null)
            {
                Debug.LogError("[TeleporterUI] Teleporter не найден, укажите вручную");
                return;
            }
        }

        GenerateButtons();
    }

    private bool IsLocalPlayerPrizrak()
    {
        NetworkConnection conn = InstanceFinder.ClientManager.Connection;
        if (conn == null) return false;
        RoleManager.Role role = RoleManager.Instance.GetPlayerRole(conn);
        return role == RoleManager.Role.Prizrak;
    }

    private void GenerateButtons()
    {
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

        int count = teleporter.TargetCount;
        if (count == 0)
        {
            Debug.LogWarning("[TeleporterUI] Нет целевых точек");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            int index = i;
            string name = teleporter.GetTargetName(i);

            GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
            Button btn = btnObj.GetComponent<Button>();
            Text text = btnObj.GetComponentInChildren<Text>();
            if (text != null) text.text = name;

            UpdateButtonColor(btn, i == teleporter.CurrentTargetIndex);

            btn.onClick.AddListener(() => {
                teleporter.CmdChangeTarget(index);
                RefreshButtons();
            });
        }
    }

    private void RefreshButtons()
    {
        int current = teleporter.CurrentTargetIndex;
        Button[] buttons = buttonContainer.GetComponentsInChildren<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            int idx = i;
            UpdateButtonColor(buttons[i], idx == current);
        }
    }

    private void UpdateButtonColor(Button btn, bool isSelected)
    {
        ColorBlock colors = btn.colors;
        colors.normalColor = isSelected ? Color.green : Color.white;
        btn.colors = colors;
    }
}