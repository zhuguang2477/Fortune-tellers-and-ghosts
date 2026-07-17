using FishNet.Object;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RoleSelector : NetworkBehaviour
{
    private RoleManager roleManager;
    private bool isInitialized = false;

    private RoleManager.Role myCurrentRole = RoleManager.Role.None;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner) return;

        if (!TryFindUIManager() || !TryFindRoleManager())
        {
            StartCoroutine(DelayedInit());
            return;
        }

        InitializeUI();
    }

    private IEnumerator DelayedInit()
    {
        int attempts = 0;
        while (attempts < 10)
        {
            yield return null;

            if (UIManager.Instance != null && TryFindRoleManager())
            {
                InitializeUI();
                yield break;
            }
            attempts++;
        }
        Debug.LogError("【RoleSelector】Не удалось найти UIManager или RoleManager после нескольких попыток, инициализация UI не удалась.");
    }

    private bool TryFindUIManager()
    {
        return UIManager.Instance != null;
    }

    private bool TryFindRoleManager()
    {
        roleManager = FindObjectOfType<RoleManager>();
        return roleManager != null;
    }

    private void InitializeUI()
    {
        if (isInitialized) return;
        if (roleManager == null || UIManager.Instance == null) return;

        Button btnGadalka = UIManager.Instance.btnGadalka;
        Button btnPrizrak = UIManager.Instance.btnPrizrak;
        Button btnStart = UIManager.Instance.btnStart;
        Text statusText = UIManager.Instance.statusText;

        if (btnGadalka == null || btnPrizrak == null || btnStart == null || statusText == null)
        {
            Debug.LogError("【RoleSelector】Ссылки на элементы UI в UIManager не полностью назначены!");
            return;
        }

        Debug.Log($"【RoleSelector】Локальный игрок инициализирован успешно, IsOwner={IsOwner}");

        btnGadalka.onClick.RemoveAllListeners();
        btnPrizrak.onClick.RemoveAllListeners();
        btnStart.onClick.RemoveAllListeners();

        btnGadalka.onClick.AddListener(() => SelectRole(RoleManager.Role.Gadalka));
        btnPrizrak.onClick.AddListener(() => SelectRole(RoleManager.Role.Prizrak));
        btnStart.onClick.AddListener(RequestStartGame);

        btnStart.gameObject.SetActive(false);
        statusText.text = "Выберите класс (1=Гадалка, 2=Призрак, 0=Отмена)";

        RoleManager.OnRoleSelectionResultEvent += OnSelectionResult;
        RoleManager.OnGadalkaOccupiedChangedEvent += OnGadalkaStatusChanged;
        RoleManager.OnPrizrakOccupiedChangedEvent += OnPrizrakStatusChanged;
        RoleManager.OnGameStartedChangedEvent += OnGameStartedChanged;

        OnGadalkaStatusChanged(roleManager.GadalkaOccupied);
        OnPrizrakStatusChanged(roleManager.PrizrakOccupied);

        isInitialized = true;
    }

    private void SelectRole(RoleManager.Role role)
    {
        if (roleManager == null || UIManager.Instance == null) return;
        if (roleManager.GameStarted) return;

        if (myCurrentRole == role)
        {
            roleManager.CmdDeselectRole();
        }
        else
        {
            roleManager.CmdSelectRole(role);
        }
    }

    private void RequestStartGame()
    {
        if (roleManager == null || UIManager.Instance == null) return;
        roleManager.CmdStartGame();
    }

    private void OnSelectionResult(bool success, string message)
    {
        if (UIManager.Instance == null) return;
        UIManager.Instance.statusText.text = message;

        if (success)
        {
            if (message.Contains("Гадалка")) myCurrentRole = RoleManager.Role.Gadalka;
            else if (message.Contains("Призрак")) myCurrentRole = RoleManager.Role.Prizrak;
            else if (message.Contains("Отмена")) myCurrentRole = RoleManager.Role.None;
        }
    }

    private void OnGadalkaStatusChanged(bool occupied)
    {
        if (UIManager.Instance == null) return;
        UIManager.Instance.btnGadalka.image.color = occupied ? Color.red : Color.white;
        UpdateStartButtonVisibility();
    }

    private void OnPrizrakStatusChanged(bool occupied)
    {
        if (UIManager.Instance == null) return;
        UIManager.Instance.btnPrizrak.image.color = occupied ? Color.red : Color.white;
        UpdateStartButtonVisibility();
    }

    private void UpdateStartButtonVisibility()
    {
        if (UIManager.Instance == null || roleManager == null) return;

        bool bothReady = roleManager.GadalkaOccupied && roleManager.PrizrakOccupied;
        UIManager.Instance.btnStart.gameObject.SetActive(bothReady);

        if (bothReady)
            UIManager.Instance.statusText.text = "Оба класса выбраны, нажмите «Начать игру» или Enter";
        else
            UIManager.Instance.statusText.text = "Ожидание выбора класса всеми игроками...";
    }

    private void OnGameStartedChanged(bool started)
    {
        if (UIManager.Instance == null || !started) return;
        UIManager.Instance.btnStart.gameObject.SetActive(false);
        UIManager.Instance.btnGadalka.interactable = false;
        UIManager.Instance.btnPrizrak.interactable = false;
        UIManager.Instance.statusText.text = "Игра идёт...";
    }

    private void Update()
    {
        if (!IsOwner || !isInitialized || roleManager == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            SelectRole(RoleManager.Role.Gadalka);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            SelectRole(RoleManager.Role.Prizrak);
        else if (Input.GetKeyDown(KeyCode.Alpha0))
            roleManager.CmdDeselectRole();
        else if (Input.GetKeyDown(KeyCode.Return) && UIManager.Instance.btnStart.gameObject.activeSelf)
            RequestStartGame();
    }

    private void OnDestroy()
    {
        if (IsOwner)
        {
            RoleManager.OnRoleSelectionResultEvent -= OnSelectionResult;
            RoleManager.OnGadalkaOccupiedChangedEvent -= OnGadalkaStatusChanged;
            RoleManager.OnPrizrakOccupiedChangedEvent -= OnPrizrakStatusChanged;
            RoleManager.OnGameStartedChangedEvent -= OnGameStartedChanged;
        }
    }
}