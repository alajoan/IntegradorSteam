using System.Collections;
using System.Collections.Generic;
using UnityEngine;
    
namespace BratusSteamLibraries
{
    [RequireComponent(typeof(Transform))]
    public class SteamLeaderboardTarget : MonoBehaviour
    {
        private void Awake()
        {
            SteamLeaderboard.m_Leaderboardtarget = gameObject.transform;
        }
    }
}

