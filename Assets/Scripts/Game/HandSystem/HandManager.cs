using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System;
using TMPro;
using System.Linq;
using UnityEngine.UI;

public class HandManager : MonoBehaviour
{
    [Header("Scripts")]
    public static HandManager Instance;
    public Hand handScript;
    public PlayerGameInfo PGI;
    public HandRewardDatabase rewardDB;
    public PlayHand PH;
    public JokerManager JM;

    [Header("References")]
    public GameObject cardPrefab;
    public GameObject floatingTextPrefab;
    public GameObject floatingSquarePrefab;
    public Transform uiCanvas;
    public Transform handArea;
    public Transform playZone;

    [Space]
    public TMP_Text currentHand;

    [Header("Temp in script"), Space]
    public bool loseGame;

    [Header("Hand Settings")]
    public List<CardData> hand = new List<CardData>();
    public bool sortByRank = true;

    public float spacing = 150f;
    public float curveHeight = 50f;
    public float rotationAmount = 5f;
    public float liftAmount = 30f;

    [Header("Play Settings")]
    public float playSpacing = 150f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void DrawStartingHand()
    {
        PGI.roundScore = 0;
        PGI.handsLeft = 4;
        PGI.discardsLeft = 4;

        StartCoroutine(handScript.DrawCard());
    }

    public void AddCard(CardData card)
    {
        PGI.deck -= 1;
        hand.Add(card);
        SortHand();
        DisplayHand();
    }

    public void PlaySelectedCards()
    {
        List<GameObject> selectedObjects = new List<GameObject>();

        foreach (Transform child in handArea)
        {
            CardDisplay cd = child.GetComponent<CardDisplay>();

            if (cd != null && cd.IsSelected())
            {
                selectedObjects.Add(child.gameObject);
            }
        }

        List<CardData> cardDataList = new List<CardData>();

        foreach (GameObject obj in selectedObjects)
        {
            CardDisplay display = obj.GetComponent<CardDisplay>();

            if (display != null)
            {
                cardDataList.Add(display.cardData);
            }
        }

        HandResult result = PokerHandEvaluator.EvaluateHand(cardDataList);

        Debug.Log($"<color=cyan>Checks: </color>Hand:  <color=cyan>{result.rank}</color>");

        foreach (var card in result.scoringCards)
        {
            Debug.Log($"<color=cyan>Checks: </color>Scoring: <color=cyan>{card.value}</color>  of <color=cyan>{card.suit}</color>");
        }

        HandReward reward = rewardDB.GetReward(result.rank);

        StartCoroutine(ResolvePlayAnimated(selectedObjects, result.scoringCards, reward));
    }

