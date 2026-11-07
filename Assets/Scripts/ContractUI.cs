using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using UIImage = UnityEngine.UI.Image;
using Object = UnityEngine.Object;

public class ContractUI : MonoBehaviour
{
    private static readonly Color Ink = new Color(.075f, .11f, .15f);
    private static readonly Color Tile = new Color(.085f, .13f, .175f);
    private static readonly Color Muted = new Color(.62f, .71f, .75f);
    private static readonly Color Accent = new Color(1f, .68f, .26f);
    private static readonly Color Mint = new Color(.36f, .86f, .72f);
    private static readonly Color Line = new Color(.19f, .27f, .32f);
    private static readonly Color White = new Color(.96f, .97f, .95f);
    private static readonly Color Danger = new Color(1f, .38f, .32f);
    private static readonly Vector2 TopLeft = new Vector2(0, 1);

    private ContractGame game;
    private PlayerHealth health;
    private PlayerWeapons weapons;
    private Canvas canvas;
    private TMP_FontAsset font;
    private Texture2D whiteTexture;
    private Sprite whiteSprite;

    private Sprite spritePanel, spritePanelInset, spriteWindow;
    private Sprite spriteBtnNormal, spriteBtnPressed, spriteFocus;
    private Sprite spriteProgressBg, spriteBarGreen, spriteBarFillRed, spriteBarFillGold;
    private Sprite spriteHeart, spriteInfo, spriteCoin, spriteStar;
    private Sprite spriteIconPistol, spriteIconRifle, spriteIconBat, spriteIconPotion, spriteIconBomb;

    private GameObject hud, menu, shop, summary, boardBody, settingsPanel, aboutDialog, upgradesGroup, suppliesGroup;
    private bool boardSettingsOpen, showingSuppliesTab, lastTabSupplies;
    public bool IsAboutOpen => aboutDialog != null && aboutDialog.activeSelf;

    private Slider masterSlider, sfxSlider, sensSlider, fovSlider;
    private TMP_Text masterValText, sfxValText, sensValText, fovValText, invertYText, qualityText, fullscreenText, pauseHeaderBadge;
    private UIImage invertYBg;
    private Button settingsBoardBtn, aboutBoardBtn, settingsAboutBoard, settingsAboutPause;
    private Button settingsResume, settingsRetry, settingsBoard, settingsQuit, settingsClose, settingsReset;

    private RectTransform shell, objectivePanel, slotStrip, healthPanel, ammoPanel, exitPanel, hudWalletPanel;
    private TMP_Text hudWalletValue, hudCoinPopup;
    private float coinPopupTimer;
    private int pendingPopupAmount;
    private TMP_Text objectiveTitle, objectiveBody, healthValue, ammoValue, reserveValue, weaponName, ammoHint, exitText, hotbarTitle;
    private TMP_Text wallet, completed, eyebrow, missionTitle, briefing, rewardCaption, rewardValue, rewardHint;
    private TMP_Text currentGear, summaryMission, summaryGear, damageValue, damagePrice, magazineValue, magazinePrice;
    private TMP_Text shopTabUpgradeText, shopTabSupplyText, map1BtnText, map2BtnText;
    private Button shopTabUpgradeBtn, shopTabSupplyBtn, map1Btn, map2Btn;
    private Button buyPotionBtn, buyGrenadeBtn, buyPistolAmmoBtn, buyRifleAmmoBtn;
    private TMP_Text potionCountText, potionPriceText, grenadeCountText, grenadePriceText;
    private TMP_Text pistolAmmoCountText, pistolAmmoPriceText, rifleAmmoCountText, rifleAmmoPriceText;
    private Image healthFill, healthChip, missionStripe;
    private Button start, resume, retry, pausedRetry, board, damageUpgrade, magazineUpgrade;
    private Image[] slotTiles, slotMarks, slotIcons;
    private TMP_Text[] slotNumbers, slotCounts;
    private ContractGame.Phase menuPhase = (ContractGame.Phase)(-1);
    private int selectedMapIndex, lastSelectedMap = -1;
    private int lastCredits = -1, lastDamage = -1, lastMagazine = -1, lastCompleted = -1;
    private int lastPotionCount = -1, lastGrenadeCount = -1, lastPistolAmmo = -1, lastRifleAmmo = -1;

