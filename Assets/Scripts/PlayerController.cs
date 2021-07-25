using UnityEngine;
using UnityEngine.Events;
using Mirror;
using Cinemachine;

[RequireComponent(typeof(CharacterController), typeof(PlayerInputHandler))]
public class PlayerController : NetworkBehaviour
{
	[Header("Movement")]
	[SerializeField] private float gravityDownForce = 20f;
	[SerializeField] private float maxSpeedOnGround = 10f;
	[SerializeField] private float movementSharpnessOnGround = 15;
	[SerializeField] private float maxSpeedInAir = 10f;
	[SerializeField] private float accelerationSpeedInAir = 25f;
	[SerializeField] private float sprintSpeedModifier = 10f;
	[SerializeField] private float rotationSpeed = 150.0f;
	[SerializeField] private Transform sphere;
	[Range(0.1f, 1f)]
	public float _rotationMultiplier = 0.4f;

	[Header("Camera")]
	[SerializeField] internal Camera playerCamera;
	[SerializeField] private Transform camTarget;
	[SerializeField] private float minCameraRotaation = -180;
	[SerializeField] private float maxCameraRotaation = 180;

	[Header("Gun")]
	[SerializeField] private Transform endOfGun;
	[SerializeField] private Transform startOfGun;
	[SerializeField] private Transform gun;

	[Header("Jump")]
	public float jumpForce = 9f;

	[Header("LayerMask")]
	[SerializeField] private LayerMask layerMask;

	public Vector3 characterVelocity { get; set; }
	public bool isGrounded { get; private set; }
	public bool hasJumpedThisFrame { get; private set; }
	public bool isDead { get; private set; }
	public bool isCrouching { get; private set; }
	
	public float RotationMultiplier = 1f;
	protected Ray ray = new Ray();
	PlayerInputHandler m_InputHandler;
	CharacterController m_Controller;
	Shooting m_Shooting;
	Vector3 m_GroundNormal;
	float m_LastTimeJumped = 0f;
	public float groundCheckDistance;
	private float CamTargetXRot = 0;
	const float k_JumpGroundingPreventionTime = 0.2f;
	const float k_GroundCheckDistanceInAir = 0.07f;
	private bool isVisible;
	void Start()
	{
		m_Controller = GetComponent<CharacterController>();
		m_InputHandler = GetComponent<PlayerInputHandler>();
		m_Shooting = GetComponent<Shooting>();
		Application.targetFrameRate = 60;
	}

	void Update()
	{
		if (!isLocalPlayer)
			return;
		hasJumpedThisFrame = false;
		GroundCheck();
		SphereRotation();
		HandleCharacterMovement();
		GunController();
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			if (!isVisible)
			{
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
				isVisible = true;
			}
			else
			{
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
				isVisible = false;
			}
		}
	}
	void GroundCheck()
	{
		float chosenGroundCheckDistance = isGrounded ? (m_Controller.skinWidth + groundCheckDistance) : k_GroundCheckDistanceInAir;

		isGrounded = false;
		m_GroundNormal = Vector3.up;

		if (Time.time >= m_LastTimeJumped + k_JumpGroundingPreventionTime)
		{
			if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(m_Controller.height), m_Controller.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance, layerMask))
			{
				m_GroundNormal = hit.normal;

				if (Vector3.Dot(hit.normal, transform.up) > 0f &&
					IsNormalUnderSlopeLimit(m_GroundNormal))
				{
					isGrounded = true;
					if (hit.distance > m_Controller.skinWidth)
					{
						m_Controller.Move(Vector3.down * hit.distance);
					}
				}
			}
		}
	}
	private void HandleCharacterMovement()
	{
		{
			transform.Rotate(new Vector3(0, Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime, 0));
			CamTargetXRot += -Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;
			CamTargetXRot = Mathf.Clamp(CamTargetXRot, minCameraRotaation, maxCameraRotaation);
			camTarget.localEulerAngles = new Vector3(CamTargetXRot, camTarget.localEulerAngles.y);
		}
		{
			float speedModifier = 1f;
			Vector3 worldspaceMoveInput = transform.TransformVector(m_InputHandler.GetMoveInput());
			if (m_Controller.isGrounded)
			{
				Vector3 targetVelocity = worldspaceMoveInput * maxSpeedOnGround * speedModifier;
				targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, m_GroundNormal) * targetVelocity.magnitude;
				characterVelocity = Vector3.Lerp(characterVelocity, targetVelocity, movementSharpnessOnGround * Time.deltaTime);
				if (m_Controller.isGrounded && Input.GetKey(KeyCode.Space))
				{
					characterVelocity = new Vector3(characterVelocity.x, 0f, characterVelocity.z);
					characterVelocity += Vector3.up * jumpForce;
					hasJumpedThisFrame = true;
					isGrounded = false;
					m_GroundNormal = Vector3.up;
				}
			}
			else
			{
				characterVelocity += worldspaceMoveInput * accelerationSpeedInAir * Time.deltaTime;
				float verticalVelocity = characterVelocity.y;
				Vector3 horizontalVelocity = Vector3.ProjectOnPlane(characterVelocity, Vector3.up);
				horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, maxSpeedInAir * speedModifier);
				characterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);
				characterVelocity += Vector3.down * gravityDownForce * Time.deltaTime;
			}
		}
		Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
		Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(m_Controller.height);
		m_Controller.Move(characterVelocity * Time.deltaTime);

		if (Physics.CapsuleCast(capsuleBottomBeforeMove, capsuleTopBeforeMove, m_Controller.radius, characterVelocity.normalized, out RaycastHit hit, characterVelocity.magnitude * Time.deltaTime))
		{
			characterVelocity = Vector3.ProjectOnPlane(characterVelocity, hit.normal);

			Debug.DrawRay(hit.point, hit.normal, Color.red, 4);

		}
	}
	private Vector3 GetCapsuleBottomHemisphere()
	{
		return transform.position + (transform.up * m_Controller.radius);
	}
	private Vector3 GetCapsuleTopHemisphere(float atHeight)
	{
		return transform.position + (transform.up * (atHeight - m_Controller.radius));
	}
	public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal)
	{
		Vector3 directionRight = Vector3.Cross(direction, Vector3.up);
		return Vector3.Cross(slopeNormal, directionRight).normalized;
	}
	bool IsNormalUnderSlopeLimit(Vector3 normal)
	{
		return Vector3.Angle(transform.up, normal) <= m_Controller.slopeLimit;
	}
	private void SphereRotation()
	{
		float x = characterVelocity.x;
		float z = characterVelocity.z;
		sphere.Rotate(new Vector3(z, 0, -x), Space.World);
	}
	private void GunController()
	{
		RaycastHit hit;
		Ray ray = m_Shooting.isFiring ? playerCamera.ScreenPointToRay(m_Shooting.Recoil()) : new Ray(playerCamera.transform.position, playerCamera.transform.forward);

		Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * 10, Color.blue);
		if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
		{
			Vector3 gunTarget = hit.point;
			float k = 1;
			if (gunTarget.y < gun.position.y)
			{
				k = -1;
			}
			float From = Vector3.ProjectOnPlane(gunTarget - gun.position, Vector3.up).magnitude;

			float disTotarget = (gunTarget - gun.position).magnitude;
			float radius = (startOfGun.position - gun.position).magnitude;
			float Alpha = Mathf.Asin(radius / disTotarget) * Mathf.Rad2Deg;
			float Beta = Mathf.Acos(From / disTotarget) * Mathf.Rad2Deg * k;
			gun.localEulerAngles = new Vector3(Alpha - Beta, gun.localEulerAngles.y);
		}
	}
}

