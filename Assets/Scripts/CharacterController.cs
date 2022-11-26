using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterController : MonoBehaviour, CharacterMap.IPlayerActions
{
    # region Fields

    [Header("Movement")] 
    [SerializeField] private float _speed = 2;
    [SerializeField] private float _maxSpeed = 2;
    [SerializeField] private float _acceleration = 2;
    [SerializeField] private float _deceleration = 2;

    [Header("Dash")]
    [SerializeField] private float _dashTime;
    [SerializeField] private float _dashBonus;
    [SerializeField] private float _dashCooldown;
    private Vector3 _dashDirection; // used so we can keep tracking the input direction without changing dash direction
    private bool _dash;
    private float dashCooldownTimer;
    private float dashingTime;

    private Rigidbody2D _rigidbody;
    private Vector2 _direction;

    private CharacterMap controls; // for input callbacks

    # endregion

    #region Properties

    private Vector2 DesiredVelocity => _direction * _speed;
    private bool Dashing => dashingTime > 0;
    private float DashSpeed => _maxSpeed * _dashBonus;

    #endregion

    # region MonoBehaviour
    
    // =============================== MonoBehaviour ===================================================================

    void Start()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        HandleTimers();
    }

    private void FixedUpdate()
    {
        MoveCharacter();
        ModifyPhysics();
    }

    private void OnEnable()
    {
        if (controls == null)
        {
            // connect this class to callbacks from "Player" input actions
            controls = new CharacterMap();
            controls.Player.SetCallbacks(this);
        }

        controls.Player.Enable();
    }

    private void OnDisable()
    {
        controls.Player.Disable();
    }

    #endregion

    #region ActionMap

    // =============================== Action Map ======================================================================
    
    public void OnMove(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Performed:
                _direction = context.ReadValue<Vector2>();
                break;
            case InputActionPhase.Canceled:
                _direction = Vector2.zero;
                break;
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Started:
                if (dashCooldownTimer > 0 || Dashing) return;
                dashingTime = _dashTime;
                dashCooldownTimer = _dashCooldown;
                _dashDirection = _direction.normalized;
                break;
        }
    }

    #endregion

    #region Class Methods
    
    // =============================== Class Methods ===================================================================

    private void MoveCharacter()
    {
        if (Dashing)
        {
            _rigidbody.velocity = _dashDirection * DashSpeed;
            return;
        }
        
        _rigidbody.velocity = Vector2.Lerp(_rigidbody.velocity,
            DesiredVelocity,
            _acceleration * Time.fixedDeltaTime);
        
        if (DesiredVelocity.magnitude > _maxSpeed)
        {
            _rigidbody.velocity = DesiredVelocity / DesiredVelocity.magnitude * _maxSpeed;
        }
    }

    private void ModifyPhysics()
    {
        //TODO: create Vector3 Utils class?
        float cosTheta = Vector3.Dot(_direction.normalized, _rigidbody.velocity.normalized);
        float theta = Mathf.Acos(cosTheta);
        bool changingDirection = Math.Abs(theta) >= Math.PI/2  ;

        // Make "linear drag" when changing direction
        if (changingDirection)
        {
            _rigidbody.velocity = Vector2.Lerp(
                _rigidbody.velocity,
                Vector2.zero, 
                _deceleration * Time.fixedDeltaTime);
        }

        if (_direction.magnitude == 0 && _rigidbody.velocity.magnitude < 0.2f) { _rigidbody.velocity *= Vector2.zero; }
    }

    private void HandleTimers()
    {
        if (dashCooldownTimer > 0) { dashCooldownTimer = Mathf.Max(dashCooldownTimer - Time.deltaTime, 0); }
        if (dashingTime > 0) { dashingTime = Mathf.Max(dashingTime - Time.deltaTime, 0); }
    }

    #endregion
}