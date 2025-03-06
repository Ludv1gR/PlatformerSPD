using System.Collections;
using UnityEngine;

public class DashTrail : MonoBehaviour
{
    private SpriteRenderer sr;
    private Color startColor;
    [SerializeField] private float fadeDuration = 0.3f;

    private void Awake() {
        sr = GetComponent<SpriteRenderer>();
    }

    public void SetSprite(Sprite sprite, Color color, Vector3 position, bool facingRight)
    {
        sr.sprite = sprite;
        startColor = color;
        sr.color = color;
        transform.position = position;

        transform.localScale = new Vector3(facingRight ? 1 : -1, 1, 1);
        
        StartCoroutine(FadeOut());
    }


    private IEnumerator FadeOut()
    {
        float elapsedTime = 0;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0, elapsedTime / fadeDuration);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        Destroy(gameObject);
    }
}
