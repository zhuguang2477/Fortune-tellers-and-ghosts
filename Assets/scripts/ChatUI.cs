using UnityEngine;
using UnityEngine.UI;
using TMPro; // 如果使用 TextMeshPro，否则用 UnityEngine.UI.Text

public class ChatUI : MonoBehaviour
{
    [Header("UI 引用")]
    public InputField inputField;     // 消息输入框
    public Button sendButton;         // 发送按钮
    public ScrollRect scrollRect;     // 滚动视图
    public Text messageDisplay;       // 显示消息的 Text（或 TMP_Text）
    // 如果使用 TMP，请替换为 TMP_Text

    private void Start()
    {
        // 订阅聊天事件
        ChatManager.OnMessageReceived += OnChatMessageReceived;

        // 按钮监听
        sendButton.onClick.AddListener(SendMessage);

        // 回车发送（绑定在 InputField 上）
        inputField.onSubmit.AddListener(delegate { SendMessage(); });
    }

    private void OnDestroy()
    {
        // 取消订阅
        ChatManager.OnMessageReceived -= OnChatMessageReceived;
    }

    private void SendMessage()
    {
        string msg = inputField.text.Trim();
        if (string.IsNullOrEmpty(msg))
            return;

        // 发送消息到服务器
        if (ChatManager.Instance != null)
            ChatManager.Instance.CmdSendChatMessage(msg);

        inputField.text = "";
        inputField.ActivateInputField(); // 焦点回到输入框
    }

    private void OnChatMessageReceived(string senderName, string message)
    {
        // 显示消息：格式 [角色名] 消息内容
        string displayText = $"[{senderName}] {message}\n";
        messageDisplay.text += displayText;

        // 滚动到最底部
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}