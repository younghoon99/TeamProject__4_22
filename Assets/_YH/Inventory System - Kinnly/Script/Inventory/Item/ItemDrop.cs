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

    private PlayerInventory playerInventory; // 플레이어 인벤토리 직접 참조
    float speed;
    float delay;

    // Start is called before the first frame update
    void Start()
    {
        // 플레이어를 태그로 찾기
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
           

            if (playerObject == null)
            {
                playerObject = GameObject.Find("Player");
            }
        }

        if (playerObject != null)
        {
            // 플레이어 인벤토리 컴포넌트 직접 찾기
            playerInventory = playerObject.GetComponent<PlayerInventory>();
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
            // 플레이어 인벤토리가 없을 경우 다시 찾기 시도
            if (playerInventory == null)
            {
                // 플레이어를 태그로 찾기
                if (playerObject == null)
                {
                    playerObject = GameObject.FindGameObjectWithTag("Player");

                    if (playerObject == null)
                    {
                        playerObject = GameObject.Find("Player");
                    }
                }

                if (playerObject != null)
                {
                    // 플레이어 인벤토리 컴포넌트 직접 찾기
                    playerInventory = playerObject.GetComponent<PlayerInventory>();
                }

                if (playerInventory == null) return; // 플레이어 인벤토리를 찾지 못하면 건너뛰기
            }

            CheckDistance();
            if (isNear)
            {
                
                CheckSlotAvailability();
                if (isSlotAvailable)
                {
                    MovingtoTarget();
                    AddingItem();
                }
                else
                {
                    
                }
            }
            else
            {
               
            }
        }
    }

    public void SetItem(Item item, int amount)
    {
        this.item = item;
        this.amount = amount;
        this.gameObject.name = item.name;
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = item.image;
        }
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
        if (playerObject == null)
        {
           
            return;
        }

        float distance = Vector2.Distance(this.transform.position, playerObject.transform.position);
        if (distance <= 5f)
        {
            isNear = true;
        }
        else
        {
            isNear = false;
        }
    }

    private void CheckSlotAvailability()
    {
        if (playerInventory == null)
        {
            return;
        }

        if (playerInventory.IsSlotAvailable(item, amount))
        {
            isSlotAvailable = true;
        }
        else
        {
            isDelay = false;
            isNear = false;
            delay = 1f;
        }
    }

    private void MovingtoTarget()
{
    if (playerObject == null)
    {
        return;
    }

    Vector3 direction = playerObject.transform.position - transform.position;
    direction.Normalize();
        transform.Translate(direction * speed * Time.deltaTime);
        speed += 20f * Time.deltaTime;
      
    }

    private void AddingItem()
    {
        
        if (playerObject == null || playerInventory == null)
        {
         
            return;
        }

        float distance = Vector2.Distance(this.transform.position, playerObject.transform.position);

        if (distance <= 0.5f)
        {
            playerInventory.AddItem(item, amount);
            Destroy(gameObject);
        }
        else
        {
        
        }
    }
}