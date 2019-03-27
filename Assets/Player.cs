using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
	// Public variables (available in the inspector)
	[Header("Movement")]
	public float		m_MovementSpeed = 10;
	public float		m_AimSpeedModifier = 0.5f;
	public float		m_Acceleration = 10;
	public float		m_SprintModifier = 1.5f;
	public float		m_JumpSpeed = 5;
	public float		m_TurnSpeed = 5;
	public float		m_AimTurnSpeed = 5;
	public float		m_AirControl = 0.1f;

	[Header("Gameplay")]
	public Transform	m_ArmAxis;
	public GameObject	m_MuzzleParticle;
	public GameObject	m_HitParticle;
	public GameObject	m_HitParticleBlood;
	public int			m_DeathForce = 200;
	public int			m_MaxHealth = 100;
	public int			m_MaxAmmo = 40;
	public int			m_GunDamage = 35;
	public float		m_GunForce = 200;

	[Header("Camera")]
	public Transform	m_CameraSlot;
	public Transform	m_CameraSlotTarget;
	public Transform	m_Camera;
	public Transform	m_FreelookPos;
	public Transform	m_AimPos;
	public float		m_CameraDrag = 0.1f;
	public float		m_CameraDragZoomed = 0.1f;
	public float		m_CameraRotSmoothing = 0.01f;
	public float		m_MouseSensitivityX = 1;
	public float		m_MouseSensitivityY = 1;
	public float		m_AimSensModifier = 1;
	public float		m_MinPitch = -50;
	public float		m_MaxPitch = 89;
	public float		m_FOVNormal = 60;
	public float		m_FOVZoomed = 30;
	public float		m_ZoomDuration = 0.1f;
	public bool			m_FlipMouseY;

	// Private variables (not available in the inspector)
	private Transform	m_Mesh;
	private Rigidbody	m_Rigidbody;
	private Vector3		m_InputVec;
	private Vector3		m_CurrentInputVec;
	private Vector3		m_CurrentCameraSlotPos;
	private Vector3		m_DeathCamPos;
	private bool		m_Grounded;
	private bool		m_ShouldJump;
	private bool		m_Aiming;
	private int			m_Health;
	private int			m_Ammo;
	private float		m_CameraYaw;
	private float		m_CameraPitch;
	private float		m_ArmPitch;

	// This is called once when the game starts
	void Start ()
	{
		// Get components
		m_Rigidbody = GetComponent<Rigidbody>();

		// Find transforms
		m_Mesh = transform.Find("Mesh");

		// Hide and lock the cursor (to the center of the screen)
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;

		// Set initial value for camera slot position
		m_CurrentCameraSlotPos = m_CameraSlot.position;

		// Set initial health and ammo values
		m_Health = m_MaxHealth;
		m_Ammo = m_MaxAmmo;
	}
	
	// This runs at a variable rate (depends on the framerate)
	void Update ()
	{
		// Update input from the keyboard/mouse
		UpdateInput();

		// Refresh the UI with information from the player
		UpdateUI();
	}

	// This runs at a fixed rate (every physics timestep)
	void FixedUpdate ()
	{
		// Update the movement of the player. Since this involves physics, we run it in FixedUpdate
		UpdateMovement();
	}

	// This runs at a variable rate (depends on the framerate), but at the very end of the frame
	void LateUpdate ()
	{
		// Update camera movement, rotation
		UpdateCamera();
	}

	void UpdateInput ()
	{
		if (IsAlive()) // Player has to be alive to be able to move/shoot
		{
			// Movement, directional vector implementation
			m_InputVec = Vector3.zero;
			m_InputVec += Input.GetKey(KeyCode.W) ? transform.forward : Vector3.zero;
			m_InputVec += Input.GetKey(KeyCode.S) ? -transform.forward : Vector3.zero;
			m_InputVec += Input.GetKey(KeyCode.D) ? transform.right : Vector3.zero;
			m_InputVec += Input.GetKey(KeyCode.A) ? -transform.right : Vector3.zero;

			// Camera-relative input
			m_InputVec = m_Camera.rotation * m_InputVec;
			m_InputVec.y = 0; // The camera rotation might give us some movement in the y-direction (up/down) so we want to remove that

			// Normalize input vector so movement speed is the same in all directions
			m_InputVec.Normalize();

			// Sprinting
			m_InputVec *= Input.GetKey(KeyCode.LeftShift) ? m_SprintModifier : 1;

			// If we are aiming, turn the mesh to face towards the aim direction
			Quaternion flatCameraRot = Quaternion.Euler(Vector3.Scale(m_Camera.rotation.eulerAngles, new Vector3(0, 1, 0)));
			if (m_Aiming)
				m_Mesh.rotation = Quaternion.Slerp(m_Mesh.rotation, flatCameraRot, Time.deltaTime * m_AimTurnSpeed);
			// If we are moving, turn the mesh to face towards the moving direction
			else if (m_InputVec.magnitude > Mathf.Epsilon)
				m_Mesh.rotation = Quaternion.Slerp(m_Mesh.rotation, Quaternion.LookRotation(m_InputVec), Time.deltaTime * m_TurnSpeed);

			// Store jump input so it can be used in the fixed update
			if (Input.GetKeyDown(KeyCode.Space) && m_Grounded && !m_Aiming) // Need to be grounded and not aiming to be able to jump
				m_ShouldJump = true;

			// Fire
			if (m_Aiming && Input.GetMouseButtonDown(0) && m_Ammo > 0)
				Fire();

			// Camera zoom/aim
			m_Aiming = Input.GetMouseButton(1);
		}

		// The rest of the input is possible even if the player is dead

		// Mouse input
		m_CameraYaw += Input.GetAxis("Mouse X") * m_MouseSensitivityX * (m_Aiming ? m_AimSensModifier : 1);
		m_CameraPitch += Input.GetAxis("Mouse Y") * m_MouseSensitivityY * (m_Aiming ? m_AimSensModifier : 1);

		// Prevent over-rotating the camera
		m_CameraPitch = Mathf.Clamp(m_CameraPitch, -m_MaxPitch, -m_MinPitch);

		// Update arms rotation
		m_ArmPitch = Mathf.Lerp(m_ArmPitch, m_Aiming ? -m_CameraPitch : 45, Time.deltaTime / m_ZoomDuration);
		m_ArmAxis.localRotation = Quaternion.Euler(m_ArmPitch, 0, 0);
	}

	void UpdateUI ()
	{
		// Toggle crosshair depending on aim state
		UIManager.manager.SetShowCrosshair(m_Aiming);
		// Update the health bar
		UIManager.manager.SetHealth((float)m_Health / (float)m_MaxHealth);
		// Update the ammo bar
		UIManager.manager.SetAmmo((float)m_Ammo / (float)m_MaxAmmo);
	}

	void UpdateMovement ()
	{
		if (!IsAlive()) // Skip updating the movement if the player is dead
			return;

		// Move the player by pushing the rigidbody (XZ only)

		// Smooth (lerp) the input vector, giving us some acceleration/deceleration
		m_CurrentInputVec = Vector3.Lerp(m_CurrentInputVec, m_InputVec, m_Acceleration * Time.fixedDeltaTime);

		// Because the members of Rigidbody.velocity cannot be modified individually, we obtain a copy, modify it, and then assign it back
		Vector3 currentVelocity = m_Rigidbody.velocity;
		Vector3 inputVelocity = m_CurrentInputVec * m_MovementSpeed * (m_Aiming ? m_AimSpeedModifier : 1); // Calculate input velocity based on the input vector and movement speed
		if (m_Grounded) // On the ground
		{
			// This will override the existing velocity, giving us absolute control of player movement on the ground
			currentVelocity.x = inputVelocity.x;
			currentVelocity.z = inputVelocity.z;
		}
		else if (m_InputVec.magnitude > Mathf.Epsilon && m_AirControl > Mathf.Epsilon) // In the air, IF the player is moving
		{
			// This will give us some amount of control in the air, but will not override the existing velocity 
			currentVelocity.x = Mathf.Lerp(currentVelocity.x, inputVelocity.x, m_AirControl);
			currentVelocity.z = Mathf.Lerp(currentVelocity.z, inputVelocity.z, m_AirControl);
		}
		// Re-assign the velocity to the rigidbody
		m_Rigidbody.velocity = currentVelocity;

		// Jump by applying some velocity straight upwards
		if (m_ShouldJump)
		{
			m_Rigidbody.velocity += Vector3.up * m_JumpSpeed;
			m_ShouldJump = false;
			m_Grounded = false;
		}
	}

	void UpdateCamera ()
	{
		// Build camera target rotation
		Quaternion targetRotation = Quaternion.Euler(m_CameraPitch * (m_FlipMouseY ? -1 : 1), m_CameraYaw, 0);

		// If we are aiming we don't do any kind of smoothing as it makes us less accurate
		if (m_Aiming)
			m_CameraSlot.rotation = targetRotation;
		else // Even though camera smoothing is inherently evil, without it we will get some ugly jittering when running around and rotating the camera at the same time
			m_CameraSlot.rotation = Quaternion.Slerp(m_CameraSlot.rotation, targetRotation, Time.deltaTime / m_CameraRotSmoothing);

		// Camera slot position drag (smoothing)
		m_CurrentCameraSlotPos = Vector3.Lerp(m_CurrentCameraSlotPos, m_CameraSlotTarget.position, Time.deltaTime / (m_Aiming ? m_CameraDragZoomed : m_CameraDrag));

		// Set actual camera slot position
		if (IsAlive())
			m_CameraSlot.position = m_CurrentCameraSlotPos;
		else // If the player is dead we want the camera to remain in place
			m_CameraSlot.position = m_DeathCamPos;

		// This (smoothly) positions the camera in the "freelook" position or the "aim" position, depending on if the player is currently aiming or not
		m_Camera.position = Vector3.Lerp(m_Camera.position, m_Aiming ? m_AimPos.position : m_FreelookPos.position, Time.deltaTime / m_ZoomDuration);

		// This (smoothly) switches between the two field of view values we have defined for freelook and aiming, respectively
		m_Camera.GetComponent<Camera>().fieldOfView = Mathf.Lerp(m_Camera.GetComponent<Camera>().fieldOfView, m_Aiming ? m_FOVZoomed : m_FOVNormal, Time.deltaTime / m_ZoomDuration);
	}

	// Fire the weapon
	void Fire ()
	{
		// Play muzzle flash particle system
		m_MuzzleParticle.GetComponent<ParticleSystem>().Play();

		// To see where we might have hit something, we are going to perform a physics raycast
		// This shoots a ray into the physics world, and the physics engine is then able to tell us what we hit and where we hit it (if we hit anything at all)

		// Build a ray going from the camera position straight forward (unless told otherwise, the physics engine will shoot the ray infinitely far)
        Ray ray = new Ray(m_Camera.position, m_Camera.forward);
		// The RaycastHit is passed in as a reference to the physics engine and is filled with information regarding the raycast
		RaycastHit hit;
		// Ray cast straight forward from the center of the screen
        if (Physics.Raycast(ray, out hit)) // Returns true if raycast hit anything
		{
			// Check if we hit a zombie by attempting to find the Zombie component of the hit object
			var zombie = hit.transform.GetComponent<Zombie>();
			var rb = hit.transform.GetComponent<Rigidbody>(); // Also check if the hit object has a rigidbody
			if (zombie && zombie.IsAlive())
			{
				// If we got here, we hit a zombie
				zombie.Hit(m_GunDamage, hit.point, transform.position);
			}
			else if (rb || zombie) // If we hit a Rigidbody or a dead Zombie (which also has a Rigidbody)
			{
				// If we hit a rigidbody, we want to apply some force to it
				Vector3 force = (hit.point - transform.position).normalized * m_GunForce; // Calculate a vector from the damage origin to the hit point
				rb.AddForceAtPosition(force, hit.point);
			}

			// Instantiate a new hit particle, type of which depends on if we hit a zombie or not
            var hitParticle = zombie ? Instantiate(m_HitParticleBlood) : Instantiate(m_HitParticle);
			// Position the hit particle at the point of impact
			hitParticle.transform.position = hit.point;
			// Schedule the destruction of the hit particle (particle systems don't autodestruct)
			Destroy(hitParticle, 1);
        }

		// Subtract one bullet from ammo
		m_Ammo--;
	}

	// Entered when our ground collider is triggered
	void OnTriggerEnter(Collider other)
	{
        m_Grounded = true;
    }

	// We can call this to check if the player is alive or not
	public bool IsAlive ()
	{
		return m_Health > 0;
	}

	// We call this to damage the player
	public void Hit (int damage, Vector3 point, Vector3 origin)
	{
		if (!IsAlive()) // Player needs to be alive to be able to take damage
			return;

		// Subtract damage from health, also make sure health is not below 0
		m_Health = Mathf.Max(m_Health - damage, 0);

		// Check if the player died from this hit
		if (m_Health <= 0)
		{
			// If the player was aiming when dying, we want to force it off
			m_Aiming = false;

			// Nullify any current movement
			m_InputVec *= 0;
			m_CurrentInputVec *= 0;

			// Remove the rotation constraints of the Rigidbody (since we are about to send it flying)
			m_Rigidbody.constraints = RigidbodyConstraints.None;

			// Apply some force to the rigidbody to send it flying
			Vector3 force = (point - origin).normalized * m_DeathForce; // Calculate a vector from the damage origin to the hit point
			m_Rigidbody.AddForceAtPosition(force, point); // Add directional force at hit position

			// Remember the camera position where the player died
			m_DeathCamPos = m_CameraSlot.position;

			// Also free the camera slot from the transform hierarchy (prevents the rotation of the player from affecting the death camera)
			m_CameraSlot.parent = null;

			// Tell the UI manager to show the death text
			UIManager.manager.PlayerDied();
		}
	}

	// Add some health to the player.
	// Returns true if add was successful, false if not
	public bool AddHealth (int count)
	{
		if (m_Health >= m_MaxHealth) // Return false if health is already full
			return false;

		if (IsAlive()) // Need to be alive
		{
			m_Health = Mathf.Min(m_Health + count, m_MaxHealth); // Make sure to not add more health than is allowed
			return true;
		}

		// Return false if player is dead
		return false;
	}

	// Add some ammo to the player
	// Returns true if add was successful, false if not
	public bool AddAmmo (int count)
	{
		if (m_Ammo >= m_MaxAmmo) // Return false if ammo is already full
			return false;

		if (IsAlive()) // Need to be alive
		{
			m_Ammo = Mathf.Min(m_Ammo + count, m_MaxAmmo); // Make sure to not add more health than is allowed
			return true;
		}

		// Return false if player is dead
		return false;
	}
}
