using UnityEngine;
using System.Collections.Generic;

public class SimpleGameManager : MonoBehaviour
{
    [Header("Setup")]
    public GameObject cardPrefab;
    public Transform cardParent;
    public Transform leftZone;
    public Transform rightZone;

    [Header("Card Sprites")]
    public Sprite[] normalSprites = new Sprite[8];
    public Sprite[] selectedSprites = new Sprite[8];
    public Sprite[] disabledSprites = new Sprite[8];

    [Header("Layout")]
    public float cardSpacing = 250f;
    public Vector2 topRowPosition = new Vector2(-375f, 50f);
    public Vector2 bottomRowPosition = new Vector2(-375f, -300f);

    private List<ClickableCard> allCards = new List<ClickableCard>();
    private List<ClickableCard> disabledCards = new List<ClickableCard>(); // 다음 턴에 사용 불가한 카드들
    private ClickableCard leftZoneCard = null;
    private ClickableCard rightZoneCard = null;

    void Start()
    {
        CreateCards();
    }

    void CreateCards()
    {
        for (int i = 0; i < 8; i++)
        {
            GameObject cardObj = Instantiate(cardPrefab, cardParent);
            ClickableCard card = cardObj.GetComponent<ClickableCard>();

            // 카드 번호 설정
            card.SetCardNumber(i + 1);

            // 스프라이트 설정
            if (i < normalSprites.Length && normalSprites[i] != null)
                card.normalSprite = normalSprites[i];
            if (i < selectedSprites.Length && selectedSprites[i] != null)
                card.selectedSprite = selectedSprites[i];
            if (i < disabledSprites.Length && disabledSprites[i] != null)
                card.disabledSprite = disabledSprites[i];

            // 위치 설정 (4장씩 2줄)
            Vector3 cardPosition;
            if (i < 4)
                cardPosition = new Vector3(topRowPosition.x + (i * cardSpacing), topRowPosition.y, 0);
            else
                cardPosition = new Vector3(bottomRowPosition.x + ((i - 4) * cardSpacing), bottomRowPosition.y, 0);

            cardObj.GetComponent<RectTransform>().localPosition = cardPosition;

            // 이벤트 연결
            card.OnCardClicked += OnCardClicked;
            allCards.Add(card);
        }
    }

    private void OnCardClicked(ClickableCard clickedCard)
    {
        // 비활성화된 카드는 클릭 불가
        if (clickedCard.IsDisabled()) return;

        // 카드가 원래 위치에 있으면 -> 존으로 이동
        if (clickedCard.IsInOriginalPosition())
        {
            if (leftZoneCard == null)
            {
                leftZoneCard = clickedCard;
                clickedCard.MoveToZone(leftZone);
            }
            else if (rightZoneCard == null)
            {
                rightZoneCard = clickedCard;
                clickedCard.MoveToZone(rightZone);
            }
        }
        // 카드가 존에 있으면 -> 원래 위치로 복귀
        else
        {
            if (clickedCard == leftZoneCard)
            {
                leftZoneCard = null;
                clickedCard.ReturnToOriginalPosition();
            }
            else if (clickedCard == rightZoneCard)
            {
                rightZoneCard = null;
                clickedCard.ReturnToOriginalPosition();
            }
        }
    }

    // 테스트용 메서드
    [ContextMenu("Clear All")]
    public void ClearAll()
    {
        if (leftZoneCard != null)
        {
            leftZoneCard.ReturnToOriginalPosition();
            leftZoneCard = null;
        }
        if (rightZoneCard != null)
        {
            rightZoneCard.ReturnToOriginalPosition();
            rightZoneCard = null;
        }
    }

    // 최종 카드 선택 (왼쪽 카드를 최종 선택)
    [ContextMenu("Choose Left Card")]
    public void ChooseLeftCard()
    {
        if (leftZoneCard != null && rightZoneCard != null)
        {
            // 오른쪽 카드를 다음 턴 비활성화 목록에 추가
            disabledCards.Add(rightZoneCard);
            rightZoneCard.ReturnToOriginalPosition();

            // 왼쪽 카드는 덱에서 제거 (게임오브젝트 비활성화)
            leftZoneCard.gameObject.SetActive(false);
            allCards.Remove(leftZoneCard);

            Debug.Log($"카드 {leftZoneCard.GetCardNumber()}를 최종 선택! 카드 {rightZoneCard.GetCardNumber()}는 다음 턴 사용 불가");

            leftZoneCard = null;
            rightZoneCard = null;
        }
        else
        {
            Debug.Log("두 장의 카드를 모두 선택해주세요.");
        }
    }

    // 최종 카드 선택 (오른쪽 카드를 최종 선택)
    [ContextMenu("Choose Right Card")]
    public void ChooseRightCard()
    {
        if (leftZoneCard != null && rightZoneCard != null)
        {
            // 왼쪽 카드를 다음 턴 비활성화 목록에 추가
            disabledCards.Add(leftZoneCard);
            leftZoneCard.ReturnToOriginalPosition();

            // 오른쪽 카드는 덱에서 제거 (게임오브젝트 비활성화)
            rightZoneCard.gameObject.SetActive(false);
            allCards.Remove(rightZoneCard);

            Debug.Log($"카드 {rightZoneCard.GetCardNumber()}를 최종 선택! 카드 {leftZoneCard.GetCardNumber()}는 다음 턴 사용 불가");

            leftZoneCard = null;
            rightZoneCard = null;
        }
        else
        {
            Debug.Log("두 장의 카드를 모두 선택해주세요.");
        }
    }

    // 다음 턴 시작 (비활성화된 카드들을 disabled 상태로 변경)
    [ContextMenu("Start Next Turn")]
    public void StartNextTurn()
    {
        // 이전 턴에서 비활성화된 카드들을 disabled 상태로 변경
        foreach (ClickableCard card in disabledCards)
        {
            card.SetDisabledState();
        }

        Debug.Log($"{disabledCards.Count}장의 카드가 이번 턴에 사용 불가능합니다.");
    }

    // 턴 종료 후 disabled 카드들을 다시 사용 가능하게 만들기
    [ContextMenu("End Turn")]
    public void EndTurn()
    {
        // disabled 상태의 카드들을 다시 normal 상태로 복구
        foreach (ClickableCard card in disabledCards)
        {
            card.SetNormalState();
        }

        disabledCards.Clear();
        Debug.Log("비활성화된 카드들이 다시 사용 가능해졌습니다.");
    }
}