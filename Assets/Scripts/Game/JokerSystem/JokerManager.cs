using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System;
using System.Collections;

public class JokerManager : MonoBehaviour
{
    [Header("Scripts"), Space]
    public static JokerManager Instance;
    public JokerDatabase jokerDatabase;
    public PlayerGameInfo PGI;
    public JokerSystem jokerSystem;
    public HandManager HM;

    [Header("Objects"), Space]
    public Transform jokerArea; // UI parent (like handArea)
    public GameObject jokerPrefab;

    public GameObject floatingTextPrefab;
    public GameObject floatingSquarePrefab;
    public Transform uiCanvas;

    [Header("Variables"), Space]
    public List<JokerData> ownedJokers = new List<JokerData>();
    public List<GameObject> jokerObjects = new List<GameObject>();

    public float jokerSpacing = 150f;

    public bool test = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void GiveJoker(int index) // DEBUG: give joker by index
    {
        if (index < 0 || index >= jokerDatabase.jokers.Count)
        {
            Debug.LogError("Invalid joker index");
            return;
        }

        if (PGI.jokers >= PGI.jokerSlots)
        {
            Debug.Log("<color=red>Error: </color>Max Jokers reached!");
            return;
        }

        JokerData data = jokerDatabase.GetJoker(index);

        if (data == null)
        {
            Debug.LogError($"Joker is NULL at index {index}");
            return;
        }
        InitializeJoker(data);
        SpawnJoker(data);
    }

    public void SpawnJoker(JokerData data) // Spawns UI object
    {
        GameObject obj = Instantiate(jokerPrefab, jokerArea, false);
        ownedJokers.Add(data);
        jokerObjects.Add(obj);
        PGI.jokers ++;

        JokerDisplay display = obj.GetComponent<JokerDisplay>();
        display.Setup(data);

        LayoutGroup lg = jokerArea.GetComponent<LayoutGroup>();
        if (lg != null) lg.enabled = false;

        ArrangeJokers();
    }

    public void RemoveJoker(int index)
    {
        if (index < 0 || index >= jokerObjects.Count) return;

        Destroy(jokerObjects[index]);

        jokerObjects.RemoveAt(index);
        ownedJokers.RemoveAt(index);
        PGI.jokers --;
    }

