using System;
using UnityEngine;

public class MobBehavior : MonoBehaviour
{
    public Transform player;
    public float speed = 2f; // 이동 속도
    public event Action OnDestroyed;

    public void Initialize(Transform playerTransform)
    {
        player = playerTransform;
    }

    private void Update()
    {
        if (player == null) return;

        // 플레이어를 추적
        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnDestroy()
    {
        OnDestroyed?.Invoke();
    }
}