using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject BackGroundImage; // 일시 정지 UI 패널
    private bool isPaused = false; // 현재 일시 정지 상태

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isPaused = !isPaused; // 일시 정지 상태 토글
            BackGroundImage.SetActive(isPaused);
            Time.timeScale = isPaused ? 0 : 1; // 일시 정지 또는 재개
        }
    }

    public void ClickSave()
    {
        Debug.Log("세이브");
        // 세이브 로직 구현 추가 가능
    }

    public void ClickLoad()
    {
        Debug.Log("로드");
        // 로드 로직 구현 추가 가능
    }

    public void ClickExit()
    {
        Debug.Log("게임 종료");
        Application.Quit(); // 게임 종료
    }
}