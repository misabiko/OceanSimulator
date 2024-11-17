using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateManager : MonoBehaviour
{
    private static PlayerState state;
    private static PlayerStateManager Instance { get; set; }
    
    public static event Action<PlayerState> OnStateChange;

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            SwitchTo(PlayerState.Boat);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static PlayerState GetState()
    {
        return state;
    }

    public static void SwitchTo(PlayerState aState)
    {
        state = aState;
        OnStateChange?.Invoke(state);
    }
}

public enum PlayerState
{
    None,
    Bird,
    Boat
}