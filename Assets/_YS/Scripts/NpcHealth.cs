using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace YS.Scripts
{
    public class NpcHealth : MonoBehaviour
    {
        [Header("체력 설정")]
        public float maxHealth = 100f; // 최대 체력
        private float currentHealth;

        [Header("UI 요소")]
        public Image healthBarImage; // 체력바 이미지 (Fill 방식)
        public float smoothSpeed = 5f; // 체력바 변화 속도
        private float targetFill;

        [Header("피격 효과")]
        public bool useFlashEffect = true; // 피격 시 플래시 효과 사용 여부
        public Image damageFlashImage; // 피격 시 화면 플래시 이미지
        public float flashSpeed = 5f; // 플래시 사라지는 속도
        private Color flashColor;

        [Header("피격 무적시간")]
        public float invincibilityTime = 1.0f; // 피격 후 무적 시간 (초)
        private bool isInvincible = false; // 무적 상태 여부

        [Header("캐릭터 깜빡임 효과")]
        public float blinkDuration = 1.0f; // 깜빡임 지속 시간
        public float blinkRate = 0.1f; // 깜빡임 간격 (초)
        private SpriteRenderer[] spriteRenderers; // NPC 스프라이트 렌더러

        private bool isDead = false; // NPC 사망 여부

        void Start()
        {
            // 초기 체력 설정
            currentHealth = maxHealth;
            targetFill = 1f;

            // 플래시 이미지 초기화
            if (damageFlashImage != null)
            {
                flashColor = damageFlashImage.color;
                flashColor.a = 0f;
                damageFlashImage.color = flashColor;
            }

            // 체력바 초기화
            UpdateHealthBar();

            // 스프라이트 렌더러 컴포넌트 가져오기
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        }

        void Update()
        {
            // 체력바 부드럽게 변화
            if (healthBarImage != null)
            {
                healthBarImage.fillAmount = Mathf.Lerp(healthBarImage.fillAmount, targetFill, Time.deltaTime * smoothSpeed);
            }

            // 피격 플래시 효과 업데이트
            if (useFlashEffect && damageFlashImage != null)
            {
                if (flashColor.a > 0)
                {
                    flashColor.a = Mathf.Max(0, flashColor.a - Time.deltaTime * flashSpeed);
                    damageFlashImage.color = flashColor;
                }
            }
        }

        public void TakeDamage(float damage)
        {
            // 무적 상태거나 사망한 경우 무시
            if (isInvincible || isDead) return;

            // 체력 감소
            currentHealth = Mathf.Max(0, currentHealth - damage);

            // 체력바 업데이트
            UpdateHealthBar();

            // 피격 효과 표시
            ShowDamageEffect();

            Debug.Log(gameObject.name + "이(가) " + damage + "의 데미지를 입었습니다. 남은 체력: " + currentHealth);

            // 사망 확인
            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                // 무적 시간 설정
                StartCoroutine(InvincibilityCoroutine());
            }
        }

        private void UpdateHealthBar()
        {
            if (healthBarImage != null)
            {
                // 체력 비율 계산 및 체력바 이미지 업데이트
                targetFill = currentHealth / maxHealth;
                healthBarImage.fillAmount = targetFill;
            }
        }

        private void ShowDamageEffect()
        {
            if (useFlashEffect && damageFlashImage != null)
            {
                flashColor.a = 0.5f; // 플래시 알파값 설정
                damageFlashImage.color = flashColor;
            }
        }

        private void Die()
        {
            if (isDead) return;

            isDead = true;
            Debug.Log(gameObject.name + "이(가) 사망했습니다.");

            // NPC 제거
            Destroy(gameObject);
        }

        private IEnumerator InvincibilityCoroutine()
        {
            isInvincible = true;

            // 캐릭터 깜빡임 효과
            float endTime = Time.time + invincibilityTime;
            bool visible = false;

            while (Time.time < endTime)
            {
                // 캐릭터 가시성 전환
                visible = !visible;
                SetCharacterVisibility(visible);

                yield return new WaitForSeconds(blinkRate);
            }

            // 깜빡임 종료 후 항상 보이게 설정
            SetCharacterVisibility(true);

            isInvincible = false;
        }

        private void SetCharacterVisibility(bool visible)
        {
            if (spriteRenderers != null && spriteRenderers.Length > 0)
            {
                foreach (SpriteRenderer renderer in spriteRenderers)
                {
                    if (renderer != null)
                    {
                        Color color = renderer.color;
                        color.a = visible ? 1f : 0.3f; // 완전히 투명하지 않고 반투명으로 설정
                        renderer.color = color;
                    }
                }
            }
        }

        public float GetCurrentHealth()
        {
            return currentHealth;
        }

        public bool IsDead()
        {
            return isDead;
        }
    }
}
