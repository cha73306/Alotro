using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class Deck : MonoBehaviour
{
    public HandManager handManager;
    public PlayerGameInfo PGI;
    public CardDatabase CD;

    public List<CardData> deck = new List<CardData>();

    void Start()
    {
        BuildDeck();
    }

    public void BuildDeck()
    {
        deck.Clear();

        int index = 0;

        foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
        {
            for (int v = 1; v <= 13; v++)
            {
                CardData card = new CardData();
                
                card.value = (Rank)v;
                card.suit = suit;
                
                card.sprite = CD.allCards[index].sprite;

                deck.Add(card);
                index++;
            }
        }

        ShuffleDeck();
        PGI.deck = deck.Count;
    }

    void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            CardData temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }
}