using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LetterManager : MinigameBehavior
{
    [Header("Letter Content")]
    [SerializeField] private LetterData currentLetter;
    [SerializeField] private TextMeshProUGUI letterBodyText;
    [SerializeField] private RectTransform letterPaperRect;

    [Header("Word Bank")]
    [SerializeField] private RectTransform wordBankPanel;
    [SerializeField] private Transform wordButtonsContainer;
    [SerializeField] private GameObject wordButtonPrefab;
    [SerializeField] private TextMeshProUGUI blankProgressText;

    [Header("Mood Sprites")]
    [SerializeField] private Sprite heartSprite;
    [SerializeField] private Sprite leafSprite;
    [SerializeField] private Sprite moonSprite;
    [SerializeField] private Sprite sunSprite;
    [SerializeField] private Sprite pushpinSprite;
    [SerializeField] private Sprite waxSealSprite;

    [Header("Mood Tracker HUD")]
    [SerializeField] private RectTransform moodTrackerPanel;
    [SerializeField] private RectTransform heartChibi;
    [SerializeField] private RectTransform leafChibi;
    [SerializeField] private RectTransform moonChibi;
    [SerializeField] private RectTransform sunChibi;
    [SerializeField] private TextMeshProUGUI heartCountText;
    [SerializeField] private TextMeshProUGUI leafCountText;
    [SerializeField] private TextMeshProUGUI moonCountText;
    [SerializeField] private TextMeshProUGUI sunCountText;
    [SerializeField] private TextMeshProUGUI floatingFeedbackText;

    [Header("Seal & Results")]
    [SerializeField] private Button sealButton;
    [SerializeField] private RectTransform sealButtonRect;
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private TextMeshProUGUI resultMoodTitle;
    [SerializeField] private TextMeshProUGUI resultMoodDesc;
    [SerializeField] private TextMeshProUGUI resultStatsText;
    [SerializeField] private Button rewriteButton;

    // Runtime state
    private int currentBlankIndex = 0;
    private string[] filledWords;
    private readonly Dictionary<LetterMoodType, int> moodScores = new Dictionary<LetterMoodType, int>();
    private Coroutine feedbackCoroutine;

    private void Awake()
    {
        if (sealButton != null)
        {
            sealButton.onClick.AddListener(OnSealButtonClicked);
        }

        if (rewriteButton != null)
        {
            rewriteButton.onClick.AddListener(ResetMinigame);
        }
    }

    private void Start()
    {
        StartMinigame();
    }

    public override void StartMinigame()
    {
        if (currentLetter == null)
        {
            Debug.LogWarning("LetterManager: No currentLetter assigned!");
            return;
        }

        ResetMinigame();
    }

    public override void StopMinigame()
    {
        // Cleanup if minigame stops
    }

    public void ResetMinigame()
    {
        if (resultsPanel != null) resultsPanel.SetActive(false);
        if (sealButton != null) sealButton.gameObject.SetActive(false);

        moodScores[LetterMoodType.Affectionate] = 0;
        moodScores[LetterMoodType.Melancholic] = 0;
        moodScores[LetterMoodType.Somber] = 0;
        moodScores[LetterMoodType.Passionate] = 0;

        int blankCount = currentLetter != null ? currentLetter.BlankCount : 0;
        filledWords = new string[blankCount];
        currentBlankIndex = 0;

        UpdateMoodUI();
        UpdateLetterDisplay();
        LoadCurrentWordBank();
    }

    private void LoadCurrentWordBank()
    {
        // Clear previous buttons
        if (wordButtonsContainer != null)
        {
            for (int i = wordButtonsContainer.childCount - 1; i >= 0; i--)
            {
                GameObject child = wordButtonsContainer.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        if (currentLetter == null) return;

        int totalBlanks = currentLetter.BlankCount;

        if (currentBlankIndex >= totalBlanks)
        {
            // All blanks filled!
            if (blankProgressText != null)
            {
                blankProgressText.text = "All blanks filled! Ready to seal.";
            }

            if (sealButton != null)
            {
                sealButton.gameObject.SetActive(true);
                StartCoroutine(AnimateSealButtonEntrance());
            }
            return;
        }

        if (blankProgressText != null)
        {
            blankProgressText.text = $"Word Bank — Blank {currentBlankIndex + 1} of {totalBlanks}";
        }

        BlankData blank = currentLetter.GetBlank(currentBlankIndex);
        if (blank == null || blank.choices == null || blank.choices.Count == 0)
        {
            // Fallback for simple letters
            CreateFallbackChoices();
            return;
        }

        foreach (var choice in blank.choices)
        {
            CreateWordButton(choice);
        }
    }

    private void CreateFallbackChoices()
    {
        var choices = new List<WordChoice>
        {
            new WordChoice("smiled at me", LetterMoodType.Affectionate),
            new WordChoice("looked away", LetterMoodType.Melancholic),
            new WordChoice("went pale", LetterMoodType.Somber),
            new WordChoice("laughed aloud", LetterMoodType.Passionate)
        };

        foreach (var choice in choices)
        {
            CreateWordButton(choice);
        }
    }

    private void CreateWordButton(WordChoice choice)
    {
        if (wordButtonPrefab == null || wordButtonsContainer == null) return;

        GameObject btnObj = Instantiate(wordButtonPrefab, wordButtonsContainer);
        LetterWordButton btnScript = btnObj.GetComponent<LetterWordButton>();

        Sprite moodSprite = GetMoodSprite(choice.mood);

        if (btnScript != null)
        {
            btnScript.Setup(choice, SelectWord, moodSprite, pushpinSprite);
        }
        else
        {
            // Fallback: configure directly
            Button btn = btnObj.GetComponent<Button>();
            TMP_Text tmp = btnObj.GetComponentInChildren<TMP_Text>();
            if (tmp != null) tmp.text = choice.word;
            if (btn != null)
            {
                btn.onClick.AddListener(() => SelectWord(choice));
            }
        }
    }

    private Sprite GetMoodSprite(LetterMoodType mood)
    {
        switch (mood)
        {
            case LetterMoodType.Affectionate: return heartSprite;
            case LetterMoodType.Melancholic: return leafSprite;
            case LetterMoodType.Somber: return moonSprite;
            case LetterMoodType.Passionate: return sunSprite;
            default: return null;
        }
    }

    public void SelectWord(WordChoice choice)
    {
        if (currentBlankIndex >= filledWords.Length) return;

        // Fill blank
        filledWords[currentBlankIndex] = choice.word;

        // Tally mood
        moodScores[choice.mood] += choice.points;

        // DDLC-style Chibi reaction
        TriggerMoodJump(choice.mood, choice.word);

        // Advance to next blank
        currentBlankIndex++;

        UpdateMoodUI();
        UpdateLetterDisplay();
        LoadCurrentWordBank();
    }

    private void TriggerMoodJump(LetterMoodType mood, string word)
    {
        RectTransform targetChibi = null;
        string moodName = "";
        Color moodCol = Color.white;

        switch (mood)
        {
            case LetterMoodType.Affectionate:
                targetChibi = heartChibi;
                moodName = "Affectionate";
                moodCol = new Color(0.95f, 0.45f, 0.65f);
                break;
            case LetterMoodType.Melancholic:
                targetChibi = leafChibi;
                moodName = "Melancholy";
                moodCol = new Color(0.40f, 0.75f, 0.45f);
                break;
            case LetterMoodType.Somber:
                targetChibi = moonChibi;
                moodName = "Somber";
                moodCol = new Color(0.45f, 0.60f, 0.90f);
                break;
            case LetterMoodType.Passionate:
                targetChibi = sunChibi;
                moodName = "Passionate";
                moodCol = new Color(0.95f, 0.70f, 0.20f);
                break;
        }

        if (targetChibi != null)
        {
            StartCoroutine(AnimateChibiJump(targetChibi));
        }

        if (floatingFeedbackText != null)
        {
            if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
            feedbackCoroutine = StartCoroutine(AnimateFloatingFeedback($"+1 {moodName} ({word})", moodCol));
        }
    }

    private IEnumerator AnimateChibiJump(RectTransform chibi)
    {
        Vector2 startPos = chibi.anchoredPosition;
        float elapsed = 0f;
        float duration = 0.35f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float height = Mathf.Sin(t * Mathf.PI) * 22f;
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.25f;

            chibi.anchoredPosition = startPos + new Vector2(0, height);
            chibi.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        chibi.anchoredPosition = startPos;
        chibi.localScale = Vector3.one;
    }

    private IEnumerator AnimateFloatingFeedback(string message, Color col)
    {
        floatingFeedbackText.text = message;
        floatingFeedbackText.color = col;
        floatingFeedbackText.gameObject.SetActive(true);

        RectTransform rt = floatingFeedbackText.rectTransform;
        Vector2 startPos = new Vector2(0, -10);
        rt.anchoredPosition = startPos;

        float elapsed = 0f;
        float duration = 1.1f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            rt.anchoredPosition = startPos + new Vector2(0, t * 25f);

            float alpha = 1f;
            if (t > 0.6f)
            {
                alpha = Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);
            }
            floatingFeedbackText.color = new Color(col.r, col.g, col.b, alpha);
            yield return null;
        }

        floatingFeedbackText.gameObject.SetActive(false);
    }

    private IEnumerator AnimateSealButtonEntrance()
    {
        if (sealButtonRect == null) yield break;

        sealButtonRect.localScale = Vector3.zero;
        float elapsed = 0f;
        float duration = 0.4f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            // Overshoot bounce
            float scale = Mathf.Sin(t * Mathf.PI * 0.7f) * 1.15f;
            if (t >= 0.8f) scale = Mathf.Lerp(scale, 1.0f, (t - 0.8f) / 0.2f);

            sealButtonRect.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        sealButtonRect.localScale = Vector3.one;
    }

    private void UpdateMoodUI()
    {
        if (heartCountText != null) heartCountText.text = moodScores[LetterMoodType.Affectionate].ToString();
        if (leafCountText != null) leafCountText.text = moodScores[LetterMoodType.Melancholic].ToString();
        if (moonCountText != null) moonCountText.text = moodScores[LetterMoodType.Somber].ToString();
        if (sunCountText != null) sunCountText.text = moodScores[LetterMoodType.Passionate].ToString();
    }

    private void UpdateLetterDisplay()
    {
        if (letterBodyText == null || currentLetter == null) return;

        string[] parts = currentLetter.textParts;
        if (parts == null || parts.Length == 0) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        int blankCount = currentLetter.BlankCount;

        for (int i = 0; i < parts.Length; i++)
        {
            sb.Append(parts[i]);

            if (i < blankCount)
            {
                // Blank i
                if (!string.IsNullOrEmpty(filledWords[i]))
                {
                    // Yellow highlighter marker tape effect
                    sb.Append($"<mark=#F5D66ECC><color=#24170D><b> {filledWords[i]} </b></color></mark>");
                }
                else if (i == currentBlankIndex)
                {
                    // Active blank - warm highlighted dashed line
                    sb.Append("<color=#C4831B><b><u>  ............  </u></b></color>");
                }
                else
                {
                    // Future blank - subtle dotted line
                    sb.Append("<color=#9E8D7A><u>............</u></color>");
                }
            }
        }

        letterBodyText.text = sb.ToString();
    }

    private void OnSealButtonClicked()
    {
        ShowResults();
    }

    private void ShowResults()
    {
        if (resultsPanel == null) return;

        resultsPanel.SetActive(true);

        // Determine dominant mood
        LetterMoodType dominant = LetterMoodType.Affectionate;
        int maxScore = -1;
        int totalPoints = 0;

        foreach (var kvp in moodScores)
        {
            totalPoints += kvp.Value;
            if (kvp.Value > maxScore)
            {
                maxScore = kvp.Value;
                dominant = kvp.Key;
            }
        }

        string title = "";
        string desc = "";

        switch (dominant)
        {
            case LetterMoodType.Affectionate:
                title = "Dominant Tone: Tender Affection";
                desc = "Your letter overflows with warmth, intimacy, and heartfelt care. The recipient will clutch it to their chest, touched by your gentle devotion.";
                break;
            case LetterMoodType.Melancholic:
                title = "Dominant Tone: Wistful Melancholy";
                desc = "Your words carry the fragrance of quiet longing and sweet nostalgia. Reading it feels like watching autumn leaves fall softly on empty stone.";
                break;
            case LetterMoodType.Somber:
                title = "Dominant Tone: Somber Mystery";
                desc = "Your letter holds a solemn, haunting depth. It whispers of unspoken truths, lingering shadows, and quiet endurance through cold nights.";
                break;
            case LetterMoodType.Passionate:
                title = "Dominant Tone: Fiery Passion";
                desc = "Your letter blazes with intense emotion, vivid memories, and unwavering fervor. The recipient's heart will race with every vibrant sentence.";
                break;
        }

        if (resultMoodTitle != null) resultMoodTitle.text = title;
        if (resultMoodDesc != null) resultMoodDesc.text = desc;

        if (resultStatsText != null)
        {
            int aff = moodScores[LetterMoodType.Affectionate];
            int mel = moodScores[LetterMoodType.Melancholic];
            int som = moodScores[LetterMoodType.Somber];
            int pas = moodScores[LetterMoodType.Passionate];

            float t = Mathf.Max(1, totalPoints);
            resultStatsText.text = $"Affectionate: {aff} ({(aff * 100f / t):F0}%)\n" +
                                   $"Melancholic: {mel} ({(mel * 100f / t):F0}%)\n" +
                                   $"Somber: {som} ({(som * 100f / t):F0}%)\n" +
                                   $"Passionate: {pas} ({(pas * 100f / t):F0}%)";
        }
    }
}


