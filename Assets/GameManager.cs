using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Needed to reload the scene

public class GameManager : MonoBehaviour
{
	// Public static variable. Accessible anywhere by calling GameManager.manager
	public static GameManager	manager;

	// Public variables (available in the inspector)
	public GameObject			m_EnemyPrefab;
	public GameObject			m_AmmoPickupPrefab;
	public float				m_EnemySpawnInterval = 0.5f;
	public float				m_PickupSpawnInterval = 2f;
	public int					m_MaxEnemyCount;
	public int					m_TotalEnemyCount = 100;

	// Private variables (not available in the inspector)
	private Player				m_Player;
	private List<Transform>		m_EnemySpawns;
	private List<Transform>		m_PickupSpawns;
	private float				m_EnemySpawnTimer;
	private float				m_PickupSpawnTimer;
	private bool				m_GameWon = false;
	private int					m_EnemiesRemaining;
	private int					m_CurrentEnemyCount;

	// Use this for initialization
	void Start ()
	{
		// Singleton pattern
		if (manager)
			Destroy(manager); // We don't allow multiple instances of the manager
		manager = this;

		// Initialize spawn points lists
		m_EnemySpawns = new List<Transform>();
		m_PickupSpawns = new List<Transform>();

		// Get a reference to the player
		m_Player = FindObjectOfType<Player>();

		// Set enemies remaining count
		m_EnemiesRemaining = m_TotalEnemyCount;

		// Gather spawns
		foreach (Transform child in transform) // Loop through all children
		{
			// Check if child is spawn point by attempting to get component
			var spawn = child.GetComponent<SpawnPoint>();
			if (spawn) // Same as if (spawn != null)
			{
				// Add to list depending on type
				if (spawn.m_Type == SpawnPointType.Enemy)
					m_EnemySpawns.Add(child);
				else if (spawn.m_Type == SpawnPointType.Pickup)
					m_PickupSpawns.Add(child);
			}
		}
	}
	
	// Update is called once per frame
	void Update ()
	{
		// Update timers
		m_EnemySpawnTimer -= Time.deltaTime;
		m_PickupSpawnTimer -= Time.deltaTime;

		// Check timers
		if (m_EnemySpawnTimer < 0 && m_EnemiesRemaining > 0)
			SpawnEnemy();
		if (m_PickupSpawnTimer < 0)
			SpawnPickup();

		// Check if we want to restart the game (reload the scene)
		if ((!m_Player.IsAlive() || m_GameWon) && Input.GetKeyDown(KeyCode.R))
		{
			// Reload the scene by telling the SceneManager to load the current (active) scene
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}
	}

	// Spawn an enemy at a random spawn point
	void SpawnEnemy ()
	{
		if (FindObjectsOfType<Zombie>().Length >= m_MaxEnemyCount) // Don't spawn more enemies than we allow at once
			return;

		if (m_EnemySpawns.Count > 0) // Can't spawn enemies if there are no enemy spawn points...
		{
			int r = Random.Range(0, m_EnemySpawns.Count); // Get a random index for the enemy spawns list
			Vector3 spawnPosition = m_EnemySpawns[r].position;
			var enemy = Instantiate(m_EnemyPrefab);
			enemy.GetComponent<Zombie>().SetPosition(spawnPosition);

			m_EnemiesRemaining--;
			m_CurrentEnemyCount++;
		}
		else
		{
			Debug.LogError("GameManager can't spawn enemy: No enemy spawn points found");
		}

		// Set the spawn timer
		m_EnemySpawnTimer = m_EnemySpawnInterval;
	}

	// Spawn a pickup at a random spawn point
	void SpawnPickup ()
	{
		if (m_PickupSpawns.Count > 0) // Can't spawn pickups if there are no pickup spawn points...
		{
			int r = Random.Range(0, m_PickupSpawns.Count); // Get a random index for the pickup spawns list
			var spawnPoint = m_PickupSpawns[r];
			if (!spawnPoint.GetComponent<SpawnPoint>().m_Occupied)
			{
				var pickup = Instantiate(m_AmmoPickupPrefab);
				pickup.transform.position = spawnPoint.position;
				pickup.GetComponent<Pickup>().m_Owner = spawnPoint.GetComponent<SpawnPoint>();
				spawnPoint.GetComponent<SpawnPoint>().m_Occupied = true;
			}
		}
		else
		{
			Debug.LogError("GameManager can't spawn pickup: No pickup spawn points found");
		}

		// Set the spawn timer
		m_PickupSpawnTimer = m_PickupSpawnInterval;
	}

	// Call this to notify the game manager that an enemy died
	public void EnemyDied ()
	{
		m_CurrentEnemyCount--;

		// Check if the game is over
		if (IsGameWon() && !m_GameWon)
		{
			UIManager.manager.GameWon();
			m_GameWon = true;
		}
	}

	// Check if the game is won
	public bool IsGameWon ()
	{
		return m_EnemiesRemaining <= 0 && m_CurrentEnemyCount<= 0;
	}
}
