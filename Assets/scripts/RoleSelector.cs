using FishNet.Object;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RoleSelector : NetworkBehaviour
{
    private RoleManager roleManager;
    private bool isInitialized = false;

    // 本地记录自己选了什么，方便按同一个键时取消选择
    private RoleManager.Role myCurrentRole = RoleManager.Role.None; 

    public override void OnStartClient()
    {
        base.OnStartClient();

        // ─── 关键防御 ───
        // 如果这个生成的物体不是本地玩家的，直接拦截！不要让它去碰本地的单例 UI
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
        Debug.LogError("【RoleSelector】多次尝试后仍未找到 UIManager 或 RoleManager，UI 初始化失败。");
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
            Debug.LogError("【RoleSelector】UIManager 中的 UI 元素引用未完全赋值！");
            return;
        }

        Debug.Log($"【RoleSelector】本地玩家初始化成功，IsOwner={IsOwner}");

        // 清除旧监听
        btnGadalka.onClick.RemoveAllListeners();
        btnPrizrak.onClick.RemoveAllListeners();
        btnStart.onClick.RemoveAllListeners();

        // 绑定按钮事件
        btnGadalka.onClick.AddListener(() => SelectRole(RoleManager.Role.Gadalka));
        btnPrizrak.onClick.AddListener(() => SelectRole(RoleManager.Role.Prizrak));
        btnStart.onClick.AddListener(RequestStartGame);

        btnStart.gameObject.SetActive(false);
        statusText.text = "请选择职业 (1=占卜师，2=鬼魂，0=取消)";

        // ─── 核心架构 ───
        // 只有本地客户端才去监听 RoleManager 的网络变量改变，以此来更新本地唯一的 UI 按钮颜色和文本
        RoleManager.OnRoleSelectionResultEvent += OnSelectionResult;
        RoleManager.OnGadalkaOccupiedChangedEvent += OnGadalkaStatusChanged;
        RoleManager.OnPrizrakOccupiedChangedEvent += OnPrizrakStatusChanged;
        RoleManager.OnGameStartedChangedEvent += OnGameStartedChanged;

        // 初始化时根据当前房间状态刷一次 UI
        OnGadalkaStatusChanged(roleManager.GadalkaOccupied);
        OnPrizrakStatusChanged(roleManager.PrizrakOccupied);

        isInitialized = true;
    }

    private void SelectRole(RoleManager.Role role)
    {
        if (roleManager == null || UIManager.Instance == null) return;
        if (roleManager.GameStarted) return;

        // 如果重复选一样的，就发送取消选择
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

    // ─── 网络通知回调 ───
    private void OnSelectionResult(bool success, string message)
    {
        if (UIManager.Instance == null) return;
        UIManager.Instance.statusText.text = message;

        // 如果自己选成功了，记录下来；如果失败了，重置
        if (success)
        {
            if (message.Contains("占卜师")) myCurrentRole = RoleManager.Role.Gadalka;
            else if (message.Contains("鬼魂")) myCurrentRole = RoleManager.Role.Prizrak;
            else if (message.Contains("取消")) myCurrentRole = RoleManager.Role.None;
        }
    }

    private void OnGadalkaStatusChanged(bool occupied)
    {
        if (UIManager.Instance == null) return;
        // 如果被占用了，且不是自己选的，就把按钮变灰色（或者不可点击）
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
        
        // 只有两个职业都被占领了，才显示开始按钮
        bool bothReady = roleManager.GadalkaOccupied && roleManager.PrizrakOccupied;
        UIManager.Instance.btnStart.gameObject.SetActive(bothReady);
        
        if (bothReady)
            UIManager.Instance.statusText.text = "双方职业已选好，点击「开始游戏」或按 Enter";
        else
            UIManager.Instance.statusText.text = "等待所有玩家选择职业...";
    }

    private void OnGameStartedChanged(bool started)
    {
        if (UIManager.Instance == null || !started) return;
        UIManager.Instance.btnStart.gameObject.SetActive(false);
        UIManager.Instance.btnGadalka.interactable = false;
        UIManager.Instance.btnPrizrak.interactable = false;
        UIManager.Instance.statusText.text = "游戏进行中...";
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
        // 仅对本地订阅过事件的本体进行注销，防止内存泄漏
        if (IsOwner)
        {
            RoleManager.OnRoleSelectionResultEvent -= OnSelectionResult;
            RoleManager.OnGadalkaOccupiedChangedEvent -= OnGadalkaStatusChanged;
            RoleManager.OnPrizrakOccupiedChangedEvent -= OnPrizrakStatusChanged;
            RoleManager.OnGameStartedChangedEvent -= OnGameStartedChanged;
        }
    }
}
