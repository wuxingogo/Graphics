using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SamplerOverrideSelection : MonoBehaviour
{
    public bool OverrideMipBias = false;
    public float MipBiasOverride = 0.0f;

    public bool OverrideFilter = false;
    public FilterMode FilterOverride = FilterMode.Bilinear;
}
