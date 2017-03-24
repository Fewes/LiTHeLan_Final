using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; // Needed for NavMeshAgent

public class Zombie : MonoBehaviour
{
	// Public variables (available in the inspector)
	public int				m_MaxHealth = 100;
	public int				m_DeathForce = 5;
	public int				m_AttackDamage = 24;
	public float			m_AttackDistance = 2f;
	public float			m_AttackInterval = 1f;
	public GameObject		m_HealthPickupPrefab;
	[Range(0f, 1f)]
	public float			m_HealthSpawnOdds = 0.1f;

	// Private variables (not available in the inspector)
	private NavMeshAgent	m_Agent;
	private Rigidbody		m_Rigidbody;
	private Player			m_Player;
	private int				m_Health;
	private float			m_AttackTimer;

	// This is called once when the object is instantiated
	void Awake ()
	{
		// Get components
		m_Agent = GetComponent<NavMeshAgent>();
		m_Rigidbody = GetComponent<Rigidbody>();

		// Find the player object in the scene. This works since there is only a single player.
		// If there are multiple players, we would have to get them all using FindObjectsOfType and select the one we want
		// (for example, checking the distance to all players and targeting the closest one)
		m_Player = FindObjectOfType<Player>();

		// Set initial health
		m_Health = m_MaxHealth;
	}
	
	// This runs at a variable rate (depends on the framerate)
	void Update ()
	{
		// Update attack timer
		m_AttackTimer -= Time.deltaTime;

		// Tell the navigation mesh agent to move to the player
		if (m_Agent.enabled) // We need to check if the agent is enabled, since we turn it off when the zombie dies
			m_Agent.SetDestination(m_Player.transform.position);

		// See if we can attack the player by checking the distance
		if (IsAlive() && Vector3.Distance(m_Player.transform.position, transform.position) < m_AttackDistance)
		{
			// Check attack timer
			if (m_AttackTimer < 0)
			{
				// We can attack!
				m_Player.Hit(m_AttackDamage, m_Player.transform.position + new Vector3(0, 1.5f, 0), transform.position);

				// Set attack timer
				m_AttackTimer = m_AttackInterval;
			}
		}
	}

	// We can call this to check if the zombie is alive or not
	public bool IsAlive ()
	{
		return m_Health > 0;
	}

	// We call this to damage the zombie
	public void Hit (int damage, Vector3 point, Vector3 origin)
	{
		if (!IsAlive()) // Zombie needs to be alive to be able to take damage
			return;

		// Subtract damage from health, also make sure health is not below 0
		m_Health = Mathf.Max(m_Health - damage, 0);

		// Check if zombie died from this hit
		if (m_Health <= 0)
		{
			// Disable the navigation mesh agent
			m_Agent.enabled = false;

			// Apply some force to the rigidbody to send it flying
			Vector3 force = (point - origin).normalized * m_DeathForce; // Calculate a vector from the damage origin to the hit point
			m_Rigidbody.velocity *= 0; // Disabling the NavMeshAgent can have some funky effects on the rigidbody, so we make sure to nullify the current velocity first
			m_Rigidbody.isKinematic = false; // Rigidbody was set to kinematic previously (since the NavMeshAgent was controlling it)
			m_Rigidbody.AddForceAtPosition(force, point); // Add directional force at hit position

			// Check if we should spawn a health pickup
			float r = Random.Range(0f, 1f);
			if (r <= m_HealthSpawnOdds)
			{
				var healthPickup = Instantiate(m_HealthPickupPrefab);
				healthPickup.transform.position = transform.position;
			}

			// Tell the game manager an enemy died
			GameManager.manager.EnemyDied();

			// Schedule destruction of game object (delay by two seconds)
			Destroy(gameObject, 2);
		}
	}

	// This is a special function which is necessary because a NavMeshAgent cannot be moved by just setting the transform.position
	public void SetPosition (Vector3 position)
	{
		m_Agent.Warp(position);
	}
}
