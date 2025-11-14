using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraArmController : MonoBehaviour
{
    public float verticalClamp = 30f;
    public Vector2 sensitivity = Vector2.one;

    private Vector2 input;

    private bool IsLockingMouse;

    public void OnLockCamera(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            IsLockingMouse = true;
        }
        else if (ctx.canceled)
        {
            IsLockingMouse = false;
        }
    }

    public void OnLook(InputAction.CallbackContext ctx)
    {
        input = ctx.ReadValue<Vector2>();
    }
    
    private void FixedUpdate()
    {
        AdjustCamera();
    }

    void AdjustCamera()
    {

        if (IsLockingMouse)
        {
            Cursor.lockState = CursorLockMode.Confined;
            input *= sensitivity;
            transform.localRotation = Quaternion.Euler(new Vector3(-input.y, input.x, 0) + transform.localRotation.eulerAngles);

            float clamped_x = 0;

            if (transform.localRotation.eulerAngles.x < 180)
                clamped_x = Mathf.Clamp(transform.localRotation.eulerAngles.x, -verticalClamp, verticalClamp);
            else
                clamped_x = Mathf.Clamp(transform.localRotation.eulerAngles.x, 360f - verticalClamp, 360f + verticalClamp);

            transform.localRotation = Quaternion.Euler(
                new Vector3(
                    clamped_x,
                    transform.localRotation.eulerAngles.y,
                    0));
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }

        
    }
}
