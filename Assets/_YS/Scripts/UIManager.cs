using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // UI 관련 요소를 다룰 때 필요
using UnityEngine.SceneManagement; // 씬 전환을 위해 필요

public class UIManager : MonoBehaviour
{
    public Button yesButton; // "Yes" 버튼 참조
    public Button StartButton; // "Start" 버튼 참조
    // Start is called before the first frame update
    void Start()
    {
        // "Yes" 버튼 클릭 시 QuitApplication 함수 연결
        if (yesButton != null)
        {
            yesButton.onClick.AddListener(QuitApplication);
        }

        // "Start" 버튼 클릭 시 ChangeScene 함수 연결
        if (StartButton != null)
        {
            StartButton.onClick.AddListener(ChangeScene);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 애플리케이션 종료 메서드
    public void QuitApplication()
    {
        Application.Quit(); // 애플리케이션 종료
        Debug.Log("애플리케이션 종료 중...");
    }

    // 씬 전환 메서드
    public void ChangeScene()
    {
        SceneManager.LoadScene(1); // Build Index가 1인 씬으로 전환
        Debug.Log("씬 전환 완료");
    }
}