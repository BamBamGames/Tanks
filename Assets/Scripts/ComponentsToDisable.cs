using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComponentsToDisable : NetworkBehaviour
{
	[SerializeField]
	private List<Behaviour> componentsToDisable;

	void Start()
	{
		if (!isLocalPlayer)
		{
			foreach (Behaviour item in componentsToDisable)
			{
				item.enabled = false;
			}
		}
	}
}
