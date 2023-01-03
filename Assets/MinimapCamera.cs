using System;
using Managers;
using UnityEngine;

public class MinimapCamera : MonoBehaviour
{
  #region Serialized Fields
  
  #endregion
  #region Non-Serialized Fields

  private Transform _playerTransform;
  
  #endregion
  #region Properties
  
  #endregion
  #region Function Events

  private void Start()
  {
    _playerTransform = GameManager.PlayerTransform;
  }

  private void LateUpdate()
  {
    if (MinimapManager.ZoomedIn)
    {
      if(transform.position != Vector3.zero) transform.position = Vector3.zero;
    }
    else
    {
      transform.position = _playerTransform.position;
    }
  }

  #endregion
  #region Public Methods
  
  #endregion
  #region Private Methods
  
  #endregion
}

