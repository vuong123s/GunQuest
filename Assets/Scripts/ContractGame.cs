using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-200)]
public class ContractGame : MonoBehaviour
{
    public enum Phase { Board, Hunting, Extracting, Paused, Won, Lost }
    public static ContractGame Instance { get; private set; }
    public static bool ControlsAllowed => Instance == null || Instance.IsPlaying;
    private static bool acceptOnLoad;
    [SerializeField] private EnemyHealth contractTarget;
    [SerializeField] private Transform extractionPoint;
    [SerializeField] private GameObject extractionMarker;
    [SerializeField] private int reward = 200;
    [SerializeField] private float extractionRadius = 2.5f;
    [Header("Mission")]
    [SerializeField] private string missionTitle = "DỌN SẠCH KHU TRẠI";
    [SerializeField, TextArea(2, 4)] private string missionBriefing = "Săn quái laser, sau đó quay về vòng xanh ở điểm xuất phát.";
    [SerializeField, TextArea(2, 4)] private string missionObjective = "Tiêu diệt quái laser trong khu trại.";
    [Header("Progression")]
    [SerializeField] private string nextSceneName = "IceboundPass";
    [SerializeField] private float autoTransitionDelay = 5f;
    public Phase State { get; private set; }
    public ContractProgress Progress { get; private set; }
    public bool IsPlaying => State == Phase.Hunting || State == Phase.Extracting;
    public float ExitDistance => player != null && extractionPoint != null
        ? Vector3.Distance(player.transform.position, extractionPoint.position) : float.PositiveInfinity;
    public string FailureReason { get; private set; }
    public int Reward => reward;
    public string MissionTitle => missionTitle;
    public string MissionBriefing => missionBriefing;
    public string MissionObjective => missionObjective;
    public string NextSceneName => nextSceneName;
    public float TransitionRemainingTime => Mathf.Max(0f, transitionTimer);
    public event System.Action<int> CreditsAdded;

    public void AddCredits(int amount)
    {
        if (amount <= 0 || Progress == null) return;
        Progress.credits += amount;
        Progress.Save();
        CreditsAdded?.Invoke(amount);
    }
    private Phase resumePhase;
    private PlayerHealth player;
    private InputManager input;
    private ContractUI ui;
    private Coroutine transitionRoutine;
    private float transitionTimer;

    private void Awake()
    {
        Instance = this;
        Progress = ContractProgress.Load();
        State = Phase.Board;
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Start()
    {
        player = Object.FindFirstObjectByType<PlayerHealth>();
        input = player != null ? player.GetComponent<InputManager>() : null;
        if (contractTarget != null) contractTarget.Died += OnTargetDied;
        // Replace only the legacy HUD in this scene; keep the original scene objects recoverable.
        var legacy = GameObject.Find("Canvas");
        if (legacy != null) legacy.SetActive(false);
        var crosshair = Object.FindFirstObjectByType<CrosshairUI>();
        if (crosshair != null) crosshair.gameObject.SetActive(false);
        ui = gameObject.AddComponent<ContractUI>();
        ui.Initialize(this, player);
        SetPhase(Phase.Board);
        if (acceptOnLoad) { acceptOnLoad = false; AcceptContract(); }
    }

    private void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (ui != null && ui.IsAboutOpen) ui.CloseAbout();
                else if (IsPlaying) Pause();
                else if (State == Phase.Paused) Resume();
                else if ((State == Phase.Board || State == Phase.Lost || State == Phase.Won) && ui != null) ui.ToggleSettingsFromBoard();
            }

