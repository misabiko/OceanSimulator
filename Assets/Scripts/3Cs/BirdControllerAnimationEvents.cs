using UnityEngine;

public class BirdControllerAnimationEvents : MonoBehaviour
{
    public void PlayWingFlapSFX()
    {
        SoundManager.HandleLocalPlaySound("Wing Flap");
        if (PlayerStateManager.GetState() == PlayerState.Bird){
            AudioManager.instance.PlayOneShot(FMODEvents.instance.wingFlap, this.transform.position);
        }
    }
}