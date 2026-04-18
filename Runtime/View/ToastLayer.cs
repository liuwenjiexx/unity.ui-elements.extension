using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Async;


namespace UnityEngine.UIElements.Extension
{

    [RequireComponent(typeof(UIDocument))]
    public class ToastLayer : MonoBehaviour
    {
        //[SerializeField]
        //private List<VisualTreeAsset> uxmlList = new();

        //[SerializeField]
        //private List<StyleSheet> ussList = new();

        private Queue<Toast> toastQueue = new Queue<Toast>();
        private Toast currentToast;
        internal const string UxmlName = "ToastLayer";
        private const string DefaultToastUxmlName = "Toast";
        public const int Layer = 110;

        private VisualElement contentContainer;

        //public List<VisualTreeAsset> UxmlList => uxmlList;

        //public List<StyleSheet> UssList => ussList;

        private UIDocument document;

        private void Awake()
        {
            name = nameof(ToastLayer);
            document = GetComponent<UIDocument>();
            document.sortingOrder = Layer;
            document.visualTreeAsset = UIElementsUtility.GetUxml(UxmlName);
            document.enabled = enabled;
            var root = document.rootVisualElement;
            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.right = 0;
            root.style.top = 0;
            root.style.bottom = 0;
            if (!transform.parent)
                GameObject.DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            document.enabled = true;
            contentContainer = document.rootVisualElement.Q("content-container");
        }

        private void OnDisable()
        {
            document.enabled = false;
            contentContainer = null;
        }

        public void ShowToast(Toast toast)
        {
            if (currentToast == null)
            {
                _ShowToast(toast);
            }
            else
            {
                toastQueue.Enqueue(toast);
            }
        }


        async void _ShowToast(Toast toast)
        {
            currentToast = toast;

            string style = toast.style;

            if (string.IsNullOrEmpty(style))
            {
                style = DefaultToastUxmlName;
            }

            var view = UIElementsUtility.InstantiateUxml(style);
            view.style.flexGrow = 1;
            view.pickingMode = PickingMode.Ignore;

            UIElementsUtility.AddStyleSheet(view, style);
            Label contentLabel = view.Q<Label>("content");
            contentLabel.text = toast.content;

            contentContainer.Add(view);

            float duration;
            if (toast.duration.HasValue)
            {
                duration = toast.duration.Value;
            }
            else if (toast.durationType == ToastDuration.Long)
            {
                duration = 4f;
            }
            else
            {
                duration = 2.5f;
            }

            FadeAnimation(view, duration, 0.2f, 0.2f);

            await new WaitForSeconds(duration);

            contentContainer.Remove(view);
            currentToast = null;

            if (!this)
            {
                toastQueue.Clear();
                return;
            }

            if (toastQueue.Count > 0)
            {
                toast = toastQueue.Dequeue();
                _ShowToast(toast);
            }
        }

        async void FadeAnimation(VisualElement view, float duration, float fadeIn, float fadeOut)
        {
            float time = 0;
            float opacity;
            float startTime = Time.unscaledTime;

            while (time < duration)
            {
                if (!this)
                    break;
                time += Time.unscaledDeltaTime;

                if (time < fadeIn)
                {
                    opacity = Mathf.InverseLerp(0, fadeIn, time);
                }
                else if (time > duration - fadeOut)
                {
                    opacity = 1f - Mathf.InverseLerp(0, fadeOut, time - (duration - fadeOut));
                }
                else
                {
                    opacity = 1f;
                }

                opacity = Mathf.Clamp01(opacity);
                view.style.opacity = opacity;
                await new WaitForEndOfFrame();
            }
        }

    }
}
