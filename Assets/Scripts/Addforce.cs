using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Addforce : MonoBehaviour
{
	// Start is called before the first frame update
	public Rigidbody rigidbody;
	RaycastHit hit;
	Ray ray;
	float dis;
	void Start()
	{

	}

	// Update is called once per frame
	void FixedUpdate()
	{
		ray.direction = Vector3.down;
		ray.origin = transform.position;
		if (Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity))
		{
			Debug.DrawRay(ray.origin, ray.direction * 5, Color.red);

			dis = hit.distance;
		}
		if (dis < 0.5f)
		{
			rigidbody.AddForceAtPosition(Vector3.up * 10, transform.position);
		}
	}
}
