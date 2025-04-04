using System.Collections;
using UnityEngine;
using TMPro;

public class TitleBlink : MonoBehaviour
{
    public TextMeshProUGUI titleText; // Title 텍스트
    public float blinkInterval = 0.5f; // 깜빡이는 간격 (초)

    private bool isBlinking = true;

    void Start()
    {
        if (titleText != null)
        {
            StartCoroutine(BlinkText());
        }
    }

    private IEnumerator BlinkText()
    {
        while (isBlinking)
        {
            // 텍스트 활성화/비활성화 반복
            titleText.enabled = !titleText.enabled;
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    public void StopBlinking()
    {
        isBlinking = false;
        if (titleText != null)
        {
            titleText.enabled = true; // 깜빡임 종료 후 텍스트를 항상 활성화
        }
    }
}
