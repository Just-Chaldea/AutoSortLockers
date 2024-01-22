using Common.Mod;
using Common.Utility;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Utility;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CraftData;

namespace AutosortLockers
{
    public class AutosortLocker : MonoBehaviour
    {
        private static readonly Color MainColor = new Color(1, 0.2f, 0.2f);
        private static readonly Color PulseColor = Color.white;

        private bool initialized;
        private Constructable constructable;
        private StorageContainer container;
        private List<AutosortTarget> singleItemTargets = new List<AutosortTarget>();
        private List<AutosortTarget> categoryTargets = new List<AutosortTarget>();
        private List<AutosortTarget> anyTargets = new List<AutosortTarget>();

        private int unsortableItems = 0;

        [SerializeField]
        private Image background;
        [SerializeField]
        private Image icon;
        [SerializeField]
        private TextMeshProUGUI text;
        [SerializeField]
        private TextMeshProUGUI sortingText;
        [SerializeField]
        private bool isSorting;
        [SerializeField]
        private bool sortedItem;

        public bool IsSorting => isSorting;

        private void Awake()
        {
            constructable = GetComponent<Constructable>();
            container = GetComponent<StorageContainer>();
            container.hoverText = "Open autosorter";
            container.storageLabel = "Autosorter";
        }

        private void Update()
        {
            if (!initialized && constructable._constructed && transform.parent != null)
            {
                Initialize();
            }

            if (!initialized || !constructable._constructed)
            {
                return;
            }

            UpdateText();
        }

        private void UpdateText()
        {
            string output = "";
            if (isSorting)
            {
                output = "Sorting...";
            }
            else if (unsortableItems > 0)
            {
                output = "Unsorted Items: " + unsortableItems;
            }
            else
            {
                output = "Ready to Sort";
            }

            sortingText.text = output;
        }

        private void Initialize()
        {
            background.gameObject.SetActive(true);
            icon.gameObject.SetActive(true);
            text.gameObject.SetActive(true);
            sortingText.gameObject.SetActive(true);

            background.sprite = ImageUtilsCommon.LoadSprite(Plugin.GetAssetPath("LockerScreen.png"));
            icon.sprite = ImageUtilsCommon.LoadSprite(Plugin.GetAssetPath("Sorter.png"));

            initialized = true;
        }

        private IEnumerator Start()
        {
            while (true)
            {
                yield return new WaitForSeconds(Mathf.Max(0, Plugin.config.SortInterval - (unsortableItems / 60.0f)));

                yield return Sort();
            }
        }

        private void AccumulateTargets()
        {
            singleItemTargets.Clear();
            categoryTargets.Clear();
            anyTargets.Clear();

            SubRoot subRoot = gameObject.GetComponentInParent<SubRoot>();
            if (subRoot == null)
            {
                return;
            }

            var allTargets = subRoot.GetComponentsInChildren<AutosortTarget>().ToList();
            foreach (var target in allTargets)
            {
                if (target.isActiveAndEnabled && target.CanAddItems())
                {
                    if (target.CanTakeAnyItem())
                    {
                        anyTargets.Add(target);
                    }
                    else
                    {
                        if (target.HasItemFilters())
                        {
                            singleItemTargets.Add(target);
                        }
                        if (target.HasCategoryFilters())
                        {
                            categoryTargets.Add(target);
                        }
                    }
                }
            }
        }

        private IEnumerator Sort()
        {
            sortedItem = false;
            unsortableItems = container.container.count;

            if (!initialized || container.IsEmpty())
            {
                isSorting = false;
                yield break;
            }

            AccumulateTargets();
            if (NoTargets())
            {
                isSorting = false;
                yield break;
            }

            isSorting = true;
            yield return SortFilteredTargets(false);
            if (sortedItem)
            {
                yield break;
            }

            yield return SortFilteredTargets(true);
            if (sortedItem)
            {
                yield break;
            }

            yield return SortAnyTargets();
            if (sortedItem)
            {
                yield break;
            }

            isSorting = false;
        }

        private bool NoTargets()
        {
            return singleItemTargets.Count <= 0 && categoryTargets.Count <= 0 && anyTargets.Count <= 0;
        }

        private IEnumerator SortFilteredTargets(bool byCategory)
        {
            int callsToCanAddItem = 0;
            const int CanAddItemCallThreshold = 10;

            foreach (AutosortTarget target in byCategory ? categoryTargets : singleItemTargets)
            {
                foreach (AutosorterFilter filter in target.GetCurrentFilters())
                {
                    if (filter.IsCategory() == byCategory)
                    {
                        foreach (var techType in filter.Types)
                        {
                            callsToCanAddItem++;
                            var items = container.container.GetItems(techType);
                            if (items != null && items.Count > 0 && target.CanAddItem(items[0].item))
                            {
                                unsortableItems -= items.Count;
                                SortItem(items[0].item, target);
                                sortedItem = true;
                                yield break;
                            }
                            else if (callsToCanAddItem > CanAddItemCallThreshold)
                            {
                                callsToCanAddItem = 0;
                                yield return null;
                            }
                        }
                    }
                }
            }
        }

