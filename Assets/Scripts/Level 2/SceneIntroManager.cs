using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SceneIntroManager : MonoBehaviour
{
    public SpriteRenderer backgroundSpriteRenderer; 
    public Transform shapeObject;

    public float delayBeforeStart = 1f;
    public float fadeDuration = 1f;
    public Vector3 targetPosition;
    public float alphaTarget = 0.5f;
    


    void Start()
    {
        shapeObject.localPosition = new Vector3(0, -Screen.height, 0); // پایین صفحه
        StartCoroutine(PlayIntro());
    }

    IEnumerator PlayIntro()
    {
        yield return new WaitForSeconds(delayBeforeStart);

        // تار شدن بکگراند
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            backgroundSpriteRenderer.color = new Color32(146, 146, 146, 255);
            yield return null;
        }
    }
}
