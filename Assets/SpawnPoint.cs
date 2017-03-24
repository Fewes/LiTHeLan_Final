using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable] // Without this, the enum will not be visible in the inspector
public enum SpawnPointType
{
	Enemy,
	Pickup
}

public class SpawnPoint : MonoBehaviour
{
	public SpawnPointType	m_Type;

	[HideInInspector] // Hide this public variable from the inspector
	public bool				m_Occupied;

	// This function is available in MonoBehaviour and can be used to draw elements ("gizmos") in the editor
	void OnDrawGizmos()
	{
		// Gizmo color depends on spawn point type
		if (m_Type == SpawnPointType.Enemy)
			Gizmos.color = Color.red;
		else
			Gizmos.color = Color.green;

		// Draw a sphere with the selected color at the object position
		Gizmos.DrawSphere(transform.position, 0.5f);
    }
}