    public List<CardData> GetSelectedCardsData()
    {
        List<CardData> selected = new List<CardData>();

        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i].isSelected)
            {
                selected.Add(hand[i]);
            }
        }

        return selected;
    }

    int GetCardMult(CardData card)
    {
        return 0; // test value for now
    }

    double GetCardXMult(CardData card)
    {
        return 0; // test value for now
    }

    public string GetHandDisplayName(HandRank rank)
    {
        switch (rank)
        {
            case HandRank.HighCard: return "High Card";
            case HandRank.Pair: return "Pair";
            case HandRank.TwoPair: return "Two Pair";
            case HandRank.ThreeOfAKind: return "Three of a Kind";
            case HandRank.Straight: return "Straight";
            case HandRank.Flush: return "Flush";
            case HandRank.FullHouse: return "Full House";
            case HandRank.FourOfAKind: return "Four of a Kind";
            case HandRank.StraightFlush: return "Straight Flush";
            case HandRank.FiveOfAKind: return "Five of a Kind";
            case HandRank.FlushHouse: return "Flush House";
            case HandRank.FlushFive: return "Flush Five";
            case HandRank.RoyalFlush: return "Royal Flush";
            default: return "";
        }
    }

    public void UpdateLiveHandPreview()
    {
        List<CardData> selected = new List<CardData>();

        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i].isSelected)
            {
                selected.Add(hand[i]);
            }
        }

        if (selected.Count == 0)
        {
            currentHand.text = "";
            PGI.chips = 0;
            PGI.mult = 0;
            return;
        }

        HandResult rank = PokerHandEvaluator.EvaluateHand(selected);

        currentHand.text = GetHandDisplayName(rank.rank);
        PGI.chips = rewardDB.GetReward(rank.rank).chips;
        PGI.mult = rewardDB.GetReward(rank.rank).mult;
    }

    IEnumerator ResolvePlayAnimated(List<GameObject> cards, List<CardData> scoringCards, HandReward reward)
    {
        currentHand.text = GetHandDisplayName(reward.handRank);

        for (int i = hand.Count - 1; i >= 0; i--)
        {
            if (hand[i].isSelected)
            {
                hand.RemoveAt(i);
                PGI.hand -= 1;
            }
        }

        PGI.handsLeft --;

        float center = (cards.Count - 1) / 2f;

        // -------------------------
        // MOVE CARDS TO PLAY ZONE (SPREAD OUT)
        // -------------------------
        for (int i = 0; i < cards.Count; i++)
        {
            RectTransform rt = cards[i].GetComponent<RectTransform>();

            rt.SetParent(playZone);

            float x = (i - center) * playSpacing;

            rt.anchoredPosition = new Vector2(x, 0);
            rt.localRotation = Quaternion.identity;
        }

        DisplayHand();

        JokerSystem.HandContext context = new JokerSystem.HandContext
        {
            handRank = reward.handRank,
            scoringCards = scoringCards
        };

        yield return new WaitForSeconds(1f);

        // -------------------------
        // SCORING CHAIN
        // -------------------------

        JM.OnPlayedJoker(scoringCards, context);

        yield return new WaitForSeconds(1f);

        for (int i = 0; i < scoringCards.Count; i++)
        {
            CardData card = scoringCards[i];

            float chipValue = 0;
            chipValue = JM.ApplyChipsOnScoredJokers(card, context);
            int multValue = 0;
            double xMultValue = 0;

            // -------------------------
            // CHIPS
            // -------------------------
            PGI.chips += chipValue;
            GameObject chips = cards.First(c =>
                c.GetComponent<CardDisplay>().cardData == card
            );

            ShowFloatingText(chips.transform, "+" + chipValue, false);

            yield return new WaitForSeconds(1.3f);

            // -------------------------
            // MULT (ADDITIVE)
            // -------------------------
            if (multValue != 0)
            {
                PGI.mult += multValue;
                GameObject mult = cards.First(c =>
                    c.GetComponent<CardDisplay>().cardData == card
                );

                ShowFloatingText(mult.transform, "+" + multValue + " Mult", true);

                yield return new WaitForSeconds(1.3f);
            }

            // -------------------------
            // XMULT (MULTIPLICATIVE)
            // -------------------------
            if (xMultValue != 0 && xMultValue != 1)
            {
                PGI.mult = Math.Round(PGI.mult * xMultValue, 2);
                GameObject xMult = cards.First(c =>
                    c.GetComponent<CardDisplay>().cardData == card
                );

                ShowFloatingText(xMult.transform, "x" + xMultValue + " Mult", true);

                yield return new WaitForSeconds(1.3f);
            }

            yield return new WaitForSeconds(0.3f);
        }

        // -------------------------
        // FINAL SCORE CALCULATION
        // -------------------------
        currentHand.text = Math.Round(PGI.chips * PGI.mult, 2).ToString();
        yield return new WaitForSeconds(1f);
        
        PGI.roundScore += Math.Round(PGI.chips * PGI.mult, 2);
        currentHand.text = "";

        Debug.Log($"<color=yellow>Values: </color>FINAL SCORE: <color=yellow>{PGI.roundScore}</color>");

        yield return new WaitForSeconds(0.3f);

        PH.FindScore();

        // -------------------------
        // CLEANUP VISUALS
        // -------------------------

        PGI.chips = 0;
        PGI.mult = 1;

        for (int i = 0; i < cards.Count; i++)
        {
            Destroy(cards[i]);
        }

        UpdateLiveHandPreview();
        StartCoroutine(handScript.DrawCard());
        SortHand();
        DisplayHand();
    }

    public void ShowFloatingText(Transform target, string text, bool isMult)
    {
        GameObject textObj = Instantiate(floatingTextPrefab, uiCanvas.transform);
        GameObject square = Instantiate(floatingSquarePrefab, uiCanvas.transform);

        Image squareColor = square.GetComponent<Image>();
        Color32 blueColor = new Color32(0, 125, 255, 224); // #007DFF at 244 opacity
        Color32 redColor = new Color32(252, 74, 68, 224); // #FC4A44 at 244 opacity

        if (isMult)
        {
            squareColor.color = redColor;
        }
        else if (!isMult)
        {
            squareColor.color = blueColor;
        }

        textObj.transform.localScale = Vector3.one;
        square.transform.localScale = Vector3.one;

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;

        RectTransform canvasRect = uiCanvas.GetComponent<RectTransform>();
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        RectTransform squareRect = square.GetComponent<RectTransform>();

        // get SCREEN position of card
        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(null, target.position);

        // convert screen → canvas
        Vector2 anchoredPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            null,
            out anchoredPos
        );

        // PERFECT ALIGNMENT ABOVE CARD
        textRect.anchoredPosition = anchoredPos + new Vector2(0, 150);
        squareRect.anchoredPosition = anchoredPos + new Vector2(0, 150);

        StartCoroutine(FloatUp(textRect));
        StartCoroutine(FloatUp(squareRect));

        

        Destroy(textObj, 0.3f);
    }

    IEnumerator FloatUp(RectTransform rt)
    {
        float time = 0f;
        Vector2 start = rt.anchoredPosition;
        Vector2 end = start + new Vector2(0, 50);

        while (time < 1f)
        {
            time += Time.deltaTime * 2f;
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.Lerp(start, end, time);
            }
            yield return null;
        }
    }

    public void DiscardSelectedCards()
    {
        // Remove selected cards safely (backwards loop)
        for (int i = hand.Count - 1; i >= 0; i--)
        {
            if (hand[i].isSelected)
            {
                hand.RemoveAt(i);
                PGI.hand -= 1;
            }
        }

        StartCoroutine(handScript.DrawCard());
        PGI.discardsLeft -= 1;
        SortHand();
        DisplayHand();
    }

    public void OutOfCards() // Lose condition for when you completly run out of cards
    {
        if (PGI.deck <= 0 && hand.Count <= 0)
        {
            loseGame = true;
            StopCoroutine(handScript.DrawCard());
            Debug.Log("<color=red>Lost: </color>Out of Cards");
        }
    }

    public void DisplayHand()
    {
        // Clear old visuals
        foreach (Transform child in handArea)
        {
            Destroy(child.gameObject);
        }

        // Spawn cards
        for (int i = 0; i < hand.Count; i++)
        {
            GameObject obj = Instantiate(cardPrefab, handArea);
            CardDisplay display = obj.GetComponent<CardDisplay>();

            display.Init(hand[i], this);

            RectTransform rt = obj.GetComponent<RectTransform>();

            float center = (hand.Count - 1) / 2f;

            float x = (i - center) * spacing;
            float y = -Mathf.Abs(i - center) * curveHeight;
            float rot = (i - center) * -rotationAmount;

            // Lift selected cards
            if (hand[i].isSelected)
            {
                y += liftAmount;
            }

            rt.anchoredPosition = new Vector2(x, y);
            rt.localRotation = Quaternion.Euler(0, 0, rot);
        }
    }

    public void ClearHand()
    {
        #if UNITY_EDITOR
        UnityEditor.Selection.activeGameObject = null;
        #endif

        // Destroy all card objects in hand area
        for (int i = handArea.childCount - 1; i >= 0; i--)
        {
            Transform child = handArea.GetChild(i);
            Destroy(child.gameObject, 0.01f);
        }

        // Clear data
        hand.Clear();
        PGI.hand = 0;

        Debug.Log("<color=cyan>Check: </color>Hand cleared");
    }

    public void HandleCardDrop(CardDrag dragged)
    {
        dragged.transform.SetParent(handArea);

        int index = GetClosestCardIndex(dragged.GetComponent<RectTransform>().anchoredPosition);
        dragged.transform.SetSiblingIndex(index);

        UpdateHandOrder();
        DisplayHand();
    }

    int GetClosestCardIndex(Vector2 draggedPos)
    {
        int closest = 0;
        float minDist = float.MaxValue;

        for (int i = 0; i < handArea.childCount; i++)
        {
            Vector2 targetPos = new Vector2((i - (handArea.childCount - 1) / 2f) * spacing, 0 );

            float dist = Vector2.Distance(draggedPos, targetPos);

            if (dist < minDist)
            {
                minDist = dist;
                closest = i;
            }
        }

        return closest;
    }

    public void UpdateHandOrder()
    {
        hand.Clear();

        for (int i = 0; i < handArea.childCount; i++)
        {
            CardDisplay display = handArea.GetChild(i).GetComponent<CardDisplay>();

            if (display != null && display.cardData != null)
            {
                hand.Add(display.cardData);
            }
        }
    }

    int GetRankValue(CardData card)
    {
        // Ace is high
        if (card.value == Rank.Ace) return 14;
        return (int)card.value;
    }

    int GetSuitOrder(string suit)
    {
        switch (suit)
        {
            case "Spades": return 1;
            case "Hearts": return 2;
            case "Clubs": return 3;
            case "Diamonds": return 4;
        }
        return 99;
    }

    public void ChangeSortMethod(bool sortByRank)
    {
        this.sortByRank = sortByRank;
        SortHand();
        DisplayHand();
    }

    public void SortByRank()
    {
        hand.Sort((a, b) => GetRankValue(b).CompareTo(GetRankValue(a)));
        DisplayHand();
    }

    public void SortBySuit()
    {
        hand.Sort((a, b) =>
        {
            int suitCompare = GetSuitOrder(a.suit.ToString()).CompareTo(GetSuitOrder(b.suit.ToString()));

            if (suitCompare == 0)
            {
                // Sort by rank DESC inside suit
                return GetRankValue(b).CompareTo(GetRankValue(a));
            }

            return suitCompare;
        });

        DisplayHand();
    }

    public void SortHand()
    {
        ResetVisual();
        
        if (sortByRank)
        {
            SortByRank();
        }
        else
        {
            SortBySuit();
        }
    }

    public void ResetVisual()
    {
        foreach (Transform t in handArea)
        {
            CardDisplay cd = t.GetComponent<CardDisplay>();
            cd.MovingResetVisual();
        }
    }

    public int GetSelectedCount()
    {
        int count = 0;

        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i].isSelected)
                count++;
        }

        return count;
    }
}