    public void Initialize(ContractGame owner, PlayerHealth player)
    {
        game = owner; health = player; weapons = player != null ? player.GetComponent<PlayerWeapons>() : null;
        if (game != null) game.CreditsAdded += OnCreditsAdded;
        if (weapons != null) weapons.StatusMessage += OnStatusMessage;
        font = Resources.Load<TMP_FontAsset>("GunQuestUIFont") ?? TMP_Settings.defaultFontAsset;
        whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        whiteTexture.SetPixel(0, 0, Color.white); whiteTexture.Apply();
        whiteSprite = Sprite.Create(whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f);

        LoadPixelSprites();
        EnsureUIInput();
        selectedMapIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "IceboundPass" ? 1 : 0;

        var root = new GameObject("ContractCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 200; canvas.pixelPerfect = true;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 1;
        BuildHUD(root.transform);
        BuildMenu(root.transform);
        Refresh();
    }

    private void LoadPixelSprites()
    {
        spritePanel = LoadSprite("panel");
        spritePanelInset = LoadSprite("panel_inset");
        spriteBtnNormal = LoadSprite("button_normal");
        spriteBtnPressed = LoadSprite("button_pressed");
        spriteFocus = LoadSprite("focus");
        spriteProgressBg = LoadSprite("progress_bg");
        spriteBarGreen = LoadSprite("bar_green");
        spriteBarFillRed = LoadSprite("bar_fill_red");
        spriteBarFillGold = LoadSprite("bar_fill_gold");
        if (spriteBarFillRed == whiteSprite) spriteBarFillRed = LoadSprite("bar_red");
        if (spriteBarFillGold == whiteSprite) spriteBarFillGold = LoadSprite("bar_gold");
        spriteHeart = LoadSprite("heart");
        spriteInfo = LoadSprite("info");
        spriteCoin = LoadSprite("coin");
        spriteStar = LoadSprite("star");
        spriteWindow = LoadSprite("window");
        spriteIconPistol = LoadSprite("icon_pistol");
        spriteIconRifle = LoadSprite("icon_rifle");
        spriteIconBat = LoadSprite("icon_bat");
        spriteIconPotion = LoadSprite("icon_potion");
        spriteIconBomb = LoadSprite("icon_bomb");
    }

    private Sprite LoadSprite(string path) => Resources.Load<Sprite>("PixelUI/" + path) ?? whiteSprite;

    private void BuildMenu(Transform root)
    {
        menu = Group("ContractMenu", root);
        var backdrop = Image(menu.transform, "Backdrop", new Color(.018f, .035f, .055f, .85f), Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch(backdrop.rectTransform);
        shell = Rect("MenuLayout", menu.transform, new Vector2(-560, 320), new Vector2(1120, 640), Vector2.one * .5f);

        Label(shell, "Brand", "GUNQUEST", new Vector2(0, -2), new Vector2(420, 56), 44, White, true);
        Label(shell, "BrandCaption", "TRẠM NHIỆM VỤ  /  SẴN SÀNG LÊN ĐƯỜNG", new Vector2(3, -61), new Vector2(460, 28), 16, Mint);

        var funds = SlicedImage(shell, "Funds", spritePanel, Color.white, new Vector2(490, 0), new Vector2(230, 80), TopLeft, 0.5f);
        IconImage(funds.transform, "CoinIcon", spriteCoin, Color.white, new Vector2(14, -26), new Vector2(28, 28), TopLeft);
        Label(funds.transform, "Caption", "NGÂN QUỸ", new Vector2(48, -10), new Vector2(170, 22), 15, Muted);
        wallet = Label(funds.transform, "Credits", "", new Vector2(48, -32), new Vector2(170, 36), 26, Accent, true);

        settingsBoardBtn = ButtonAt(shell, "SettingsBtn", "CÀI ĐẶT", new Vector2(720, 0), new Vector2(125, 44), Tile, Mint, ToggleSettingsFromBoard, 16);
        aboutBoardBtn = ButtonAt(shell, "AboutBtn", "ABOUT", new Vector2(855, 0), new Vector2(120, 44), Tile, Accent, OpenAbout, 16);
        ButtonAt(shell, "Quit", "THOÁT GAME", new Vector2(985, 0), new Vector2(135, 44), Tile, White, () => game.Quit(), 16);
        completed = Label(shell, "Completed", "", new Vector2(720, -56), new Vector2(400, 26), 15, Muted); completed.alignment = TextAlignmentOptions.Right;
        Image(shell, "HeaderLine", Line, new Vector2(0, -94), new Vector2(1120, 1), TopLeft);

        boardBody = Group("BoardBody", shell);

        var mission = SlicedImage(boardBody.transform, "MissionPaper", spritePanel, new Color(.95f, .94f, .90f), new Vector2(0, -112), new Vector2(700, 366), TopLeft, 0.5f);
        SlicedImage(mission.transform, "MissionInset", spritePanelInset, new Color(1f, 1f, 1f, .55f), new Vector2(10, -10), new Vector2(680, 346), TopLeft, 0.5f);
        missionStripe = Image(mission.transform, "StateStripe", Accent, new Vector2(10, -10), new Vector2(6, 346), TopLeft);
        eyebrow = Label(mission.transform, "State", "", new Vector2(30, -18), new Vector2(640, 22), 15, new Color(.34f, .39f, .39f), true);

        map1Btn = ButtonAt(mission.transform, "Map1Select", "MAP 1: THUNG LŨNG LÁ ĐỎ", new Vector2(26, -42), new Vector2(318, 50), Tile, White, () => SelectMap(0), 16);
        map1BtnText = map1Btn.GetComponentInChildren<TMP_Text>();
        map2Btn = ButtonAt(mission.transform, "Map2Select", "MAP 2: ĐÈO BĂNG GIÁ", new Vector2(352, -42), new Vector2(318, 50), Tile, White, () => SelectMap(1), 16);
        map2BtnText = map2Btn.GetComponentInChildren<TMP_Text>();

        missionTitle = Label(mission.transform, "MissionTitle", "", new Vector2(30, -100), new Vector2(644, 38), 26, Ink, true);
        missionTitle.enableAutoSizing = true; missionTitle.fontSizeMin = 20; missionTitle.fontSizeMax = 26;
        briefing = Label(mission.transform, "Briefing", "", new Vector2(30, -142), new Vector2(640, 118), 19, Ink);
        Image(mission.transform, "Divider", new Color(.65f, .68f, .62f), new Vector2(30, -266), new Vector2(640, 1), TopLeft);
        rewardCaption = Label(mission.transform, "RewardCaption", "", new Vector2(34, -280), new Vector2(335, 20), 14, new Color(.34f, .39f, .39f), true);
        rewardValue = Label(mission.transform, "Reward", "", new Vector2(32, -300), new Vector2(270, 44), 30, Ink, true);
        rewardHint = Label(mission.transform, "RewardHint", "", new Vector2(310, -288), new Vector2(360, 50), 16, new Color(.34f, .39f, .39f));

        start = ButtonAt(boardBody.transform, "AcceptContract", "NHẬN NHIỆM VỤ   →", new Vector2(0, -496), new Vector2(700, 60), Accent, Ink, OnStartButtonClicked, 24);
        resume = ButtonAt(boardBody.transform, "Resume", "TIẾP TỤC LƯỢT SĂN   →", new Vector2(0, -496), new Vector2(700, 60), Accent, Ink, () => game.Resume(), 24);
        retry = ButtonAt(boardBody.transform, "Retry", "CHƠI LẠI   →", new Vector2(0, -496), new Vector2(700, 60), Accent, Ink, () => { if (game.State == ContractGame.Phase.Won) game.NextStage(); else game.Retry(); }, 24);

        var side = SlicedImage(boardBody.transform, "PreparationPanel", spritePanel, Color.white, new Vector2(724, -112), new Vector2(396, 444), TopLeft, 0.5f);
        shop = Group("ShopContainer", side.transform);
        shopTabUpgradeBtn = ButtonAt(shop.transform, "TabUpgrades", "NÂNG CẤP", new Vector2(16, -16), new Vector2(178, 38), Tile, Accent, () => SetShopTab(false), 16);
        shopTabUpgradeText = shopTabUpgradeBtn.GetComponentInChildren<TMP_Text>();
        shopTabSupplyBtn = ButtonAt(shop.transform, "TabSupplies", "MUA VẬT PHẨM", new Vector2(202, -16), new Vector2(178, 38), Tile, White, () => SetShopTab(true), 16);
        shopTabSupplyText = shopTabSupplyBtn.GetComponentInChildren<TMP_Text>();

        upgradesGroup = Group("UpgradesGroup", shop.transform);
        damageUpgrade = UpgradeAt(upgradesGroup.transform, "DamageUpgrade", "SÁT THƯƠNG", -66, true, out damageValue, out damagePrice);
        magazineUpgrade = UpgradeAt(upgradesGroup.transform, "MagazineUpgrade", "BĂNG ĐẠN", -186, false, out magazineValue, out magazinePrice);
        currentGear = Label(upgradesGroup.transform, "CurrentGear", "", new Vector2(24, -308), new Vector2(348, 64), 16, Muted);

        suppliesGroup = Group("SuppliesGroup", shop.transform);
        buyPotionBtn = SupplyItemAt(suppliesGroup.transform, "BuyPotion", "BÌNH MÁU (+50 HP)", -64, spriteIconPotion, () => { game.BuyPotion(); Refresh(); }, out potionCountText, out potionPriceText);
        buyGrenadeBtn = SupplyItemAt(suppliesGroup.transform, "BuyGrenade", "LỰU ĐẠN NỔ (120 DMG)", -140, spriteIconBomb, () => { game.BuyGrenade(); Refresh(); }, out grenadeCountText, out grenadePriceText);
        buyPistolAmmoBtn = SupplyItemAt(suppliesGroup.transform, "BuyPistolAmmo", "ĐẠN SÚNG LỤC (+30)", -216, spriteIconPistol, () => { game.BuyPistolAmmo(); Refresh(); }, out pistolAmmoCountText, out pistolAmmoPriceText);
        buyRifleAmmoBtn = SupplyItemAt(suppliesGroup.transform, "BuyRifleAmmo", "ĐẠN SÚNG TRƯỜNG (+60)", -292, spriteIconRifle, () => { game.BuyRifleAmmo(); Refresh(); }, out rifleAmmoCountText, out rifleAmmoPriceText);
        SetShopTab(false);

        summary = Group("RunSummary", side.transform);
        Label(summary.transform, "MissionCaption", "KHU VỰC ĐANG SĂN", new Vector2(24, -20), new Vector2(348, 22), 15, Mint, true);
        summaryMission = Label(summary.transform, "SummaryMission", "", new Vector2(24, -50), new Vector2(348, 72), 24, White, true);
        summaryGear = Label(summary.transform, "SummaryGear", "", new Vector2(24, -130), new Vector2(348, 44), 18, Muted);
        pausedRetry = ButtonAt(summary.transform, "RestartPaused", "CHƠI LẠI TỪ ĐẦU", new Vector2(24, -195), new Vector2(348, 48), Tile, White, () => game.Retry(), 18);
        board = ButtonAt(summary.transform, "ReturnToBoard", "VỀ BẢNG NHIỆM VỤ", new Vector2(24, -255), new Vector2(348, 48), Tile, White, () => game.ReturnToBoard(), 18);

        BuildSettingsPanel(shell);
        BuildAboutDialog(menu.transform);

        Image(shell, "FooterLine", Line, new Vector2(0, -581), new Vector2(1120, 1), TopLeft);
        var help = Label(shell, "Controls",
            "WASD  Di chuyển     •     Chuột trái  Bắn / Dùng     •     R  Nạp đạn     •     1–5 / Cuộn chuột  Đổi trang bị\nEnter  Mở Bảng Nhiệm Vụ & Cửa Hàng     •     Esc  Cài đặt & Tạm dừng",
            new Vector2(0, -597), new Vector2(1120, 43), 16, Muted);
        help.alignment = TextAlignmentOptions.Top;
    }

    private void BuildSettingsPanel(Transform parent)
    {
        settingsPanel = Group("SettingsPanel", parent);
        var r = (RectTransform)settingsPanel.transform;
        r.anchorMin = r.anchorMax = r.pivot = TopLeft;
        r.anchoredPosition = new Vector2(0, -106);
        r.sizeDelta = new Vector2(1120, 470);

        pauseHeaderBadge = Label(settingsPanel.transform, "HeaderBadge", "CÀI ĐẶT TRÒ CHƠI", new Vector2(0, -2), new Vector2(1120, 26), 16, Accent, true);
        pauseHeaderBadge.alignment = TextAlignmentOptions.Center;

        var leftCard = SlicedImage(settingsPanel.transform, "LeftCard", spritePanel, Color.white, new Vector2(0, -28), new Vector2(545, 372), TopLeft, 0.5f);
        SlicedImage(leftCard.transform, "Inset", spritePanelInset, new Color(0.12f, 0.1f, 0.08f, 0.5f), new Vector2(8, -8), new Vector2(529, 356), TopLeft, 0.5f);
        IconImage(leftCard.transform, "Icon", spriteInfo, Mint, new Vector2(18, -14), new Vector2(20, 20), TopLeft);
        Label(leftCard.transform, "Title", "ÂM THANH & ĐIỀU KHIỂN", new Vector2(46, -14), new Vector2(470, 24), 18, Mint, true);
        Image(leftCard.transform, "Divider", Line, new Vector2(16, -40), new Vector2(513, 1), TopLeft);

        Label(leftCard.transform, "MasterVolLabel", "ÂM LƯỢNG TỔNG", new Vector2(20, -50), new Vector2(280, 24), 16, White, true);
        masterValText = Label(leftCard.transform, "MasterVolVal", "100%", new Vector2(340, -50), new Vector2(175, 24), 16, Accent, true);
        masterValText.alignment = TextAlignmentOptions.Right;
        masterSlider = CreatePixelSlider(leftCard.transform, "MasterSlider", 0f, 1f, 1f, new Vector2(20, -78), new Vector2(505, 26), OnMasterSliderChanged);

        Label(leftCard.transform, "SfxVolLabel", "ÂM LƯỢNG HIỆU ỨNG (SFX)", new Vector2(20, -116), new Vector2(280, 24), 16, White, true);
        sfxValText = Label(leftCard.transform, "SfxVolVal", "80%", new Vector2(340, -116), new Vector2(175, 24), 16, Accent, true);
        sfxValText.alignment = TextAlignmentOptions.Right;
        sfxSlider = CreatePixelSlider(leftCard.transform, "SfxSlider", 0f, 1f, 0.8f, new Vector2(20, -144), new Vector2(505, 26), OnSfxSliderChanged);

        Label(leftCard.transform, "SensLabel", "ĐỘ NHẠY CHUỘT", new Vector2(20, -182), new Vector2(250, 24), 16, White, true);
        sensValText = Label(leftCard.transform, "SensVal", "1.0x (0.12)", new Vector2(300, -182), new Vector2(215, 24), 16, Accent, true);
        sensValText.alignment = TextAlignmentOptions.Right;
        sensSlider = CreatePixelSlider(leftCard.transform, "SensSlider", 0.02f, 0.36f, 0.12f, new Vector2(20, -210), new Vector2(505, 26), OnSensitivitySliderChanged);

        Label(leftCard.transform, "InvertLabel", "ĐẢO TRỤC CHUỘT Y", new Vector2(20, -260), new Vector2(270, 36), 16, White, true);
        var invertBtn = ButtonAt(leftCard.transform, "InvertBtn", "", new Vector2(300, -256), new Vector2(225, 42), Tile, White, ToggleInvertY, 15);
        invertYBg = invertBtn.GetComponent<UIImage>();
        invertYText = invertBtn.GetComponentInChildren<TMP_Text>();

        var rightCard = SlicedImage(settingsPanel.transform, "RightCard", spritePanel, Color.white, new Vector2(575, -28), new Vector2(545, 372), TopLeft, 0.5f);
        SlicedImage(rightCard.transform, "Inset", spritePanelInset, new Color(0.12f, 0.1f, 0.08f, 0.5f), new Vector2(8, -8), new Vector2(529, 356), TopLeft, 0.5f);
        IconImage(rightCard.transform, "Icon", spriteStar, Mint, new Vector2(18, -14), new Vector2(20, 20), TopLeft);
        Label(rightCard.transform, "Title", "HÌNH ẢNH & HIỂN THỊ", new Vector2(46, -14), new Vector2(470, 24), 18, Mint, true);
        Image(rightCard.transform, "Divider", Line, new Vector2(16, -40), new Vector2(513, 1), TopLeft);

        Label(rightCard.transform, "FovLabel", "GÓC NHÌN (FIELD OF VIEW)", new Vector2(20, -50), new Vector2(280, 24), 16, White, true);
        fovValText = Label(rightCard.transform, "FovVal", "75°", new Vector2(340, -50), new Vector2(175, 24), 16, Accent, true);
        fovValText.alignment = TextAlignmentOptions.Right;
        fovSlider = CreatePixelSlider(rightCard.transform, "FovSlider", 60f, 105f, 75f, new Vector2(20, -78), new Vector2(505, 26), OnFOVSliderChanged);

        Label(rightCard.transform, "QualityLabel", "CHẤT LƯỢNG ĐỒ HỌA", new Vector2(20, -122), new Vector2(260, 24), 16, White, true);
        var qualityBtn = ButtonAt(rightCard.transform, "QualityBtn", "", new Vector2(295, -116), new Vector2(230, 42), Tile, Mint, CycleQuality, 15);
        qualityText = qualityBtn.GetComponentInChildren<TMP_Text>();

        Label(rightCard.transform, "ScreenLabel", "CHẾ ĐỘ MÀN HÌNH", new Vector2(20, -176), new Vector2(260, 24), 16, White, true);
        var screenBtn = ButtonAt(rightCard.transform, "ScreenBtn", "", new Vector2(295, -170), new Vector2(230, 42), Tile, White, ToggleFullscreen, 15);
        fullscreenText = screenBtn.GetComponentInChildren<TMP_Text>();

        Image(rightCard.transform, "Divider3", Line, new Vector2(16, -226), new Vector2(513, 1), TopLeft);
        settingsReset = ButtonAt(rightCard.transform, "ResetBtn", "MẶC ĐỊNH", new Vector2(20, -244), new Vector2(235, 44), Tile, White, ResetSettingsToDefaults, 15);
        ButtonAt(rightCard.transform, "AboutSettingBtn", "ABOUT (TÁC GIẢ)", new Vector2(275, -244), new Vector2(250, 44), Tile, Accent, OpenAbout, 15);
        Label(rightCard.transform, "AutoSaveNote", "Cài đặt được áp dụng tức thì & lưu tự động.", new Vector2(20, -300), new Vector2(505, 22), 14, Mint);

        settingsResume = ButtonAt(settingsPanel.transform, "ResumeAction", "TIẾP TỤC (ESC)   ▶", new Vector2(0, -412), new Vector2(260, 56), Accent, Ink, () => game.Resume(), 20);
        settingsRetry = ButtonAt(settingsPanel.transform, "RetryAction", "CHƠI LẠI", new Vector2(275, -412), new Vector2(165, 56), Tile, White, () => game.Retry(), 18);
        settingsBoard = ButtonAt(settingsPanel.transform, "BoardAction", "VỀ BẢNG NHIỆM VỤ", new Vector2(455, -412), new Vector2(245, 56), Tile, White, () => game.ReturnToBoard(), 18);
        settingsAboutPause = ButtonAt(settingsPanel.transform, "AboutPauseAction", "ABOUT", new Vector2(715, -412), new Vector2(165, 56), Tile, Mint, OpenAbout, 18);
        settingsQuit = ButtonAt(settingsPanel.transform, "QuitAction", "THOÁT GAME", new Vector2(895, -412), new Vector2(225, 56), Tile, Danger, () => game.Quit(), 18);

        settingsClose = ButtonAt(settingsPanel.transform, "CloseAction", "LƯU & ĐÓNG CÀI ĐẶT (ESC)", new Vector2(0, -412), new Vector2(360, 56), Accent, Ink, ToggleSettingsFromBoard, 22);
        settingsAboutBoard = ButtonAt(settingsPanel.transform, "AboutBoardAction", "ABOUT (NHÓM TÁC GIẢ)", new Vector2(380, -412), new Vector2(260, 56), Tile, Mint, OpenAbout, 18);
        settingsPanel.SetActive(false);
    }

    public void OpenAbout()
    {
        if (aboutDialog == null) return;
        aboutDialog.SetActive(true);
        aboutDialog.transform.SetAsLastSibling();
    }

    public void CloseAbout()
    {
        if (aboutDialog != null) aboutDialog.SetActive(false);
    }

    private void BuildAboutDialog(Transform parent)
    {
        aboutDialog = Group("AboutDialog", parent);
        var dimBtn = ButtonAt(aboutDialog.transform, "BackdropDim", "", new Vector2(-1500, 1500), new Vector2(3000, 3000), new Color(0.015f, 0.025f, 0.04f, 0.82f), Color.clear, CloseAbout, 10);
        var dimRect = (RectTransform)dimBtn.transform;
        dimRect.anchorMin = dimRect.anchorMax = dimRect.pivot = new Vector2(0.5f, 0.5f);
        dimRect.anchoredPosition = Vector2.zero;

        var modal = SlicedImage(aboutDialog.transform, "ModalWindow", spriteWindow != null ? spriteWindow : spritePanel, Color.white, new Vector2(-330, 210), new Vector2(660, 420), new Vector2(0.5f, 0.5f), 0.5f);
        SlicedImage(modal.transform, "Inset", spritePanelInset, new Color(0.12f, 0.1f, 0.08f, 0.65f), new Vector2(8, -8), new Vector2(644, 404), TopLeft, 0.5f);

        IconImage(modal.transform, "Icon", spriteStar, Accent, new Vector2(24, -20), new Vector2(28, 28), TopLeft);
        Label(modal.transform, "Title", "THÔNG TIN DỰ ÁN  •  ABOUT", new Vector2(62, -18), new Vector2(570, 32), 22, Accent, true);
        Label(modal.transform, "Subtitle", "GUNQUEST 3D  •  ĐỒ ÁN PHÁT TRIỂN GAME", new Vector2(62, -48), new Vector2(570, 22), 15, Mint);
        Image(modal.transform, "Divider", Line, new Vector2(20, -74), new Vector2(620, 1), TopLeft);
        Label(modal.transform, "TeamCaption", "NHÓM SINH VIÊN THỰC HIỆN:", new Vector2(24, -86), new Vector2(610, 24), 15, Muted, true);

        string[,] members =
        {
            { "2312802", "Phan Khánh Vương", "Trưởng nhóm • Gameplay & Hệ thống" },
            { "2312585", "Phùng Nguyễn Hoài Bo", "Thành viên • Môi trường & Vật phẩm" },
            { "2312590", "Nguyễn Ngọc Trường Dân", "Thành viên • Enemy AI & Giao diện" }
        };

        for (int i = 0; i < 3; i++)
        {
            float y = -114f - i * 64f;
            var card = SlicedImage(modal.transform, "Member" + (i + 1), spritePanelInset, new Color(0.18f, 0.15f, 0.12f, 0.9f), new Vector2(24, y), new Vector2(612, 54f), TopLeft, 0.5f);
            var badge = SlicedImage(card.transform, "IdBadge", spriteBtnPressed, new Color(0.25f, 0.2f, 0.15f, 1f), new Vector2(8, -8), new Vector2(130, 38), TopLeft, 0.5f);
            var idText = Label(badge.transform, "IdText", members[i, 0], Vector2.zero, new Vector2(130, 38), 16, Accent, true);
            idText.alignment = TextAlignmentOptions.Center;
            Label(card.transform, "NameText", members[i, 1], new Vector2(150, -8), new Vector2(450, 24), 18, White, true);
            Label(card.transform, "RoleText", members[i, 2], new Vector2(150, -30), new Vector2(450, 20), 13, Mint);
        }

        var schoolText = Label(modal.transform, "SchoolNote", "Trường ĐH Khoa học Tự nhiên, ĐHQG-HCM", new Vector2(24, -310), new Vector2(612, 22), 14, Muted);
        schoolText.alignment = TextAlignmentOptions.Center;
        ButtonAt(modal.transform, "CloseBtn", "ĐÓNG (ESC)", new Vector2(220, -348), new Vector2(220, 48), Accent, Ink, CloseAbout, 20);
        aboutDialog.SetActive(false);
    }

    private Slider CreatePixelSlider(Transform parent, string name, float min, float max, float initial, Vector2 position, Vector2 size, UnityAction<float> onValueChanged)
    {
        var sliderGo = new GameObject(name, typeof(RectTransform), typeof(Slider));
        var r = (RectTransform)sliderGo.transform;
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = r.pivot = TopLeft;
        r.anchoredPosition = position;
        r.sizeDelta = size;

        var bg = SlicedImage(sliderGo.transform, "Background", spriteProgressBg, new Color(0.12f, 0.1f, 0.08f, 0.95f), Vector2.zero, size, TopLeft, 0.5f);
        Stretch(bg.rectTransform);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
        fillArea.SetParent(sliderGo.transform, false);
        fillArea.anchorMin = Vector2.zero; fillArea.anchorMax = Vector2.one;
        fillArea.offsetMin = new Vector2(4, 4); fillArea.offsetMax = new Vector2(-4, -4);

        var fill = SlicedImage(fillArea, "Fill", spriteBarGreen, Color.white, Vector2.zero, Vector2.zero, TopLeft, 0.5f);
        Stretch(fill.rectTransform);

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform)).GetComponent<RectTransform>();
        handleArea.SetParent(sliderGo.transform, false);
        handleArea.anchorMin = Vector2.zero; handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = new Vector2(8, 0); handleArea.offsetMax = new Vector2(-8, 0);

        var handle = SlicedImage(handleArea, "Handle", spriteBtnNormal, Color.white, Vector2.zero, new Vector2(16, size.y + 4), new Vector2(0.5f, 0.5f), 0.5f);
        handle.raycastTarget = true;

        var slider = sliderGo.GetComponent<Slider>();
        slider.targetGraphic = handle;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min; slider.maxValue = max; slider.value = initial;
        slider.onValueChanged.AddListener(onValueChanged);
        return slider;
    }

