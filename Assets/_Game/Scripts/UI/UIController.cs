using DG.Tweening;
using UnityEngine;

namespace UISystem
{
    public class UIController<T> : Helpers.Singleton<T> where T : MonoBehaviour
    {
        protected CanvasGroup _group;    

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
        [SerializeField] protected bool scalePunch = false;   

        protected virtual void Awake()
        {

            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();


        }

        public virtual void Show()
        {

            _group.DOKill();
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            _group
                .DOFade(1f, showFadeTime)
                .SetEase(showEase)
                .OnComplete(() =>
                {
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
