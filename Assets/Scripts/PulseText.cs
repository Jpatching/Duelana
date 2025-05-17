using TMPro;
using UnityEngine;

public class PulseText : MonoBehaviour {
    private TextMeshProUGUI tmp;
    private float baseSize;

    void Start() {
        tmp = GetComponent<TextMeshProUGUI>();
        baseSize = tmp.fontSize;
    }

    void Update() {
        tmp.fontSize = baseSize + Mathf.Sin(Time.time * 3f) * 2f;
    }
}