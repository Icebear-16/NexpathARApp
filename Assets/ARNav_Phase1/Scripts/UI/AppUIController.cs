using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class AppUIController : MonoBehaviour
{
    [Header("Main Screen Elements")]
    public GameObject searchToggleButton;
    public GameObject tipsToggleButton;
    public GameObject styleToggleButton;
    public GameObject distanceBarPanel;
    public TMP_Text distanceText;

    [Header("Search Popup Elements")]
    public GameObject searchPopupPanel;
    public TMP_InputField searchBar;
    public Button officeFilterBtn, classroomFilterBtn, labFilterBtn;
    public Button startNavButton;
    public Button closeSearchButton;

    [Header("Tips Popup Elements")]
    public GameObject tipsPopupPanel;
    public Button closeTipsButton;

    [Header("Destination Arrived Elements")]
    public GameObject destinationArrivedPanel;
    public TMP_Text arrivedLocText;
    public TMP_Text arrivedDescText;
    public Button finishNavButton;
    public Button dismissArrivedButton;

    [Header("Search Results Dropdown")]
    public Transform resultsContent;           // Content transform under ScrollView/Viewport/Content
    public GameObject resultItemPrefab;        // Prefab with Button + TMP_Text child

    private string selectedCategory = "";
    private LocationRow? selectedLocation;
    private readonly List<GameObject> spawnedItems = new();
    private float lastDistanceUpdateTime = 0f;
    private bool hasTriggeredArrival = false;

    void Start()
    {
        // Set proper initial states when app opens
        if (searchToggleButton != null) searchToggleButton.SetActive(true);
        if (tipsToggleButton != null) tipsToggleButton.SetActive(true);
        if (styleToggleButton != null) styleToggleButton.SetActive(true);
        if (distanceBarPanel != null) distanceBarPanel.SetActive(false);
        if (searchPopupPanel != null) searchPopupPanel.SetActive(false);
        if (tipsPopupPanel != null) tipsPopupPanel.SetActive(false);
        if (destinationArrivedPanel != null) destinationArrivedPanel.SetActive(false);

        // Assign search button hooks
        if (searchToggleButton != null)
        {
            var btn = searchToggleButton.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OpenSearchPanel);
        }

        // Assign tips button hooks
        if (tipsToggleButton != null)
        {
            var btn = tipsToggleButton.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OpenTipsPanel);
        }

        // Assign path style toggle button hook
        if (styleToggleButton != null)
        {
            var btn = styleToggleButton.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(TogglePathStyle);
        }

        if (startNavButton != null) startNavButton.onClick.AddListener(StartNavigationMode);
        if (closeSearchButton != null) closeSearchButton.onClick.AddListener(CloseSearchPanel);
        if (closeTipsButton != null) closeTipsButton.onClick.AddListener(CloseTipsPanel);

        // Destination arrived button hooks
        if (finishNavButton != null) finishNavButton.onClick.AddListener(FinishNavigation);
        if (dismissArrivedButton != null) dismissArrivedButton.onClick.AddListener(DismissArrivedPopup);

        // Hook up filters to a generic selector function
        if (officeFilterBtn != null) officeFilterBtn.onClick.AddListener(() => ToggleFilter("Office"));
        if (classroomFilterBtn != null) classroomFilterBtn.onClick.AddListener(() => ToggleFilter("Classroom"));
        if (labFilterBtn != null) labFilterBtn.onClick.AddListener(() => ToggleFilter("Laboratory"));

        if (searchBar != null) searchBar.onValueChanged.AddListener(_ => RefreshResults());

        // Preload default selection so navigation is ready immediately
        var initialRows = DbQueries.Search(DatabaseManager.I, "", "");
        if (initialRows.Count > 0)
        {
            var r105 = initialRows.Find(r => r.name.Contains("105"));
            selectedLocation = r105.nodeName != null ? r105 : initialRows[0];
        }
    }

    void Update()
    {
        if (distanceBarPanel != null && distanceBarPanel.activeSelf && Time.time - lastDistanceUpdateTime > 0.4f)
        {
            lastDistanceUpdateTime = Time.time;
            UpdateDistanceDisplay();
        }
    }

    public void OpenSearchPanel()
    {
        if (tipsPopupPanel != null) tipsPopupPanel.SetActive(false);
        if (searchPopupPanel != null) searchPopupPanel.SetActive(true);
        if (searchBar != null) searchBar.text = "";
        ToggleFilter("");
    }

    public void CloseSearchPanel()
    {
        if (searchPopupPanel != null) searchPopupPanel.SetActive(false);
        ClearResults();
    }

    public void OpenTipsPanel()
    {
        if (searchPopupPanel != null) searchPopupPanel.SetActive(false);
        if (tipsPopupPanel != null) tipsPopupPanel.SetActive(true);
    }

    public void CloseTipsPanel()
    {
        if (tipsPopupPanel != null) tipsPopupPanel.SetActive(false);
    }

    private void ToggleFilter(string category)
    {
        if (selectedCategory == category)
            selectedCategory = "";
        else
            selectedCategory = category;

        RefreshResults();
    }

    public void RefreshResults()
    {
        string query = searchBar != null ? searchBar.text : "";
        var rows = DbQueries.Search(DatabaseManager.I, query, selectedCategory);
        PopulateResultsDropdown(rows);

        if (rows.Count > 0)
        {
            selectedLocation = rows[0];
        }
    }

    private void PopulateResultsDropdown(List<LocationRow> rows)
    {
        ClearResults();

        if (resultsContent == null || resultItemPrefab == null)
            return;

        foreach (var row in rows)
        {
            var item = Instantiate(resultItemPrefab, resultsContent);
            item.SetActive(true);
            spawnedItems.Add(item);

            var tmp = item.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
                tmp.text = $"{row.name} ({row.category})";

            var btn = item.GetComponent<Button>();
            if (btn != null)
            {
                var capturedRow = row;
                btn.onClick.AddListener(() => OnResultClicked(capturedRow));
            }
        }
    }

    private void OnResultClicked(LocationRow row)
    {
        selectedLocation = row;
        Debug.Log($"[Search] Selected: {row.name} -> {row.nodeName}");
        if (searchBar != null)
            searchBar.text = row.name;
    }

    private void ClearResults()
    {
        foreach (var item in spawnedItems)
        {
            if (item != null) Destroy(item);
        }
        spawnedItems.Clear();
    }

    public void StartNavigationMode()
    {
        if (searchPopupPanel != null) searchPopupPanel.SetActive(false);
        if (tipsPopupPanel != null) tipsPopupPanel.SetActive(false);
        if (destinationArrivedPanel != null) destinationArrivedPanel.SetActive(false);
        hasTriggeredArrival = false;

        if (!selectedLocation.HasValue)
        {
            var rows = DbQueries.Search(DatabaseManager.I, "", "");
            if (rows.Count > 0)
            {
                var r105 = rows.Find(r => r.name.Contains("105"));
                selectedLocation = r105.nodeName != null ? r105 : rows[0];
            }
        }

        if (selectedLocation.HasValue && NavigationManager.I != null)
        {
            NavigationManager.I.DestinationNodeId = selectedLocation.Value.nodeName;
            if (PathVisualManager.I != null)
            {
                PathVisualManager.I.SetDestinationName(selectedLocation.Value.name);
            }
            NavigationManager.I.Recalculate();
        }

        if (distanceBarPanel != null) distanceBarPanel.SetActive(true);
        UpdateDistanceDisplay();
    }

    private void UpdateDistanceDisplay()
    {
        if (distanceText == null) return;

        if (!selectedLocation.HasValue)
        {
            distanceText.text = "Select a destination";
            return;
        }

        string locName = selectedLocation.Value.name;
        float remainingDistance = 0f;

        if (NavigationManager.I != null && NavigationManager.I.CurrentWorldPath != null && NavigationManager.I.CurrentWorldPath.Count > 0)
        {
            var path = NavigationManager.I.CurrentWorldPath;
            if (Camera.main != null)
            {
                remainingDistance += Vector3.Distance(Camera.main.transform.position, path[0]);
            }
            for (int i = 0; i < path.Count - 1; i++)
            {
                remainingDistance += Vector3.Distance(path[i], path[i + 1]);
            }
        }

        if (remainingDistance > 0.1f)
            distanceText.text = $"Navigating to {locName} ({remainingDistance:F1} m)";
        else
            distanceText.text = $"Navigating to {locName}";

        // Arrival Check: When user is within 1.8 meters of destination
        if (remainingDistance > 0f && remainingDistance <= 1.8f && !hasTriggeredArrival && (NavigationManager.I != null && NavigationManager.I.IsNavigating))
        {
            TriggerDestinationArrived();
        }
    }

    private void TriggerDestinationArrived()
    {
        hasTriggeredArrival = true;
        if (destinationArrivedPanel != null)
        {
            if (arrivedLocText != null && selectedLocation.HasValue)
            {
                arrivedLocText.text = $" {selectedLocation.Value.name}";
            }
            if (arrivedDescText != null && selectedLocation.HasValue)
            {
                arrivedDescText.text = string.IsNullOrEmpty(selectedLocation.Value.description)
                    ? "You have reached your destination."
                    : selectedLocation.Value.description;
            }
            destinationArrivedPanel.SetActive(true);
        }
    }

    public void FinishNavigation()
    {
        if (destinationArrivedPanel != null) destinationArrivedPanel.SetActive(false);
        if (distanceBarPanel != null) distanceBarPanel.SetActive(false);

        if (NavigationManager.I != null)
        {
            NavigationManager.I.StopNavigation();
        }
        if (PathVisualManager.I != null)
        {
            PathVisualManager.I.Clear();
        }
    }

    public void DismissArrivedPopup()
    {
        if (destinationArrivedPanel != null) destinationArrivedPanel.SetActive(false);
    }

    public void TogglePathStyle()
    {
        if (PathVisualManager.I != null)
        {
            PathVisualManager.I.TogglePathStyle();
        }
    }
}
