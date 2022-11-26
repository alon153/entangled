using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public partial class PlayerController
{
    #region Fields

    [Header("Shooting")] 
    [SerializeField] private float _rotationSpeed;
    private Vector2 _aimDirection;
    private GameObject _aimLine;
    private GameObject _aimPivot;

    #endregion

    #region Properties

    private bool Aiming => _aimLine.gameObject.activeSelf;
    
    #endregion
    
    #region Input Actions

    public void OnAim(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Performed:
                _aimDirection = context.ReadValue<Vector2>();
                break;
            case InputActionPhase.Canceled:
                _aimDirection = Vector2.zero;
                break;
        }
    }

    #endregion
    
    #region Class Methods

    private void SetAim()
    {
        if (_aimDirection == Vector2.zero)
        {
            _aimLine.SetActive(false);   
        }
        else
        {
            var zRotation = Vector3.SignedAngle(_aimDirection, Vector3.up, -Vector3.forward);
            var q = Quaternion.Euler(0,0,zRotation);
            if (!Aiming)
            {
                _aimPivot.transform.rotation = q;
                _aimLine.SetActive(true);
            }
            else
            {
                _aimPivot.transform.rotation = Quaternion.Slerp(_aimPivot.transform.rotation, q, Time.deltaTime * _rotationSpeed);
            }
        }


    }
    
    #endregion
}