using UdonSharp;
using UnityEngine;

public class CameraWaterCheck : UdonSharpBehaviour {

    private Collider[] triggers = new Collider[128];
    private int triggersActualLength;

    private void OnTriggerEnter (Collider other) {

        if (!ShimTriggersContains (other))
            ShimTriggersAdd (other);

    }

    private void OnTriggerExit (Collider other) {

        if (ShimTriggersContains (other))
            ShimTriggersRemove (other);

    }

    private bool ShimTriggersContains(Collider other)
    {
        // Naive implementation
        for (var index = 0; index < triggersActualLength; index++)
        {
            var trigger = triggers[index];
            if (trigger == other) return true;
        }

        return false;
    }

    private void ShimTriggersAdd(Collider other)
    {
        // Naive implementation
        if (triggers.Length > triggersActualLength)
        {
            triggers[triggersActualLength] = other;
            triggersActualLength++;
        }
    }

    private void ShimTriggersRemove(Collider other)
    {
        // Naive implementation
        for (var index = 0; index < triggersActualLength; index++)
        {
            var trigger = triggers[index];
            if (trigger == other)
            {
                triggers[index] = triggers[triggersActualLength - 1];
                triggersActualLength--;
            }
        }
    }

    public bool IsUnderwater ()
    {
        for (var index = 0; index < triggersActualLength; index++)
        {
            Collider trigger = triggers[index];
            if (trigger.GetComponentInParent<Water>())
                return true;
        }

        return false;
    }

}
