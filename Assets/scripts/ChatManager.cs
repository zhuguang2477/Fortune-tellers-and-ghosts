using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using UnityEngine;

public class ChatManager : NetworkBehaviour
{
    public static ChatManager Instance { get; private set; }

    // 事件：当收到新消息时触发（客户端）
    public static event System.Action<string, string> OnMessageReceived; // (senderName, message)

    // 服务器端维护的角色映射（可直接使用 RoleManager 的字典，但为了解耦，这里自己维护）
    // 实际可直接引用 RoleManager，但为了避免循环依赖，我们通过事件或单例获取。
    // 简单做法：在服务器端，从 RoleManager 获取角色名。
    // 由于我们已有 RoleManager，可以在 ChatManager 中引用 RoleManager 组件。
    // 但为保持独立，我们使用 RoleManager 的静态方法或公共属性。

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// 客户端调用此方法发送消息
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void CmdSendChatMessage(string message, NetworkConnection sender = null)
    {
        if (sender == null || string.IsNullOrWhiteSpace(message))
            return;

        // 从 RoleManager 获取发送者的角色名
        string senderName = "未知";
        if (RoleManager.Instance != null)
        {
            // 假设 RoleManager 有一个公共方法或字典可查询
            // 或者直接使用 sender 的 FirstObject 的 tag
            GameObject playerObj = sender.FirstObject?.gameObject;
            if (playerObj != null)
            {
                senderName = playerObj.tag; // "Gadalka" 或 "Prizrak"
            }
        }

        // 广播消息给所有客户端（包括发送者）
        RpcBroadcastMessage(senderName, message);
    }

    /// <summary>
    /// 服务器广播消息到所有客户端
    /// </summary>
    [ObserversRpc]
    private void RpcBroadcastMessage(string senderName, string message)
    {
        // 客户端触发事件
        OnMessageReceived?.Invoke(senderName, message);
    }
}