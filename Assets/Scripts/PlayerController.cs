using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Components")] 
    public CharacterController controller;
    public Transform playerCamera;
    
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 1.5f;
    public float gravity = 20f;
    
    [Header("Camera")]
    public float sensitivity = 5.0f;
    float yLimit = 85f;

    private Vector3 velocity;
    Vector2 cameraRotation;
    
    private void Update()
    {
        // Mouse 
        // https://gist.github.com/KarlRamstedt/407d50725c7b6abeaf43aee802fdd88e
        cameraRotation.x += Input.GetAxis("Mouse X") * sensitivity;
        cameraRotation.y += Input.GetAxis("Mouse Y") * sensitivity;
        cameraRotation.y = Mathf.Clamp(cameraRotation.y, -yLimit, yLimit);
        var xQuat = Quaternion.AngleAxis(cameraRotation.x, Vector3.up);
        var yQuat = Quaternion.AngleAxis(cameraRotation.y, Vector3.left);
        playerCamera.localRotation = xQuat * yQuat;
        
        
        // Make player face camera horizontal direction
        transform.rotation = Quaternion.Euler(0, cameraRotation.x, 0);
        
        
        
        // https://docs.unity3d.com/ScriptReference/CharacterController.Move
        Vector3 frameInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        if(frameInput.magnitude > 1)
            frameInput.Normalize();
        
        // Rotate input to match camera direction
        // Rotate frame input by camera rotation
        Vector3 cameraForward = playerCamera.transform.forward;
        Vector3 cameraRight = playerCamera.transform.right;
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();
        Vector3 forwardInput = frameInput.z * cameraForward;
        Vector3 rightInput = frameInput.x * cameraRight;
        frameInput = forwardInput + rightInput;
        
        
        if (controller.isGrounded)
            velocity.y = 0;
        

        // Jump
        if (Input.GetKey(KeyCode.Space) && controller.isGrounded)
            velocity.y = jumpForce;
        
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        
        Vector3 frameVelocity = frameInput * moveSpeed + new Vector3(0, velocity.y, 0);
        
        controller.Move(frameVelocity * Time.deltaTime);
        
    }
}
