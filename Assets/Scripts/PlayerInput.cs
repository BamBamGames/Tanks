
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
	public  Vector3 Move()
	{
		Vector3 move = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
		move = Vector3.ClampMagnitude(move, 1);
		return move;
	}
}
