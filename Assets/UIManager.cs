using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
	public static UIManager	manager;

	public RectTransform	m_CrossHair;
	public RectTransform	m_HealthBar;
	public RectTransform	m_AmmoBar;
	public RectTransform	m_DiedText;
	public RectTransform	m_WonText;

	void Start ()
	{
		// Singleton pattern
		if (manager)
			Destroy(manager);
		manager = this;

		// Hide the death text
		m_DiedText.gameObject.SetActive(false);
		m_WonText.gameObject.SetActive(false);
	}
	
	// We can call this to tell the UI to show/hide the crosshair
	public void SetShowCrosshair (bool state)
	{
		// Toggle display of the crosshair
		m_CrossHair.gameObject.SetActive(state);
	}

	// We call this to set the size of the health bar
	public void SetHealth (float health)
	{
		// Scale the health bar
		var scale = m_HealthBar.localScale;
		scale.x = health;
		m_HealthBar.localScale = scale;
	}

	// We call this to set the size of the ammo bar
	public void SetAmmo (float ammo)
	{
		// Scale the health bar
		var scale = m_AmmoBar.localScale;
		scale.x = ammo;
		m_AmmoBar.localScale = scale;
	}

	// We call this to tell the UI the player died
	public void PlayerDied ()
	{
		// Show the death text
		m_DiedText.gameObject.SetActive(true);
	}

	// We call this to tell the UI the game is won
	public void GameWon ()
	{
		m_WonText.gameObject.SetActive(true);
	}
}
