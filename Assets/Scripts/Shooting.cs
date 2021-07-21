using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class Shooting : NetworkBehaviour
{
	[SerializeField]
	private RectTransform sight;
	[SerializeField]
	private CanvasScaler canvasScaler;
	[SerializeField]
	private Transform endOfGun;
	[SerializeField]
	private GameObject bullet;
	[SerializeField]
	private PlayerController playerController;

	internal bool isFiring;
	internal Vector3 _ray;
	System.Random random = new System.Random();
	
	void Start()
	{
		if (!isLocalPlayer)
			return;
	}
	void Update()
	{
	//	Debug.Log(playerController.playerCamera.scaledPixelWidth + "                " + playerController.playerCamera.scaledPixelHeight);
		if (!isLocalPlayer)
			return;
		if (Input.GetMouseButtonDown(0))
		{
			isFiring = true;
			Recoil();
			CmdFire();
		}
		else
		{
			isFiring = false;
		}
	
	}

	public Vector2 Recoil()
	{
		float radius = (sight.rect.height / 2) * (playerController.playerCamera.scaledPixelHeight / 720f);
		float Xcord = random.Next(-Mathf.CeilToInt(radius), Mathf.CeilToInt(radius));
		int Y = Mathf.FloorToInt(Mathf.Sqrt(Mathf.Pow(radius, 2) - Mathf.Pow(Xcord, 2)));
		float Ycord = random.Next(-Y, Y);
		return new Vector2(Xcord + playerController.playerCamera.scaledPixelWidth / 2, Ycord + playerController.playerCamera.scaledPixelHeight / 2);
	}

	[Command]
	public void CmdFire()
	{
		float radius = (sight.rect.height / 2)* (playerController.playerCamera.scaledPixelHeight / 720f);
		float Xcord = random.Next(-Mathf.CeilToInt(radius), Mathf.CeilToInt(radius));
		int Y = Mathf.FloorToInt(Mathf.Sqrt(Mathf.Pow(radius, 2) - Mathf.Pow(Xcord, 2)));
		float Ycord = random.Next(-Y, Y);
		Vector2 recoil =  new Vector2(Xcord + playerController.playerCamera.scaledPixelWidth / 2, Ycord + playerController.playerCamera.scaledPixelHeight / 2);
		Debug.Log(recoil +"        " + radius);
		Ray ray = playerController.playerCamera.ScreenPointToRay(recoil);
		Debug.DrawRay(ray.origin, ray.direction, Color.green, 3);
		if (Physics.Raycast(ray, out RaycastHit hit, 4000f))
		{
			_ray = hit.point - endOfGun.transform.position;
			GameObject _bullet = Instantiate(bullet, endOfGun.transform.position, Quaternion.identity, transform);
			_bullet.GetComponent<Bullet>().Bullettr = _ray.normalized;
			NetworkServer.Spawn(_bullet); 
		}
		else
		{
			_ray = ray.GetPoint(300) - endOfGun.transform.position;
			GameObject _bullet = Instantiate(bullet, endOfGun.transform.position, Quaternion.identity, transform);
			_bullet.GetComponent<Bullet>().Bullettr = _ray.normalized;
			NetworkServer.Spawn(_bullet);
	    }
	}
}
