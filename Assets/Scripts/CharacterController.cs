using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterController : MonoBehaviour, CharacterMap.IPlayerActions
{
    # region Fields

    [Header("Movement")] 
    [Range(1, 10)] [SerializeField] private float _speed = 2;
    [Range(1, 10)] [SerializeField] private float _maxSpeed = 2;
    [Range(1, 10)] [SerializeField] private float _acceleration = 2;
    [Range(1, 10)] [SerializeField] private float _deceleration = 2;

    private Rigidbody2D _rigidbody;
    private Vector2 _direction;

    private CharacterMap controls; // for input callbacks

    # endregion

    #region Properties

    private Vector2 DesiredVelocity => _direction * _speed;

    #endregion

    # region MonoBehaviour

    void Start()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
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

    public void OnMove(InputAction.CallbackContext context)
    {
        var inputDirection = Vector2.zero;
        switch (context.phase)
        {
            case InputActionPhase.Performed:
                inputDirection = context.ReadValue<Vector2>();
                _direction = inputDirection;
                break;
            case InputActionPhase.Canceled:
                _direction = Vector2.zero;
                break;
        }
    }

    #endregion

    #region Class Methods

    private void MoveCharacter()
    {
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
        //TODO: create Utils class?
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

    #endregion
}