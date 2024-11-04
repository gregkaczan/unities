
using UnityEngine;
using UnityEngine.UI;

public class showFps : MonoBehaviour {
	public Text fpsText;

	public Text countText;
	private float deltaTime;

	void Update () {

		// FPS counter
		deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
		float fps = 1.0f / deltaTime;
		fpsText.text = Mathf.Ceil (fps).ToString ();


	}
}