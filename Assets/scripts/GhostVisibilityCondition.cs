using FishNet.Connection;
using FishNet.Observing;
using UnityEngine;

[CreateAssetMenu(menuName = "FishNet/Observers/Ghost Visibility Condition", fileName = "New Ghost Visibility Condition")]
public class GhostVisibilityCondition : ObserverCondition
{
    public override bool ConditionMet(NetworkConnection connection, bool currentlyAdded, out bool notProcessed)
    {
        notProcessed = false;

        if (NetworkObject != null && NetworkObject.Owner == connection)
            return true;

        if (connection.FirstObject != null)
        {
            if (connection.FirstObject.CompareTag("Gadalka"))
                return false;
        }

        return true;
    }

    public override ObserverConditionType GetConditionType()
    {
        return ObserverConditionType.Normal;
    }
}