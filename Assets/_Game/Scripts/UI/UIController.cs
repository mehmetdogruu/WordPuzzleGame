using DG.Tweening;
using UnityEngine;
using Helpers;
namespace UISystem
{
    public class UIController<T> : Singleton<T> where T : MonoBehaviour
    {
        protected Canvas _canvas;
        protected CanvasGroup _group;

        [Header("UI Controller Defaults")]
        [SerializeField] protected GameObject globalBackground;
        //[SerializeField] protected GameObject panel;
        [SerializeField] protected GameObject[] delayedItems;
        [SerializeField] protected GameObject[] notMonetizedItems;
        [SerializeField] protected float monetizeDelayTime;

        private float panelAnimationTime = .25f;
        private float elementsAnimationTime = .115f;

        private const float ADDITIONCONSTANT = .1f;

        protected virtual void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _group = GetComponent<CanvasGroup>();
        }

        public virtual void Show()
        {
            //_canvas.enabled = true;
            _group.alpha = 1;

            //panel.transform.DOScale(Vector3.one, panelAnimationTime + ADDITIONCONSTANT).SetEase(Ease.OutBack).OnComplete(() =>
            //{
            //    if (delayedItems.Length > 0)
            //    {
            //        foreach (var item in delayedItems)
            //        {
            //            item.transform.DOScale(Vector3.one, elementsAnimationTime + ADDITIONCONSTANT).SetEase(Ease.OutBack);
            //        }
            //    }


            //    if (notMonetizedItems.Length > 0)
            //    {
            //        DOVirtual.DelayedCall(monetizeDelayTime, () =>
            //        {
            //            foreach (var item in notMonetizedItems)
            //            {
            //                item.transform.DOScale(Vector3.one, elementsAnimationTime + ADDITIONCONSTANT).SetEase(Ease.OutBack);
            //            }
            //        });
            //    }
            _group.interactable = true;
            _group.blocksRaycasts = true;


            // });
        }

        public virtual void Hide()
        {
            _group.interactable = false;
            //_canvas.enabled = false;
            _group.alpha = 0;
            _group.blocksRaycasts = false;

            //panel.transform.DOScale(Vector3.zero, panelAnimationTime).SetEase(Ease.InBack).OnComplete(() =>
            //{
            //    if (delayedItems.Length > 0)
            //    {
            //        foreach (var item in delayedItems)
            //        {
            //            item.transform.DOScale(Vector3.zero, elementsAnimationTime).SetEase(Ease.InBack);
            //        }
            //    }

            //    if (notMonetizedItems.Length > 0)
            //    {
            //        foreach (var item in notMonetizedItems)
            //        {
            //            item.transform.DOScale(Vector3.zero, elementsAnimationTime).SetEase(Ease.InBack);
            //        }
            //    }
            //    DOVirtual.DelayedCall(.1f, () => _canvas.enabled = false);
            //});
        }
        public virtual void ShowInstant()
        {
            _canvas.enabled = true;
            _group.interactable = true;
            _group.alpha = 1f;
        }

        public virtual void HideInstant()
        {
            //_canvas.enabled = false;
            _group.interactable = false;
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            // panel.transform.localScale = Vector3.zero;
            // foreach (var item in delayedItems)
            // {
            //     item.transform.localScale = Vector3.zero;
            // }
            //
            // foreach (var item in notMonetizedItems)
            // {
            //     item.transform.localScale = Vector3.zero;
            // }
        }
    }
}

