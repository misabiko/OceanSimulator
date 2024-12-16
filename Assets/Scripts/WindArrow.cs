using UnityEngine;

public class WindArrow : MonoBehaviour {
	[SerializeField] Ocean ocean;
	[SerializeField] Vector2 modifier = Vector2.one;

	Transform arrowTip;

	void Awake() => arrowTip = transform.GetChild(0);

	void Update() => ocean.U10 = new Vector2(
		modifier.x * arrowTip.localPosition.x,
		modifier.y * arrowTip.localPosition.z
	);

	void OnDrawGizmos() {
		Gizmos.color = Color.yellow;
		Gizmos.DrawLine(transform.position, transform.GetChild(0).position);
	}
}