using UnityEngine;
using System;
using UnityEngine.UI;
using UIAnimations;

public class Popup : MonoBehaviour
{
	#region Enums

	private enum State
	{
		Shown,
		Hidden,
		Showing,
		Hidding
	}

	#endregion

	#region Inspector Variables

	[SerializeField] protected bool				canAndroidBackClosePopup;
	[SerializeField] private UIAnimationStateMachine showAnimationStateMachine;
	[SerializeField] private UIAnimationStateMachine hideAnimationStateMachine;

	#endregion

	#region Member Variables

	public bool IsInitialized { get; private set; }

	// private CanvasGroup canvasGroup;
    private State		state;
	private PopupClosed	callback;

	#endregion

	#region Properties

	public bool CanAndroidBackClosePopup { get { return canAndroidBackClosePopup; } }
	// private UIAnimator Animator
	// {
	// 	get
	// 	{
	// 		if (uiAnimator == null)
	// 		{
	// 			uiAnimator = GetComponent<UIAnimator>();
	// 		}
	// 		return uiAnimator;
	// 	}
	// }
    // public CanvasGroup CG
    // {
    //     get
    //     {
    //         if (canvasGroup == null)
    //         {
    //             canvasGroup = gameObject.GetComponent<CanvasGroup>();

    //             if (canvasGroup == null)
    //             {
    //                 canvasGroup = gameObject.AddComponent<CanvasGroup>();
    //             }
    //         }

    //         return canvasGroup;
    //     }
    // }
    #endregion

    #region Delegates

    public delegate void PopupClosed(bool cancelled, object[] outData);

	#endregion

	#region Public Methods

	public virtual void Initialize()
	{
		gameObject.SetActive(true);
        showAnimationStateMachine.StopAll();
		hideAnimationStateMachine.OnSequenceCompleted.AddListener(()=>
		{
			state = State.Hidden;
			gameObject.SetActive(false);
		});
		state = State.Hidden;
		IsInitialized = true;
		gameObject.SetActive(false);
	}

	public void Show()
	{
		Show(null, null);
	}

	public bool Show(object[] inData, PopupClosed callback)
	{
		if (state == State.Hidding)
		{
			InstantHide();
		}
		if (state != State.Hidden)
		{
			return false;
		}

		this.callback	= callback;
		this.state		= State.Showing;

		// Show the popup object
		gameObject.SetActive(true);

        showAnimationStateMachine.PlaySequence();

		OnShowing(inData);

		return true;
	}

	public void Hide(bool cancelled)
	{
		Hide(cancelled, null);
	}

	public void Hide(bool cancelled, object[] outData)
	{
		if (state == State.Hidding) { return; }

		state = State.Hidding;

		if (callback != null)
		{
			callback(cancelled, outData);
		}

        hideAnimationStateMachine.PlaySequence(true);

		OnHiding();
	}

	public void HideWithAction(string action)
	{
		Hide(false, new object[] { action });
	}

	public void InstantHide()
	{
		hideAnimationStateMachine.StopAll();
        state = State.Hidden;
        gameObject.SetActive(false);
    }

	public virtual void OnShowing(object[] inData)
	{

	}

	public virtual void OnHiding()
	{
		PopupManager.Instance.OnPopupHiding(this);
	}

    #endregion
}
