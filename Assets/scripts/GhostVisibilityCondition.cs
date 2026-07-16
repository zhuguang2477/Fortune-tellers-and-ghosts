using FishNet.Connection;
using FishNet.Observing;  // 修正：正确的命名空间
using UnityEngine;

/// <summary>
/// 自定义观察条件：占卜师看不到鬼魂，鬼魂自身和其他鬼魂可见。
/// </summary>
[CreateAssetMenu(menuName = "FishNet/Observers/Ghost Visibility Condition", fileName = "New Ghost Visibility Condition")]
public class GhostVisibilityCondition : ObserverCondition
{
    /// <summary>
    /// 服务器评估该物体对某个连接是否可见时调用。
    /// </summary>
    public override bool ConditionMet(NetworkConnection connection, bool currentlyAdded, out bool notProcessed)
    {
        notProcessed = false;

        // 1. 鬼魂自己的 Owner 永远可见（否则自己看不到自己）
        if (NetworkObject != null && NetworkObject.Owner == connection)
            return true;

        // 2. 检查观察者（其他玩家）的标签
        if (connection.FirstObject != null)
        {
            // 如果观察者是占卜师（Tag == "Gadalka"），则不可见
            if (connection.FirstObject.CompareTag("Gadalka"))
                return false;
        }

        // 其他情况（未分配角色、同为鬼魂等）可见
        return true;
    }

    // ---------- FishNet 必须实现的方法 ----------
    public override ObserverConditionType GetConditionType()
    {
        return ObserverConditionType.Normal;
    }
}