using UnityEngine;
using System;

public class PlayerInputHandler : MonoBehaviour
{
	[Tooltip("Sensitivity multiplier for moving the camera around")]
	public float lookSensitivity = 1f;
	[Tooltip("Additional sensitivity multiplier for WebGL")]
	public float webglLookSensitivityMultiplier = 0.25f;
	[Tooltip("Limit to consider an input when using a trigger on a controller")]
	public float triggerAxisThreshold = 0.4f;
	[Tooltip("Used to flip the vertical input axis")]
	public bool invertYAxis = false;
	[Tooltip("Used to flip the horizontal input axis")]
	public bool invertXAxis = false;
	PlayerController m_PlayerCharacterController;
	private void Start()
	{
		m_PlayerCharacterController = GetComponent<PlayerController>();
		    Cursor.lockState = CursorLockMode.Locked;
		    Cursor.visible = false;
	}
	// public bool CanProcessInput()
	// {
	//     return Cursor.lockState == CursorLockMode.Locked ;
	//  }

	public Vector3 GetMoveInput()
	{
		if (true)
		{
			Vector3 move = new Vector3(Input.GetAxisRaw(GameConstants.k_AxisNameHorizontal), 0f, Input.GetAxisRaw(GameConstants.k_AxisNameVertical));
			move = Vector3.ClampMagnitude(move, 1);

			return move;
		}
	}
	public float GetLookInputsHorizontal()
	{
		return Input.GetAxis("Mouse x");
	}

	public float GetLookInputsVertical()
	{
		return Input.GetAxis("Mouse Y");
	}
}
