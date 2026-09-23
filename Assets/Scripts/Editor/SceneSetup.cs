using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class SceneSetup
{
    [MenuItem("HoopOut/Build Complete Match Scene")]
    public static void BuildScene()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        cam.orthographic = true;
        cam.orthographicSize = 4f;
        cam.backgroundColor = new Color(0.047f, 0.031f, 0.078f, 1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0, 0, -10);

        GameObject bgObj = GameObject.Find("Environment_Background");
        if (bgObj == null) bgObj = new GameObject("Environment_Background");
        SpriteRenderer bgSr = bgObj.GetComponent<SpriteRenderer>();
        if (bgSr == null) bgSr = bgObj.AddComponent<SpriteRenderer>();

        Sprite envSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/GamePrototype/Environment_Full.png");
        if (envSprite != null)
        {
            bgSr.sprite = envSprite;
            bgSr.sortingOrder = -10;
            bgObj.transform.position = Vector3.zero;
            bgObj.transform.localScale = Vector3.one;
        }

        HoopCamera hoopCam = cam.GetComponent<HoopCamera>();
        if (hoopCam == null) hoopCam = cam.gameObject.AddComponent<HoopCamera>();
        SerializedObject camSo = new SerializedObject(hoopCam);
        camSo.FindProperty("backgroundTransform").objectReferenceValue = bgObj.transform;
        camSo.ApplyModifiedProperties();
        hoopCam.Adjust();

        GameObject bossObj = GameObject.Find("Boss");
        if (bossObj == null) bossObj = new GameObject("Boss");
        bossObj.transform.position = Vector3.zero;
        bossObj.transform.localScale = Vector3.one;

        SpriteRenderer bossSr = bossObj.GetComponent<SpriteRenderer>();
        if (bossSr == null) bossSr = bossObj.AddComponent<SpriteRenderer>();
        bossSr.sortingOrder = 1;

        HitFlash bossFlash = bossObj.GetComponent<HitFlash>();
        if (bossFlash == null) bossFlash = bossObj.AddComponent<HitFlash>();

        BossController bossCtrl = bossObj.GetComponent<BossController>();
        if (bossCtrl == null) bossCtrl = bossObj.AddComponent<BossController>();

        SerializedObject bossSo = new SerializedObject(bossCtrl);
        bossSo.FindProperty("spriteRenderer").objectReferenceValue = bossSr;
        bossSo.FindProperty("hitFlash").objectReferenceValue = bossFlash;
        bossSo.FindProperty("idleSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/GamePrototype/Boss_Idle.png");
        bossSo.FindProperty("fakeSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/GamePrototype/Boss_Windup_Fake.png");
        bossSo.FindProperty("attackSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/GamePrototype/Boss_Windup_Attack.png");
        bossSo.FindProperty("stunnedSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/GamePrototype/Boss_Stunned.png");
        bossSo.ApplyModifiedProperties();

        Sprite bossIdle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/GamePrototype/Boss_Idle.png");
        if (bossIdle != null) bossSr.sprite = bossIdle;

        GameObject playerObj = GameObject.Find("Player");
        if (playerObj == null) playerObj = new GameObject("Player");
        playerObj.transform.position = Vector3.zero;
        playerObj.transform.localScale = Vector3.one;

        SpriteRenderer playerSr = playerObj.GetComponent<SpriteRenderer>();
        if (playerSr == null) playerSr = playerObj.AddComponent<SpriteRenderer>();
        playerSr.sortingOrder = 5;

        HitFlash playerFlash = playerObj.GetComponent<HitFlash>();
        if (playerFlash == null) playerFlash = playerObj.AddComponent<HitFlash>();

        PlayerController playerCtrl = playerObj.GetComponent<PlayerController>();
        if (playerCtrl == null) playerCtrl = playerObj.AddComponent<PlayerController>();

        SerializedObject playerSo = new SerializedObject(playerCtrl);
        playerSo.FindProperty("spriteRenderer").objectReferenceValue = playerSr;
        playerSo.FindProperty("hitFlash").objectReferenceValue = playerFlash;
        playerSo.FindProperty("normalSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/GamePrototype/Player_Back.png");
        playerSo.FindProperty("dodgeLeftSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/GamePrototype/Player_Dodge_Left.png");
        playerSo.FindProperty("dodgeRightSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/GamePrototype/Player_Dodge_Right.png");
        playerSo.FindProperty("chargingSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/GamePrototype/Player_Charging.png");
        playerSo.ApplyModifiedProperties();

        HoopBot hoopBot = playerObj.GetComponent<HoopBot>();
        if (hoopBot == null) hoopBot = playerObj.AddComponent<HoopBot>();
        SerializedObject botSo = new SerializedObject(hoopBot);
        botSo.FindProperty("player").objectReferenceValue = playerCtrl;
        botSo.FindProperty("boss").objectReferenceValue = bossCtrl;
        botSo.ApplyModifiedProperties();

        Sprite playerNormal = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/GamePrototype/Player_Back.png");
        if (playerNormal != null) playerSr.sprite = playerNormal;

        TMP_FontAsset libFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        TMP_FontAsset bungeeFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Bungee-Regular SDF.asset");
        TMP_FontAsset markerFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/PermanentMarker-Regular SDF.asset");
        if (bungeeFont == null) bungeeFont = libFont;
        if (markerFont == null) markerFont = libFont;
        Sprite startBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/GamePrototype/Button_Start_Gradient.png");
        Sprite restartBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/GamePrototype/Button_Restart_Gradient.png");

        UnityEngine.EventSystems.EventSystem es = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            es = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
        else
        {
            var oldModule = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (oldModule != null) Object.DestroyImmediate(oldModule);

            var newModule = es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (newModule == null) es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        if (bungeeFont != null && bungeeFont.material != null)
        {
            bungeeFont.material.SetFloat("_FaceDilate", 0f);
            bungeeFont.material.SetFloat("_Sharpness", 0.15f);
            bungeeFont.material.SetFloat("_OutlineWidth", 0.10f);
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        GameObject canvasObj;
        if (canvas == null)
        {
            canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(450, 800);
            scaler.matchWidthOrHeight = 0f;
            scaler.dynamicPixelsPerUnit = 2f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvasObj = canvas.gameObject;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(450, 800);
                scaler.matchWidthOrHeight = 0f;
                scaler.dynamicPixelsPerUnit = 2f;
            }
        }

        for (int i = canvasObj.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(canvasObj.transform.GetChild(i).gameObject);
        }

        GameObject hudPanel = new GameObject("HUD_Panel", typeof(RectTransform));
        hudPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform hudRt = hudPanel.GetComponent<RectTransform>();
        hudRt.anchorMin = Vector2.zero;
        hudRt.anchorMax = Vector2.one;
        hudRt.offsetMin = Vector2.zero;
        hudRt.offsetMax = Vector2.zero;

        GameObject bannerBorder = new GameObject("TopBannerBorder", typeof(Image));
        bannerBorder.transform.SetParent(hudPanel.transform, false);
        Image bBorderImg = bannerBorder.GetComponent<Image>();
        bBorderImg.color = new Color(1f, 0f, 0.5f, 0.95f);
        RectTransform bBorderRt = bannerBorder.GetComponent<RectTransform>();
        bBorderRt.anchorMin = new Vector2(0.5f, 1f);
        bBorderRt.anchorMax = new Vector2(0.5f, 1f);
        bBorderRt.pivot = new Vector2(0.5f, 1f);
        bBorderRt.anchoredPosition = new Vector2(0, -8);
        bBorderRt.sizeDelta = new Vector2(410, 44);

        GameObject topBanner = new GameObject("TopBanner", typeof(Image));
        topBanner.transform.SetParent(bannerBorder.transform, false);
        Image topImg = topBanner.GetComponent<Image>();
        topImg.color = new Color(0.047f, 0.024f, 0.094f, 0.96f);
        RectTransform bannerRt = topBanner.GetComponent<RectTransform>();
        bannerRt.anchorMin = Vector2.zero;
        bannerRt.anchorMax = Vector2.one;
        bannerRt.offsetMin = new Vector2(2.5f, 2.5f);
        bannerRt.offsetMax = new Vector2(-2.5f, -2.5f);

        GameObject pScoreObj = new GameObject("PlayerScoreText", typeof(TextMeshProUGUI));
        pScoreObj.transform.SetParent(topBanner.transform, false);
        TextMeshProUGUI pScoreTmp = pScoreObj.GetComponent<TextMeshProUGUI>();
        if (libFont != null) pScoreTmp.font = libFont;
        pScoreTmp.fontStyle = FontStyles.Bold;
        pScoreTmp.text = "VOCÊ: 00";
        pScoreTmp.fontSize = 18f;
        pScoreTmp.alignment = TextAlignmentOptions.MidlineLeft;
        pScoreTmp.color = new Color(0f, 0.96f, 1f);
        RectTransform pScoreRt = pScoreObj.GetComponent<RectTransform>();
        pScoreRt.anchorMin = new Vector2(0f, 0f);
        pScoreRt.anchorMax = new Vector2(0.42f, 1f);
        pScoreRt.offsetMin = new Vector2(14, 0);
        pScoreRt.offsetMax = Vector2.zero;

        GameObject timerObj = new GameObject("TimerText", typeof(TextMeshProUGUI));
        timerObj.transform.SetParent(topBanner.transform, false);
        TextMeshProUGUI timerTmp = timerObj.GetComponent<TextMeshProUGUI>();
        if (libFont != null) timerTmp.font = libFont;
        timerTmp.fontStyle = FontStyles.Bold;
        timerTmp.text = "02:00";
        timerTmp.fontSize = 22f;
        timerTmp.alignment = TextAlignmentOptions.Center;
        timerTmp.color = new Color(1f, 0.92f, 0f);
        RectTransform timerRt = timerObj.GetComponent<RectTransform>();
        timerRt.anchorMin = new Vector2(0.38f, 0f);
        timerRt.anchorMax = new Vector2(0.62f, 1f);
        timerRt.offsetMin = Vector2.zero;
        timerRt.offsetMax = Vector2.zero;

        GameObject bScoreObj = new GameObject("BossScoreText", typeof(TextMeshProUGUI));
        bScoreObj.transform.SetParent(topBanner.transform, false);
        TextMeshProUGUI bScoreTmp = bScoreObj.GetComponent<TextMeshProUGUI>();
        if (libFont != null) bScoreTmp.font = libFont;
        bScoreTmp.fontStyle = FontStyles.Bold;
        bScoreTmp.text = "TIJOLO: 00";
        bScoreTmp.fontSize = 18f;
        bScoreTmp.alignment = TextAlignmentOptions.MidlineRight;
        bScoreTmp.color = new Color(1f, 0.28f, 0.55f);
        RectTransform bScoreRt = bScoreObj.GetComponent<RectTransform>();
        bScoreRt.anchorMin = new Vector2(0.58f, 0f);
        bScoreRt.anchorMax = new Vector2(1f, 1f);
        bScoreRt.offsetMin = Vector2.zero;
        bScoreRt.offsetMax = new Vector2(-14, 0);

        GameObject comboObj = new GameObject("ComboText", typeof(TextMeshProUGUI));
        comboObj.transform.SetParent(hudPanel.transform, false);
        TextMeshProUGUI comboTmp = comboObj.GetComponent<TextMeshProUGUI>();
        if (bungeeFont != null) comboTmp.font = bungeeFont;
        comboTmp.text = "COMBO x2!";
        comboTmp.fontSize = 26;
        comboTmp.alignment = TextAlignmentOptions.Center;
        comboTmp.color = new Color(1f, 0.2f, 0.8f);
        RectTransform comboRt = comboObj.GetComponent<RectTransform>();
        comboRt.anchorMin = new Vector2(0.5f, 1f);
        comboRt.anchorMax = new Vector2(0.5f, 1f);
        comboRt.anchoredPosition = new Vector2(0, -78);
        comboRt.sizeDelta = new Vector2(320, 36);
        comboObj.SetActive(false);

        GameObject feedbackObj = new GameObject("FeedbackText", typeof(TextMeshProUGUI));
        feedbackObj.transform.SetParent(hudPanel.transform, false);
        TextMeshProUGUI feedbackTmp = feedbackObj.GetComponent<TextMeshProUGUI>();
        if (bungeeFont != null) feedbackTmp.font = bungeeFont;
        feedbackTmp.text = "";
        feedbackTmp.fontSize = 20;
        feedbackTmp.enableAutoSizing = true;
        feedbackTmp.fontSizeMin = 13f;
        feedbackTmp.fontSizeMax = 20f;
        feedbackTmp.alignment = TextAlignmentOptions.Center;
        feedbackTmp.color = Color.yellow;
        RectTransform feedbackRt = feedbackObj.GetComponent<RectTransform>();
        feedbackRt.anchorMin = new Vector2(0.5f, 0.5f);
        feedbackRt.anchorMax = new Vector2(0.5f, 0.5f);
        feedbackRt.anchoredPosition = new Vector2(0, 115);
        feedbackRt.sizeDelta = new Vector2(380, 44);

        GameObject sliderObj = new GameObject("AttackTimingSlider", typeof(Slider));
        sliderObj.transform.SetParent(hudPanel.transform, false);
        Slider slider = sliderObj.GetComponent<Slider>();
        RectTransform sliderRt = sliderObj.GetComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0.5f, 1f);
        sliderRt.anchorMax = new Vector2(0.5f, 1f);
        sliderRt.pivot = new Vector2(0.5f, 0.5f);
        sliderRt.anchoredPosition = new Vector2(0, -62);
        sliderRt.sizeDelta = new Vector2(210, 13);

        GameObject sliderBgBorder = new GameObject("Border", typeof(Image));
        sliderBgBorder.transform.SetParent(sliderObj.transform, false);
        Image sbbImg = sliderBgBorder.GetComponent<Image>();
        sbbImg.color = new Color(0f, 0.96f, 1f, 0.95f);
        RectTransform sbbRt = sliderBgBorder.GetComponent<RectTransform>();
        sbbRt.anchorMin = Vector2.zero;
        sbbRt.anchorMax = Vector2.one;
        sbbRt.offsetMin = Vector2.zero;
        sbbRt.offsetMax = Vector2.zero;
        sbbRt.sizeDelta = Vector2.zero;

        GameObject sliderBg = new GameObject("Background", typeof(Image));
        sliderBg.transform.SetParent(sliderBgBorder.transform, false);
        Image sliderBgImg = sliderBg.GetComponent<Image>();
        sliderBgImg.color = new Color(0.08f, 0.04f, 0.16f, 1f);
        RectTransform sBgRt = sliderBg.GetComponent<RectTransform>();
        sBgRt.anchorMin = Vector2.zero;
        sBgRt.anchorMax = Vector2.one;
        sBgRt.offsetMin = new Vector2(1.5f, 1.5f);
        sBgRt.offsetMax = new Vector2(-1.5f, -1.5f);
        sBgRt.sizeDelta = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderBg.transform, false);
        RectTransform fillAreaRt = fillArea.GetComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.offsetMin = Vector2.zero;
        fillAreaRt.offsetMax = Vector2.zero;
        fillAreaRt.sizeDelta = Vector2.zero;

        GameObject fill = new GameObject("Fill", typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImg = fill.GetComponent<Image>();
        fillImg.color = new Color(1f, 0.85f, 0f, 1f);
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        fillRt.sizeDelta = Vector2.zero;

        slider.fillRect = fillRt;
        slider.targetGraphic = fillImg;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        GameObject sliderLabel = new GameObject("SliderLabel", typeof(TextMeshProUGUI));
        sliderLabel.transform.SetParent(sliderObj.transform, false);
        TextMeshProUGUI sLabelTmp = sliderLabel.GetComponent<TextMeshProUGUI>();
        if (libFont != null) sLabelTmp.font = libFont;
        sLabelTmp.fontStyle = FontStyles.Bold;
        sLabelTmp.text = "JANELA DE STUN";
        sLabelTmp.fontSize = 10f;
        sLabelTmp.alignment = TextAlignmentOptions.Center;
        sLabelTmp.color = new Color(0.04f, 0.02f, 0.1f, 1f);
        RectTransform sLabelRt = sliderLabel.GetComponent<RectTransform>();
        sLabelRt.anchorMin = Vector2.zero;
        sLabelRt.anchorMax = Vector2.one;
        sLabelRt.pivot = new Vector2(0.5f, 0.5f);
        sLabelRt.anchoredPosition = Vector2.zero;
        sLabelRt.sizeDelta = Vector2.zero;
        sliderLabel.transform.SetAsLastSibling();

        sliderObj.SetActive(false);

        GameObject touchBar = new GameObject("TouchIndicatorBar", typeof(RectTransform));
        touchBar.transform.SetParent(hudPanel.transform, false);
        RectTransform tbRt = touchBar.GetComponent<RectTransform>();
        tbRt.anchorMin = new Vector2(0.5f, 0f);
        tbRt.anchorMax = new Vector2(0.5f, 0f);
        tbRt.pivot = new Vector2(0.5f, 0f);
        tbRt.anchoredPosition = new Vector2(0, 14);
        tbRt.sizeDelta = new Vector2(406, 54);

        string[] chipTitles = new string[] { "ARRASTE ←", "TOQUE", "SEGURE", "ARRASTE →" };
        string[] chipActions = new string[] { "DESVIAR", "ARREMESSO", "ENTERRADA", "DESVIAR" };
        Color[] chipTitleColors = new Color[]
        {
            new Color(0f, 0.96f, 1f),
            new Color(1f, 0.92f, 0f),
            new Color(1f, 0.45f, 0.75f),
            new Color(0f, 0.96f, 1f)
        };
        Color[] chipBorderColors = new Color[]
        {
            new Color(0f, 0.96f, 1f),
            new Color(1f, 0.92f, 0f),
            new Color(1f, 0.2f, 0.5f),
            new Color(0f, 0.96f, 1f)
        };

        float chipWidth = 94f;
        float chipSpacing = 10f;
        for (int c = 0; c < 4; c++)
        {
            GameObject chipBorder = new GameObject("Chip_" + c, typeof(Image));
            chipBorder.transform.SetParent(touchBar.transform, false);
            Image cBorderImg = chipBorder.GetComponent<Image>();
            cBorderImg.color = chipBorderColors[c];
            RectTransform cbRt = chipBorder.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0f, 0f);
            cbRt.anchorMax = new Vector2(0f, 1f);
            cbRt.pivot = new Vector2(0f, 0.5f);
            cbRt.anchoredPosition = new Vector2(c * (chipWidth + chipSpacing), 0f);
            cbRt.sizeDelta = new Vector2(chipWidth, 0);

            GameObject chipInner = new GameObject("Inner", typeof(Image));
            chipInner.transform.SetParent(chipBorder.transform, false);
            Image cInnerImg = chipInner.GetComponent<Image>();
            cInnerImg.color = new Color(0.04f, 0.02f, 0.09f, 0.96f);
            RectTransform ciRt = chipInner.GetComponent<RectTransform>();
            ciRt.anchorMin = Vector2.zero;
            ciRt.anchorMax = Vector2.one;
            ciRt.offsetMin = new Vector2(2f, 2f);
            ciRt.offsetMax = new Vector2(-2f, -2f);

            GameObject chipTextObj = new GameObject("Text", typeof(TextMeshProUGUI));
            chipTextObj.transform.SetParent(chipInner.transform, false);
            TextMeshProUGUI ctTmp = chipTextObj.GetComponent<TextMeshProUGUI>();
            if (libFont != null) ctTmp.font = libFont;
            ctTmp.text = $"<b>{chipTitles[c]}</b>\n<size=10><color=#FFFFFF>{chipActions[c]}</color></size>";
            ctTmp.fontSize = 12.5f;
            ctTmp.lineSpacing = 3f;
            ctTmp.alignment = TextAlignmentOptions.Center;
            ctTmp.color = chipTitleColors[c];
            RectTransform ctRt = chipTextObj.GetComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero;
            ctRt.anchorMax = Vector2.one;
            ctRt.offsetMin = Vector2.zero;
            ctRt.offsetMax = Vector2.zero;
        }

        GameObject startPanel = new GameObject("Start_Panel", typeof(Image));
        startPanel.transform.SetParent(canvasObj.transform, false);
        Image startPanelImg = startPanel.GetComponent<Image>();
        startPanelImg.color = new Color(0.035f, 0.018f, 0.075f, 0.88f);
        RectTransform startPanelRt = startPanel.GetComponent<RectTransform>();
        startPanelRt.anchorMin = Vector2.zero;
        startPanelRt.anchorMax = Vector2.one;
        startPanelRt.offsetMin = Vector2.zero;
        startPanelRt.offsetMax = Vector2.zero;

        GameObject badgeBorder = new GameObject("StreetBadgeBorder", typeof(Image));
        badgeBorder.transform.SetParent(startPanel.transform, false);
        Image bgbImg = badgeBorder.GetComponent<Image>();
        bgbImg.color = new Color(0f, 0.96f, 1f, 0.85f);
        RectTransform bgbRt = badgeBorder.GetComponent<RectTransform>();
        bgbRt.anchorMin = new Vector2(0.5f, 1f);
        bgbRt.anchorMax = new Vector2(0.5f, 1f);
        bgbRt.pivot = new Vector2(0.5f, 1f);
        bgbRt.anchoredPosition = new Vector2(0, -46);
        bgbRt.sizeDelta = new Vector2(310, 32);

        GameObject badgeInner = new GameObject("Inner", typeof(Image));
        badgeInner.transform.SetParent(badgeBorder.transform, false);
        Image bgiImg = badgeInner.GetComponent<Image>();
        bgiImg.color = new Color(0.02f, 0.12f, 0.2f, 0.95f);
        RectTransform bgiRt = badgeInner.GetComponent<RectTransform>();
        bgiRt.anchorMin = Vector2.zero;
        bgiRt.anchorMax = Vector2.one;
        bgiRt.offsetMin = new Vector2(2f, 2f);
        bgiRt.offsetMax = new Vector2(-2f, -2f);

        GameObject badgeText = new GameObject("Text", typeof(TextMeshProUGUI));
        badgeText.transform.SetParent(badgeInner.transform, false);
        TextMeshProUGUI bTxt = badgeText.GetComponent<TextMeshProUGUI>();
        if (libFont != null) bTxt.font = libFont;
        bTxt.text = "BASQUETE DE RUA RÍTMICO";
        bTxt.fontStyle = FontStyles.Bold;
        bTxt.fontSize = 12.5f;
        bTxt.alignment = TextAlignmentOptions.Center;
        bTxt.color = new Color(0f, 0.96f, 1f);
        RectTransform btRt = badgeText.GetComponent<RectTransform>();
        btRt.anchorMin = Vector2.zero;
        btRt.anchorMax = Vector2.one;
        btRt.offsetMin = Vector2.zero;
        btRt.offsetMax = Vector2.zero;

        GameObject sTitleShadow = new GameObject("TitleShadow", typeof(TextMeshProUGUI));
        sTitleShadow.transform.SetParent(startPanel.transform, false);
        TextMeshProUGUI sTsTmp = sTitleShadow.GetComponent<TextMeshProUGUI>();
        if (bungeeFont != null) sTsTmp.font = bungeeFont;
        sTsTmp.text = "HOOP-OUT!!";
        sTsTmp.fontSize = 52;
        sTsTmp.alignment = TextAlignmentOptions.Center;
        sTsTmp.color = new Color(1f, 0f, 0.5f, 0.95f);
        RectTransform sTsRt = sTitleShadow.GetComponent<RectTransform>();
        sTsRt.anchorMin = new Vector2(0.5f, 1f);
        sTsRt.anchorMax = new Vector2(0.5f, 1f);
        sTsRt.pivot = new Vector2(0.5f, 1f);
        sTsRt.anchoredPosition = new Vector2(3, -90);
        sTsRt.sizeDelta = new Vector2(400, 70);

        GameObject sTitle = new GameObject("Title", typeof(TextMeshProUGUI));
        sTitle.transform.SetParent(startPanel.transform, false);
        TextMeshProUGUI sTitleTmp = sTitle.GetComponent<TextMeshProUGUI>();
        if (bungeeFont != null) sTitleTmp.font = bungeeFont;
        sTitleTmp.text = "HOOP-OUT!!";
        sTitleTmp.fontSize = 52;
        sTitleTmp.alignment = TextAlignmentOptions.Center;
        sTitleTmp.color = new Color(1f, 0.9f, 0f);
        RectTransform sTitleRt = sTitle.GetComponent<RectTransform>();
        sTitleRt.anchorMin = new Vector2(0.5f, 1f);
        sTitleRt.anchorMax = new Vector2(0.5f, 1f);
        sTitleRt.pivot = new Vector2(0.5f, 1f);
        sTitleRt.anchoredPosition = new Vector2(0, -87);
        sTitleRt.sizeDelta = new Vector2(400, 70);

        NeonPulse np = sTitle.AddComponent<NeonPulse>();
        SerializedObject npSo = new SerializedObject(np);
        npSo.FindProperty("shadowTransform").objectReferenceValue = sTsRt;
        npSo.ApplyModifiedProperties();

        GameObject sSub = new GameObject("Subtitle", typeof(TextMeshProUGUI));
        sSub.transform.SetParent(startPanel.transform, false);
        TextMeshProUGUI sSubTmp = sSub.GetComponent<TextMeshProUGUI>();
        if (libFont != null) sSubTmp.font = libFont;
        sSubTmp.text = "DOMINE O ASFALTO";
        sSubTmp.fontStyle = FontStyles.Bold;
        sSubTmp.fontSize = 18;
        sSubTmp.alignment = TextAlignmentOptions.Center;
        sSubTmp.color = new Color(0f, 0.96f, 1f, 0.95f);
        RectTransform sSubRt = sSub.GetComponent<RectTransform>();
        sSubRt.anchorMin = new Vector2(0.5f, 1f);
        sSubRt.anchorMax = new Vector2(0.5f, 1f);
        sSubRt.pivot = new Vector2(0.5f, 1f);
        sSubRt.anchoredPosition = new Vector2(0, -162);
        sSubRt.sizeDelta = new Vector2(380, 32);

        GameObject startBtnObj = new GameObject("StartButton", typeof(Image), typeof(Button));
        startBtnObj.transform.SetParent(startPanel.transform, false);
        Image startBtnImg = startBtnObj.GetComponent<Image>();
        if (startBtnSprite != null)
        {
            startBtnImg.sprite = startBtnSprite;
            startBtnImg.color = Color.white;
        }
        Button startBtn = startBtnObj.GetComponent<Button>();
        RectTransform btnRt = startBtnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.22f);
        btnRt.anchorMax = new Vector2(0.5f, 0.22f);
        btnRt.sizeDelta = new Vector2(390, 66);
        startBtn.targetGraphic = startBtnImg;
        ColorBlock sbColors = startBtn.colors;
        sbColors.normalColor = Color.white;
        sbColors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        sbColors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        startBtn.colors = sbColors;

        GameObject goPanel = new GameObject("GameOver_Panel", typeof(Image));
        goPanel.transform.SetParent(canvasObj.transform, false);
        Image goPanelImg = goPanel.GetComponent<Image>();
        goPanelImg.color = new Color(0.035f, 0.018f, 0.075f, 0.92f);
        RectTransform goPanelRt = goPanel.GetComponent<RectTransform>();
        goPanelRt.anchorMin = Vector2.zero;
        goPanelRt.anchorMax = Vector2.one;
        goPanelRt.offsetMin = Vector2.zero;
        goPanelRt.offsetMax = Vector2.zero;

        GameObject goTitleShadow = new GameObject("TitleShadow", typeof(TextMeshProUGUI));
        goTitleShadow.transform.SetParent(goPanel.transform, false);
        TextMeshProUGUI goTsTmp = goTitleShadow.GetComponent<TextMeshProUGUI>();
        if (bungeeFont != null) goTsTmp.font = bungeeFont;
        goTsTmp.text = "VITÓRIA!";
        goTsTmp.fontSize = 48;
        goTsTmp.alignment = TextAlignmentOptions.Center;
        goTsTmp.color = new Color(0.03f, 0.01f, 0.07f, 1f);
        RectTransform goTsRt = goTitleShadow.GetComponent<RectTransform>();
        goTsRt.anchorMin = new Vector2(0.5f, 0.78f);
        goTsRt.anchorMax = new Vector2(0.5f, 0.78f);
        goTsRt.anchoredPosition = new Vector2(2, -3);
        goTsRt.sizeDelta = new Vector2(380, 64);

        GameObject goTitle = new GameObject("Title", typeof(TextMeshProUGUI));
        goTitle.transform.SetParent(goPanel.transform, false);
        TextMeshProUGUI goTitleTmp = goTitle.GetComponent<TextMeshProUGUI>();
        if (bungeeFont != null) goTitleTmp.font = bungeeFont;
        goTitleTmp.text = "VITÓRIA!";
        goTitleTmp.fontSize = 48;
        goTitleTmp.alignment = TextAlignmentOptions.Center;
        goTitleTmp.color = new Color(1f, 0.9f, 0f);
        RectTransform goTitleRt = goTitle.GetComponent<RectTransform>();
        goTitleRt.anchorMin = new Vector2(0.5f, 0.78f);
        goTitleRt.anchorMax = new Vector2(0.5f, 0.78f);
        goTitleRt.sizeDelta = new Vector2(380, 64);

        NeonPulse goNp = goTitle.AddComponent<NeonPulse>();
        SerializedObject goNpSo = new SerializedObject(goNp);
        goNpSo.FindProperty("shadowTransform").objectReferenceValue = goTsRt;
        goNpSo.ApplyModifiedProperties();

        GameObject goSub = new GameObject("Subtitle", typeof(TextMeshProUGUI));
        goSub.transform.SetParent(goPanel.transform, false);
        TextMeshProUGUI goSubTmp = goSub.GetComponent<TextMeshProUGUI>();
        if (libFont != null) goSubTmp.font = libFont;
        goSubTmp.text = "Você quebrou os tornozelos do Tijolo!";
        goSubTmp.fontStyle = FontStyles.Bold;
        goSubTmp.fontSize = 16;
        goSubTmp.alignment = TextAlignmentOptions.Center;
        goSubTmp.color = new Color(0f, 0.94f, 1f);
        RectTransform goSubRt = goSub.GetComponent<RectTransform>();
        goSubRt.anchorMin = new Vector2(0.5f, 0.71f);
        goSubRt.anchorMax = new Vector2(0.5f, 0.71f);
        goSubRt.sizeDelta = new Vector2(380, 30);

        GameObject scBorder = new GameObject("ScoreCardBorder", typeof(Image));
        scBorder.transform.SetParent(goPanel.transform, false);
        Image scbImg = scBorder.GetComponent<Image>();
        scbImg.color = new Color(1f, 0f, 0.5f, 0.9f);
        RectTransform scbRt = scBorder.GetComponent<RectTransform>();
        scbRt.anchorMin = new Vector2(0.5f, 0.48f);
        scbRt.anchorMax = new Vector2(0.5f, 0.48f);
        scbRt.sizeDelta = new Vector2(370, 150);

        GameObject goScoreCard = new GameObject("ScoreCard", typeof(Image));
        goScoreCard.transform.SetParent(scBorder.transform, false);
        Image scImg = goScoreCard.GetComponent<Image>();
        scImg.color = new Color(0.06f, 0.03f, 0.14f, 0.96f);
        RectTransform scRt = goScoreCard.GetComponent<RectTransform>();
        scRt.anchorMin = Vector2.zero;
        scRt.anchorMax = Vector2.one;
        scRt.offsetMin = new Vector2(2f, 2f);
        scRt.offsetMax = new Vector2(-2f, -2f);

        GameObject goScoreHeader = new GameObject("Header", typeof(TextMeshProUGUI));
        goScoreHeader.transform.SetParent(goScoreCard.transform, false);
        TextMeshProUGUI gshTmp = goScoreHeader.GetComponent<TextMeshProUGUI>();
        if (libFont != null) gshTmp.font = libFont;
        gshTmp.text = "PLACAR FINAL";
        gshTmp.fontStyle = FontStyles.Bold;
        gshTmp.fontSize = 14;
        gshTmp.alignment = TextAlignmentOptions.Center;
        gshTmp.color = new Color(0f, 0.96f, 1f);
        RectTransform gshRt = goScoreHeader.GetComponent<RectTransform>();
        gshRt.anchorMin = new Vector2(0.5f, 1f);
        gshRt.anchorMax = new Vector2(0.5f, 1f);
        gshRt.pivot = new Vector2(0.5f, 1f);
        gshRt.anchoredPosition = new Vector2(0, -12);
        gshRt.sizeDelta = new Vector2(350, 22);

        GameObject goScore = new GameObject("FinalScore", typeof(TextMeshProUGUI));
        goScore.transform.SetParent(goScoreCard.transform, false);
        TextMeshProUGUI goScoreTmp = goScore.GetComponent<TextMeshProUGUI>();
        if (libFont != null) goScoreTmp.font = libFont;
        goScoreTmp.text = "VOCÊ 15  x  08 O TIJOLO\nMaior Combo: x3";
        goScoreTmp.fontStyle = FontStyles.Bold;
        goScoreTmp.fontSize = 19;
        goScoreTmp.lineSpacing = 8;
        goScoreTmp.alignment = TextAlignmentOptions.Center;
        goScoreTmp.color = Color.white;
        RectTransform goScoreRt = goScore.GetComponent<RectTransform>();
        goScoreRt.anchorMin = Vector2.zero;
        goScoreRt.anchorMax = Vector2.one;
        goScoreRt.offsetMin = new Vector2(10, 10);
        goScoreRt.offsetMax = new Vector2(-10, -35);

        GameObject restartBtnObj = new GameObject("RestartButton", typeof(Image), typeof(Button));
        restartBtnObj.transform.SetParent(goPanel.transform, false);
        Image restartBtnImg = restartBtnObj.GetComponent<Image>();
        if (restartBtnSprite != null)
        {
            restartBtnImg.sprite = restartBtnSprite;
            restartBtnImg.color = Color.white;
        }
        Button restartBtn = restartBtnObj.GetComponent<Button>();
        RectTransform rBtnRt = restartBtnObj.GetComponent<RectTransform>();
        rBtnRt.anchorMin = new Vector2(0.5f, 0.20f);
        rBtnRt.anchorMax = new Vector2(0.5f, 0.20f);
        rBtnRt.sizeDelta = new Vector2(390, 66);
        restartBtn.targetGraphic = restartBtnImg;
        ColorBlock rbColors = restartBtn.colors;
        rbColors.normalColor = Color.white;
        rbColors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        rbColors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        restartBtn.colors = rbColors;

        goPanel.SetActive(false);

        GameObject audioObj = GameObject.Find("HoopAudio");
        if (audioObj == null) audioObj = new GameObject("HoopAudio");
        HoopAudio hoopAudio = audioObj.GetComponent<HoopAudio>();
        if (hoopAudio == null) hoopAudio = audioObj.AddComponent<HoopAudio>();

        AudioSource[] sources = audioObj.GetComponents<AudioSource>();
        AudioSource sfxSrc = sources.Length > 0 ? sources[0] : audioObj.AddComponent<AudioSource>();
        AudioSource bgmSrc = sources.Length > 1 ? sources[1] : audioObj.AddComponent<AudioSource>();
        AudioSource crowdSrc = sources.Length > 2 ? sources[2] : audioObj.AddComponent<AudioSource>();
        bgmSrc.loop = true;

        SerializedObject audioSo = new SerializedObject(hoopAudio);
        audioSo.FindProperty("sfxSource").objectReferenceValue = sfxSrc;
        audioSo.FindProperty("bgmSource").objectReferenceValue = bgmSrc;
        audioSo.FindProperty("crowdSource").objectReferenceValue = crowdSrc;

        audioSo.FindProperty("dribbleClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_dribble.wav");
        audioSo.FindProperty("squeakClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_squeak.wav");
        audioSo.FindProperty("swishClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_swish.wav");
        audioSo.FindProperty("slamClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_slam.wav");
        audioSo.FindProperty("whistleClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_whistle.wav");
        audioSo.FindProperty("dodgeClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_dodge.wav");
        audioSo.FindProperty("cheerClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_crowd_cheer.wav");
        audioSo.FindProperty("crowdOohClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_crowd_ooh.wav");
        audioSo.FindProperty("defenseChantClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_defense_chant.wav");
        audioSo.FindProperty("streetballBgmClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/bgm_streetball_beat.wav");
        audioSo.ApplyModifiedProperties();

        GameObject speedLinesObj = new GameObject("SpeedLines_Overlay", typeof(RectTransform), typeof(CanvasGroup), typeof(SpeedLinesEffect));
        speedLinesObj.transform.SetParent(canvasObj.transform, false);
        RectTransform slRt = speedLinesObj.GetComponent<RectTransform>();
        slRt.anchorMin = Vector2.zero;
        slRt.anchorMax = Vector2.one;
        slRt.offsetMin = Vector2.zero;
        slRt.offsetMax = Vector2.zero;
        speedLinesObj.transform.SetSiblingIndex(hudPanel.transform.GetSiblingIndex() + 1);
        SpeedLinesEffect speedLinesEff = speedLinesObj.GetComponent<SpeedLinesEffect>();

        GameObject vfxObj = GameObject.Find("VFXManager");
        if (vfxObj == null) vfxObj = new GameObject("VFXManager");
        VFXManager vfxMgr = vfxObj.GetComponent<VFXManager>();
        if (vfxMgr == null) vfxMgr = vfxObj.AddComponent<VFXManager>();
        SerializedObject vfxSo = new SerializedObject(vfxMgr);
        vfxSo.FindProperty("fontAsset").objectReferenceValue = bungeeFont;
        vfxSo.FindProperty("speedLines").objectReferenceValue = speedLinesEff;
        vfxSo.ApplyModifiedProperties();

        GameObject mgrObj = GameObject.Find("MatchManager");
        if (mgrObj == null) mgrObj = new GameObject("MatchManager");

        MatchManager mgr = mgrObj.GetComponent<MatchManager>();
        if (mgr == null) mgr = mgrObj.AddComponent<MatchManager>();

        SerializedObject mgrSo = new SerializedObject(mgr);
        mgrSo.FindProperty("boss").objectReferenceValue = bossCtrl;
        mgrSo.FindProperty("player").objectReferenceValue = playerCtrl;
        mgrSo.FindProperty("scoreText").objectReferenceValue = pScoreTmp;
        mgrSo.FindProperty("bossScoreText").objectReferenceValue = bScoreTmp;
        mgrSo.FindProperty("timerText").objectReferenceValue = timerTmp;
        mgrSo.FindProperty("comboText").objectReferenceValue = comboTmp;
        mgrSo.FindProperty("feedbackText").objectReferenceValue = feedbackTmp;
        mgrSo.FindProperty("attackTimingSlider").objectReferenceValue = slider;
        mgrSo.FindProperty("hudPanel").objectReferenceValue = hudPanel;
        mgrSo.FindProperty("startMenuPanel").objectReferenceValue = startPanel;
        mgrSo.FindProperty("gameOverPanel").objectReferenceValue = goPanel;
        mgrSo.FindProperty("gameOverTitleText").objectReferenceValue = goTitleTmp;
        mgrSo.FindProperty("gameOverTitleShadowText").objectReferenceValue = goTsTmp;
        mgrSo.FindProperty("gameOverSubtitleText").objectReferenceValue = goSubTmp;
        mgrSo.FindProperty("gameOverScoreText").objectReferenceValue = goScoreTmp;
        mgrSo.FindProperty("startButton").objectReferenceValue = startBtn;
        mgrSo.FindProperty("restartButton").objectReferenceValue = restartBtn;
        mgrSo.ApplyModifiedProperties();

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
    }
}
