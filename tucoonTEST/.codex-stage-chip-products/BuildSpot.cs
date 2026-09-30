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
        [SerializeField] private string requiredExpansionId;

        private BuildingService service;
        private ProgressionService progression;
        private bool expansionUnlocked;

        public string StableId => stableId;
        public BuildingDefinition Definition => definition;
        public Transform PlacementRoot => placementRoot != null ? placementRoot : transform;
        public GameObject BuildingObject { get; private set; }
        public bool IsBuilt => BuildingObject != null;
        public int CurrentLevel { get; private set; }
        public string RequiredExpansionId => requiredExpansionId;
        public bool IsExpansionUnlocked => expansionUnlocked;

        public void Configure(
            string id,
            BuildingDefinition buildingDefinition,
            Transform placement,
            BuildSpotView buildView,
            string expansionId = null)
        {
            stableId = id;
            definition = buildingDefinition;
            placementRoot = placement;
            view = buildView;
            requiredExpansionId = expansionId;
        }

        public void Initialize(BuildingService buildingService, ProgressionService progressionService)
        {
            service = buildingService ?? throw new ArgumentNullException(nameof(buildingService));
            progression = progressionService ?? throw new ArgumentNullException(nameof(progressionService));
            expansionUnlocked = string.IsNullOrEmpty(requiredExpansionId);

            view.BuildButton.onClick.RemoveListener(OnBuildClicked);
            view.BuildButton.onClick.AddListener(OnBuildClicked);
            service.StateChanged += Refresh;
            Refresh();
        }

        public void MarkBuilt(GameObject building, int level = 1)
        {
            BuildingObject = building;
            CurrentLevel = Mathf.Max(1, level);
            Refresh();
        }

        public void SetLevel(int level)
        {
            CurrentLevel = Mathf.Clamp(level, 1, definition != null ? definition.MaxLevel : 1);
            Refresh();
        }

        public void SetExpansionUnlocked(bool unlocked)
        {
            expansionUnlocked = string.IsNullOrEmpty(requiredExpansionId) || unlocked;
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
            if (IsBuilt)
            {
                service.Select(this);
                return;
            }

            service.TryBuild(this);
        }

        private void Refresh()
        {
            if (view == null || definition == null || progression == null || service == null)
            {
                return;
            }

            if (!expansionUnlocked)
            {
                view.SetVisible(false);
            }
            else if (IsBuilt)
            {
                view.ShowBuilt(definition, CurrentLevel, BuildingObject);
            }
            else if (progression.CurrentLevel < definition.RequiredLevel)
            {
                view.ShowLocked(definition);
            }
            else
            {
                view.ShowAvailable(definition, service.CanAfford(definition));
            }
        }
    }
}
