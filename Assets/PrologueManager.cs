
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class PrologueManager : MonoBehaviour
{
    [Header("Prologue Images")]
    public Sprite[] prologueImages;

    [Header("Image Layers")]
    public Image imageA;
    public Image imageB;

    [Header("Black Overlay")]
    public Image blackOverlay;

    [Header("Buttons")]
    public CanvasGroup buttonsGroup;
    public Button backButton;
    public Button continueButton;

    [Header("Speech Bubble Groups")]
    public CanvasGroup[] imageBubbleGroups;

    [Header("Fade Settings")]
    public float fadeDuration = 1f;
    public float buttonFadeDuration = 0.5f;
    public float speechBubbleFadeDuration = 0.5f;

    private int currentIndex = 0;
    private bool showingImageA = true;
    private bool isTransitioning = true;

    void Start()
    {
        if (prologueImages == null ||
            prologueImages.Length == 0)
        {
            Debug.LogError("No prologue images assigned!");
            enabled = false;
            return;
        }

        if (imageA == null || imageB == null ||
            blackOverlay == null || buttonsGroup == null ||
            backButton == null || continueButton == null)
        {
            Debug.LogError("Missing Inspector references!");
            enabled = false;
            return;
        }

        if (imageBubbleGroups == null ||
            imageBubbleGroups.Length != prologueImages.Length)
        {
            Debug.LogError(
                "Speech bubble groups must match the number of prologue images!"
            );
            enabled = false;
            return;
        }

        for (int i = 0; i < imageBubbleGroups.Length; i++)
        {
            if (imageBubbleGroups[i] == null)
            {
                Debug.LogError("A speech bubble group is missing!");
                enabled = false;
                return;
            }
        }

        // Set first image
        imageA.sprite = prologueImages[0];

        SetAlpha(imageA, 1f);
        SetAlpha(imageB, 0f);

        showingImageA = true;

        // Start with black screen
        SetAlpha(blackOverlay, 1f);

        // Hide buttons
        buttonsGroup.alpha = 0f;
        buttonsGroup.interactable = false;
        buttonsGroup.blocksRaycasts = false;

        // Hide all speech bubbles
        for (int i = 0; i < imageBubbleGroups.Length; i++)
        {
            imageBubbleGroups[i].alpha = 0f;
            imageBubbleGroups[i].interactable = false;
            imageBubbleGroups[i].blocksRaycasts = false;
        }

        // Connect buttons
        backButton.onClick.RemoveListener(GoBack);
        continueButton.onClick.RemoveListener(Continue);

        backButton.onClick.AddListener(GoBack);
        continueButton.onClick.AddListener(Continue);

        StartCoroutine(InitialFade());
    }

    // FIRST IMAGE FADE
    IEnumerator InitialFade()
    {
        yield return FadeImage(
            blackOverlay, 1f, 0f, fadeDuration
        );

        yield return FadeSpeechBubbles(0, 1f);

        yield return FadeButtons(0f, 1f);

        isTransitioning = false;

        UpdateButtons();
    }

    // CONTINUE BUTTON
    public void Continue()
    {
        if (isTransitioning)
            return;

        if (currentIndex == prologueImages.Length - 1)
        {
            StartCoroutine(LoadMainMenu());
        }
        else
        {
            int previousIndex = currentIndex;

            currentIndex++;

            StartCoroutine(
                ChangeImage(previousIndex, currentIndex)
            );
        }
    }

    // BACK BUTTON
    public void GoBack()
    {
        if (isTransitioning || currentIndex <= 0)
            return;

        int previousIndex = currentIndex;

        currentIndex--;

        StartCoroutine(
            ChangeImage(previousIndex, currentIndex)
        );
    }

    // CHANGE IMAGE
    IEnumerator ChangeImage(int previousIndex, int targetIndex)
    {
        isTransitioning = true;

        // Fade out buttons
        yield return FadeButtons(1f, 0f);

        // Fade out old speech bubbles
        yield return FadeSpeechBubbles(previousIndex, 0f);

        // FINAL IMAGE: FADE FROM BLACK
        if (targetIndex == prologueImages.Length - 1)
        {
            yield return FadeImage(
                blackOverlay, 0f, 1f, fadeDuration
            );

            SetNewImage(targetIndex);

            yield return FadeImage(
                blackOverlay, 1f, 0f, fadeDuration
            );

            yield return FadeSpeechBubbles(targetIndex, 1f);
        }
        else
        {
            // MIDDLE IMAGES: CROSSFADE
            Image oldImage = showingImageA ? imageA : imageB;
            Image newImage = showingImageA ? imageB : imageA;

            newImage.sprite = prologueImages[targetIndex];

            SetAlpha(newImage, 0f);

            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;

                float progress = Mathf.Clamp01(
                    timer / fadeDuration
                );

                SetAlpha(oldImage, 1f - progress);
                SetAlpha(newImage, progress);

                yield return null;
            }

            SetAlpha(oldImage, 0f);
            SetAlpha(newImage, 1f);

            showingImageA = !showingImageA;

            yield return FadeSpeechBubbles(targetIndex, 1f);
        }

        // Fade buttons back in
        yield return FadeButtons(0f, 1f);

        isTransitioning = false;

        UpdateButtons();
    }

    // SET FINAL IMAGE
    void SetNewImage(int index)
    {
        if (showingImageA)
        {
            imageA.sprite = prologueImages[index];

            SetAlpha(imageA, 1f);
            SetAlpha(imageB, 0f);
        }
        else
        {
            imageB.sprite = prologueImages[index];

            SetAlpha(imageB, 1f);
            SetAlpha(imageA, 0f);
        }
    }

    // FADE SPEECH BUBBLES
    IEnumerator FadeSpeechBubbles(int index, float targetAlpha)
    {
        if (index < 0 || index >= imageBubbleGroups.Length)
            yield break;

        CanvasGroup group = imageBubbleGroups[index];

        float startAlpha = group.alpha;
        float timer = 0f;

        // Control raycasts based on visibility
        group.blocksRaycasts = targetAlpha > 0f;
        group.interactable = targetAlpha > 0f;

        while (timer < speechBubbleFadeDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(
                timer / speechBubbleFadeDuration
            );

            group.alpha = Mathf.Lerp(
                startAlpha, targetAlpha, progress
            );

            yield return null;
        }

        group.alpha = targetAlpha;

        group.interactable = targetAlpha > 0f;
        group.blocksRaycasts = targetAlpha > 0f;
    }

    // LOAD MAIN MENU
    IEnumerator LoadMainMenu()
    {
        isTransitioning = true;

        yield return FadeButtons(1f, 0f);

        yield return FadeSpeechBubbles(currentIndex, 0f);

        yield return FadeImage(
            blackOverlay, 0f, 1f, fadeDuration
        );

        SceneManager.LoadScene("MainMenu");
    }

    // IMAGE AND BLACK FADE
    IEnumerator FadeImage(
        Image image,
        float startAlpha,
        float endAlpha,
        float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(
                timer / duration
            );

            SetAlpha(
                image,
                Mathf.Lerp(startAlpha, endAlpha, progress)
            );

            yield return null;
        }

        SetAlpha(image, endAlpha);
    }

    // BUTTON FADE
    IEnumerator FadeButtons(float startAlpha, float endAlpha)
    {
        if (endAlpha < startAlpha)
        {
            buttonsGroup.interactable = false;
            buttonsGroup.blocksRaycasts = false;
        }

        float timer = 0f;

        while (timer < buttonFadeDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(
                timer / buttonFadeDuration
            );

            buttonsGroup.alpha = Mathf.Lerp(
                startAlpha, endAlpha, progress
            );

            yield return null;
        }

        buttonsGroup.alpha = endAlpha;

        buttonsGroup.interactable = endAlpha > 0f;
        buttonsGroup.blocksRaycasts = endAlpha > 0f;
    }

    // SET IMAGE ALPHA
    void SetAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    // UPDATE BUTTONS
    void UpdateButtons()
    {
        backButton.interactable = currentIndex > 0;
        continueButton.interactable = true;
    }
}