        private IEnumerator SortAnyTargets()
        {
            int callsToCanAddItem = 0;
            const int CanAddItemCallThreshold = 10;
            foreach (var item in container.container.ToList())
            {
                foreach (AutosortTarget target in anyTargets)
                {
                    callsToCanAddItem++;
                    if (target.CanAddItem(item.item))
                    {
                        SortItem(item.item, target);
                        unsortableItems--;
                        sortedItem = true;
                        yield break;
                    }
                    else if (callsToCanAddItem > CanAddItemCallThreshold)
                    {
                        callsToCanAddItem = 0;
                        yield return null;
                    }
                }
            }
        }

        private void SortItem(Pickupable pickup, AutosortTarget target)
        {
            container.container.RemoveItem(pickup, true);
            target.AddItem(pickup);

            StartCoroutine(PulseIcon());
        }

        public IEnumerator PulseIcon()
        {
            float t = 0;
            float rate = 0.5f;
            while (t < 1.0)
            {
                t += Time.deltaTime * rate;
                icon.color = Color.Lerp(PulseColor, MainColor, t);
                yield return null;
            }
        }

        private AutosortTarget FindTarget(Pickupable item)
        {
            foreach (AutosortTarget target in singleItemTargets)
            {
                if (target.CanAddItemByItemFilter(item))
                {
                    return target;
                }
            }
            foreach (AutosortTarget target in categoryTargets)
            {
                if (target.CanAddItemByCategoryFilter(item))
                {
                    return target;
                }
            }
            foreach (AutosortTarget target in anyTargets)
            {
                if (target.CanAddItem(item))
                {
                    return target;
                }
            }
            return null;
        }

        #region Prefab

        public static void AddBuildable()
        {
            var module = new CustomPrefab(PrefabInfo.WithTechType("Autosorter",
                "Autosort Locker",
                "Small, wall-mounted smart-locker that automatically transfers items into linked Autosort Receptacles.", unlockAtStart: true)
                .WithIcon(ImageUtils.LoadSpriteFromFile(Plugin.GetAssetPath("AutosortLocker.png"))));

            var template = new CloneTemplate(module.Info, TechType.SmallLocker)
            {
                ModifyPrefab = ModifyPrefab
            };

            module.SetGameObject(template);
            module.SetPdaGroupCategory(TechGroup.InteriorModules, TechCategory.InteriorModule);
            module.SetRecipe(new Nautilus.Crafting.RecipeData
            {
                craftAmount = 1,
                Ingredients = Plugin.config.EasyBuild
                    ? new List<Ingredient>
                    {
                        new(TechType.Titanium, 2)
                    }
                    : new List<Ingredient>
                    {
                        new(TechType.Titanium, 2),
                        new(TechType.ComputerChip, 1),
                        new(TechType.AluminumOxide, 2)
                    }
            });
            module.Register();
        }

        private static void ModifyPrefab(GameObject prefab)
        {
            var container = prefab.GetComponent<StorageContainer>();
            container.width = Plugin.config.AutosorterWidth;
            container.height = Plugin.config.AutosorterHeight;
            container.container.Resize(Plugin.config.AutosorterWidth, Plugin.config.AutosorterHeight);

            var meshRenderers = prefab.GetComponentsInChildren<MeshRenderer>();
            foreach (var meshRenderer in meshRenderers)
            {
                meshRenderer.material.color = new Color(1, 0, 0);
            }

            var prefabText = prefab.GetComponentInChildren<TextMeshProUGUI>();

            // NOTE: removing the locker label completely with DestroyImmediate will cause NullRefException in TriggerCull,
            // so we workaround by making the label really small
            var label = prefab.FindChild("Label");
            label.transform.localScale = new Vector3(0.00001f, 0.00001f);

            var autoSorter = prefab.AddComponent<AutosortLocker>();

            var canvas = LockerPrefabShared.CreateCanvas(prefab.transform);
            autoSorter.background = LockerPrefabShared.CreateBackground(canvas.transform);
            autoSorter.icon = LockerPrefabShared.CreateIcon(autoSorter.background.transform, MainColor, 40);
            autoSorter.text = LockerPrefabShared.CreateText(autoSorter.background.transform, prefabText, MainColor, 0, 14, "Autosorter");

            autoSorter.sortingText = LockerPrefabShared.CreateText(autoSorter.background.transform, prefabText, MainColor, -120, 12, "Sorting...");
            autoSorter.sortingText.alignment = TextAlignmentOptions.Top;

            autoSorter.background.gameObject.SetActive(false);
            autoSorter.icon.gameObject.SetActive(false);
            autoSorter.text.gameObject.SetActive(false);
            autoSorter.sortingText.gameObject.SetActive(false);
        }

        #endregion Prefab
    }
}
