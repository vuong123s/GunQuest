using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

public class ContractUI : MonoBehaviour
{
    private ContractGame game;
    private PlayerHealth health;
    private PlayerWeapons weapons;
    private GameObject hud, modal;
    private TMP_Text objective, ammo, healthValue, weaponList, title, description, wallet, controls;
    private Button start, resume, retry, board, damageUpgrade, magazineUpgrade, quit;
    private TMP_FontAsset font;
    private Canvas canvas;
    private static readonly Color Teal = new Color(0.12f, 0.54f, 0.46f);
    private static readonly Color Panel = new Color(0.035f, 0.06f, 0.08f, 0.97f);

    public void Initialize(ContractGame owner, PlayerHealth player)
    {
        game = owner; health = player;
        weapons = player != null ? player.GetComponent<PlayerWeapons>() : null;
        font = Resources.Load<TMP_FontAsset>("GunQuestUIFont");
        if (font == null) font = TMP_Settings.defaultFontAsset;
        EnsureUIInput();
        var root = new GameObject("ContractCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 200;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
        hud = Group("HUD", root.transform);
        objective = TextAt(hud.transform, "Objective", "", new Vector2(32, -30), new Vector2(780, 100), new Vector2(0, 1), 22);
        var healthBack = ImageAt(hud.transform, "HealthBack", new Color(0.9f, 0.7f, 0.2f), new Vector2(32, 78), new Vector2(250, 14), Vector2.zero);
        var healthFront = ImageAt(hud.transform, "HealthFront", Teal, new Vector2(32, 78), new Vector2(250, 14), Vector2.zero);
        var white = new Texture2D(1, 1); white.SetPixel(0, 0, Color.white); white.Apply();
        var sprite = Sprite.Create(white, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
        healthBack.sprite = healthFront.sprite = sprite;
        healthBack.type = healthFront.type = Image.Type.Filled;
        healthBack.fillMethod = healthFront.fillMethod = Image.FillMethod.Horizontal;
        healthValue = TextAt(hud.transform, "Health", "", new Vector2(32, 115), new Vector2(260, 30), Vector2.zero, 22);
        ammo = TextAt(hud.transform, "Ammo", "", new Vector2(-330, 95), new Vector2(300, 80), new Vector2(1, 0), 26);
        weaponList = TextAt(hud.transform, "WeaponSlots", "", new Vector2(-270, 40), new Vector2(540, 30), new Vector2(0.5f, 0), 18);
        weaponList.alignment = TextAlignmentOptions.Center;
        var crosshair = TextAt(hud.transform, "Crosshair", "+", new Vector2(-16, 16), new Vector2(32, 32), Vector2.one * 0.5f, 24);
        crosshair.alignment = TextAlignmentOptions.Center;
        var overlay = ImageAt(hud.transform, "DamageOverlay", new Color(0.6f, 0, 0, 0), Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch(overlay.rectTransform);
        if (health != null) { health.frontHealthBar = healthFront; health.backHealthBar = healthBack; health.overlay = overlay; }
        modal = Group("Menu", root.transform);
        var dim = ImageAt(modal.transform, "Backdrop", new Color(0, 0, 0, 0.65f), Vector2.zero, Vector2.zero, Vector2.zero); Stretch(dim.rectTransform);
        var card = ImageAt(modal.transform, "ContractPanel", Panel, new Vector2(-390, 310), new Vector2(780, 620), Vector2.one * 0.5f);
        title = TextAt(card.transform, "Title", "GUNQUEST", new Vector2(36, -30), new Vector2(708, 50), new Vector2(0, 1), 32);
        description = TextAt(card.transform, "Description", "", new Vector2(36, -100), new Vector2(708, 165), new Vector2(0, 1), 22);
        wallet = TextAt(card.transform, "Wallet", "", new Vector2(36, -275), new Vector2(708, 60), new Vector2(0, 1), 19);
        start = ButtonAt(card.transform, "NHẬN HỢP ĐỒNG", 36, -350, 708, () => game.AcceptContract());
        resume = ButtonAt(card.transform, "TIẾP TỤC", 36, -350, 708, () => game.Resume());
        retry = ButtonAt(card.transform, "CHƠI LẠI", 36, -350, 340, () => game.Retry());
        board = ButtonAt(card.transform, "BẢNG HỢP ĐỒNG", 404, -350, 340, () => game.ReturnToBoard());
        damageUpgrade = ButtonAt(card.transform, "NÂNG SÁT THƯƠNG", 36, -412, 340, () => game.BuyUpgrade(true));
        magazineUpgrade = ButtonAt(card.transform, "NÂNG BĂNG ĐẠN", 404, -412, 340, () => game.BuyUpgrade(false));
        quit = ButtonAt(card.transform, "THOÁT GAME", 36, -474, 708, () => game.Quit());
        controls = TextAt(card.transform, "Controls",
            "WASD: di chuyển  •  Chuột: nhìn / bắn  •  Space: nhảy\nShift: chạy  •  Ctrl: ngồi  •  1–4: đổi vũ khí  •  R: nạp đạn  •  Esc: tạm dừng",
            new Vector2(36, -538), new Vector2(708, 65), new Vector2(0, 1), 18);
        Refresh();
    }

    private void Update() { if (game != null && canvas != null) Refresh(); }
    public void Refresh()
    {
        if (hud == null) return;
        bool active = game.IsPlaying;
        hud.SetActive(active); modal.SetActive(!active);
        if (health != null) healthValue.text = "MÁU  " + Mathf.CeilToInt(health.GetCurrentHealth()) + " / " + Mathf.CeilToInt(health.maxHealth);
        if (weapons != null && weapons.Current != null)
        {
            ammo.text = weapons.Current.label + "\n" + (weapons.Current.melee ? "CẬN CHIẾN" :
                weapons.IsReloading ? "ĐANG NẠP ĐẠN..." : weapons.Ammo + " / " + weapons.Reserve + "   [R]");
            string slots = "";
            for (int i = 0; i < weapons.Count; i++) slots += (i == weapons.SelectedIndex ? "[ " : "") +
                (i + 1) + " " + weapons.GetLabel(i) + (i == weapons.SelectedIndex ? " ]" : "") + "   ";
            weaponList.text = slots;
        }
        objective.text = game.State == ContractGame.Phase.Hunting
            ? "HỢP ĐỒNG: DỌN SẠCH KHU TRẠI\nTiêu diệt quái laser trong khu trại."
            : "MỤC TIÊU ĐÃ BỊ HẠ • QUAY VỀ ĐIỂM RÚT LUI\n" +
                (game.ExitDistance <= 2.5f ? "Nhấn E để rút lui và nhận thưởng." : "Điểm rút lui: " + Mathf.CeilToInt(game.ExitDistance) + " m");
        var state = game.State;
        bool isBoard = state == ContractGame.Phase.Board;
        start.gameObject.SetActive(isBoard);
        damageUpgrade.gameObject.SetActive(isBoard); magazineUpgrade.gameObject.SetActive(isBoard);
        damageUpgrade.interactable = game.Progress.CanBuy(true); magazineUpgrade.interactable = game.Progress.CanBuy(false);
        damageUpgrade.GetComponentInChildren<TMP_Text>().text = "SÁT THƯƠNG +" + ((game.Progress.damageLevel + 1) * 10) + "% • " +
            (game.Progress.damageLevel >= ContractProgress.MaxLevel ? "TỐI ĐA" : game.Progress.UpgradeCost(true) + " xu");
        magazineUpgrade.GetComponentInChildren<TMP_Text>().text = "BĂNG ĐẠN Lv." + (game.Progress.magazineLevel + 1) + " • " +
            (game.Progress.magazineLevel >= ContractProgress.MaxLevel ? "TỐI ĐA" : game.Progress.UpgradeCost(false) + " xu");
        resume.gameObject.SetActive(state == ContractGame.Phase.Paused);
        retry.gameObject.SetActive(state == ContractGame.Phase.Lost || state == ContractGame.Phase.Won || state == ContractGame.Phase.Paused);
        board.gameObject.SetActive(state == ContractGame.Phase.Lost || state == ContractGame.Phase.Won || state == ContractGame.Phase.Paused);
        ((RectTransform)retry.transform).anchoredPosition = new Vector2(36, state == ContractGame.Phase.Paused ? -412 : -350);
        ((RectTransform)board.transform).anchoredPosition = new Vector2(404, state == ContractGame.Phase.Paused ? -412 : -350);
        wallet.text = "VÍ: " + game.Progress.credits + " xu  •  ĐÃ HOÀN THÀNH: " + game.Progress.completedContracts +
            "\nNâng cấp hiện tại: sát thương +" + (game.Progress.damageLevel * 10) + "% / băng đạn Lv." + game.Progress.magazineLevel;
        title.text = isBoard ? "GUNQUEST / BẢNG HỢP ĐỒNG" : state == ContractGame.Phase.Paused ? "TẠM DỪNG" :
            state == ContractGame.Phase.Won ? "HỢP ĐỒNG HOÀN THÀNH" : "HỢP ĐỒNG THẤT BẠI";
        description.text = isBoard ? "DỌN SẠCH KHU TRẠI\nSăn quái laser, sau đó quay về vòng xanh ở điểm xuất phát.\nPhần thưởng: " + game.Reward + " xu. Chết sẽ không nhận thưởng."
            : state == ContractGame.Phase.Paused ? "Nhấn Esc hoặc TIẾP TỤC để trở lại hợp đồng.\nNạp đạn và chiến đấu được tạm dừng."
            : state == ContractGame.Phase.Won ? "Mục tiêu đã bị hạ và bạn đã rút lui an toàn.\n+" + game.Reward + " xu đã được lưu. Dùng tiền để nâng cấp cho lượt tiếp theo."
            : game.FailureReason;
    }

    private static void EnsureUIInput()
    {
        var events = EventSystem.current;
        if (events == null) events = Object.FindFirstObjectByType<EventSystem>();
        if (events == null) events = new GameObject("ContractEventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        events.enabled = true;
        var module = events.GetComponent<InputSystemUIInputModule>();
        if (module == null) module = events.gameObject.AddComponent<InputSystemUIInputModule>();
        foreach (var other in events.GetComponents<BaseInputModule>())
            if (other != module) other.enabled = false;
        module.enabled = true;
        if (module.point == null || module.point.action == null || module.leftClick == null || module.leftClick.action == null)
            module.AssignDefaultActions();
    }
    private static GameObject Group(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); Stretch((RectTransform)go.transform); return go;
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private static RectTransform Place(GameObject go, Transform parent, Vector2 position, Vector2 size, Vector2 anchor)
    {
        go.transform.SetParent(parent, false); var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = anchor; rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
    }
    private TMP_Text TextAt(Transform parent, string name, string value, Vector2 position, Vector2 size, Vector2 anchor, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); Place(go, parent, position, size, anchor);
        var text = go.GetComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = fontSize; text.color = Color.white;
        text.text = value; text.raycastTarget = false; text.overflowMode = TextOverflowModes.Overflow; text.alignment = TextAlignmentOptions.TopLeft; return text;
    }
    private static Image ImageAt(Transform parent, string name, Color color, Vector2 position, Vector2 size, Vector2 anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); Place(go, parent, position, size, anchor);
        var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
    }
    private Button ButtonAt(Transform parent, string label, float x, float y, float width, UnityEngine.Events.UnityAction action)
    {
        var image = ImageAt(parent, label, Teal, new Vector2(x, y), new Vector2(width, 46), new Vector2(0, 1));
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.highlightedColor = new Color(0.7f, 1f, 0.85f); colors.disabledColor = new Color(0.4f, 0.4f, 0.4f); button.colors = colors;
        button.onClick.AddListener(action);
        var text = TextAt(image.transform, "Label", label, Vector2.zero, Vector2.zero, Vector2.zero, 20);
        Stretch(text.rectTransform); text.alignment = TextAlignmentOptions.Center; return button;
    }
}