    public void ToggleSettingsFromBoard()
    {
        boardSettingsOpen = !boardSettingsOpen;
        Refresh();
    }

    private void RefreshSettingsUI(bool isPaused)
    {
        var s = GameSettings.Instance;
        if (s == null) return;

        if (masterSlider != null)
        {
            masterSlider.SetValueWithoutNotify(s.masterVolume);
            masterValText.text = Mathf.RoundToInt(s.masterVolume * 100f) + "%";
        }
        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(s.sfxVolume);
            sfxValText.text = Mathf.RoundToInt(s.sfxVolume * 100f) + "%";
        }
        if (sensSlider != null)
        {
            sensSlider.SetValueWithoutNotify(s.mouseSensitivity);
            sensValText.text = (s.mouseSensitivity / 0.12f).ToString("0.0") + "x (" + s.mouseSensitivity.ToString("0.00") + ")";
        }
        if (fovSlider != null)
        {
            fovSlider.SetValueWithoutNotify(s.fieldOfView);
            fovValText.text = Mathf.RoundToInt(s.fieldOfView) + "°";
        }
        if (invertYText != null)
        {
            invertYText.text = s.invertY ? "BẬT (ĐẢO TRỤC)" : "TẮT (CHUẨN)";
            invertYText.color = s.invertY ? Ink : White;
            if (invertYBg != null) invertYBg.color = s.invertY ? Mint : Tile;
        }
        if (qualityText != null)
        {
            string qName = (QualitySettings.names != null && s.qualityLevel >= 0 && s.qualityLevel < QualitySettings.names.Length)
                ? QualitySettings.names[s.qualityLevel].ToUpperInvariant() : "MẶC ĐỊNH";
            qualityText.text = "<  " + qName + "  >";
        }
        if (fullscreenText != null) fullscreenText.text = s.fullscreen ? "TOÀN MÀN HÌNH" : "CỬA SỔ";

