using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdControllerAnimationEvents : MonoBehaviour
{
    public void PlayWingFlapSFX()
    {
        SoundManager.HandleLocalPlaySound("Wing Flap");
    }
}
