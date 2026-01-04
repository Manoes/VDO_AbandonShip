using UnityEngine;

public class FakeYSpin2D : MonoBehaviour
{
    [SerializeField] float cyclesPerSecond = 1.2f;
    [SerializeField] float minScaleX = 0.15f;  // never hit 0 (avoids disappearing)
    [SerializeField] bool flipSpriteAtBackface = true;

    SpriteRenderer sr;
    float t;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    void Update()
    {
        t += Time.deltaTime * cyclesPerSecond * Mathf.PI * 2f;

        // -1..1
        float c = Mathf.Cos(t);

        // width squish (0..1) mapped to minScaleX..1
        float width = Mathf.Lerp(minScaleX, 0.5f, Mathf.Abs(c));
        var s = transform.localScale;
        s.x = width;
        transform.localScale = s;

        // optional "back face" flip
        if (flipSpriteAtBackface && sr)
            sr.flipX = (c < 0f);
    }
}
