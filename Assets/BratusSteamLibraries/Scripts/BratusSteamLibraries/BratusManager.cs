using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BratusSteamLibraries;

public class BratusManager : MonoBehaviour
{
    // Use this for initialization
    IEnumerator test()
    {
        yield return new WaitForSeconds(2f);
        SteamLeaderboard.InstantiateLeaderboard(this);
    }

    private void Start()
    {
        StartCoroutine("test");
    }
}
