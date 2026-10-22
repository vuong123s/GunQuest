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
    public Phase State { get; private set; }
    public ContractProgress Progress { get; private set; }
    public bool IsPlaying => State == Phase.Hunting || State == Phase.Extracting;
    public float ExitDistance => player != null && extractionPoint != null
        ? Vector3.Distance(player.transform.position, extractionPoint.position) : float.PositiveInfinity;
    public string FailureReason { get; private set; }
    public int Reward => reward;
    private Phase resumePhase;
    private PlayerHealth player;
    private InputManager input;
    private ContractUI ui;

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
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (IsPlaying) Pause();
            else if (State == Phase.Paused) Resume();
        }
        if (!IsPlaying) return;
        if (player == null || player.IsDead) { Fail("Bạn đã bị hạ. Thử lại hợp đồng."); return; }
        if (State == Phase.Hunting && contractTarget == null)
        {
            Fail("Không tìm thấy mục tiêu hợp đồng."); return;
        }
        if (State == Phase.Extracting && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            TryExtract();
        if (player.transform.position.y < -10f) player.TakeDamage(player.maxHealth);
    }

    public void AcceptContract()
    {
        if (State != Phase.Board) return;
        if (player == null || contractTarget == null || extractionPoint == null)
        {
            Fail("Hợp đồng chưa được cấu hình đầy đủ."); return;
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
