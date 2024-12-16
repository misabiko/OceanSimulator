using TMPro;
using UnityEngine;

public class TimeUI : MonoBehaviour
{
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private TMP_Text time_txt;
    // Update is called once per frame
    void Update()
    {
        time_txt.text = timeManager.GetTimeOfDay();
    }
}