    public void ArrangeJokers()
    {
        int count = jokerArea.childCount;

        if (count == 0) return;

        float totalWidth = (count - 1) * jokerSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            RectTransform rt = jokerArea.GetChild(i).GetComponent<RectTransform>();

            if (rt == null) continue;

            float x = startX + (i * jokerSpacing);

            rt.anchoredPosition = new Vector2(x, 0);
            rt.localRotation = Quaternion.identity;
        }
    }

    GameObject GetJokerObject(JokerData joker)
    {
        return jokerObjects.First(j =>
            j.GetComponent<JokerDisplay>().jokerData == joker
        );
    }

    public void ShowFloatingText(Transform target, string text, string colorHex)
    {
        GameObject textObj = Instantiate(floatingTextPrefab, uiCanvas.transform);
        GameObject square = Instantiate(floatingSquarePrefab, uiCanvas.transform);

        Image squareColor = square.GetComponent<Image>();
        Color32 blueColor = new Color32(0, 125, 255, 224); // #007DFF at 244 opacity
        Color32 redColor = new Color32(252, 74, 68, 224); // #FC4A44 at 244 opacity
        Color32 orangeColor = new Color32(255, 206, 0, 224); // #FFCE00 at 244 opacity

        if (colorHex == "red")
        {
            squareColor.color = redColor;
        }
        else if (colorHex == "blue")
        {
            squareColor.color = blueColor;
        }
        else if (colorHex == "orange")
        {
            squareColor.color = orangeColor;
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
        textRect.anchoredPosition = anchoredPos + new Vector2(0, -120);
        squareRect.anchoredPosition = anchoredPos + new Vector2(0, -120);

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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) GiveJoker(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) GiveJoker(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) GiveJoker(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) GiveJoker(3);

        if (test == true) 
        {
            for (int i = ownedJokers.Count; i >= 0; i--) 
            {    
                RemoveJoker(i);
            }
            test = false;
        }
        
    }

    public float ApplyChipsOnScoredJokers(CardData card, JokerSystem.HandContext context) // Get Rid Of Soon
    {
        float chips = GetBaseChipValue(card);

        foreach (var joker in ownedJokers)
        {
            if (joker.activation != Activation.OnScored)
            continue;

            // IMPORTANT: check card condition
            if (!jokerSystem.DoesCardMatch(joker, card, context))
                continue;

            switch (joker.type)
            {
                case JokerType.AddChips:
                    chips += joker.value;
                    break;

                case JokerType.AddMult:
                    // handle later
                    break;
            }
        }

        return chips;
    }
    
    public void OnPlayedJoker(List<CardData> cards, JokerSystem.HandContext context)
    {
        foreach (var joker in ownedJokers)
        {
            if (joker.activation != Activation.OnPlayed && 
                joker.activation != Activation.Mixed)
                continue;
            
            if (!DoesJokerMeetCondition(joker, context))
            {
                Debug.Log("<color=cyan>Checks: </color>Meet <color=red>Not</color> Conditions");
                continue;
            }

            Debug.Log("<color=cyan>Checks: </color>Meet Conditions"); // From DoesJokerMeetCondition()
            switch (joker.type)
            {
                case JokerType.Effect:
                    OnPlayedEffects(joker); // Later add arguments like: joker, card, context
                    break;
                
                case JokerType.Economy:
                    ShowFloatingText(GetJokerObject(joker).transform, "$" + joker.value, "orange");
                    PGI.money += (int)joker.value;
                    break;
            }
        }
    }

    void OnPlayedEffects(JokerData joker) // Not here yet
    {
        
    }

    public void InitializeJoker(JokerData joker)
    {
        if (joker.useRandomTargetHand)
        {
            joker.targetHand = GetRandomHandRank();
        }
    }

    private HandRank GetRandomHandRank()
    {
        var values = Enum.GetValues(typeof(HandRank));
        var randomHand = (HandRank)values.GetValue(UnityEngine.Random.Range(0, values.Length));
        Debug.Log($"<color=#FF0081>Jokers:</color> Play a <color=#FF0081>{HM.GetHandDisplayName(randomHand)}</color> this round"); // color = neon Rose or bubblegum bright
        return randomHand;
        
    }

    bool DoesJokerMeetCondition(JokerData joker , JokerSystem.HandContext context)
    {
        switch (joker.condition)
        {
            case CardCondition.Any:
                return true;
            
            case CardCondition.FaceCard:
                return context.scoringCards.Any(c =>
                c.value == Rank.Jack ||
                c.value == Rank.Queen ||
                c.value == Rank.King);

            case CardCondition.Odd:
                return context.scoringCards.Any(c =>
                c.value == Rank.Ace ||
                c.value == Rank.Nine ||
                c.value == Rank.Seven ||
                c.value == Rank.Five ||
                c.value == Rank.Three);
            
            case CardCondition.Even:
                return context.scoringCards.Any(c =>
                c.value == Rank.Two ||
                c.value == Rank.Four ||
                c.value == Rank.Six ||
                c.value == Rank.Eight ||
                c.value == Rank.Ten);
            
            case CardCondition.SpecificRank:
                return context.scoringCards.Any(c =>
                c.value == joker.targetRank);

            case CardCondition.SpecificSuit:
                return context.scoringCards.Any(c =>
                c.suit == joker.targetSuit);
            
            case CardCondition.SpecificHand:
            
                if (context.handRank != joker.targetHand)
                    return false;

                if (joker.requiresAce)
                    return context.scoringCards.Any(c => c.value == Rank.Ace);

                return true;
                
            default:
                return true;
        }
    }

    public float GetBaseChipValue(CardData card)
    {
        switch (card.value)
        {
            case Rank.Ace: return 11;
            case Rank.King: return 10;
            case Rank.Queen: return 10;
            case Rank.Jack: return 10;
            default: return (int)card.value;
        }
    }

    public void HandleDrop(JokerDrag dragged)
    {
        // Put it back into the joker area
        dragged.transform.SetParent(jokerArea);

        // Set order
        int newIndex = GetClosestIndex(dragged.GetComponent<RectTransform>().anchoredPosition);
        dragged.transform.SetSiblingIndex(newIndex);

        UpdateOwnedJokerOrder();

        ArrangeJokers();
    }

    int GetClosestIndex(Vector3 draggedPos)
    {
        int closest = 0;
        float minDist = float.MaxValue;

        for (int i = 0; i < jokerArea.childCount; i++)
        {
            Vector2 targetPos = new Vector2(
                (i - (jokerArea.childCount - 1) / 2f) * jokerSpacing, 0);

            float dist = Vector2.Distance(draggedPos, targetPos);

            if (dist < minDist)
            {
                minDist = dist;
                closest = i;
            }
        }

        return closest;
    }

    void UpdateOwnedJokerOrder()
    {
        ownedJokers.Clear();

        for (int i = 0; i < jokerArea.childCount; i++)
        {
            Debug.Log(i + ": " + jokerArea.GetChild(i).name);
            JokerDisplay jd = jokerArea.GetChild(i).GetComponent<JokerDisplay>();
            ownedJokers.Add(jd.jokerData);
        }
    }
}