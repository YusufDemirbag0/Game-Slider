using UnityEngine;

public class SlideLifecycleHost : MonoBehaviour
{
	[SerializeField] MonoBehaviour slideBehaviour; 
	IGameSlide gameSlide;

	void Awake()
	{
		if (slideBehaviour != null) gameSlide = (IGameSlide)slideBehaviour;
	}

	public void Enter() { gameSlide?.OnEnter(); }
	public void Exit()  { gameSlide?.OnExit(); }
}