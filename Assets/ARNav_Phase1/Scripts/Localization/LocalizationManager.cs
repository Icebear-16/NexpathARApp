using UnityEngine;
using Vuforia;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager I;
    public string ActiveAnchorName = "";
    public ObserverBehaviour ActiveAnchor;
    public System.Action<string> OnAnchorChanged;
    public System.Action<string> OnTargetLost;

    private float lastSwitchTime;

    void Awake() { I = this; }

    void Start()
    {
        foreach (var obs in FindObjectsByType<ObserverBehaviour>(FindObjectsInactive.Include))
        {
            obs.OnTargetStatusChanged += HandleStatus;
        }
    }

    void HandleStatus(ObserverBehaviour behaviour, TargetStatus status)
    {
        // Only trust solid TRACKED + NORMAL. Ignore flicker.
        if (status.Status == Status.TRACKED && status.StatusInfo == StatusInfo.NORMAL)
        {
            // Hysteresis: don't switch more than once per 1 second in overlap zones
            if (behaviour.TargetName != ActiveAnchorName && Time.time - lastSwitchTime > 1.0f)
            {
                ActiveAnchorName = behaviour.TargetName;
                ActiveAnchor = behaviour;
                lastSwitchTime = Time.time;
                OnAnchorChanged?.Invoke(ActiveAnchorName);
                Debug.Log("[Loc] Localized to: " + ActiveAnchorName);
            }
        }
        else if (status.Status == Status.NO_POSE)
        {
            if (behaviour.TargetName == ActiveAnchorName)
                OnTargetLost?.Invoke(behaviour.TargetName);
        }
    }
}