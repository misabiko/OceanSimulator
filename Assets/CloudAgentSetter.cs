using UnityEngine;

public class CloudAgentSetter : MonoBehaviour
{
    [SerializeField] private ParticleSystem _particleSystem;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Initialize()
    {
        _particleSystem.trigger.AddCollider(GameObject.FindGameObjectWithTag("CloudAgent").transform);
    }
}
