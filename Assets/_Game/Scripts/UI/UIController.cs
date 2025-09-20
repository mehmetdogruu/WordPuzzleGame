using DG.Tweening;
using UnityEngine;

namespace UISystem
{
    public class UIController<T> : Helpers.Singleton<T> where T : MonoBehaviour
    {
        //protected Canvas _canvas;          // sadece referans (tek canvas mimarisi: asla enable/disable etme)
        protected CanvasGroup _group;      // panel k�k�ndeki CanvasGroup

        [Header("UI Controller Defaults")]
        [SerializeField] protected GameObject globalBackground;
        [SerializeField] protected GameObject[] delayedItems;
        [SerializeField] protected GameObject[] notMonetizedItems;
        [SerializeField] protected float monetizeDelayTime = 0f;

        [Header("Anim")]
        [SerializeField] protected float showFadeTime = 0.20f;
        [SerializeField] protected float hideFadeTime = 0.15f;
        [SerializeField] protected Ease showEase = Ease.OutQuad;
        [SerializeField] protected Ease hideEase = Ease.InQuad;
        [SerializeField] protected bool scalePunch = false;   // istersen k���k bir scale anim

        protected virtual void Awake()
        {
            // Tek canvas mimarisi: ebeveynden canvas�� REFERANS olarak bul (dokunmayaca��z)
           // _canvas = GetComponentInParent<Canvas>(true);

            // Panel k�k�ne CanvasGroup koy (yoksa ekle)
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();

            // Ba�lang��ta gizli kals�n
            //HideInstant();
        }

        public virtual void Show()
        {

            // �nce haz�r state
            _group.DOKill();
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            // fade in
            _group
                .DOFade(1f, showFadeTime)
                .SetEase(showEase)
                .OnComplete(() =>
                {
                    // DOTween callback: null guard
                    if (_group != null)
                    {
                        _group.interactable = true;
                        _group.blocksRaycasts = true;
                        _group.alpha = 1f;


                    }
                });

            if (scalePunch)
            {
                transform.DOKill();
                var t = transform as RectTransform;
                if (t != null)
                {
                    t.localScale = Vector3.one * 0.98f;
                    t.DOScale(1f, showFadeTime).SetEase(Ease.OutBack);
                }
            }
        }

        public virtual void Hide()
        {
            // interactivity kapat, fade out
            _group.DOKill();
            _group.interactable = false;
            _group.blocksRaycasts = false;

            _group
                .DOFade(0f, hideFadeTime)
                .SetEase(hideEase)
                .OnComplete(() =>
                {
                    // DOTween callback: null guard
                    if (this != null && gameObject != null)
                        _group.alpha = 0;              
                        });
        }

        public virtual void ShowInstant()
        {
            _group.DOKill();
            _group.alpha = 1f;
            _group.interactable = true;
            _group.blocksRaycasts = true;
        }

        public virtual void HideInstant()
        {
            _group.DOKill();
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;
        }
    }
}
