using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Render-only rig. Bone poses come from the gameplay Animator, so there are no
// duplicate animation timers, reload events, damage scripts or input actions.
public sealed class AnimatedFirstPersonRig : IDisposable
{
    private readonly GameObject root;
    private readonly Animator source;
    private readonly PlayerWeapons weapons;
    private readonly Transform cameraTransform;
    private readonly Transform[] hands = new Transform[2];
    private readonly Transform[] sleeves = new Transform[2];
    private readonly Dictionary<Transform, Transform> copies = new Dictionary<Transform, Transform>();
    private readonly List<KeyValuePair<Transform, Transform>> poses = new List<KeyValuePair<Transform, Transform>>();
    private Transform muzzle;
    public Transform Muzzle => muzzle;
    public GameObject Root => root;

    public AnimatedFirstPersonRig(Animator source, PlayerWeapons weapons, Camera camera)
    {
        this.source = source;
        this.weapons = weapons;
        cameraTransform = camera.transform;
        root = Object.Instantiate(source.gameObject);
        root.name = "FirstPersonAnimatedArms";
        root.transform.SetParent(camera.transform, false);
        root.transform.localScale = Vector3.one;
        Map(source.transform, root.transform, true);
        foreach (var animator in root.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
        foreach (var script in root.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
        foreach (var collider in root.GetComponentsInChildren<Collider>(true))
        { collider.enabled = false; Object.Destroy(collider); }
        foreach (var body in root.GetComponentsInChildren<Rigidbody>(true)) Object.Destroy(body);
        foreach (var particles in root.GetComponentsInChildren<ParticleSystem>(true))
        { particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); Object.Destroy(particles); }
        foreach (var light in root.GetComponentsInChildren<Light>(true))
        { light.enabled = false; Object.Destroy(light); }
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = false;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        int arms = 0;
        foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mesh = Resources.Load<Mesh>("FemaleFirstPerson" + skin.name);
            if (mesh == null) continue;
            skin.sharedMesh = mesh;
            skin.enabled = true;
            skin.updateWhenOffscreen = true;
            // Bounds encompass all reload poses; culling must not hide the hands mid-reload.
            skin.localBounds = new Bounds(Vector3.zero, Vector3.one * 6f);
            arms++;
        }
        if (arms == 0) Debug.LogError("Female first-person arm meshes are missing.", source);
        CreateSleeves();
        SetWeapon();
    }

    private void CreateSleeves()
    {
        // Third-person elbows sit beside the camera. Join animated hands to
        // sleeves entering from below the frame, without changing bone poses.
        var mesh = Resources.Load<Mesh>("FemaleFirstPersonSleeve");
        var material = Resources.Load<Material>("FemaleFirstPersonSleeve");
        if (mesh == null || material == null) return;
        for (int i = 0; i < 2; i++)
        {
            var original = source.GetBoneTransform(i == 0 ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            if (original == null || !copies.TryGetValue(original, out hands[i])) continue;
            var sleeve = new GameObject(i == 0 ? "LeftViewSleeve" : "RightViewSleeve");
            sleeves[i] = sleeve.transform;
            sleeves[i].SetParent(root.transform, false);
            sleeve.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = sleeve.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private void Map(Transform original, Transform copy, bool isRoot)
    {
        copies.Add(original, copy);
        if (!isRoot) poses.Add(new KeyValuePair<Transform, Transform>(original, copy));
        for (int i = 0; i < original.childCount; i++) Map(original.GetChild(i), copy.GetChild(i), false);
    }

    public void SetWeapon()
    {
        if (weapons.Current == null) return;
        // Mirror source slot activation, including switches while reload is cancelled.
        foreach (var pair in copies)
        {
            if (pair.Key == source.transform || pair.Key == null || pair.Value == null) continue;
            pair.Value.gameObject.SetActive(pair.Key.gameObject.activeSelf);
        }
        if (copies.TryGetValue(weapons.Current.model.transform, out var model))
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true)) renderer.enabled = true;
        muzzle = null;
        if (weapons.Current.muzzle != null) copies.TryGetValue(weapons.Current.muzzle, out muzzle);
        if (muzzle == null && model != null) muzzle = model;
    }

    public void UpdatePose(Vector3 offset)
    {
        if (root == null || source == null) return;
        root.SetActive(ContractGame.ControlsAllowed);
        root.transform.localPosition = offset;
        root.transform.localRotation = Quaternion.identity;
        foreach (var pair in poses)
        {
            if (pair.Key == null || pair.Value == null) continue;
            pair.Value.localPosition = pair.Key.localPosition;
            pair.Value.localRotation = pair.Key.localRotation;
            pair.Value.localScale = pair.Key.localScale;
        }
        for (int i = 0; i < 2; i++)
        {
            if (sleeves[i] == null || hands[i] == null) continue;
            float side = i == 0 ? -1f : 1f;
            Vector3 start = cameraTransform.TransformPoint(new Vector3(side * 0.45f, -0.65f, -0.15f));
            Vector3 end = hands[i].TransformPoint(new Vector3(side * 0.025f, 0f, 0f));
            Vector3 direction = end - start;
            sleeves[i].position = start;
            sleeves[i].rotation = Quaternion.LookRotation(direction, cameraTransform.up);
            sleeves[i].localScale = new Vector3(0.1f, 0.1f, direction.magnitude);
        }
    }

    public void Dispose()
    {
        if (root != null) Object.Destroy(root);
    }
}
