using UnityEngine;

public class BirdControllerAnimationEvents : MonoBehaviour
{
    public void PlayWingFlapSFX()
    {
        SoundManager.HandleLocalPlaySound("Wing Flap");
    }
}