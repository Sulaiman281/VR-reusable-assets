using TMPro;
using UnityEngine;

public class TestUI : MonoBehaviour
{
    [SerializeField] private TMP_Text valueText;

    private int _counter = 0;

    public void AddValue()
    {
        _counter++;
        valueText.text = _counter.ToString();
    }

    public void RemoveValue()
    {
        _counter--;
        valueText.text = _counter.ToString();
    }
}