            // Nhấn phím Enter (hoặc NumpadEnter) để mở / quay lại Bảng Nhiệm Vụ (Board) bất kỳ lúc nào
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (IsPlaying)
                {
                    OpenBoardFromGameplay();
                }
                else if (State == Phase.Board)
                {
                    if (ui != null) ui.OnStartButtonClicked();
                    else AcceptContract();
                }
                else if (State == Phase.Won)
                {
                    NextStage();
                }
                else if (State == Phase.Lost)
                {
                    Retry();
                }
            }
        }
        if (!IsPlaying) return;
        if (player == null || player.IsDead) { Fail("Bạn đã bị hạ. Thử lại nhiệm vụ."); return; }
        if (State == Phase.Hunting && contractTarget == null)
        {
            Fail("Không tìm thấy mục tiêu nhiệm vụ."); return;
        }
        if (State == Phase.Extracting && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            TryExtract();
        if (player.transform.position.y < -10f) player.TakeDamage(player.maxHealth);
    }

    public bool IsContractStarted => resumePhase == Phase.Hunting || resumePhase == Phase.Extracting;

    public void OpenBoardFromGameplay()
    {
        resumePhase = State;
        SetPhase(Phase.Board);
    }

    public void AcceptContract()
    {
        if (State != Phase.Board) return;
        if (resumePhase == Phase.Hunting || resumePhase == Phase.Extracting)
        {
            SetPhase(resumePhase);
            return;
        }
        if (player == null || contractTarget == null || extractionPoint == null)
        {
            Fail("Nhiệm vụ chưa được cấu hình đầy đủ."); return;
        }
        player.GetComponent<PlayerWeapons>()?.ApplyProgress(Progress);
        SetPhase(Phase.Hunting);
    }

    private void OnTargetDied(EnemyHealth target)
    {
        if (State == Phase.Hunting && target == contractTarget) SetPhase(Phase.Extracting);
    }

    public void TryExtract()
    {
        if (State != Phase.Extracting || player == null || player.IsDead || ExitDistance > extractionRadius) return;
        Progress.credits += reward;
        Progress.completedContracts++;
        Progress.Save();
        SetPhase(Phase.Won);
        if (!string.IsNullOrEmpty(nextSceneName) && autoTransitionDelay > 0)
        {
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = StartCoroutine(AutoTransitionRoutine());
        }
    }

    private System.Collections.IEnumerator AutoTransitionRoutine()
    {
        transitionTimer = autoTransitionDelay;
        while (transitionTimer > 0f)
        {
            yield return null;
            if (State != Phase.Won) yield break;
            transitionTimer -= Time.unscaledDeltaTime;
        }
        NextStage();
    }

    public void NextStage()
    {
        if (transitionRoutine != null) { StopCoroutine(transitionRoutine); transitionRoutine = null; }
        if (string.IsNullOrEmpty(nextSceneName))
        {
            ReturnToBoard();
            return;
        }
        acceptOnLoad = false;
        Time.timeScale = 1;
        SceneManager.LoadScene(nextSceneName);
    }

    public void Pause() { if (!IsPlaying) return; resumePhase = State; SetPhase(Phase.Paused); }
    public void Resume() { if (State == Phase.Paused) SetPhase(resumePhase); }
    public void Retry() => Reload(true);
    public void ReturnToBoard() => Reload(false);
    private void Reload(bool accept)
    {
        acceptOnLoad = accept;
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public bool BuyUpgrade(bool damage)
    {
        if (State != Phase.Board || !Progress.TryBuy(damage)) return false;
        Progress.Save();
        player?.GetComponent<PlayerWeapons>()?.ApplyProgress(Progress);
        return true;
    }

    public bool BuyPistolAmmo()
    {
        if (State != Phase.Board || !Progress.TryBuyPistolAmmo()) return false;
        Progress.Save();
        player?.GetComponent<PlayerWeapons>()?.ApplyProgress(Progress);
        return true;
    }

    public bool BuyRifleAmmo()
    {
        if (State != Phase.Board || !Progress.TryBuyRifleAmmo()) return false;
        Progress.Save();
        player?.GetComponent<PlayerWeapons>()?.ApplyProgress(Progress);
        return true;
    }

    public bool BuyPotion()
    {
        if (State != Phase.Board || !Progress.TryBuyPotion()) return false;
        Progress.Save();
        player?.GetComponent<PlayerWeapons>()?.ApplyProgress(Progress);
        return true;
    }

    public bool BuyGrenade()
    {
        if (State != Phase.Board || !Progress.TryBuyGrenade()) return false;
        Progress.Save();
        player?.GetComponent<PlayerWeapons>()?.ApplyProgress(Progress);
        return true;
    }

    private void Fail(string reason) { FailureReason = reason; SetPhase(Phase.Lost); }
    private void SetPhase(Phase phase)
    {
        State = phase;
        Time.timeScale = IsPlaying ? 1f : 0f;
        Cursor.lockState = IsPlaying ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !IsPlaying;
        if (input != null) input.enabled = IsPlaying;
        if (extractionMarker != null) extractionMarker.SetActive(phase == Phase.Extracting);
        if (ui != null) ui.Refresh();
    }

    private void OnDestroy()
    {
        if (transitionRoutine != null) { StopCoroutine(transitionRoutine); transitionRoutine = null; }
        if (contractTarget != null) contractTarget.Died -= OnTargetDied;
        if (Instance != this) return;
        Instance = null;
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
