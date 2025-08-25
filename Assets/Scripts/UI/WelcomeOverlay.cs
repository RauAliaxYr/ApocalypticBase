using UnityEngine;
using UnityEngine.UI;



public class WelcomeOverlay : MonoBehaviour
    {
        [Header("UI Elements")]
        public CanvasGroup canvasGroup;
        public Text titleText;
        public Text subtitleText;
        public Button playButton;
        public Button settingsButton;
        public Button exitButton;
        
        [Header("Animation")]
        public float fadeInDuration = 1f;
        public float buttonDelay = 0.5f;
        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Content")]
        public string gameTitle = "APOCALYPTIC BASE";
        public string gameSubtitle = "Survive the apocalypse by building and upgrading your base";
        
        private bool isVisible = false;
        private bool isAnimating = false;
        
        private void Awake()
        {
            // Get components if not assigned
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
                
            if (titleText == null)
                titleText = transform.Find("TitleText")?.GetComponent<Text>();
                
            if (subtitleText == null)
                subtitleText = transform.Find("SubtitleText")?.GetComponent<Text>();
                
            if (playButton == null)
                playButton = transform.Find("PlayButton")?.GetComponent<Button>();
                
            if (settingsButton == null)
                settingsButton = transform.Find("SettingsButton")?.GetComponent<Button>();
                
            if (exitButton == null)
                exitButton = transform.Find("ExitButton")?.GetComponent<Button>();
        }
        
        private void Start()
        {
            // Set initial state
            SetVisible(false);
            
            // Set text content
            if (titleText != null)
                titleText.text = gameTitle;
                
            if (subtitleText != null)
                subtitleText.text = gameSubtitle;
            
            // Setup button listeners
            SetupButtonListeners();
            
            // Show welcome screen
            ShowWelcomeScreen();
        }
        
        private void SetupButtonListeners()
        {
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayButtonClicked);
                
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsButtonClicked);
                
            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitButtonClicked);
        }
        
        public void ShowWelcomeScreen()
        {
            if (isAnimating) return;
            
            StartCoroutine(ShowWelcomeScreenCoroutine());
        }
        
        private System.Collections.IEnumerator ShowWelcomeScreenCoroutine()
        {
            isAnimating = true;
            
            // Show canvas
            SetVisible(true);
            
            // Fade in
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / fadeInDuration;
                float alpha = fadeCurve.Evaluate(progress);
                
                canvasGroup.alpha = alpha;
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
            
            // Animate buttons in sequence
            yield return StartCoroutine(AnimateButtonIn(playButton, 0f));
            yield return StartCoroutine(AnimateButtonIn(settingsButton, buttonDelay));
            yield return StartCoroutine(AnimateButtonIn(exitButton, buttonDelay * 2));
            
            isAnimating = false;
        }
        
        private System.Collections.IEnumerator AnimateButtonIn(Button button, float delay)
        {
            if (button == null) yield break;
            
            yield return new WaitForSeconds(delay);
            
            // Animate button scale
            Vector3 originalScale = button.transform.localScale;
            button.transform.localScale = Vector3.zero;
            
            float duration = 0.3f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float scale = Mathf.Lerp(0f, 1f, fadeCurve.Evaluate(progress));
                
                button.transform.localScale = originalScale * scale;
                yield return null;
            }
            
            button.transform.localScale = originalScale;
        }
        
        public void HideWelcomeScreen()
        {
            if (isAnimating) return;
            
            StartCoroutine(HideWelcomeScreenCoroutine());
        }
        
        private System.Collections.IEnumerator HideWelcomeScreenCoroutine()
        {
            isAnimating = true;
            
            // Fade out
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / fadeInDuration;
                float alpha = 1f - fadeCurve.Evaluate(progress);
                
                canvasGroup.alpha = alpha;
                yield return null;
            }
            
            canvasGroup.alpha = 0f;
            
            // Hide canvas
            SetVisible(false);
            
            isAnimating = false;
        }
        
        private void SetVisible(bool visible)
        {
            isVisible = visible;
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }
        }
        
        // Button event handlers
        private void OnPlayButtonClicked()
        {
            Debug.Log("Play button clicked - starting game");
            
            // Hide welcome screen
            HideWelcomeScreen();
            
            // Start game
            GameManager.Instance.StartGame();
        }
        
        private void OnSettingsButtonClicked()
        {
            Debug.Log("Settings button clicked");
            // TODO: Show settings menu
        }
        
        private void OnExitButtonClicked()
        {
            Debug.Log("Exit button clicked");
            
            // Show confirmation dialog
            ShowExitConfirmation();
        }
        
        private void ShowExitConfirmation()
        {
            // Simple confirmation - could be replaced with proper dialog
            bool confirmed = UnityEngine.Application.isEditor ? 
                UnityEditor.EditorUtility.DisplayDialog("Exit Game", "Are you sure you want to exit?", "Yes", "No") :
                true; // On device, just exit
                
            if (confirmed)
            {
                GameManager.Instance.ExitGame();
            }
        }
        
        // Public methods for external control
        public void SetGameTitle(string title)
        {
            gameTitle = title;
            if (titleText != null)
                titleText.text = title;
        }
        
        public void SetGameSubtitle(string subtitle)
        {
            gameSubtitle = subtitle;
            if (subtitleText != null)
                subtitleText.text = subtitle;
        }
        
        public bool IsWelcomeScreenVisible()
        {
            return isVisible;
        }
        
        public void ForceShow()
        {
            StopAllCoroutines();
            SetVisible(true);
            canvasGroup.alpha = 1f;
            isAnimating = false;
        }
        
        public void ForceHide()
        {
            StopAllCoroutines();
            SetVisible(false);
            canvasGroup.alpha = 0f;
            isAnimating = false;
        }
    }

