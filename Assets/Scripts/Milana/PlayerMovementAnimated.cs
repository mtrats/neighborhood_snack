using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PlayerMovementAnimated : NetworkBehaviour
{
    private Vector3 _velocity;
    private bool _jumpPressed;

    private CharacterController _controller;
    private Animator _animator;

    public float PlayerSpeed = 2f;

    public float JumpForce = 5f;
    public float GravityValue = -9.81f;

    private int m_SpeedAnimatorHash = Animator.StringToHash("Speed");
    private int m_GroundedAnimatorHash = Animator.StringToHash("Grounded");
    //private int m_SprintAnimatorHash = Animator.StringToHash("Sprint");
    private int m_VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");


    private bool _isMoving = false;
    private bool _isGrounded;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (Input.GetButtonDown("Jump"))
        {
            _jumpPressed = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        // FixedUpdateNetwork is only executed on the StateAuthority

        if (_controller.isGrounded)
        {
            _velocity = new Vector3(0, -1, 0);
        }

        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")) * Runner.DeltaTime * PlayerSpeed;

        _velocity.y += GravityValue * Runner.DeltaTime;
        if (_jumpPressed && _controller.isGrounded)
        {
            _velocity.y += JumpForce;
        }

        _controller.Move(move + _velocity * Runner.DeltaTime);

        if (move != Vector3.zero)
        {
            gameObject.transform.forward = move;
            //_animator.SetFloat(m_SpeedAnimatorHash, 1);
            _isMoving = true;
        }
        else
        {
            //_animator.SetFloat(m_SpeedAnimatorHash, 0);
            _isMoving = false;
        }
        _jumpPressed = false;

        _isGrounded = _controller.isGrounded;
        //_animator.SetFloat(m_VerticalSpeedHash, _velocity.y);
        
    }

    public override void Render()
    {
        _animator.SetBool(m_GroundedAnimatorHash, true);
        if (_isMoving)
        {
            _animator.SetFloat(m_SpeedAnimatorHash, 1);
            _animator.SetFloat(m_VerticalSpeedHash, _velocity.y);
        }
        else
        {
            _animator.SetFloat(m_SpeedAnimatorHash, 0);
        }

        if (_jumpPressed)
        {
            _animator.SetBool(m_GroundedAnimatorHash, false);
            _animator.SetFloat(m_VerticalSpeedHash, _velocity.y);
        }
    }
}