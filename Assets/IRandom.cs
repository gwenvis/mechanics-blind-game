using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRandom
{
    int Range(int exclusiveMax);
    int Range(int inclusiveMin, int exclusiveMax);
}
