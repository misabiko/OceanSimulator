using UnityEngine;

public class NormalDebug : MonoBehaviour {
	public float arrowLength = 1f;
	public float headSize = 0.04f;
	void OnDrawGizmos() {
		var light = GameObject.Find("Spot Light").transform;
		DrawArrow(light, (transform.localScale.y * Vector3.up / 2f), Vector3.up);
		DrawArrow(light, (transform.localScale.y * Vector3.down / 2f), Vector3.down);
		DrawArrow(light, (transform.localScale.x * Vector3.right / 2f), Vector3.right);
		DrawArrow(light, (transform.localScale.x * Vector3.left / 2f), Vector3.left);
		DrawArrow(light, (transform.localScale.z * Vector3.forward / 2f), Vector3.forward);
		DrawArrow(light, (transform.localScale.z * Vector3.back / 2f), Vector3.back);
	}

	void DrawArrow(Transform light, Vector3 origin, Vector3 direction) {
		float factor = Mathf.Max(0, Vector3.Dot(transform.rotation * direction, light.rotation * Vector3.back));
		Gizmos.color = Color.Lerp(Color.red, Color.yellow, factor);
		Gizmos.DrawLine(
			transform.position + transform.rotation * origin,
			transform.position + transform.rotation * origin + transform.rotation * direction * arrowLength
		);
		Gizmos.DrawSphere(transform.position + transform.rotation * origin + transform.rotation * direction * arrowLength, 0.01f + factor * headSize);
	}
}