using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public static class SetupTipsAndArrivalUI
{
    static GameObject GetOrCreateUI(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    [MenuItem("ARNav/UI/Setup Tips and Arrival UI")]
    public static void Setup()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[Setup] Canvas not found.");
            return;
        }

        var appUi = Object.FindFirstObjectByType<AppUIController>();
        if (appUi == null)
        {
            Debug.LogError("[Setup] AppUIController not found.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(canvas, "Setup Tips & Arrival UI");

        // Load Sprites
        Sprite button2Sprite = null;
        var b2Objs = AssetDatabase.LoadAllAssetsAtPath("Assets/ARNav_Phase1/button2.png");
        foreach (var obj in b2Objs) { if (obj is Sprite s) { button2Sprite = s; break; } }

        Sprite closeSprite = null;
        var closeObjs = AssetDatabase.LoadAllAssetsAtPath("Assets/ARNav_Phase1/CloseIcon.png");
        foreach (var obj in closeObjs) { if (obj is Sprite s) { closeSprite = s; break; } }

        Sprite tipsIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ARNav_Phase1/Textures/tips_icon.png");
        Sprite styleIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ARNav_Phase1/Textures/path_toggle_icon.png");

        // ═══════════════════════════════════════════════════════
        // 1. CREATE TIPS TOGGLE BUTTON (Mirrored Left)
        // ═══════════════════════════════════════════════════════
        GameObject tipsBtnGo = GetOrCreateUI(canvas.transform, "TipsToggleButton");
        var tipsRt = tipsBtnGo.GetComponent<RectTransform>();
        tipsRt.anchorMin = new Vector2(0f, 0.5f);
        tipsRt.anchorMax = new Vector2(0f, 0.5f);
        tipsRt.pivot = new Vector2(0.5f, 0.5f);
        tipsRt.anchoredPosition = new Vector2(80f, -300f);
        tipsRt.sizeDelta = new Vector2(150f, 150f);

        var tipsImg = tipsBtnGo.AddComponent<Image>();
        tipsImg.sprite = button2Sprite;
        tipsImg.color = new Color(0.482f, 0.224f, 0.855f, 1.0f); // Sleek modern purple

        var tipsBtn = tipsBtnGo.AddComponent<Button>();

        // Icon child
        GameObject iconGo = GetOrCreateUI(tipsBtnGo.transform, "TipsIcon");
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.sizeDelta = new Vector2(-40f, -40f);
        iconRt.anchoredPosition = Vector2.zero;

        var iconImg = iconGo.AddComponent<Image>();
        iconImg.sprite = tipsIconSprite;
        iconImg.color = Color.white;
        iconImg.raycastTarget = false;

        tipsBtnGo.SetActive(true);

        // ═══════════════════════════════════════════════════════
        // 1B. CREATE STYLE TOGGLE BUTTON (Right above Search)
        // ═══════════════════════════════════════════════════════
        GameObject styleBtnGo = GetOrCreateUI(canvas.transform, "StyleToggleButton");
        var styleRt = styleBtnGo.GetComponent<RectTransform>();
        styleRt.anchorMin = new Vector2(1f, 0.5f);
        styleRt.anchorMax = new Vector2(1f, 0.5f);
        styleRt.pivot = new Vector2(0.5f, 0.5f);
        styleRt.anchoredPosition = new Vector2(-80f, -120f);
        styleRt.sizeDelta = new Vector2(150f, 150f);

        var styleImg = styleBtnGo.AddComponent<Image>();
        styleImg.sprite = button2Sprite;
        styleImg.color = new Color(0.0f, 0.72f, 0.90f, 1.0f); // Sleek electric cyan

        var styleBtn = styleBtnGo.AddComponent<Button>();

        GameObject styleIconGo = GetOrCreateUI(styleBtnGo.transform, "StyleIcon");
        var styleIconRt = styleIconGo.GetComponent<RectTransform>();
        styleIconRt.anchorMin = Vector2.zero;
        styleIconRt.anchorMax = Vector2.one;
        styleIconRt.sizeDelta = new Vector2(-40f, -40f);
        styleIconRt.anchoredPosition = Vector2.zero;

        var styleIconImg = styleIconGo.AddComponent<Image>();
        styleIconImg.sprite = styleIconSprite;
        styleIconImg.color = Color.white;
        styleIconImg.raycastTarget = false;

        styleBtnGo.SetActive(true);

        // ═══════════════════════════════════════════════════════
        // 2. CREATE TIPS POPUP PANEL
        // ═══════════════════════════════════════════════════════
        GameObject tipsPopupGo = GetOrCreateUI(canvas.transform, "TipsPopupPanel");
        var popupRt = tipsPopupGo.GetComponent<RectTransform>();
        popupRt.anchorMin = new Vector2(0f, 0f);
        popupRt.anchorMax = new Vector2(1f, 1f);
        popupRt.pivot = new Vector2(0.5f, 0.5f);
        popupRt.anchoredPosition = new Vector2(0f, 165f);
        popupRt.sizeDelta = new Vector2(-120f, -470f);

        var popupImg = tipsPopupGo.AddComponent<Image>();
        popupImg.sprite = button2Sprite;
        popupImg.color = new Color(0.20f, 0.10f, 0.38f, 0.94f); // Rich dark translucent purple

        // Close button
        GameObject closeGo = GetOrCreateUI(tipsPopupGo.transform, "CloseTipsButton");
        var closeRt = closeGo.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 1f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.anchoredPosition = new Vector2(-20f, -20f);
        closeRt.sizeDelta = new Vector2(70f, 70f);

        var closeBtnImg = closeGo.AddComponent<Image>();
        closeBtnImg.sprite = closeSprite;
        closeBtnImg.color = Color.white;
        var closeBtn = closeGo.AddComponent<Button>();

        // Header Title
        GameObject titleGo = GetOrCreateUI(tipsPopupGo.transform, "TipsTitle");
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -40f);
        titleRt.sizeDelta = new Vector2(-160f, 70f);

        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "💡 AR Navigation Guide & Tips";
        titleTmp.fontSize = 42;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;

        // Content Area
        GameObject contentGo = GetOrCreateUI(tipsPopupGo.transform, "TipsContent");
        var contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 0f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 0.5f);
        contentRt.anchoredPosition = new Vector2(0f, -40f);
        contentRt.sizeDelta = new Vector2(-60f, -170f);

        var contentTmp = contentGo.AddComponent<TextMeshProUGUI>();
        contentTmp.fontSize = 28;
        contentTmp.lineSpacing = 16;
        contentTmp.color = new Color(0.92f, 0.92f, 0.98f, 1.0f);
        contentTmp.text =
            "<b>🎯 How to Navigate:</b>\n" +
            "Tap the blue Search icon, pick a room or filter by category (Office, Lab, Classroom), then tap <i>Start Navigation</i>. Follow the glowing floor runway.\n\n" +
            "<b>📱 Camera Positioning:</b>\n" +
            "Hold your phone upright at chest/eye level. Point towards doorways, signs, and hallways. Avoid pointing only at blank floors or ceiling.\n\n" +
            "<b>🚶 Walking & Lighting:</b>\n" +
            "Walk steadily. Avoid sudden rapid twists. Well-lit corridors give the most stable AR tracking.\n\n" +
            "<b>🪜 Staircase (Floor 1 ↔ Floor 2):</b>\n" +
            "Follow the yellow staircase markers (AT_02). Waypoints will automatically switch to your new floor as you ascend or descend.\n\n" +
            "<b>🔄 If Tracking Drifts:</b>\n" +
            "Slowly pan your camera across nearby room entrance doorframes to re-localize instantly.";

        tipsPopupGo.SetActive(false);

        // ═══════════════════════════════════════════════════════
        // 3. CREATE DESTINATION ARRIVED POPUP MODAL
        // ═══════════════════════════════════════════════════════
        GameObject arrivedGo = GetOrCreateUI(canvas.transform, "DestinationArrivedPanel");
        var arrRt = arrivedGo.GetComponent<RectTransform>();
        arrRt.anchorMin = new Vector2(0.5f, 0.5f);
        arrRt.anchorMax = new Vector2(0.5f, 0.5f);
        arrRt.pivot = new Vector2(0.5f, 0.5f);
        arrRt.anchoredPosition = new Vector2(0f, 0f);
        arrRt.sizeDelta = new Vector2(820f, 520f);

        var arrImg = arrivedGo.AddComponent<Image>();
        arrImg.sprite = button2Sprite;
        arrImg.color = new Color(0.04f, 0.12f, 0.28f, 0.97f); // Deep navy blue

        // Arrived Title
        GameObject arrTitleGo = GetOrCreateUI(arrivedGo.transform, "ArrivedTitle");
        var arrTitleRt = arrTitleGo.GetComponent<RectTransform>();
        arrTitleRt.anchorMin = new Vector2(0f, 1f);
        arrTitleRt.anchorMax = new Vector2(1f, 1f);
        arrTitleRt.pivot = new Vector2(0.5f, 1f);
        arrTitleRt.anchoredPosition = new Vector2(0f, -40f);
        arrTitleRt.sizeDelta = new Vector2(-40f, 70f);

        var arrTitleTmp = arrTitleGo.AddComponent<TextMeshProUGUI>();
        arrTitleTmp.text = "🎯 You Have Arrived!";
        arrTitleTmp.fontSize = 44;
        arrTitleTmp.fontStyle = FontStyles.Bold;
        arrTitleTmp.alignment = TextAlignmentOptions.Center;
        arrTitleTmp.color = new Color(0f, 0.88f, 1f);

        // Location Name Text
        GameObject arrLocGo = GetOrCreateUI(arrivedGo.transform, "ArrivedLocText");
        var arrLocRt = arrLocGo.GetComponent<RectTransform>();
        arrLocRt.anchorMin = new Vector2(0f, 0.5f);
        arrLocRt.anchorMax = new Vector2(1f, 0.5f);
        arrLocRt.pivot = new Vector2(0.5f, 0.5f);
        arrLocRt.anchoredPosition = new Vector2(0f, 50f);
        arrLocRt.sizeDelta = new Vector2(-60f, 60f);

        var arrLocTmp = arrLocGo.AddComponent<TextMeshProUGUI>();
        arrLocTmp.text = "Room 101";
        arrLocTmp.fontSize = 38;
        arrLocTmp.fontStyle = FontStyles.Bold;
        arrLocTmp.alignment = TextAlignmentOptions.Center;
        arrLocTmp.color = Color.white;

        // Description Text
        GameObject arrDescGo = GetOrCreateUI(arrivedGo.transform, "ArrivedDescText");
        var arrDescRt = arrDescGo.GetComponent<RectTransform>();
        arrDescRt.anchorMin = new Vector2(0f, 0.5f);
        arrDescRt.anchorMax = new Vector2(1f, 0.5f);
        arrDescRt.pivot = new Vector2(0.5f, 0.5f);
        arrDescRt.anchoredPosition = new Vector2(0f, -10f);
        arrDescRt.sizeDelta = new Vector2(-60f, 50f);

        var arrDescTmp = arrDescGo.AddComponent<TextMeshProUGUI>();
        arrDescTmp.text = "Office of Prof Udanor";
        arrDescTmp.fontSize = 26;
        arrDescTmp.alignment = TextAlignmentOptions.Center;
        arrDescTmp.color = new Color(0.8f, 0.85f, 0.95f);

        // Finish Navigation Button
        GameObject finishGo = GetOrCreateUI(arrivedGo.transform, "FinishNavButton");
        var finishRt = finishGo.GetComponent<RectTransform>();
        finishRt.anchorMin = new Vector2(0.5f, 0f);
        finishRt.anchorMax = new Vector2(0.5f, 0f);
        finishRt.pivot = new Vector2(0.5f, 0f);
        finishRt.anchoredPosition = new Vector2(170f, 40f);
        finishRt.sizeDelta = new Vector2(300f, 90f);

        var finishImg = finishGo.AddComponent<Image>();
        finishImg.sprite = button2Sprite;
        finishImg.color = new Color(0.0f, 0.75f, 0.45f, 1.0f); // Vibrant emerald green
        var finishBtn = finishGo.AddComponent<Button>();

        GameObject finishTextGo = GetOrCreateUI(finishGo.transform, "FinishText");
        var fTextRt = finishTextGo.GetComponent<RectTransform>();
        fTextRt.anchorMin = Vector2.zero; fTextRt.anchorMax = Vector2.one; fTextRt.sizeDelta = Vector2.zero;
        var fTmp = finishTextGo.AddComponent<TextMeshProUGUI>();
        fTmp.text = "Finish";
        fTmp.fontSize = 32;
        fTmp.fontStyle = FontStyles.Bold;
        fTmp.alignment = TextAlignmentOptions.Center;
        fTmp.color = Color.white;

        // Dismiss Button
        GameObject dismissGo = GetOrCreateUI(arrivedGo.transform, "DismissArrivedButton");
        var dismissRt = dismissGo.GetComponent<RectTransform>();
        dismissRt.anchorMin = new Vector2(0.5f, 0f);
        dismissRt.anchorMax = new Vector2(0.5f, 0f);
        dismissRt.pivot = new Vector2(0.5f, 0f);
        dismissRt.anchoredPosition = new Vector2(-170f, 40f);
        dismissRt.sizeDelta = new Vector2(260f, 90f);

        var dismissImg = dismissGo.AddComponent<Image>();
        dismissImg.sprite = button2Sprite;
        dismissImg.color = new Color(0.25f, 0.35f, 0.55f, 0.9f);
        var dismissBtn = dismissGo.AddComponent<Button>();

        GameObject disTextGo = GetOrCreateUI(dismissGo.transform, "DismissText");
        var dTextRt = disTextGo.GetComponent<RectTransform>();
        dTextRt.anchorMin = Vector2.zero; dTextRt.anchorMax = Vector2.one; dTextRt.sizeDelta = Vector2.zero;
        var dTmp = disTextGo.AddComponent<TextMeshProUGUI>();
        dTmp.text = "Dismiss";
        dTmp.fontSize = 30;
        dTmp.alignment = TextAlignmentOptions.Center;
        dTmp.color = Color.white;

        arrivedGo.SetActive(false);

        // ═══════════════════════════════════════════════════════
        // 4. WIRE ALL REFERENCES INTO APP UI CONTROLLER
        // ═══════════════════════════════════════════════════════
        appUi.tipsToggleButton = tipsBtnGo;
        appUi.styleToggleButton = styleBtnGo;
        appUi.tipsPopupPanel = tipsPopupGo;
        appUi.closeTipsButton = closeBtn;

        appUi.destinationArrivedPanel = arrivedGo;
        appUi.arrivedLocText = arrLocTmp;
        appUi.arrivedDescText = arrDescTmp;
        appUi.finishNavButton = finishBtn;
        appUi.dismissArrivedButton = dismissBtn;

        EditorUtility.SetDirty(appUi);
        EditorUtility.SetDirty(canvas);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.scene);

        Debug.Log("[Setup] Tips and Arrival UI successfully wired to AppUIController!");
    }
}
