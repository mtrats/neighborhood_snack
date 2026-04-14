using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PlayerMovementAnimated : NetworkBehaviour
{
    private bool _jumpPressed;

    private CharacterController _controller;
    private Animator _animator;

    public float PlayerSpeed = 1f;
    public float JumpForce = 5f;
    public float GravityValue = -9.81f;

    private int m_SpeedAnimatorHash = Animator.StringToHash("Speed");
    private int m_GroundedAnimatorHash = Animator.StringToHash("Grounded");
    //private int m_SprintAnimatorHash = Animator.StringToHash("Sprint");
    private int m_VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");

    [Networked] private Vector3 NetworkedVelocity { get; set; }
    [Networked] private NetworkBool IsMoving { get; set; }
    [Networked] private NetworkBool IsGrounded { get; set; }
    

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if(Object.HasStateAuthority)
        {
            if (Input.GetButtonDown("Jump"))
            {
                _jumpPressed = true;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        // FixedUpdateNetwork is only executed on the StateAuthority
        if (!Object.HasStateAuthority) return;

        Vector3 currentVelocity = NetworkedVelocity;

        if (_controller.isGrounded && currentVelocity.y < 0)
        {
            currentVelocity.y = -2f;
        }

        // --- CAMERA RELATIVE MOVEMENT ---
        // 1. Get Input
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // 2. Get Camera Directions
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;

        // 3. Flatten them so jumping/movement isn't tilted
        camForward.y = 0;
        camRight.y = 0;
        //camForward.Normalize();
        //camRight.Normalize();

        Vector3 move = (camForward * v + camRight * h).normalized * PlayerSpeed;
        Vector3 moveDirection = (camForward.normalized * v + camRight.normalized * h).normalized;
        Vector3 moveStep = moveDirection * PlayerSpeed;

        currentVelocity.y += GravityValue * Runner.DeltaTime;
        if (_jumpPressed && _controller.isGrounded)
        {
            currentVelocity.y = JumpForce;
            _jumpPressed = false;
        }

        Vector3 finalMotion = (moveStep + currentVelocity) * Runner.DeltaTime;
        _controller.Move(finalMotion);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            gameObject.transform.forward = moveDirection;
            //_animator.SetFloat(m_SpeedAnimatorHash, 1);
            //_isMoving = true;
            IsMoving = true;
        }
        else
        {
            //_animator.SetFloat(m_SpeedAnimatorHash, 0);
            //_isMoving = false;
            IsMoving = false;
        }
        _jumpPressed = false;

        // _isGrounded = _controller.isGrounded;
        IsGrounded = _controller.isGrounded;  
        NetworkedVelocity = currentVelocity;

        //_animator.SetFloat(m_VerticalSpeedHash, _velocity.y);
        
    }

    public override void Render()
    {
        _animator.SetBool(m_GroundedAnimatorHash, IsGrounded);
        _animator.SetFloat(m_VerticalSpeedHash, NetworkedVelocity.y);

        if (IsMoving)
        {
            _animator.SetFloat(m_SpeedAnimatorHash, 1);
        }
        else
        {
            _animator.SetFloat(m_SpeedAnimatorHash, 0);
        }
    }
}