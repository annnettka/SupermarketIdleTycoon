using System;
using SupermarketTycoon.Progression;
using UnityEngine;

namespace SupermarketTycoon.Buildings
{
    [DisallowMultipleComponent]
    public sealed class BuildSpot : MonoBehaviour
    {
        [SerializeField] private string stableId;
        [SerializeField] private BuildingDefinition definition;
        [SerializeField] private Transform placementRoot;
        [SerializeField] private BuildSpotView view;

        private BuildingService service;
        private ProgressionService progression;

        public string StableId => stableId;
        public BuildingDefinition Definition => definition;
        public Transform PlacementRoot => placementRoot != null ? placementRoot : transform;
        public GameObject BuildingObject { get; private set; }
        public bool IsBuilt => BuildingObject != null;

        public void Configure(
            string id,
            BuildingDefinition buildingDefinition,
            Transform placement,
            BuildSpotView buildView)
        {
            stableId = id;
            definition = buildingDefinition;
            placementRoot = placement;
            view = buildView;
        }

        public void Initialize(BuildingService buildingService, ProgressionService progressionService)
        {
            service = buildingService ?? throw new ArgumentNullException(nameof(buildingService));
            progression = progressionService ?? throw new ArgumentNullException(nameof(progressionService));

            view.BuildButton.onClick.RemoveListener(OnBuildClicked);
            view.BuildButton.onClick.AddListener(OnBuildClicked);
            service.StateChanged += Refresh;
            Refresh();
        }

        public void MarkBuilt(GameObject building)
        {
            BuildingObject = building;
            Refresh();
        }

        private void OnDestroy()
        {
            if (service != null)
            {
                service.StateChanged -= Refresh;
            }

            if (view != null && view.BuildButton != null)
            {
                view.BuildButton.onClick.RemoveListener(OnBuildClicked);
            }
        }

        private void OnBuildClicked()
        {
            service.TryBuild(this);
        }

        private void Refresh()
        {
            if (view == null || definition == null || progression == null || service == null)
            {
                return;
            }

            if (IsBuilt)
            {
                view.ShowBuilt();
            }
            else if (progression.CurrentLevel < definition.RequiredLevel)
            {
                view.ShowLocked(definition.RequiredLevel);
            }
            else
            {
                view.ShowAvailable(definition, service.CanAfford(definition));
            }
        }
    }
}