        pauseHeaderBadge.text = isPaused ? "LƯỢT SĂN TẠM DỪNG  •  CÀI ĐẶT TRÒ CHƠI" : "BẢNG CÀI ĐẶT TRÒ CHƠI  •  GUNQUEST";
        settingsResume.gameObject.SetActive(isPaused);
        settingsRetry.gameObject.SetActive(isPaused);
        settingsBoard.gameObject.SetActive(isPaused);
        settingsQuit.gameObject.SetActive(isPaused);
        settingsClose.gameObject.SetActive(!isPaused);
        if (settingsAboutBoard != null) settingsAboutBoard.gameObject.SetActive(!isPaused);
        if (settingsAboutPause != null) settingsAboutPause.gameObject.SetActive(isPaused);
    }

    private void UpdateSetting(System.Action<GameSettings> update)
    {
        update(GameSettings.Instance);
        GameSettings.Instance.Apply();
        RefreshSettingsUI(game != null && game.State == ContractGame.Phase.Paused);
    }

    private void OnMasterSliderChanged(float val) => UpdateSetting(s => s.masterVolume = Mathf.Clamp01(val));
    private void OnSfxSliderChanged(float val) => UpdateSetting(s => s.sfxVolume = Mathf.Clamp01(val));
    private void OnSensitivitySliderChanged(float val) => UpdateSetting(s => s.mouseSensitivity = Mathf.Clamp(val, 0.02f, 0.36f));
    private void ToggleInvertY() => UpdateSetting(s => s.invertY = !s.invertY);
    private void OnFOVSliderChanged(float val) => UpdateSetting(s => s.fieldOfView = Mathf.Clamp(val, 60f, 105f));
    private void CycleQuality() => UpdateSetting(s => { if (QualitySettings.names.Length > 0) s.qualityLevel = (s.qualityLevel + 1) % QualitySettings.names.Length; });
    private void ToggleFullscreen() => UpdateSetting(s => s.fullscreen = !s.fullscreen);
    private void ResetSettingsToDefaults() => UpdateSetting(s => s.ResetDefaults());

    private void SelectMap(int index)
    {
        selectedMapIndex = index;
        lastSelectedMap = -1;
        Refresh();
    }

    public void OnStartButtonClicked()
    {
        string curScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isCurrent = (selectedMapIndex == 0 && curScene == "AutumnValley") || (selectedMapIndex == 1 && curScene == "IceboundPass");
        if (isCurrent) game.AcceptContract();
        else
        {
            Time.timeScale = 1;
            UnityEngine.SceneManagement.SceneManager.LoadScene(selectedMapIndex == 0 ? "AutumnValley" : "IceboundPass");
        }
    }

    private void SetShopTab(bool supplies)
    {
        showingSuppliesTab = supplies;
        if (upgradesGroup != null) upgradesGroup.SetActive(!supplies);
        if (suppliesGroup != null) suppliesGroup.SetActive(supplies);

        if (shopTabUpgradeBtn != null)
        {
            shopTabUpgradeBtn.image.color = supplies ? Tile : Accent;
            if (shopTabUpgradeText != null) shopTabUpgradeText.color = supplies ? White : Ink;
        }
        if (shopTabSupplyBtn != null)
        {
            shopTabSupplyBtn.image.color = supplies ? Accent : Tile;
            if (shopTabSupplyText != null) shopTabSupplyText.color = supplies ? Ink : White;
        }
    }

    private Button SupplyItemAt(Transform parent, string name, string title, float y, Sprite icon, UnityAction onBuy, out TMP_Text countText, out TMP_Text priceText)
    {
        var button = ButtonAt(parent, name, "", new Vector2(24, y), new Vector2(348, 72), Tile, White, onBuy, 18);
        Object.Destroy(button.GetComponentInChildren<TMP_Text>().gameObject);
        SlicedImage(button.transform, "InnerInset", spritePanelInset, new Color(0.12f, 0.1f, 0.08f, 0.6f), new Vector2(4, -4), new Vector2(340, 64), TopLeft, 0.5f);
        if (icon != null) IconImage(button.transform, "ItemIcon", icon, Color.white, new Vector2(10, -12), new Vector2(44, 44), TopLeft);
        Label(button.transform, "ItemTitle", title, new Vector2(62, -8), new Vector2(275, 22), 15, Mint, true);
        countText = Label(button.transform, "ItemCount", "", new Vector2(62, -28), new Vector2(275, 20), 14, White, false);
        priceText = Label(button.transform, "ItemPrice", "", new Vector2(62, -47), new Vector2(275, 20), 14, Accent, false);
        return button;
    }

    private Button UpgradeAt(Transform parent, string name, string title, float y, bool damage, out TMP_Text value, out TMP_Text price)
    {
        var button = ButtonAt(parent, name, "", new Vector2(24, y), new Vector2(348, 112), Tile, White, () => { game.BuyUpgrade(damage); Refresh(); }, 18);
        Object.Destroy(button.GetComponentInChildren<TMP_Text>().gameObject);
        SlicedImage(button.transform, "InnerInset", spritePanelInset, new Color(0.12f, 0.1f, 0.08f, 0.6f), new Vector2(6, -6), new Vector2(336, 100), TopLeft, 0.5f);
        Label(button.transform, "UpgradeTitle", title, new Vector2(16, -12), new Vector2(316, 22), 15, Mint, true);
        value = Label(button.transform, "UpgradeValue", "", new Vector2(16, -36), new Vector2(316, 34), 25, White, true);
        price = Label(button.transform, "UpgradePrice", "", new Vector2(16, -79), new Vector2(316, 24), 17, Accent);
        return button;
    }

    private void BuildHUD(Transform root)
    {
        hud = Group("HUD", root);
        var damageOverlay = Image(hud.transform, "DamageOverlay", new Color(.6f, 0, 0, 0), Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch(damageOverlay.rectTransform);

        objectivePanel = SlicedImage(hud.transform, "ObjectivePanel", spritePanel, Color.white, new Vector2(28, -28), new Vector2(490, 136), TopLeft, 0.5f).rectTransform;
        IconImage(objectivePanel, "ObjectiveIcon", spriteStar != null ? spriteStar : spriteInfo, Accent, new Vector2(16, -13), new Vector2(20, 20), TopLeft);
        Label(objectivePanel, "ObjectiveCaption", "NHIỆM VỤ", new Vector2(42, -14), new Vector2(410, 22), 15, Mint, true);
        SlicedImage(objectivePanel, "ObjectivePlate", spritePanelInset, new Color(0.12f, 0.1f, 0.08f, 0.72f), new Vector2(12, -38), new Vector2(466, 86), TopLeft, 0.5f);
        objectiveTitle = Label(objectivePanel, "ObjectiveTitle", "", new Vector2(24, -45), new Vector2(442, 32), 23, White, true);
        objectiveTitle.enableAutoSizing = true; objectiveTitle.fontSizeMin = 18; objectiveTitle.fontSizeMax = 23;
        objectiveBody = Label(objectivePanel, "ObjectiveDetail", "", new Vector2(24, -77), new Vector2(442, 42), 16, new Color(0.9f, 0.88f, 0.82f));

        hudWalletPanel = SlicedImage(hud.transform, "HUDWallet", spritePanel, Color.white, new Vector2(-228, -28), new Vector2(200, 50), new Vector2(1, 1), 0.5f).rectTransform;
        IconImage(hudWalletPanel, "CoinIcon", spriteCoin, Color.white, new Vector2(12, -13), new Vector2(24, 24), TopLeft);
        hudWalletValue = Label(hudWalletPanel, "Credits", "0 xu", new Vector2(42, -11), new Vector2(146, 28), 20, Accent, true);

        hudCoinPopup = Label(hud.transform, "CoinPopup", "+10 xu", new Vector2(-228, -82), new Vector2(200, 30), 22, Mint, true);
        hudCoinPopup.alignment = TextAlignmentOptions.Right;
        hudCoinPopup.gameObject.SetActive(false);

        exitPanel = SlicedImage(hud.transform, "ExtractionDistance", spritePanel, Color.white, new Vector2(-228, -84), new Vector2(200, 50), new Vector2(1, 1), 0.5f).rectTransform;
        IconImage(exitPanel, "ExitIcon", spriteCoin, Color.white, new Vector2(12, -13), new Vector2(24, 24), TopLeft);
        exitText = Label(exitPanel, "ExtractionPrompt", "", new Vector2(42, -11), new Vector2(146, 28), 18, Mint, true);

        var boardHintPanel = SlicedImage(hud.transform, "BoardHintPanel", spritePanel, Color.white, new Vector2(-228, -140), new Vector2(200, 36), new Vector2(1, 1), 0.5f).rectTransform;
        var hintLbl = Label(boardHintPanel, "BoardHint", "[ENTER] TIỆM ĐỒ", new Vector2(10, -6), new Vector2(180, 24), 14, Accent, true);
        hintLbl.alignment = TextAlignmentOptions.Center;

        healthPanel = SlicedImage(hud.transform, "HealthPanel", spritePanel, Color.white, new Vector2(28, 134), new Vector2(270, 114), Vector2.zero, 0.5f).rectTransform;
        IconImage(healthPanel, "HeartIcon", spriteHeart, Color.white, new Vector2(16, -14), new Vector2(22, 22), TopLeft);
        Label(healthPanel, "HealthCaption", "SINH LỰC", new Vector2(44, -16), new Vector2(200, 22), 15, Accent, true);
        healthValue = Label(healthPanel, "HealthValue", "", new Vector2(20, -40), new Vector2(230, 36), 28, White, true);

        SlicedImage(healthPanel, "HealthTrack", spriteProgressBg, new Color(0.15f, 0.11f, 0.09f, 1f), new Vector2(14, -78), new Vector2(242, 22), TopLeft, 0.5f);
        healthChip = SlicedImage(healthPanel, "HealthChip", spriteBarFillGold, Color.white, new Vector2(16, -80), new Vector2(238, 18), TopLeft, 0.5f);
        healthFill = SlicedImage(healthPanel, "HealthFill", spriteBarFillRed, Color.white, new Vector2(16, -80), new Vector2(238, 18), TopLeft, 0.5f);
        foreach (var bar in new[] { healthChip, healthFill })
        {
            bar.type = UIImage.Type.Filled;
            bar.fillMethod = UIImage.FillMethod.Horizontal;
            bar.fillOrigin = (int)UIImage.OriginHorizontal.Left;
            bar.fillAmount = 1f;
        }
        if (health != null)
        {
            health.frontHealthBar = healthFill;
            health.backHealthBar = healthChip;
            health.overlay = damageOverlay;
        }

        ammoPanel = SlicedImage(hud.transform, "AmmoPanel", spritePanel, Color.white, new Vector2(-298, 154), new Vector2(270, 126), new Vector2(1, 0), 0.5f).rectTransform;
        weaponName = Label(ammoPanel, "WeaponName", "", new Vector2(18, -14), new Vector2(234, 26), 18, Accent, true);
        SlicedImage(ammoPanel, "AmmoPlate", spritePanelInset, new Color(0.12f, 0.1f, 0.08f, 0.72f), new Vector2(14, -40), new Vector2(242, 52), TopLeft, 0.5f);
        ammoValue = Label(ammoPanel, "AmmoValue", "", new Vector2(24, -43), new Vector2(110, 48), 40, White, true);
        reserveValue = Label(ammoPanel, "ReserveAmmo", "", new Vector2(136, -55), new Vector2(110, 30), 22, new Color(0.85f, 0.85f, 0.8f));
        ammoHint = Label(ammoPanel, "AmmoHint", "", new Vector2(18, -98), new Vector2(234, 22), 16, Mint, true);

        int count = weapons != null ? weapons.Count : 4;
        slotTiles = new Image[count]; slotMarks = new Image[count]; slotIcons = new Image[count];
        slotNumbers = new TMP_Text[count]; slotCounts = new TMP_Text[count];

        float slotSize = 68f, slotSpacing = 8f;
        float windowWidth = count * slotSize + (count - 1) * slotSpacing + 28f;
        slotStrip = SlicedImage(hud.transform, "WeaponInventoryWindow", spriteWindow != null ? spriteWindow : spritePanel, Color.white, new Vector2(-windowWidth * .5f, 136), new Vector2(windowWidth, 108f), new Vector2(.5f, 0), 0.5f).rectTransform;

        hotbarTitle = Label(slotStrip, "InventoryTitle", "INVENTORY", new Vector2(0, -3), new Vector2(windowWidth, 20), 14, Ink, true);
        hotbarTitle.alignment = TextAlignmentOptions.Center;

        for (int i = 0; i < count; i++)
        {
            float x = 14f + i * (slotSize + slotSpacing);
            slotTiles[i] = SlicedImage(slotStrip, "Slot" + (i + 1), spritePanelInset, Color.white, new Vector2(x, -28f), new Vector2(slotSize, slotSize), TopLeft, 0.5f);
            slotIcons[i] = IconImage(slotTiles[i].transform, "Icon", GetWeaponIcon(i), Color.white, new Vector2(12, -12), new Vector2(44, 44), TopLeft);
            slotNumbers[i] = Label(slotTiles[i].transform, "Key", (i + 1).ToString(), new Vector2(4, -3), new Vector2(18, 18), 12, new Color(0.95f, 0.88f, 0.72f), true);
            slotCounts[i] = Label(slotTiles[i].transform, "Count", "", new Vector2(18, -48), new Vector2(46, 16), 11, Accent, true);
            slotCounts[i].alignment = TextAlignmentOptions.Right;
            slotMarks[i] = SlicedImage(slotTiles[i].transform, "Selected", spriteFocus != null ? spriteFocus : spriteBtnPressed, new Color(1f, 0.85f, 0.4f, 1f), new Vector2(-3, 3), new Vector2(slotSize + 6, slotSize + 6), TopLeft, 0.5f);
        }

        var crosshair = Rect("Crosshair", hud.transform, new Vector2(-16, 16), new Vector2(32, 32), Vector2.one * .5f);
        Image(crosshair, "DotOutline", Ink, new Vector2(14, -14), new Vector2(4, 4), TopLeft);
        Image(crosshair, "Dot", White, new Vector2(15, -15), new Vector2(2, 2), TopLeft);
        foreach (var position in new[] { new Vector2(2, -15), new Vector2(25, -15) })
            Image(crosshair, "Horizontal", White, position, new Vector2(5, 2), TopLeft);
        foreach (var position in new[] { new Vector2(15, -2), new Vector2(15, -25) })
            Image(crosshair, "Vertical", White, position, new Vector2(2, 5), TopLeft);
    }

    private void Update()
    {
        if (game != null && canvas != null) Refresh();
        if (coinPopupTimer <= 0f) return;
        coinPopupTimer -= Time.deltaTime;
        if (hudCoinPopup == null) return;
        hudCoinPopup.color = new Color(Accent.r, Accent.g, Accent.b, Mathf.Clamp01(coinPopupTimer / 0.4f));
        if (coinPopupTimer <= 0f)
        {
            hudCoinPopup.gameObject.SetActive(false);
            pendingPopupAmount = 0;
        }
    }

    private void OnCreditsAdded(int amount)
    {
        pendingPopupAmount += amount;
        coinPopupTimer = 1.6f;
        if (hudCoinPopup != null)
        {
            hudCoinPopup.gameObject.SetActive(true);
            hudCoinPopup.text = "+" + pendingPopupAmount + " xu";
            hudCoinPopup.color = Accent;
        }
        if (hudWalletValue != null && game != null && game.Progress != null)
            hudWalletValue.text = game.Progress.credits.ToString("N0") + " <size=14>xu</size>";
    }

    private void OnStatusMessage(string message, Color color)
    {
        coinPopupTimer = 1.8f;
        if (hudCoinPopup == null) return;
        hudCoinPopup.gameObject.SetActive(true);
        hudCoinPopup.text = message;
        hudCoinPopup.color = color;
    }

    public void Refresh()
    {
        if (hud == null || game == null || game.Progress == null) return;
        FitLayout();
        hud.SetActive(game.IsPlaying);
        menu.SetActive(!game.IsPlaying);
        if (game.IsPlaying) { boardSettingsOpen = false; RefreshHUD(); }
        else RefreshMenu();
    }

    private void FitLayout()
    {
        Vector2 available = ((RectTransform)canvas.transform).rect.size;
        if (available.x <= 0 || available.y <= 0) return;
        float fit = Mathf.Min(1, Mathf.Min((available.x - 48) / 1120, (available.y - 40) / 640));
        shell.localScale = Vector3.one * Mathf.Max(.1f, fit);
        shell.anchoredPosition = new Vector2(-560 * fit, 320 * fit);
        float objectiveWidth = Mathf.Min(490, available.x - 56);
        objectivePanel.sizeDelta = new Vector2(objectiveWidth, 136);
        objectiveTitle.rectTransform.sizeDelta = new Vector2(objectiveWidth - 48, 32);
        objectiveBody.rectTransform.sizeDelta = new Vector2(objectiveWidth - 48, 44);
        if (hudWalletPanel != null) hudWalletPanel.anchoredPosition = new Vector2(-228, -28);
        exitPanel.anchoredPosition = new Vector2(-228, available.x < 900 ? -146 : -84);
        if (hudCoinPopup != null) hudCoinPopup.rectTransform.anchoredPosition = new Vector2(-228, available.x < 900 ? -202 : -140);
        float panelFit = Mathf.Min(1, Mathf.Max(.1f, (available.x - 68) / 550));
        healthPanel.localScale = ammoPanel.localScale = Vector3.one * panelFit;
        healthPanel.anchoredPosition = new Vector2(28, 28 + 114 * panelFit);
        ammoPanel.anchoredPosition = new Vector2(-28 - 270 * panelFit, 28 + 126 * panelFit);
        float slotsFit = Mathf.Min(1, Mathf.Max(.1f, (available.x - 56) / slotStrip.sizeDelta.x));
        slotStrip.localScale = Vector3.one * slotsFit;
        slotStrip.anchoredPosition = new Vector2(-slotStrip.sizeDelta.x * slotsFit * .5f,
            available.x < 1040 ? 28 + 126 * panelFit + 24 + slotStrip.sizeDelta.y * slotsFit : 28 + slotStrip.sizeDelta.y * slotsFit);
    }

    private void RefreshHUD()
    {
        if (hudWalletValue != null && game != null && game.Progress != null)
            hudWalletValue.text = game.Progress.credits.ToString("N0") + " <size=14>xu</size>";

        if (health != null)
        {
            float hp = health.GetCurrentHealth();
            healthValue.text = Mathf.CeilToInt(hp) + " <size=18><color=#D4C8B8>/ " + Mathf.CeilToInt(health.maxHealth) + "</color></size>";
            healthFill.color = hp / Mathf.Max(1, health.maxHealth) <= .25f ? new Color(1f, 0.25f, 0.25f) : Color.white;
        }
        bool extracting = game.State == ContractGame.Phase.Extracting;
        objectiveTitle.text = extracting ? "RÚT LUI AN TOÀN" : game.MissionTitle;
        objectiveBody.text = extracting ? "Quay về vòng xanh tại điểm xuất phát." : game.MissionObjective;
        exitPanel.gameObject.SetActive(extracting);
        if (extracting) exitText.text = game.ExitDistance <= 2.5f ? "[E]  NHẬN THƯỞNG" : Mathf.CeilToInt(game.ExitDistance) + " m  •  RÚT LUI";
        if (weapons == null || weapons.Current == null) return;
        var weapon = weapons.Current;
        weaponName.text = weapon.label.ToUpperInvariant();
        var kind = weapons.CurrentKind;
        if (kind == SlotKind.Melee)
        {
            ammoValue.text = "—"; reserveValue.text = ""; ammoHint.text = "CẬN CHIẾN"; ammoValue.color = White;
        }
        else if (kind == SlotKind.Consumable || kind == SlotKind.Throwable)
        {
            ammoValue.text = weapon.count.ToString();
            reserveValue.text = "/ " + weapon.maxCount;
            ammoHint.text = kind == SlotKind.Consumable ? "[CHUỘT TRÁI]  HỒI 50 HP" : "[CHUỘT TRÁI]  NÉM BOM (SÁT THƯƠNG AOE)";
            ammoValue.color = weapon.count <= 1 ? Accent : White;
        }
        else
        {
            ammoValue.text = weapons.IsReloading ? "···" : weapons.Ammo.ToString("00");
            reserveValue.text = "/ " + weapons.Reserve;
            ammoHint.text = weapons.IsReloading ? "ĐANG NẠP ĐẠN..." : weapons.Ammo >= weapons.Capacity(weapons.SelectedIndex) ? "BĂNG ĐẠN ĐẦY" : weapons.Reserve <= 0 ? "HẾT ĐẠN DỰ TRỮ" : "[R]  NẠP ĐẠN";
            ammoValue.color = !weapons.IsReloading && weapons.Ammo <= 3 ? Accent : White;
        }

        for (int i = 0; i < slotTiles.Length; i++)
        {
            bool selected = i == weapons.SelectedIndex;
            slotTiles[i].color = selected ? Color.white : new Color(0.72f, 0.68f, 0.64f, 0.95f);
            if (slotIcons != null && i < slotIcons.Length && slotIcons[i] != null)
                slotIcons[i].color = selected ? Color.white : new Color(0.75f, 0.75f, 0.75f, 0.85f);
            if (slotNumbers != null && i < slotNumbers.Length && slotNumbers[i] != null)
                slotNumbers[i].color = selected ? Accent : new Color(0.85f, 0.8f, 0.7f);
            if (slotCounts != null && i < slotCounts.Length && slotCounts[i] != null)
            {
                var k = weapons.GetKind(i);
                if (k == SlotKind.Consumable || k == SlotKind.Throwable)
                {
                    int c = weapons.GetCount(i);
                    slotCounts[i].text = "x" + c;
                    slotCounts[i].color = c > 0 ? (selected ? Accent : Color.white) : new Color(0.8f, 0.3f, 0.3f);
                }
                else slotCounts[i].text = "";
            }
            if (slotMarks != null && i < slotMarks.Length && slotMarks[i] != null)
                slotMarks[i].gameObject.SetActive(selected);
        }
    }

    private Sprite GetWeaponIcon(int index)
    {
        if (weapons != null && index >= 0 && index < weapons.Count)
        {
            string label = weapons.GetLabel(index).ToLowerInvariant();
            if (label.Contains("pistol") || label.Contains("lục")) return spriteIconPistol;
            if (label.Contains("rifle") || label.Contains("trường") || label.Contains("súng")) return spriteIconRifle;
            if (label.Contains("bat") || label.Contains("gậy")) return spriteIconBat;
            if (label.Contains("potion") || label.Contains("máu") || label.Contains("bình") || label.Contains("heal")) return spriteIconPotion;
            if (label.Contains("bomb") || label.Contains("bom") || label.Contains("lựu đạn") || label.Contains("grenade")) return spriteIconBomb;
        }
        return index switch { 0 => spriteIconPistol, 1 => spriteIconRifle, 2 => spriteIconBat, 3 => spriteIconPotion, _ => spriteIconBomb };
    }

    private void RefreshMenu()
    {
        var p = game.Progress; var state = game.State;
        bool isBoard = state == ContractGame.Phase.Board;
        bool isPaused = state == ContractGame.Phase.Paused;
        bool won = state == ContractGame.Phase.Won;
        bool showSettings = isPaused || boardSettingsOpen;

        if (settingsPanel != null) settingsPanel.SetActive(showSettings);
        if (boardBody != null) boardBody.SetActive(!showSettings);
        if (settingsBoardBtn != null) settingsBoardBtn.gameObject.SetActive(!showSettings);
        if (aboutBoardBtn != null) aboutBoardBtn.gameObject.SetActive(!showSettings);

        wallet.text = p.credits.ToString("N0") + " <size=18>xu</size>";
        completed.text = "ĐÃ HOÀN THÀNH  " + p.completedContracts;

        if (showSettings) { RefreshSettingsUI(isPaused); return; }

        int curPotion = weapons != null ? weapons.PotionCount : p.healthPotionCount;
        int curGrenade = weapons != null ? weapons.GrenadeCount : p.grenadeCount;
        int curPistol = weapons != null ? weapons.PistolReserve : p.pistolAmmoReserve;
        int curRifle = weapons != null ? weapons.RifleReserve : p.rifleAmmoReserve;

        if (menuPhase == state && lastCredits == p.credits && lastDamage == p.damageLevel &&
            lastMagazine == p.magazineLevel && lastCompleted == p.completedContracts &&
            lastPotionCount == curPotion && lastGrenadeCount == curGrenade &&
            lastPistolAmmo == curPistol && lastRifleAmmo == curRifle &&
            lastTabSupplies == showingSuppliesTab && lastSelectedMap == selectedMapIndex) return;

        menuPhase = state; lastCredits = p.credits; lastDamage = p.damageLevel;
        lastMagazine = p.magazineLevel; lastCompleted = p.completedContracts;
        lastPotionCount = curPotion; lastGrenadeCount = curGrenade;
        lastPistolAmmo = curPistol; lastRifleAmmo = curRifle;
        lastTabSupplies = showingSuppliesTab; lastSelectedMap = selectedMapIndex;

        start.gameObject.SetActive(isBoard);
        resume.gameObject.SetActive(isPaused);
        retry.gameObject.SetActive(!isBoard && !isPaused);

        var retryTxt = retry.GetComponentInChildren<TMP_Text>();
        if (retryTxt != null)
        {
            if (won)
            {
                string nextName = !string.IsNullOrEmpty(game.NextSceneName) ? "ĐÈO BĂNG (ICEBOUND PASS)" : "MÀN TIẾP THEO";
                int secs = Mathf.CeilToInt(game.TransitionRemainingTime);
                retryTxt.text = secs > 0 ? $"QUA MÀN: {nextName} ({secs}s)   →  [ENTER]" : $"QUA MÀN: {nextName}   →  [ENTER]";
            }
            else retryTxt.text = "CHƠI LẠI NHIỆM VỤ NÀY   →  [ENTER]";
        }
        pausedRetry.gameObject.SetActive(isPaused);
        if (board != null) ((RectTransform)board.transform).anchoredPosition = new Vector2(24, isPaused ? -255 : -195);
        shop.SetActive(isBoard);
        summary.SetActive(!isBoard);
        if (map1Btn != null) map1Btn.gameObject.SetActive(isBoard);
        if (map2Btn != null) map2Btn.gameObject.SetActive(isBoard);

        if (isBoard)
        {
            eyebrow.text = game.IsContractStarted ? "ĐANG SĂN QUÁI  /  CỬA HÀNG TIẾP TẾ" : "CHỌN MAP  •  BẢN ĐỒ CHIẾN DỊCH";
            bool isMap1 = selectedMapIndex == 0;
            string curScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            bool isCurrent = (isMap1 && curScene == "AutumnValley") || (!isMap1 && curScene == "IceboundPass");

            if (map1Btn != null)
            {
                map1Btn.image.color = isMap1 ? Accent : Tile;
                if (map1BtnText != null)
                {
                    map1BtnText.color = isMap1 ? Ink : White;
                    map1BtnText.text = isMap1 ? "<b>MAP 1: THUNG LŨNG LÁ ĐỎ</b>" : "MAP 1: THUNG LŨNG LÁ ĐỎ";
                }
            }
            if (map2Btn != null)
            {
                map2Btn.image.color = !isMap1 ? Accent : Tile;
                if (map2BtnText != null)
                {
                    map2BtnText.color = !isMap1 ? Ink : White;
                    map2BtnText.text = !isMap1 ? "<b>MAP 2: ĐÈO BĂNG GIÁ</b>" : "MAP 2: ĐÈO BĂNG GIÁ";
                }
            }

            if (isMap1)
            {
                missionTitle.text = "THUNG LŨNG LÁ ĐỎ" + (isCurrent ? "  <size=18><color=#2A9D8F>(BẢN ĐỒ HIỆN TẠI)</color></size>" : "");
                briefing.text = "Theo đường đất qua cầu phía tây hoặc đi vòng qua cầu phía đông.\nHạ quái laser tại trại cờ đỏ, rồi quay về vòng xanh ở trại xuất phát.";
                rewardValue.text = "300 <size=22>xu</size>";
            }
            else
            {
                missionTitle.text = "ĐÈO BĂNG GIÁ (ICEBOUND)" + (isCurrent ? "  <size=18><color=#2A9D8F>(BẢN ĐỒ HIỆN TẠI)</color></size>" : "");
                briefing.text = "Băng qua hồ đóng băng hoặc theo đường tuyết ven núi phía đông.\nHạ quái vật chỉ huy tại tiền đồn băng, rồi quay về vòng xanh ở làng xuất phát.";
                rewardValue.text = "350 <size=22>xu</size>";
            }

            missionStripe.color = Accent;
            rewardCaption.text = "THƯỞNG HOÀN THÀNH";
            rewardHint.text = isCurrent ? "Hạ mục tiêu. Rút lui an toàn.\nThưởng chỉ được nhận khi trở về." : "Nhấn [ENTER] hoặc bấm nút\ndưới để nạp bản đồ này.";

            var startTxt = start.GetComponentInChildren<TMP_Text>();
            if (startTxt != null)
            {
                string mapLabel = isMap1 ? "THUNG LŨNG LÁ ĐỎ" : "ĐÈO BĂNG GIÁ";
                startTxt.text = isCurrent
                    ? (game.IsContractStarted ? "TIẾP TỤC TRẬN ĐẤU   →  [ENTER]" : $"VÀO TRẬN: {mapLabel}   →  [ENTER]")
                    : $"CHUYỂN SANG MAP: {mapLabel}   →  [ENTER]";
            }
        }
        else
        {
            eyebrow.text = isPaused ? "LƯỢT SĂN ĐANG TẠM DỪNG" : won ? "NHIỆM VỤ THÀNH CÔNG" : "NHIỆM VỤ KHÔNG HOÀN THÀNH";
            missionTitle.text = isPaused ? "TẠM DỪNG" : won ? "NHIỆM VỤ HOÀN TẤT" : "LƯỢT SĂN KẾT THÚC";
            briefing.text = isPaused
                ? game.MissionTitle + "\nTiếp tục khi bạn đã sẵn sàng. Lượt săn và quá trình nạp đạn đang tạm dừng."
                : won
                    ? "Mục tiêu đã bị tiêu diệt và bạn đã rút lui an toàn!\nĐang chuẩn bị tiến vào " + (!string.IsNullOrEmpty(game.NextSceneName) ? "ĐÈO BĂNG GIÁ (Icebound Pass)" : "màn tiếp theo") + ".\nNhấn [ENTER] hoặc bấm nút bên dưới để chuyển màn ngay."
                    : (string.IsNullOrEmpty(game.FailureReason) ? "Bạn đã bị hạ. Chuẩn bị lại trang bị và thử sức một lần nữa." : game.FailureReason);
            missionStripe.color = won ? Mint : (!isBoard && !isPaused ? Danger : Accent);
            rewardCaption.text = won ? "PHẦN THƯỞNG ĐÃ LƯU" : !isBoard && !isPaused ? "PHẦN THƯỞNG NHẬN ĐƯỢC" : "THƯỞNG HOÀN THÀNH";
            rewardValue.text = (!isBoard && !isPaused && !won ? "0" : game.Reward.ToString()) + " <size=22>xu</size>";
            rewardHint.text = won ? "Rút lui thành công.\nTiến vào Icebound Pass." : !isBoard && !isPaused
                ? "Chết sẽ không nhận thưởng.\nBạn có thể thử lại nhiệm vụ." : "Hạ mục tiêu. Rút lui an toàn.\nThưởng chỉ được nhận khi trở về.";
        }
        currentGear.text = "ĐANG DÙNG  +" + p.damageLevel * 10 + "% sát thương\nBăng đạn Lv." + p.magazineLevel;
        summaryMission.text = game.MissionTitle;
        summaryGear.text = "Sát thương +" + p.damageLevel * 10 + "%   •   Băng đạn Lv." + p.magazineLevel;
        Offer(true, damageUpgrade, damageValue, damagePrice);
        Offer(false, magazineUpgrade, magazineValue, magazinePrice);
        RefreshShopSupplies();
    }

    private void RefreshSupplyItem(Button btn, TMP_Text countText, TMP_Text priceText, int current, int max, int cost, string prefix)
    {
        bool isFull = current >= max;
        bool canBuy = !isFull && game.Progress.credits >= cost;
        btn.interactable = canBuy;
        countText.text = $"{prefix}: {current}/{max}" + (isFull ? " (ĐẦY)" : "");
        priceText.text = isFull ? "ĐÃ ĐẦY TỐI ĐA" : canBuy ? $"MUA  •  {cost} xu" : $"CẦN {cost} XU";
        priceText.color = canBuy ? Accent : Muted;
    }

    private void RefreshShopSupplies()
    {
        var p = game.Progress;
        RefreshSupplyItem(buyPotionBtn, potionCountText, potionPriceText, weapons != null ? weapons.PotionCount : p.healthPotionCount, ContractProgress.MaxPotions, ContractProgress.CostPotion, "Đang có");
        RefreshSupplyItem(buyGrenadeBtn, grenadeCountText, grenadePriceText, weapons != null ? weapons.GrenadeCount : p.grenadeCount, ContractProgress.MaxGrenades, ContractProgress.CostGrenade, "Đang có");
        RefreshSupplyItem(buyPistolAmmoBtn, pistolAmmoCountText, pistolAmmoPriceText, weapons != null ? weapons.PistolReserve : p.pistolAmmoReserve, ContractProgress.MaxPistolReserve, ContractProgress.CostPistolAmmo, "Dự trữ");
        RefreshSupplyItem(buyRifleAmmoBtn, rifleAmmoCountText, rifleAmmoPriceText, weapons != null ? weapons.RifleReserve : p.rifleAmmoReserve, ContractProgress.MaxRifleReserve, ContractProgress.CostRifleAmmo, "Dự trữ");
    }

    private void Offer(bool damage, Button button, TMP_Text value, TMP_Text price)
    {
        var p = game.Progress; int level = damage ? p.damageLevel : p.magazineLevel;
        bool max = level >= ContractProgress.MaxLevel; button.interactable = p.CanBuy(damage);
        value.text = damage ? (level * 10) + "%" + (max ? "  /  TỐI ĐA" : "  →  " + ((level + 1) * 10) + "%") :
            "Lv." + level + (max ? "  /  TỐI ĐA" : "  →  Lv." + (level + 1));
        price.text = max ? "ĐÃ NÂNG CẤP TỐI ĐA" : button.interactable ? "MUA  •  " + p.UpgradeCost(damage) + " xu" :
            "CẦN THÊM " + Mathf.Max(0, p.UpgradeCost(damage) - p.credits) + " XU";
        price.color = button.interactable ? Accent : Muted;
    }

    private static void EnsureUIInput()
    {
        var events = EventSystem.current ?? Object.FindFirstObjectByType<EventSystem>() ?? new GameObject("ContractEventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        events.enabled = true;
        var module = events.GetComponent<InputSystemUIInputModule>() ?? events.gameObject.AddComponent<InputSystemUIInputModule>();
        foreach (var other in events.GetComponents<BaseInputModule>()) if (other != module) other.enabled = false;
        module.enabled = true;
        if (module.point == null || module.point.action == null || module.leftClick == null || module.leftClick.action == null) module.AssignDefaultActions();
    }

    private static GameObject Group(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); Stretch((RectTransform)go.transform); return go;
    }
    private static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size, Vector2 anchor)
    {
        var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent, false); r.anchorMin = r.anchorMax = anchor; r.pivot = TopLeft; r.anchoredPosition = position; r.sizeDelta = size; return r;
    }
    private Image Image(Transform parent, string name, Color color, Vector2 position, Vector2 size, Vector2 anchor)
    {
        var image = Rect(name, parent, position, size, anchor).gameObject.AddComponent<Image>();
        image.sprite = whiteSprite; image.color = color; image.raycastTarget = false; return image;
    }
    private Image SlicedImage(Transform parent, string name, Sprite sprite, Color color, Vector2 position, Vector2 size, Vector2 anchor, float ppuMultiplier = 0.5f)
    {
        var image = Rect(name, parent, position, size, anchor).gameObject.AddComponent<UIImage>();
        image.sprite = sprite != null ? sprite : whiteSprite;
        image.type = sprite != null ? UIImage.Type.Sliced : UIImage.Type.Simple;
        image.pixelsPerUnitMultiplier = ppuMultiplier;
        image.color = color; image.raycastTarget = false; return image;
    }
    private Image IconImage(Transform parent, string name, Sprite sprite, Color color, Vector2 position, Vector2 size, Vector2 anchor)
    {
        var image = Rect(name, parent, position, size, anchor).gameObject.AddComponent<UIImage>();
        image.sprite = sprite != null ? sprite : whiteSprite;
        image.type = UIImage.Type.Simple; image.preserveAspect = true;
        image.color = color; image.raycastTarget = false; return image;
    }
    private TMP_Text Label(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, Color color, bool bold = false)
    {
        var text = Rect(name, parent, position, size, TopLeft).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font; text.fontSize = fontSize; text.color = color; text.text = value;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.raycastTarget = false; text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.TopLeft; text.extraPadding = true; return text;
    }
    private Button ButtonAt(Transform parent, string name, string label, Vector2 position, Vector2 size, Color color, Color textColor, UnityAction action, int fontSize)
    {
        var image = SlicedImage(parent, name, spriteBtnNormal, color, position, size, TopLeft, 0.5f);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        colors.pressedColor = new Color(.75f, .75f, .75f); colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(.5f, .5f, .5f, .7f); colors.fadeDuration = .05f; button.colors = colors;
        button.onClick.AddListener(action);
        var text = Label(image.transform, "Label", label, Vector2.zero, size, fontSize, textColor, true);
        text.alignment = TextAlignmentOptions.Center; return button;
    }

    private void OnDestroy()
    {
        if (game != null) game.CreditsAdded -= OnCreditsAdded;
        if (whiteSprite != null) Destroy(whiteSprite);
        if (whiteTexture != null) Destroy(whiteTexture);
    }
}
