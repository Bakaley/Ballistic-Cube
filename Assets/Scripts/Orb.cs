using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orb : MonoBehaviour
{
    public enum ORB_TYPE { 
        ADD_AIM,
        GRAVITY_UP,
        GRAVITY_DOWN,
        GRAVITY_LEFT,
        GRAVITY_RIGHT,
        COLLECTIBLE
    }

    [SerializeField]
    ORB_TYPE orbType;

    public ORB_TYPE Type
    {
        get
        {
            return orbType;
        }
    }
}
