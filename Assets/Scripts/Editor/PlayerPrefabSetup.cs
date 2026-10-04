using UnityEditor;
using UnityEngine;

public static class PlayerPrefabSetup
{
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";

    [MenuItem("Tools/Gun Quest/Attach Required Scripts To Player Prefab")]
    private static void SetupPlayerPrefab()
    {
        GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        if (player == null)
        {
            Debug.LogError("Player prefab was not found at " + PlayerPrefabPath);
            return;
        }

        try
        {
            player.tag = "Player";
            AddIfMissing<CharacterController>(player);
            AddIfMissing<PlayerMotor>(player);
            AddIfMissing<PlayerLook>(player);
            AddIfMissing<PlayerHealth>(player);
            AddIfMissing<PlayerInteract>(player);
            AddIfMissing<PlayerShoot>(player);
            AddIfMissing<WeaponEffects>(player);
            AddIfMissing<PlayerMelee>(player);
            AddIfMissing<PlayerCameraFollow>(player);
            AddIfMissing<InputManager>(player);
            PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(player);
        }

        Debug.Log("Player prefab setup completed.");
    }

    private static void AddIfMissing<T>(GameObject target) where T : Component
    {
        if (target.GetComponent<T>() == null)
        {
            target.AddComponent<T>();
        }
    }
}
