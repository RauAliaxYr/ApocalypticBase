using UnityEngine;



public class BoardLayoutAnchor : MonoBehaviour
    {
        [Header("Board Sprite References")]
        public SpriteRenderer boardSprite;

        [Header("Anchors (child transforms on board sprite)")]
        public Transform topLeftAnchor;
        public Transform bottomRightAnchor;

        [Header("Targets")]
        public GridController gridController;

        [Header("Responsive Settings")]
        public bool applyOnStart = true;
        public bool applyOnResolutionChange = true;
        public float reapplyDebounce = 0.1f;

        private int lastScreenW;
        private int lastScreenH;
        private float nextAllowedTime;

        private void Start()
        {
            lastScreenW = Screen.width;
            lastScreenH = Screen.height;
            if (applyOnStart)
            {
                Apply();
            }
        }

        private void Update()
        {
            if (!applyOnResolutionChange)
            {
                return;
            }

            if (Screen.width != lastScreenW || Screen.height != lastScreenH)
            {
                lastScreenW = Screen.width;
                lastScreenH = Screen.height;
                DebouncedApply();
            }
        }

        public void DebouncedApply()
        {
            if (Time.unscaledTime < nextAllowedTime)
            {
                return;
            }
            nextAllowedTime = Time.unscaledTime + reapplyDebounce;
            Apply();
        }

        [ContextMenu("Apply Layout Now")]
        public void Apply()
        {
            if (gridController == null)
            {
                return;
            }

            // If anchors are not assigned, try to derive from sprite bounds
            if ((topLeftAnchor == null || bottomRightAnchor == null) && boardSprite != null)
            {
                Bounds b = boardSprite.bounds;
                Vector3 tl = new Vector3(b.min.x, b.max.y, boardSprite.transform.position.z);
                Vector3 br = new Vector3(b.max.x, b.min.y, boardSprite.transform.position.z);

                // Create temporary anchor objects if needed
                if (topLeftAnchor == null)
                {
                    GameObject goTL = new GameObject("TopLeftAnchor_Auto");
                    goTL.transform.SetParent(boardSprite.transform, false);
                    goTL.transform.position = tl;
                    topLeftAnchor = goTL.transform;
                }
                if (bottomRightAnchor == null)
                {
                    GameObject goBR = new GameObject("BottomRightAnchor_Auto");
                    goBR.transform.SetParent(boardSprite.transform, false);
                    goBR.transform.position = br;
                    bottomRightAnchor = goBR.transform;
                }
            }

            gridController.ApplyLayoutFromAnchors(topLeftAnchor, bottomRightAnchor);
        }
    }


