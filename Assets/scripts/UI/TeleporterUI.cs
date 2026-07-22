using UnityEngine;
using UnityEngine.UI;
using FishNet;
using FishNet.Connection;

public class TeleporterUI : MonoBehaviour
{
    [Header("Ссылка на телепортер")]
    public Teleporter teleporter;

    [Header("Список кнопок целей (порядок соответствует индексам точек)")]
    public Button[] targetButtons;

    [Header("Настройки цвета")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.green;

    private int currentSelectedIndex = -1;

    private void Start()
    {
        Debug.Log("[TeleporterUI] Start начат");

        if (!InstanceFinder.IsClientStarted)
        {
            Debug.Log("[TeleporterUI] Клиент не запущен, UI отключен");
            gameObject.SetActive(false);
            return;
        }

        if (teleporter == null)
        {
            teleporter = FindObjectOfType<Teleporter>();
            if (teleporter == null)
            {
                Debug.LogError("[TeleporterUI] Teleporter не найден, UI отключен");
                gameObject.SetActive(false);
                return;
            }
            Debug.Log("[TeleporterUI] Teleporter найден автоматически");
        }

        if (teleporter.targetPoints == null || teleporter.targetPoints.Length == 0)
        {
            Debug.LogWarning("[TeleporterUI] У телепортера нет точек назначения, UI отключен");
            gameObject.SetActive(false);
            return;
        }

        if (targetButtons == null || targetButtons.Length == 0)
        {
            targetButtons = GetComponentsInChildren<Button>();
            if (targetButtons == null || targetButtons.Length == 0)
            {
                Debug.LogError("[TeleporterUI] Кнопки не найдены, UI отключен");
                gameObject.SetActive(false);
                return;
            }
            Debug.Log($"[TeleporterUI] Автоматически найдено {targetButtons.Length} кнопок");
        }

        int buttonCount = Mathf.Min(targetButtons.Length, teleporter.targetPoints.Length);
        for (int i = 0; i < buttonCount; i++)
        {
            int index = i;
            Button btn = targetButtons[i];
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnButtonClick(index));

                Text txt = btn.GetComponentInChildren<Text>();
                if (txt != null && teleporter.targetPoints[i] != null)
                {
                    txt.text = teleporter.targetPoints[i].name;
                }

                ColorBlock colors = btn.colors;
                colors.normalColor = normalColor;
                btn.colors = colors;

                Debug.Log($"[TeleporterUI] Привязана кнопка {i} -> цель {teleporter.targetPoints[i].name}");
            }
            else
            {
                Debug.LogWarning($"[TeleporterUI] Кнопка {i} равна null, пропущено");
            }
        }

        int startIndex = Mathf.Clamp(teleporter.CurrentTargetIndex, 0, buttonCount - 1);
        SetSelected(startIndex);
        Debug.Log($"[TeleporterUI] Привязка завершена, выбрано {startIndex}");

        if (!IsLocalPlayerPrizrak())
        {
            Debug.Log("[TeleporterUI] Не Prizrak, UI скрыт");
            gameObject.SetActive(false);
        }
    }

    private bool IsLocalPlayerPrizrak()
    {
        NetworkConnection conn = InstanceFinder.ClientManager.Connection;
        if (conn == null)
        {
            Debug.LogWarning("[TeleporterUI] Локальное соединение пусто");
            return false;
        }
        RoleManager.Role role = RoleManager.Instance.GetPlayerRole(conn);
        Debug.Log($"[TeleporterUI] Роль локального игрока: {role}");
        return role == RoleManager.Role.Prizrak;
    }

    private void OnButtonClick(int index)
    {
        Debug.LogError($"[TeleporterUI] Нажата кнопка, индекс цели {index}");
        if (teleporter != null)
        {
            teleporter.CmdChangeTarget(index);
            SetSelected(index);
        }
        else
        {
            Debug.LogError("[TeleporterUI] teleporter равен null, невозможно переключить");
        }
    }

    private void SetSelected(int index)
    {
        foreach (var btn in targetButtons)
        {
            if (btn != null)
            {
                ColorBlock colors = btn.colors;
                colors.normalColor = normalColor;
                btn.colors = colors;
            }
        }

        if (index >= 0 && index < targetButtons.Length && targetButtons[index] != null)
        {
            ColorBlock colors = targetButtons[index].colors;
            colors.normalColor = selectedColor;
            targetButtons[index].colors = colors;
            currentSelectedIndex = index;
            Debug.Log($"[TeleporterUI] Подсвечена кнопка {index}");
        }
    }

    private void Update()
    {
        if (teleporter != null && teleporter.CurrentTargetIndex != currentSelectedIndex)
        {
            Debug.Log($"[TeleporterUI] Индекс на сервере изменён на {teleporter.CurrentTargetIndex}, синхронизация подсветки");
            SetSelected(teleporter.CurrentTargetIndex);
        }
    }
}