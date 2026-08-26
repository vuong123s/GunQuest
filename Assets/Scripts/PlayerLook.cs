using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerLook : MonoBehaviour
{
    public Camera cam;
    private float xRotation = 0f;

    public float xSensitivity = 30f;
    public float ySensitivity = 30f;

    void Awake()
    {
        if (cam == null)
        {
            cam = GetComponentInChildren<Camera>();
        }

        if (cam == null && Camera.main != null)
        {
            cam = Camera.main;
        }
    }

    public void ProcessLook(Vector2 input)
    {
        if (cam == null)
        {
            return;
        }

        float mouseX = input.x;
        float mouseY = input.y;

        // Tính toán xoay camera lên và xuống [9]
        xRotation -= (mouseY * Time.deltaTime) * ySensitivity;
        // Giới hạn góc nhìn trong khoảng -80 đến 80 độ để tránh lộn ngược [10]
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        // Áp dụng xoay cho Camera [10]
        cam.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);

        // Xoay toàn bộ cơ thể nhân vật để nhìn sang trái và phải [10]
        transform.Rotate(Vector3.up * (mouseX * Time.deltaTime) * xSensitivity);
    }
}
