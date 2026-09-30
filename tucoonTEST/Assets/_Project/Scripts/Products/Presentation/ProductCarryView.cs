using System;
using UnityEngine;

namespace SupermarketTycoon.Products
{
    [Serializable]
    public sealed class CarryProductVisual
    {
        [SerializeField] private ProductDefinition definition;
        [SerializeField] private GameObject visualRoot;

        public CarryProductVisual(ProductDefinition product, GameObject root)
        {
            definition = product;
            visualRoot = root;
        }

        public ProductDefinition Definition => definition;
        public GameObject VisualRoot => visualRoot;
    }

    [DisallowMultipleComponent]
    public sealed class ProductCarryView : MonoBehaviour
    {
        [SerializeField] private Transform carryAnchor;
        [SerializeField] private CarryProductVisual[] visuals;

        public ProductDefinition ActiveProduct { get; private set; }
        public Transform CarryAnchor => carryAnchor;

        public void Configure(Transform anchor, CarryProductVisual[] productVisuals)
        {
            carryAnchor = anchor;
            visuals = productVisuals;
            DisableVisualColliders();
            Clear();
        }

        public void Show(ProductDefinition product)
        {
            ActiveProduct = product;
            if (visuals == null)
            {
                return;
            }

            for (var i = 0; i < visuals.Length; i++)
            {
                var item = visuals[i];
                if (item?.VisualRoot != null)
                {
                    item.VisualRoot.SetActive(item.Definition == product);
                }
            }
        }

        public void Clear()
        {
            ActiveProduct = null;
            if (visuals == null)
            {
                return;
            }

            for (var i = 0; i < visuals.Length; i++)
            {
                if (visuals[i]?.VisualRoot != null)
                {
                    visuals[i].VisualRoot.SetActive(false);
                }
            }
        }

        private void Awake()
        {
            DisableVisualColliders();
            Clear();
        }

        private void DisableVisualColliders()
        {
            if (carryAnchor == null)
            {
                return;
            }

            var colliders = carryAnchor.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }
    }
}
