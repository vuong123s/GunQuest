using UnityEngine;

public class GameSettings
{
    private static GameSettings instance;
    public static GameSettings Instance => instance ??= Load();

    // Configurable Settings
    public float masterVolume = 1.0f;      // 0.0f - 1.0f
    public float sfxVolume = 0.8f;         // 0.0f - 1.0f
    public float mouseSensitivity = 0.12f; // 0.02f - 0.36f
    public bool invertY = false;
    public float fieldOfView = 75f;        // 60f - 105f
    public int qualityLevel = 1;           // Index in QualitySettings.names
    public bool fullscreen = true;

    public event System.Action Changed;

    private const string KEY_MASTER = "GQ_MasterVol";
    private const string KEY_SFX = "GQ_SfxVol";
    private const string KEY_SENS = "GQ_Sensitivity";
    private const string KEY_INVERT = "GQ_InvertY";
    private const string KEY_FOV = "GQ_FOV";
    private const string KEY_QUALITY = "GQ_Quality";
    private const string KEY_FULLSCREEN = "GQ_Fullscreen";

    public static GameSettings Load()
    {
        var s = new GameSettings();
        s.masterVolume = PlayerPrefs.GetFloat(KEY_MASTER, 1.0f);
        s.sfxVolume = PlayerPrefs.GetFloat(KEY_SFX, 0.8f);
        s.mouseSensitivity = PlayerPrefs.GetFloat(KEY_SENS, 0.12f);
        s.invertY = PlayerPrefs.GetInt(KEY_INVERT, 0) == 1;
        s.fieldOfView = PlayerPrefs.GetFloat(KEY_FOV, 75f);

        int defaultQuality = QualitySettings.names.Length > 1 ? 1 : 0;
        s.qualityLevel = Mathf.Clamp(PlayerPrefs.GetInt(KEY_QUALITY, defaultQuality), 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        s.fullscreen = PlayerPrefs.GetInt(KEY_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
        return s;
    }

    public void Save()
    {
        PlayerPrefs.SetFloat(KEY_MASTER, masterVolume);
        PlayerPrefs.SetFloat(KEY_SFX, sfxVolume);
        PlayerPrefs.SetFloat(KEY_SENS, mouseSensitivity);
        PlayerPrefs.SetInt(KEY_INVERT, invertY ? 1 : 0);
        PlayerPrefs.SetFloat(KEY_FOV, fieldOfView);
        PlayerPrefs.SetInt(KEY_QUALITY, qualityLevel);
        PlayerPrefs.SetInt(KEY_FULLSCREEN, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ResetDefaults()
    {
        masterVolume = 1.0f;
        sfxVolume = 0.8f;
        mouseSensitivity = 0.12f;
        invertY = false;
        fieldOfView = 75f;
        qualityLevel = Mathf.Clamp(1, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        fullscreen = true;
        Apply();
    }

    public void Apply()
    {
        // 1. Audio Master
        AudioListener.volume = Mathf.Clamp01(masterVolume);

        // 2. Quality
        if (qualityLevel >= 0 && qualityLevel < QualitySettings.names.Length)
        {
            if (QualitySettings.GetQualityLevel() != qualityLevel)
            {
                QualitySettings.SetQualityLevel(qualityLevel, true);
            }
        }

        // 3. Screen mode
        if (Screen.fullScreen != fullscreen)
        {
            Screen.fullScreen = fullscreen;
        }

        // 4. Camera FOV
        var camFollow = Object.FindFirstObjectByType<PlayerCameraFollow>();
        if (camFollow != null && camFollow.TargetCamera != null)
        {
            camFollow.TargetCamera.fieldOfView = fieldOfView;
            if (camFollow.WeaponCamera != null)
                camFollow.WeaponCamera.fieldOfView = fieldOfView;
        }
        else if (Camera.main != null)
        {
            Camera.main.fieldOfView = fieldOfView;
        }

        // 5. Player Look
        var look = Object.FindFirstObjectByType<PlayerLook>();
        if (look != null)
        {
            look.xSensitivity = mouseSensitivity;
            look.ySensitivity = mouseSensitivity;
            look.invertY = invertY;
        }

        Save();
        Changed?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        Instance.Apply();
    }
}
