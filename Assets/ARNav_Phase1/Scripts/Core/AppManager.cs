using UnityEngine;
public class AppManager : MonoBehaviour
{
    public static AppManager I;
    void Awake()
    {
        I = this;
        DontDestroyOnLoad(transform.parent.gameObject); // keep Managers alive
        // Init order enforced by Script Execution Order, but this guarantees refs
        Debug.Log("[App] Managers ready: Loc > Nav > Visual");
    }
}