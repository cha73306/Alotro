using UnityEngine;
using UnityEngine.UI;

public class JokerDisplay : MonoBehaviour
{
    public Image artwork;

    public JokerData jokerData;

    public void Setup(JokerData data)
    {
        jokerData = data;
        artwork.sprite = data.artwork;
    }
}