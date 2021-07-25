using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Bullet : NetworkBehaviour
{
	[SerializeField]
	private float _bulletSpeed = 10f;
	[SerializeField]
	private float _damage;
	internal Vector3 Bullettr;
	void Start()
	{

		DamagedBody();
	}
	private void Update()
	{
		Debug.Log(transform.position);
	}
	void FixedUpdate()
	{
	//	transform.Translate(Bullettr * _bulletSpeed * Time.deltaTime);
		
		DamagedBody();
		Destroy(transform.gameObject, 5);
	}
	public void DamagedBody()
	{
		RaycastHit raycastHit;
		Ray ray = new Ray(transform.position, Bullettr);
		if (Physics.Raycast(ray, out raycastHit, _bulletSpeed * Time.deltaTime))
		{
			if ((transform.position - raycastHit.point).magnitude < _bulletSpeed * Time.deltaTime)
			{
				Destroy(transform.gameObject);
				Debug.Log(transform.gameObject + "  destroed  +" + raycastHit.transform.gameObject);
				if (raycastHit.transform.gameObject.GetComponent<Damageable>())
					raycastHit.transform.gameObject.GetComponent<Damageable>().HealthDamage(_damage);
			}
		}
	}
	private void GravityForce()
	{

	}
}
