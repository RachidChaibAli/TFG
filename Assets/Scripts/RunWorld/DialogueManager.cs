using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    public GameObject dialoguePanel;
    public TMP_Text dialogueText;
    public UnityEngine.UI.Button continueButton;

    private GameOrquestrator.DialogueLine[] currentLines;
    private int currentIndex = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (continueButton != null)
            continueButton.onClick.AddListener(NextLine);
    }

    // Ensure there is a DialogueManager instance. If none exists, create one at runtime.
    public static void EnsureInstance()
    {
        if (Instance != null)
            return;

        // Create a minimal GameObject with DialogueManager; Awake will set Instance
        var go = new GameObject("DialogueManager_Auto");
        // Create manager component first (Awake will run)
        var dm = go.AddComponent<DialogueManager>();

        // If UI references are not assigned, create a minimal Canvas + Panel + TMP Text + Button
        try
        {
            // Canvas
            var canvasGO = new GameObject("DialogueCanvas");
            canvasGO.transform.SetParent(go.transform);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Panel
            var panelGO = new GameObject("DialoguePanel");
            panelGO.transform.SetParent(canvasGO.transform);
            var img = panelGO.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.6f);
            var panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.05f);
            panelRect.anchorMax = new Vector2(0.9f, 0.25f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Dialogue Text (TMP)
            var textGO = new GameObject("DialogueText");
            textGO.transform.SetParent(panelGO.transform);
            var tmpText = textGO.AddComponent<TextMeshProUGUI>();
            tmpText.fontSize = 22;
            tmpText.alignment = TextAlignmentOptions.TopLeft;
            tmpText.textWrappingMode = TextWrappingModes.Normal;
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.03f, 0.2f);
            textRect.anchorMax = new Vector2(0.97f, 0.95f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Continue Button
            var buttonGO = new GameObject("ContinueButton");
            buttonGO.transform.SetParent(panelGO.transform);
            var btnImage = buttonGO.AddComponent<Image>();
            btnImage.color = new Color(1f, 1f, 1f, 0.9f);
            var button = buttonGO.AddComponent<Button>();
            var btnRect = buttonGO.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.8f, 0.03f);
            btnRect.anchorMax = new Vector2(0.96f, 0.18f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(buttonGO.transform);
            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = "Continuar";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 18;
            var lblRect = labelGO.GetComponent<RectTransform>();
            lblRect.anchorMin = Vector2.zero;
            lblRect.anchorMax = Vector2.one;
            lblRect.offsetMin = Vector2.zero;
            lblRect.offsetMax = Vector2.zero;

            // Assign references to the DialogueManager instance
            dm.dialoguePanel = panelGO;
            dm.dialogueText = tmpText;
            dm.continueButton = button;

            // Ensure panel hidden initially
            dm.dialoguePanel.SetActive(false);

            // Add listener now that we have the button (Awake already ran earlier)
            if (dm.continueButton != null)
            {
                dm.continueButton.onClick.AddListener(dm.NextLine);
            }

            // Ensure there's an EventSystem so the button is interactable
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"DialogueManager.EnsureInstance: failed to create automatic UI: {ex.Message}");
        }
    }

    public void ShowDialogue(GameOrquestrator.DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0)
            return;
        currentLines = lines;
        currentIndex = 0;
        // If UI references are not assigned, fallback to console logging
        if (dialoguePanel != null && dialogueText != null)
        {
            dialoguePanel.SetActive(true);
            RenderCurrentLine();
        }
        else
        {
            Debug.Log("DialogueManager: UI not assigned — falling back to console output for dialogue:");
            for (int i = 0; i < currentLines.Length; i++)
            {
                Debug.Log($"[Dialogue] {currentLines[i].type}: {currentLines[i].text}");
            }
            // Immediately close since no UI interaction is possible
            CloseDialogue();
        }
    }

    private void RenderCurrentLine()
    {
        if (currentLines == null || currentIndex >= currentLines.Length)
        {
            CloseDialogue();
            return;
        }

        var line = currentLines[currentIndex];

        // Resolve speaker display name: prefer object's GameObject name if available in orchestrator
        string speaker = line.id;
        try
        {
            if (!string.IsNullOrEmpty(line.id) && GameOrquestrator.Instance != null && GameOrquestrator.Instance.objectDictionary != null && GameOrquestrator.Instance.objectDictionary.ContainsKey(line.id))
            {
                var go = GameOrquestrator.Instance.objectDictionary[line.id];
                if (go != null && !string.IsNullOrEmpty(go.name))
                    speaker = go.name;
            }
        }
        catch { }

        // Prepare display text: "Speaker: text"
        string display = $"<b>{speaker}:</b> {line.text}";

        if (dialogueText != null)
        {
            // Apply simple styling: keep prefijo de nombre y color, pero NO cambiar alineamiento (queda mejor sin alineado)
            if (!string.IsNullOrEmpty(line.type) && line.type.ToLower().Contains("player"))
            {
                dialogueText.color = Color.cyan;
            }
            else
            {
                dialogueText.color = Color.white;
            }

            dialogueText.text = display;
        }
        else
        {
            Debug.Log($"[Dialogue] {speaker}: {line.text}");
        }
    }

    public void NextLine()
    {
        currentIndex++;
        if (currentLines != null && currentIndex < currentLines.Length)
        {
            RenderCurrentLine();
        }
        else
        {
            CloseDialogue();
        }
    }

    public void CloseDialogue()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        currentLines = null;
        currentIndex = 0;
    }

    // Show a short transient notification (e.g., "Nombre recogido!")
    public void ShowNotification(string text, float duration = 2.0f)
    {
        try
        {
            // Ensure we have a canvas to parent the notification
            Canvas canvas = null;
            if (dialoguePanel != null)
            {
                canvas = dialoguePanel.GetComponentInParent<Canvas>();
            }
            if (canvas == null)
                canvas = FindAnyObjectByType<Canvas>();

            if (canvas == null)
            {
                Debug.LogWarning("ShowNotification: no Canvas found to display notification");
                return;
            }

            // Create panel
            var panelGO = new GameObject("NotificationPanel");
            panelGO.transform.SetParent(canvas.transform, false);
            var img = panelGO.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0f, 0f, 0f, 0.7f);
            var panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.7f, 0.85f);
            panelRect.anchorMax = new Vector2(0.98f, 0.95f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Text
            var textGO = new GameObject("NotificationText");
            textGO.transform.SetParent(panelGO.transform, false);
            var tmpText = textGO.AddComponent<TextMeshProUGUI>();
            tmpText.fontSize = 20;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.text = text;
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Start coroutine to destroy after duration with fade
            StartCoroutine(NotificationCoroutine(panelGO, tmpText, duration));
        }
        catch (Exception ex)
        {
            Debug.LogWarning("ShowNotification failed: " + ex.Message);
        }
    }

    private System.Collections.IEnumerator NotificationCoroutine(GameObject panelGO, TextMeshProUGUI tmpText, float duration)
    {
        float elapsed = 0f;
        CanvasGroup cg = panelGO.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Fade out
        float fadeTime = 0.5f;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
            yield return null;
        }
        Destroy(panelGO);
    }

    // Called when a script requests to go to the next scene
    public void OnNextSceneRequested()
    {
        // Default behavior: just log for now. You can implement scene loading here.
        Debug.Log("DialogueManager: nextScene requested");
    }
}
