using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonSound : MonoBehaviour
{
	#region Inspector Variables

	[SerializeField] private string soundId = "";

	#endregion

	#region Unity Methods

	private void Awake()
	{
		gameObject.GetComponent<Button>().onClick.AddListener(PlaySound);
	}

	#endregion

	#region Public Methods

	public void PlaySound()
	{
		if (SoundManager.Instance != null)
		{
			SoundManager.Instance.Play(soundId);
		}
	}

	#endregion
}