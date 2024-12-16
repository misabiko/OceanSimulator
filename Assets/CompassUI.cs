using UnityEngine;
using UnityEngine.UI;

public class CompassUI : MonoBehaviour
{
    [SerializeField] private BirdController bird;
    [SerializeField] private BoatController boat;

    [SerializeField] private Transform compass;

    // Update is called once per frame
    void Update()
    {
        Vector3 dir = compass.transform.eulerAngles;
        if(PlayerStateManager.GetState() == PlayerState.Bird)
            compass.eulerAngles= new Vector3(dir.x, dir.y,bird.transform.eulerAngles.y);
        else if(PlayerStateManager.GetState() == PlayerState.Boat)
            compass.eulerAngles= new Vector3(dir.x, dir.y,boat.transform.eulerAngles.y);
    }
}
