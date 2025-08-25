using UnityEngine;
using UnityEngine.UI;



public class GameHUD : MonoBehaviour
    {
        [Header("HUD Elements")]
        public CanvasGroup hudCanvasGroup;
        
        [Header("Top Panel")]
        public Button menuButton;
        public Button soundToggleButton;
        public Image soundIcon;
        public Sprite soundOnSprite;
        public Sprite soundOffSprite;
        
        [Header("Bottom Panel")]
        public Text healthText;
        public Text swapsText;
        public Text dayText;
        public Text goldText;
        
        [Header("Status Indicators")]
        public Image healthBar;
        public Image swapsBar;
        public Color healthColor = Color.green;
        public Color swapsColor = Color.blue;
        
        [Header("Animation")]
        public float updateInterval = 0.1f;
        public AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        private ProgressState progressState;
        private EconomyManager economyManager;
        private bool isVisible = true;
        private float lastUpdateTime;
        
        private void Awake()
        {
            // Get components if not assigned
            if (hudCanvasGroup == null)
                hudCanvasGroup = GetComponent<CanvasGroup>();
                
            if (menuButton == null)
                menuButton = transform.Find("TopPanel/MenuButton")?.GetComponent<Button>();
                
            if (soundToggleButton == null)
                soundToggleButton = transform.Find("TopPanel/SoundToggleButton")?.GetComponent<Button>();
                
            if (soundIcon == null)
                soundIcon = transform.Find("TopPanel/SoundToggleButton/Icon")?.GetComponent<Image>();
                
            if (healthText == null)
                healthText = transform.Find("BottomPanel/HealthPanel/HealthText")?.GetComponent<Text>();
                
            if (swapsText == null)
                swapsText = transform.Find("BottomPanel/SwapsPanel/SwapsText")?.GetComponent<Text>();
                
            if (dayText == null)
                dayText = transform.Find("BottomPanel/DayPanel/DayText")?.GetComponent<Text>();
                
            if (goldText == null)
                goldText = transform.Find("BottomPanel/GoldPanel/GoldText")?.GetComponent<Text>();
                
            if (healthBar == null)
                healthBar = transform.Find("BottomPanel/HealthPanel/HealthBar")?.GetComponent<Image>();
                
            if (swapsBar == null)
                swapsBar = transform.Find("BottomPanel/SwapsPanel/SwapsBar")?.GetComponent<Image>();
        }
        
        private void Start()
        {
            // Get references
            progressState = GameManager.Instance.ProgressState;
            economyManager = GameManager.Instance.EconomyManager;
            
            // Setup button listeners
            SetupButtonListeners();
            
            // Set initial state
            UpdateHUD();
            
            // Subscribe to events
            EventBus.Instance.Subscribe<EnemyDiedEvent>(OnEnemyDied);
            EventBus.Instance.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
        }
        
        private void Update()
        {
            // Update HUD at intervals
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateHUD();
                lastUpdateTime = Time.time;
            }
        }
        
        private void SetupButtonListeners()
        {
            if (menuButton != null)
                menuButton.onClick.AddListener(OnMenuButtonClicked);
                
            if (soundToggleButton != null)
                soundToggleButton.onClick.AddListener(OnSoundToggleClicked);
        }
        
        private void UpdateHUD()
        {
            if (progressState == null) return;
            
            // Update health
            if (healthText != null)
            {
                healthText.text = $"HP: {progressState.baseHealth}/{progressState.maxBaseHealth}";
            }
            
            if (healthBar != null)
            {
                float healthPercent = (float)progressState.baseHealth / progressState.maxBaseHealth;
                healthBar.fillAmount = healthPercent;
                healthBar.color = Color.Lerp(Color.red, healthColor, healthPercent);
            }
            
            // Update swaps
            if (swapsText != null)
            {
                swapsText.text = $"Swaps: {progressState.swapsLeft}/{progressState.maxSwapsPerDay}";
            }
            
            if (swapsBar != null)
            {
                float swapsPercent = (float)progressState.swapsLeft / progressState.maxSwapsPerDay;
                swapsBar.fillAmount = swapsPercent;
                swapsBar.color = Color.Lerp(Color.red, swapsColor, swapsPercent);
            }
            
            // Update day
            if (dayText != null)
            {
                dayText.text = $"Day {progressState.currentDay}";
            }
            
            // Update gold
            if (goldText != null)
            {
                goldText.text = $"Gold: {progressState.gold}";
            }
            
            // Update sound icon
            UpdateSoundIcon();
        }
        
        private void UpdateSoundIcon()
        {
            if (soundIcon == null) return;
            
            if (progressState.soundEnabled)
            {
                soundIcon.sprite = soundOnSprite;
            }
            else
            {
                soundIcon.sprite = soundOffSprite;
            }
        }
        
        // Button event handlers
        private void OnMenuButtonClicked()
        {
            Debug.Log("Menu button clicked");
            
            // Pause game and show menu
            GameManager.Instance.PauseGame();
            
            // Show menu UI
            ShowMenu();
        }
        
        private void OnSoundToggleClicked()
        {
            Debug.Log("Sound toggle clicked");
            
            // Toggle sound
            progressState.soundEnabled = !progressState.soundEnabled;
            
            // Update icon
            UpdateSoundIcon();
            
            // Apply sound settings
            ApplySoundSettings();
        }
        
        private void ShowMenu()
        {
            // This would show the game menu
            // For now, just log it
            Debug.Log("Showing game menu");
        }
        
        private void ApplySoundSettings()
        {
            // Apply sound settings to audio system
            AudioListener.volume = progressState.soundEnabled ? progressState.masterVolume : 0f;
            
            Debug.Log($"Sound {(progressState.soundEnabled ? "enabled" : "disabled")}");
        }
        
        // Event handlers
        private void OnEnemyDied(EnemyDiedEvent enemyEvent)
        {
            // Animate gold gain
            AnimateGoldGain(enemyEvent.GoldReward);
        }
        
        private void OnWaveCompleted(WaveCompletedEvent waveEvent)
        {
            // Animate day completion
            AnimateDayCompletion();
        }
        
        private void AnimateGoldGain(int amount)
        {
            // Animate gold text
            if (goldText != null)
            {
                StartCoroutine(AnimateTextPulse(goldText, Color.yellow));
            }
            
            // Show floating text
            ShowFloatingText($"+{amount}", Color.yellow);
        }
        
        private void AnimateDayCompletion()
        {
            // Animate day text
            if (dayText != null)
            {
                StartCoroutine(AnimateTextPulse(dayText, Color.green));
            }
            
            // Show floating text
            ShowFloatingText("Day Complete!", Color.green);
        }
        
        private System.Collections.IEnumerator AnimateTextPulse(Text text, Color pulseColor)
        {
            Color originalColor = text.color;
            Vector3 originalScale = text.transform.localScale;
            
            float duration = 0.5f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float curveValue = pulseCurve.Evaluate(progress);
                
                // Pulse color
                text.color = Color.Lerp(originalColor, pulseColor, curveValue);
                
                // Pulse scale
                float scale = 1f + curveValue * 0.2f;
                text.transform.localScale = originalScale * scale;
                
                yield return null;
            }
            
            // Reset to original
            text.color = originalColor;
            text.transform.localScale = originalScale;
        }
        
        private void ShowFloatingText(string message, Color color)
        {
            // This would show floating text above the HUD
            // For now, just log it
            Debug.Log($"Floating text: {message}");
        }
        
        // Public methods
        public void ShowHUD()
        {
            isVisible = true;
            if (hudCanvasGroup != null)
            {
                hudCanvasGroup.alpha = 1f;
                hudCanvasGroup.interactable = true;
                hudCanvasGroup.blocksRaycasts = true;
            }
        }
        
        public void HideHUD()
        {
            isVisible = false;
            if (hudCanvasGroup != null)
            {
                hudCanvasGroup.alpha = 0f;
                hudCanvasGroup.interactable = false;
                hudCanvasGroup.blocksRaycasts = false;
            }
        }
        
        public void SetHealth(int current, int max)
        {
            if (progressState != null)
            {
                progressState.baseHealth = current;
                progressState.maxBaseHealth = max;
            }
        }
        
        public void SetSwaps(int current, int max)
        {
            if (progressState != null)
            {
                progressState.swapsLeft = current;
                progressState.maxSwapsPerDay = max;
            }
        }
        
        public void SetDay(int day)
        {
            if (progressState != null)
            {
                progressState.currentDay = day;
            }
        }
        
        public void SetGold(int gold)
        {
            if (progressState != null)
            {
                progressState.gold = gold;
            }
        }
        
        public bool IsHUDVisible()
        {
            return isVisible;
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            EventBus.Instance.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
            EventBus.Instance.Unsubscribe<WaveCompletedEvent>(OnWaveCompleted);
        }
    }