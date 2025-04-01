using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using Kinnly; // Item과 PlayerInventory 클래스를 사용하기 위한 네임스페이스 추가

public class ItemDrop : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Item item;
    [SerializeField] int amount;

    [SerializeField] private GameObject playerObject; // Inspector에서 할당 가능

    bool isDelay;
    bool isNear;
    bool isSlotAvailable;

    private Player player;
    float speed;
    float delay;

    // Start is called before the first frame update
    void Start()
    {
        // 플레이어를 태그로 찾기
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
            Debug.Log("Start: 플레이어 태그로 찾기 " + (playerObject != null ? "성공" : "실패"));
        }
        
        if (playerObject != null)
        {
            player = playerObject.GetComponent<Player>();
            Debug.Log("Start: Player 컴포넌트 찾기 " + (player != null ? "성공" : "실패"));
        }

        if (item == null)
        {
            return;
        }

        speed = 10f;
        delay = 0.25f;
        isDelay = true;
        spriteRenderer.sprite = item.image;
    }

    // Update is called once per frame
    void Update()
    {
        if (isDelay)
        {
            TimeCountDown();
        }
        else
        {
            // Player가 없을 경우 다시 찾기 시도
            if (player == null)
            {
                // 플레이어를 태그로 찾기
                if (playerObject == null)
                {
                    playerObject = GameObject.FindGameObjectWithTag("Player");
                    Debug.Log("Update: 플레이어 태그로 찾기 " + (playerObject != null ? "성공" : "실패"));
                    
                    if (playerObject == null)
                    {
                        playerObject = GameObject.Find("Player");
                        Debug.Log("Update: 플레이어 이름으로 찾기 " + (playerObject != null ? "성공" : "실패"));
                    }
                }
                
                if (playerObject != null)
                {
                    player = playerObject.GetComponent<Player>();
                    Debug.Log("Update: Player 컴포넌트 찾기 " + (player != null ? "성공" : "실패"));
                }
                
                if (player == null) return; // 플레이어를 찾지 못하면 건너뛰기
            }
            
            CheckDistance();
            if (isNear)
            {
                Debug.Log("플레이어 근처에 있음: 거리 체크 성공");
                CheckSlotAvailability();
                if (isSlotAvailable)
                {
                    Debug.Log("인벤토리 슬롯 사용 가능");
                    MovingtoTarget();
                    AddingItem();
                }
                else
                {
                    Debug.Log("인벤토리 슬롯 사용 불가능");
                }
            }
            else
            {
                Debug.Log("플레이어와의 거리: " + Vector2.Distance(this.transform.position, player.transform.position));
            }
        }
    }

    public void SetItem(Item item, int amount)
    {
        this.item = item;
        this.amount = amount;
        this.gameObject.name = item.name;
        Start();
    }

    private void TimeCountDown()
    {
        delay -= 1f * Time.deltaTime;
        if (delay <= 0f)
        {
            isDelay = false;
        }
    }

    private void CheckDistance()
    {
        if (player == null)
        {
            Debug.Log("CheckDistance: 플레이어가 null입니다");
            return;
        }
        
        float distance = Vector2.Distance(this.transform.position, player.transform.position);
        if (distance <= 5f)
        {
            isNear = true;
            Debug.Log("CheckDistance: 플레이어와 충분히 가까움 (거리: " + distance + ")");
        }
        else
        {
            isNear = false;
            Debug.Log("CheckDistance: 플레이어와 너무 멈 (거리: " + distance + ")");
        }
    }

    private void CheckSlotAvailability()
    {
        if (player == null)
        {
            Debug.Log("CheckSlotAvailability: 플레이어가 null입니다");
            return;
        }
        
        PlayerInventory playerInventory = player.GetComponent<PlayerInventory>();
        Debug.Log("CheckSlotAvailability: 인벤토리 컴포넌트 " + (playerInventory != null ? "찾음" : "찾지 못함"));
        
        if (playerInventory != null && playerInventory.IsSlotAvailable(item, amount))
        {
            isSlotAvailable = true;
            Debug.Log("CheckSlotAvailability: 슬롯 사용 가능");
        }
        else
        {
            isDelay = false;
            isNear = false;
            delay = 1f;
            Debug.Log("CheckSlotAvailability: 슬롯 사용 불가능, 딜레이 재설정");
        }
    }

    private void MovingtoTarget()
    {
        if (player == null)
        {
            Debug.Log("MovingtoTarget: 플레이어가 null입니다");
            return;
        }
        
        Vector3 direction = player.transform.position - transform.position;
        direction.Normalize();
        transform.Translate(direction * speed * Time.deltaTime);
        speed += 20f * Time.deltaTime;
        Debug.Log("MovingtoTarget: 플레이어 방향으로 이동 중 (속도: " + speed + ")");
    }

    private void AddingItem()
    {
        Debug.Log("아이템 추가 실행 시도");
        if (player == null)
        {
            Debug.Log("AddingItem: 플레이어가 null입니다");
            return;
        }
        
        float distance = Vector2.Distance(this.transform.position, player.transform.position);
        Debug.Log("AddingItem: 플레이어와의 거리: " + distance);
        
        if (distance <= 0.5f)
        {
            Debug.Log("AddingItem: 습득 거리 도달 (거리: " + distance + ")");
            PlayerInventory playerInventory = player.GetComponent<PlayerInventory>();
            if (playerInventory != null)
            {
                Debug.Log("AddingItem: 인벤토리에 아이템 추가 시도 - " + item.name + " x" + amount);
                playerInventory.AddItem(item, amount);
                Debug.Log("AddingItem: 아이템 객체 파괴 직전");
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("AddingItem: PlayerInventory 컴포넌트를 찾을 수 없음");
            }
        }
        else
        {
            Debug.Log("AddingItem: 아직 습득 거리에 도달하지 않음 (거리: " + distance + ")");
        }
    }
}