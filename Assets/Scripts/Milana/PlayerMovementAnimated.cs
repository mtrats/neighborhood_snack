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

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            if (_controller != null) _controller.enabled = false;
            transform.position = transform.position;
            if (_controller != null) _controller.enabled = true;
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

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        camForward.y = 0;
        camRight.y = 0;
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
            IsMoving = true;
        }
        else
        {
            IsMoving = false;
        }
        _jumpPressed = false;

        IsGrounded = _controller.isGrounded;  
        NetworkedVelocity = currentVelocity;
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