// 이 스크립트는 더 이상 사용되지 않습니다.
// 인벤토리 시스템 개선을 위해 비활성화되었습니다.
/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Kinnly
{
    public class ToolbarItem : MonoBehaviour
    {
        [SerializeField] Image image;
        [SerializeField] TMP_Text amountText;

        int amount;

        public void SetItem(Item item, int amount)
        {
            image.sprite = item.image;
            this.amount = amount;
            UpdateUI();
        }

        void UpdateUI()
        {
            if (amount <= 1)
            {
                amountText.gameObject.SetActive(false);
            }
            else
            {
                amountText.gameObject.SetActive(true);
                amountText.text = amount.ToString();
            }

            if (amount <= 0)
            {
                Destroy(this.gameObject);
            }
        }
    }
}
*/