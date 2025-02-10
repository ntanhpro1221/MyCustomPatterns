using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.UI;
#endif

[AddComponentMenu("Layout/Aspect Ratio Fitter Layout Element", 142)]
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
/// <summary>
/// Resizes a RectTransform to fit a specified aspect ratio.
/// </summary>
public class AspectRatioFitterLayoutElement : UIBehaviour, ILayoutSelfController {
    [SerializeField] protected int m_HorizontalPadding;
    public int HorizontalPadding {
        get => m_HorizontalPadding;
        set { if (SetStruct(ref m_HorizontalPadding, value)) SetDirty(); }
    }
    [SerializeField] protected int m_VerticalPadding;
    public int VerticalPadding {
        get => m_VerticalPadding;
        set { if (SetStruct(ref m_VerticalPadding, value)) SetDirty(); }
    }
    /// <summary>
    /// Specifies a mode to use to enforce an aspect ratio.
    /// </summary>
    public enum AspectMode {
        /// <summary>
        /// The aspect ratio != enforced
        /// </summary>
        None,
        /// <summary>
        /// Changes the height of the rectangle to match the aspect ratio.
        /// </summary>
        WidthControlsHeight,
        /// <summary>
        /// Changes the width of the rectangle to match the aspect ratio.
        /// </summary>
        HeightControlsWidth,
        /// <summary>
        /// Sizes the rectangle such that it's fully contained within the parent rectangle.
        /// </summary>
        FitInParent,
        /// <summary>
        /// Sizes the rectangle such that the parent rectangle is fully contained within.
        /// </summary>
        EnvelopeParent
    }
    private bool SetStruct<T>(ref T currentValue, T newValue) where T : struct {
        if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
            return false;

        currentValue = newValue;
        return true;
    }

    [SerializeField] private AspectMode m_AspectMode = AspectMode.None;

    /// <summary>
    /// The mode to use to enforce the aspect ratio.
    /// </summary>
    public AspectMode aspectMode { get { return m_AspectMode; } set { if (SetStruct(ref m_AspectMode, value)) SetDirty(); } }

    [SerializeField] private float m_AspectRatio = 1;

    /// <summary>
    /// The aspect ratio to enforce. This means width divided by height.
    /// </summary>
    public float aspectRatio { get { return m_AspectRatio; } set { if (SetStruct(ref m_AspectRatio, value)) SetDirty(); } }

    [System.NonSerialized]
    private RectTransform m_Rect;

    // This "delayed" mechanism is required for case 1014834.
    private bool m_DelayedSetDirty = false;

    //Does the gameobject has a parent for reference to enable FitToParent/EnvelopeParent modes.
    private bool m_DoesParentExist = false;

    private RectTransform rectTransform {
        get {
            if (m_Rect == null)
                m_Rect = GetComponent<RectTransform>();
            return m_Rect;
        }
    }

    // field is never assigned warning
#pragma warning disable 649
    private DrivenRectTransformTracker m_Tracker;
#pragma warning restore 649

    protected AspectRatioFitterLayoutElement() { }

    protected override void OnEnable() {
        base.OnEnable();
        m_DoesParentExist = rectTransform.parent ? true : false;
        if (m_DoesParentExist) {
            if (transform.parent.GetComponent<VerticalLayoutGroupWithRatio>() != null) {
                aspectMode = AspectMode.WidthControlsHeight;
            } else if (transform.parent.GetComponent<HorizontalLayoutGroupWithRatio>() != null) {
                aspectMode = AspectMode.HeightControlsWidth;
            }
        }
        SetDirty();
    }

    protected override void Start() {
        base.Start();
        //Disable the component if the aspect mode != valid or the object state/setup != supported with AspectRatio setup.
        if (!IsComponentValidOnObject() || !IsAspectModeValid())
            this.enabled = false;
    }

    protected override void OnDisable() {
        m_Tracker.Clear();
        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        base.OnDisable();
    }

    protected override void OnTransformParentChanged() {
        base.OnTransformParentChanged();

        m_DoesParentExist = rectTransform.parent ? true : false;
        SetDirty();
    }

    /// <summary>
    /// Update the rect based on the delayed dirty.
    /// Got around issue of calling onValidate from OnEnable function.
    /// </summary>
    protected virtual void Update() {
        if (m_DelayedSetDirty) {
            m_DelayedSetDirty = false;
            SetDirty();
        }
    }

