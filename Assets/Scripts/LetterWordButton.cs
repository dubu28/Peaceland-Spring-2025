using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class LetterWordButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI wordLabel;
    [SerializeField] private Image moodIcon;
    [SerializeField] private Image pushpinIcon;
    [SerializeField] private RectTransform cardRoot;

    private WordChoice currentChoice;
    private Action<WordChoice> onSelectedCallback;
    private Vector3 originalScale = Vector3.one;
    private Quaternion originalRotation = Quaternion.identity;
    private float targetScale = 1f;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (cardRoot == null) cardRoot = GetComponent<RectTransform>();
        if (cardRoot != null)
        {
            originalScale = cardRoot.localScale;
            originalRotation = cardRoot.localRotation;
        }

        if (button != null)
        {
            button.onClick.AddListener(HandleClicked);
        }
    }

    public void Setup(WordChoice choice, Action<WordChoice> onSelected, Sprite moodSprite, Sprite pushpinSprite)
    {
        currentChoice = choice;
        onSelectedCallback = onSelected;

        if (wordLabel != null && choice != null)
        {
            wordLabel.text = choice.word;
        }

        if (moodIcon != null)
        {
            if (moodSprite != null)
            {
                moodIcon.sprite = moodSprite;
                moodIcon.gameObject.SetActive(true);
            }
            else
            {
                moodIcon.gameObject.SetActive(false);
            }
        }

        if (pushpinIcon != null && pushpinSprite != null)
        {
            pushpinIcon.sprite = pushpinSprite;
        }

        targetScale = 1f;
        if (cardRoot != null)
        {
            cardRoot.localScale = originalScale;
            cardRoot.localRotation = originalRotation;
        }
    }

    private void Update()
    {
        if (cardRoot != null)
        {
            cardRoot.localScale = Vector3.Lerp(cardRoot.localScale, originalScale * targetScale, Time.unscaledDeltaTime * 15f);
        }
    }

    private void HandleClicked()
    {
        if (currentChoice != null && onSelectedCallback != null)
        {
            onSelectedCallback.Invoke(currentChoice);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = 1.06f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = 1.0f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = 0.96f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = 1.06f;
    }
}
