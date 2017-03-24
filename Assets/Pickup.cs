using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable] // Without this, the enum will not be visible in the inspector
public enum PickupType
{
	Health,
	Ammo
}

public class Pickup : MonoBehaviour
{
	public PickupType	m_Type;
	public float		m_SpinSpeed = 60;
	public int			m_Count = 25;
	
	[HideInInspector] // Hide this public variable from the inspector
	public SpawnPoint	m_Owner;

	// Entered when our ground collider is triggered
	void OnTriggerEnter(Collider other)
	{
        // Check if a player entered the trigger
		var player = other.transform.GetComponent<Player>();
		if (player)
		{
			bool pickedUp = false;

			switch (m_Type)
			{
			case PickupType.Health:
				pickedUp = player.AddHealth(m_Count);
			break;
			case PickupType.Ammo:
				pickedUp = player.AddAmmo(m_Count);
			break;
			default:
			break;
			}

			// Tell the pickup spawn point (if one exists) it is no longer occupied
			if (m_Owner)
				m_Owner.m_Occupied = false;

			// Destroy the game object when picked up
			if (pickedUp)
				Destroy(gameObject);
		}
    }
	
	// Update is called once per frame
	void Update ()
	{
		// Spin so we look a bit enticing
		transform.Rotate(Vector3.up, m_SpinSpeed * Time.deltaTime);
	}
}