    /// <summary>
    /// Function called when this RectTransform or parent RectTransform has changed dimensions.
    /// </summary>
    protected override void OnRectTransformDimensionsChange() {
        UpdateRect();
    }
    private void UpdateRect() {
        if (!IsActive() || !IsComponentValidOnObject())
            return;

        m_Tracker.Clear();

        var groupTrans = (RectTransform)transform.parent;
        var group = groupTrans.GetComponent<HorizontalOrVerticalLayoutGroupWithRatio>();
        float verMain = groupTrans.rect.height - VerticalPadding - group.padding.vertical;
        float horMain = groupTrans.rect.width - HorizontalPadding - group.padding.horizontal;
        switch (m_AspectMode) {
#if UNITY_EDITOR
            case AspectMode.None:
                if (!Application.isPlaying)
                    m_AspectRatio = Mathf.Clamp(rectTransform.rect.width / rectTransform.rect.height, 0.001f, 1000f);

                break;
#endif
            case AspectMode.HeightControlsWidth:
                m_Tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaX);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, verMain * m_AspectRatio);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, verMain);
                break;
            case AspectMode.WidthControlsHeight:
                m_Tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaY);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, horMain / m_AspectRatio);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, horMain);
                break;
            case AspectMode.FitInParent:
            case AspectMode.EnvelopeParent:
                if (!DoesParentExists())
                    break;

                m_Tracker.Add(this, rectTransform,
                    DrivenTransformProperties.Anchors |
                    DrivenTransformProperties.AnchoredPosition |
                    DrivenTransformProperties.SizeDeltaX |
                    DrivenTransformProperties.SizeDeltaY);

                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.anchoredPosition = Vector2.zero;

                Vector2 sizeDelta = Vector2.zero;
                Vector2 parentSize = GetParentSize();
                if ((parentSize.y * aspectRatio < parentSize.x) ^ (m_AspectMode == AspectMode.FitInParent)) {
                    sizeDelta.y = GetSizeDeltaToProduceSize(parentSize.x / aspectRatio, 1);
                } else {
                    sizeDelta.x = GetSizeDeltaToProduceSize(parentSize.y * aspectRatio, 0);
                }
                rectTransform.sizeDelta = sizeDelta;

                break;
        }
    }

    private float GetSizeDeltaToProduceSize(float size, int axis) {
        return size - GetParentSize()[axis] * (rectTransform.anchorMax[axis] - rectTransform.anchorMin[axis]);
    }

    private Vector2 GetParentSize() {
        RectTransform parent = rectTransform.parent as RectTransform;
        return !parent ? Vector2.zero : parent.rect.size;
    }

    /// <summary>
    /// Method called by the layout system. Has no effect
    /// </summary>
    public virtual void SetLayoutHorizontal() { }

    /// <summary>
    /// Method called by the layout system. Has no effect
    /// </summary>
    public virtual void SetLayoutVertical() { }

    /// <summary>
    /// Mark the AspectRatioFitter as dirty.
    /// </summary>
    protected void SetDirty() {
        UpdateRect();
    }

    public bool IsComponentValidOnObject() {
        Canvas canvas = gameObject.GetComponent<Canvas>();
        if (canvas && canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace) {
            return false;
        }
        return true;
    }

    public bool IsAspectModeValid() {
        if (!DoesParentExists() && (aspectMode == AspectMode.EnvelopeParent || aspectMode == AspectMode.FitInParent))
            return false;

        return true;
    }

    private bool DoesParentExists() {
        return m_DoesParentExist;
    }

#if UNITY_EDITOR
    protected override void OnValidate() {
        m_AspectRatio = Mathf.Clamp(m_AspectRatio, 0.001f, 1000f);
        m_DelayedSetDirty = true;
    }

#endif
#if UNITY_EDITOR

    [CustomEditor(typeof(AspectRatioFitterLayoutElement), true)]
    [CanEditMultipleObjects]
    /// <summary>
    ///   Custom Editor for the AspectRatioFitter component.
    ///   Extend this class to write a custom editor for a component derived from AspectRatioFitter.
    /// </summary>
    public class AspectRatioFitterLayoutElementEditor : SelfControllerEditor {
        SerializedProperty m_VerticalPadding;
        SerializedProperty m_HorizontalPadding;
        SerializedProperty m_AspectMode;
        SerializedProperty m_AspectRatio;

        AnimBool m_ModeBool;
        private AspectRatioFitterLayoutElement aspectRatioFitter;

        protected virtual void OnEnable() {
            m_VerticalPadding = serializedObject.FindProperty("m_VerticalPadding");
            m_HorizontalPadding = serializedObject.FindProperty("m_HorizontalPadding");
            m_AspectMode = serializedObject.FindProperty("m_AspectMode");
            m_AspectRatio = serializedObject.FindProperty("m_AspectRatio");
            aspectRatioFitter = target as AspectRatioFitterLayoutElement;

            m_ModeBool = new AnimBool(m_AspectMode.intValue != 0);
            m_ModeBool.valueChanged.AddListener(Repaint);
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            GUI.enabled = false;
            EditorGUILayout.PropertyField(m_AspectMode);
            GUI.enabled = true;

            m_ModeBool.target = m_AspectMode.intValue != 0;

            if (EditorGUILayout.BeginFadeGroup(m_ModeBool.faded)) {
                EditorGUILayout.PropertyField(m_AspectRatio);
                if (aspectRatioFitter.aspectMode ==
                    AspectRatioFitterLayoutElement.AspectMode.WidthControlsHeight)
                    EditorGUILayout.PropertyField(m_HorizontalPadding);
                else if (aspectRatioFitter.aspectMode ==
                    AspectRatioFitterLayoutElement.AspectMode.HeightControlsWidth)
                    EditorGUILayout.PropertyField(m_VerticalPadding);
            }
            EditorGUILayout.EndFadeGroup();

            serializedObject.ApplyModifiedProperties();

            if (aspectRatioFitter) {
                if (!aspectRatioFitter.IsAspectModeValid())
                    ShowNoParentWarning();
                if (!aspectRatioFitter.IsComponentValidOnObject())
                    ShowCanvasRenderModeInvalidWarning();
            }

            base.OnInspectorGUI();
        }

        protected virtual void OnDisable() {
            aspectRatioFitter = null;
            m_ModeBool.valueChanged.RemoveListener(Repaint);
        }

        private static void ShowNoParentWarning() {
            var text = L10n.Tr("You cannot use this Aspect Mode because this Component's GameObject does not have a parent object.");
            EditorGUILayout.HelpBox(text, MessageType.Warning, true);
        }

        private static void ShowCanvasRenderModeInvalidWarning() {
            var text = L10n.Tr("You cannot use this Aspect Mode because this Component is attached to a Canvas with a fixed width and height.");
            EditorGUILayout.HelpBox(text, MessageType.Warning, true);
        }
    }
#endif
}
