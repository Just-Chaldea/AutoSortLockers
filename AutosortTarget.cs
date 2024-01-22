using Common.Mod;
using Common.Utility;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Assets;
using Nautilus.Utility;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CraftData;
using Nautilus.Assets.Gadgets;

namespace AutosortLockers
{
    public class AutosortTarget : MonoBehaviour
    {
        public const int MaxTypes = 7;
        public const float MaxDistance = 3;

        private bool initialized;
        private Constructable constructable;
        private StorageContainer container;
        private AutosortTypePicker picker;
        private CustomizeScreen customizeScreen;
        private Coroutine plusCoroutine;
        private SaveDataEntry saveData;

        [SerializeField]
        private TextMeshProUGUI textPrefab;
        [SerializeField]
        private Image background;
        [SerializeField]
        private Image icon;
        [SerializeField]
        private ConfigureButton configureButton;
        [SerializeField]
        private Image configureButtonImage;
        [SerializeField]
        private ConfigureButton customizeButton;
        [SerializeField]
        private Image customizeButtonImage;
        [SerializeField]
        private TextMeshProUGUI text;
        [SerializeField]
        private TextMeshProUGUI label;
        [SerializeField]
        private TextMeshProUGUI plus;
        [SerializeField]
        private TextMeshProUGUI quantityText;
        [SerializeField]
        private List<AutosorterFilter> currentFilters = new List<AutosorterFilter>();

        private void Awake()
        {
            constructable = GetComponent<Constructable>();
            container = gameObject.GetComponent<StorageContainer>();

            Plugin.OnDataLoaded += OnDataLoaded;
        }

        private void OnDataLoaded(SaveData allSaveData)
        {
            Logger.Log("OnDataLoaded");
            saveData = GetSaveData();
            InitializeFromSaveData();
            InitializeFilters();
            UpdateText();
        }

        public void SetPicker(AutosortTypePicker picker)
        {
            this.picker = picker;
        }

        public List<AutosorterFilter> GetCurrentFilters()
        {
            return currentFilters;
        }

        public void AddFilter(AutosorterFilter filter)
        {
            if (currentFilters.Count >= AutosortTarget.MaxTypes)
            {
                return;
            }
            if (ContainsFilter(filter))
            {
                return;
            }
            if (AnAutosorterIsSorting())
            {
                return;
            }

            currentFilters.Add(filter);
            UpdateText();
        }

        private bool ContainsFilter(AutosorterFilter filter)
        {
            foreach (var f in currentFilters)
            {
                if (f.IsSame(filter))
                {
                    return true;
                }
            }

            return false;
        }

        public void RemoveFilter(AutosorterFilter filter)
        {
            if (AnAutosorterIsSorting())
            {
                return;
            }
            foreach (var f in currentFilters)
            {
                if (f.IsSame(filter))
                {
                    currentFilters.Remove(f);
                    break;
                }
            }
            UpdateText();
        }

        private void UpdateText()
        {
            if (text != null)
            {
                if (currentFilters == null || currentFilters.Count == 0)
                {
                    text.text = "[Any]";
                }
                else
                {
                    string filtersText = string.Join("\n", currentFilters.Select((f) => f.IsCategory() ? "[" + f.GetString() + "]" : f.GetString()).ToArray());
                    text.text = filtersText;
                }
            }
        }

        internal void AddItem(Pickupable item)
        {
            container.container.AddItem(item);

            if (plusCoroutine != null)
            {
                StopCoroutine(plusCoroutine);
            }
            plusCoroutine = StartCoroutine(ShowPlus());
        }

        internal bool CanAddItemByItemFilter(Pickupable item)
        {
            bool allowed = IsTypeAllowedByItemFilter(item.GetTechType());
            return allowed && container.container.HasRoomFor(item);
        }

        internal bool CanAddItemByCategoryFilter(Pickupable item)
        {
            bool allowed = IsTypeAllowedByCategoryFilter(item.GetTechType());
            return allowed && container.container.HasRoomFor(item);
        }

        internal bool CanAddItem(Pickupable item)
        {
            bool allowed = CanTakeAnyItem() || IsTypeAllowed(item.GetTechType());
            return allowed && container.container.HasRoomFor(item);
        }

        internal bool CanTakeAnyItem()
        {
            return currentFilters == null || currentFilters.Count == 0;
        }

        internal bool CanAddItems()
        {
            return constructable.constructed;
        }

        internal bool HasCategoryFilters()
        {
            foreach (var filter in currentFilters)
            {
                if (filter.IsCategory())
                {
                    return true;
                }
            }
            return false;
        }

        internal bool HasItemFilters()
        {
            foreach (var filter in currentFilters)
            {
                if (!filter.IsCategory())
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsTypeAllowedByCategoryFilter(TechType techType)
        {
            foreach (var filter in currentFilters)
            {
                if (filter.IsCategory() && filter.IsTechTypeAllowed(techType))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsTypeAllowedByItemFilter(TechType techType)
        {
            foreach (var filter in currentFilters)
            {
                if (!filter.IsCategory() && filter.IsTechTypeAllowed(techType))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsTypeAllowed(TechType techType)
        {
            foreach (var filter in currentFilters)
            {
                if (filter.IsTechTypeAllowed(techType))
                {
                    return true;
                }
            }

            return false;
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

            if (Player.main != null)
            {
                float distSq = (Player.main.transform.position - transform.position).sqrMagnitude;
                bool playerInRange = distSq <= (MaxDistance * MaxDistance);
                configureButton.enabled = playerInRange;
                customizeButton.enabled = playerInRange;

                if (picker != null && picker.isActiveAndEnabled && !playerInRange)
                {
                    picker.gameObject.SetActive(false);
                }
                if (customizeScreen != null && customizeScreen.isActiveAndEnabled && !playerInRange)
                {
                    customizeScreen.gameObject.SetActive(false);
                }
            }

            container.enabled = ShouldEnableContainer();

            if (SaveLoadManager.main != null && SaveLoadManager.main.isSaving && !Plugin.IsSaving())
            {
                Plugin.Save();
            }

            UpdateQuantityText();
        }

        private bool AnAutosorterIsSorting()
        {
            var root = GetComponentInParent<SubRoot>();
            if (root != null && root.isBase)
            {
                var autosorters = root.GetComponentsInChildren<AutosortLocker>();
                foreach (var autosorter in autosorters)
                {
                    if (autosorter.IsSorting)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool ShouldEnableContainer()
        {
            return (picker == null || !picker.isActiveAndEnabled)
                && (customizeScreen == null || !customizeScreen.isActiveAndEnabled)
                && (!configureButton.pointerOver || !configureButton.enabled)
                && (!customizeButton.pointerOver || !customizeButton.enabled);
        }

        internal void ShowConfigureMenu()
        {
            foreach (var otherPicker in GameObject.FindObjectsOfType<AutosortTarget>())
            {
                otherPicker.HideAllMenus();
            }
            picker.gameObject.SetActive(true);
        }

        internal void ShowCustomizeMenu()
        {
            foreach (var otherPicker in GameObject.FindObjectsOfType<AutosortTarget>())
            {
                otherPicker.HideAllMenus();
            }
            customizeScreen.gameObject.SetActive(true);
        }

        internal void HideConfigureMenu()
        {
            if (picker != null)
            {
                picker.gameObject.SetActive(false);
            }
        }

        internal void HideCustomizeMenu()
        {
            if (customizeScreen != null)
            {
                customizeScreen.gameObject.SetActive(false);
            }
        }

        internal void HideAllMenus()
        {
            if (initialized)
            {
                HideConfigureMenu();
                HideCustomizeMenu();
            }
        }

        private void Initialize()
        {
            background.gameObject.SetActive(true);
            icon.gameObject.SetActive(true);
            text.gameObject.SetActive(true);

            background.sprite = ImageUtilsCommon.LoadSprite(Plugin.GetAssetPath("LockerScreen.png"));
            icon.sprite = ImageUtilsCommon.LoadSprite(Plugin.GetAssetPath("Receptacle.png"));
            configureButtonImage.sprite = ImageUtilsCommon.LoadSprite(Plugin.GetAssetPath("Configure.png"));
            customizeButtonImage.sprite = ImageUtilsCommon.LoadSprite(Plugin.GetAssetPath("Edit.png"));

            configureButton.onClick = ShowConfigureMenu;
            customizeButton.onClick = ShowCustomizeMenu;

            saveData = GetSaveData();
            InitializeFromSaveData();

            InitializeFilters();

            UpdateText();

            CreatePicker();
            CreateCustomizeScreen(textPrefab);

            initialized = true;
        }

        private void InitializeFromSaveData()
        {
            Logger.Log("Object Initialize from Save Data");
            label.text = saveData.Label;
            label.color = saveData.LabelColor.ToColor();
            icon.color = saveData.IconColor.ToColor();
            configureButtonImage.color = saveData.ButtonsColor.ToColor();
            customizeButtonImage.color = saveData.ButtonsColor.ToColor();
            text.color = saveData.OtherTextColor.ToColor();
            quantityText.color = saveData.ButtonsColor.ToColor();
            SetLockerColor(saveData.LockerColor.ToColor());
        }

        private void SetLockerColor(Color color)
        {
            var meshRenderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var meshRenderer in meshRenderers)
            {
                meshRenderer.material.color = color;
            }
        }

        private SaveDataEntry GetSaveData()
        {
            var prefabIdentifier = GetComponent<PrefabIdentifier>();
            var id = prefabIdentifier?.Id ?? string.Empty;

            return Plugin.GetSaveData(id);
        }

        private void InitializeFilters()
        {
            if (saveData == null)
            {
                currentFilters = new List<AutosorterFilter>();
                return;
            }

            currentFilters = GetNewVersion(saveData.FilterData);
        }

        private List<AutosorterFilter> GetNewVersion(List<AutosorterFilter> filterData)
        {
            Dictionary<TechType, AutosorterFilter> validItems = new Dictionary<TechType, AutosorterFilter>();
            Dictionary<string, AutosorterFilter> validCategories = new Dictionary<string, AutosorterFilter>();
            var filterList = AutosorterList.GetFilters();
            foreach (var filter in filterList)
            {
                if (filter.IsCategory())
                {
                    validCategories[filter.Category] = filter;
                }
                else
                {
                    validItems[filter.Types[0]] = filter;
                }
            }

            var newData = new List<AutosorterFilter>();
            foreach (var filter in filterData)
            {
                if (validCategories.ContainsKey(filter.Category) || filter.Category == "")
                {
                    newData.Add(filter);
                    continue;
                }

                if (filter.Category == "0")
                {
                    filter.Category = "";
                    newData.Add(filter);
                    continue;
                }

                var newTypes = AutosorterList.GetOldFilter(filter.Category, out bool success, out string newCategory);
                if (success)
                {
                    newData.Add(new AutosorterFilter() { Category = newCategory, Types = newTypes });
                    continue;
                }

                newData.Add(filter);
            }
            return newData;
        }

        private void CreatePicker()
        {
            SetPicker(AutosortTypePicker.Create(transform, textPrefab));
            picker.transform.localPosition = background.canvas.transform.localPosition + new Vector3(0, 0, 0.04f);
            picker.Initialize(this);
            picker.gameObject.SetActive(false);
        }

        private void CreateCustomizeScreen(TextMeshProUGUI textPrefab)
        {
            customizeScreen = CustomizeScreen.Create(background.transform, textPrefab, saveData);
            customizeScreen.onModified += InitializeFromSaveData;
            customizeScreen.Initialize(saveData);
            customizeScreen.gameObject.SetActive(false);
        }

        public void Save(SaveData saveDataList)
        {
            var prefabIdentifier = GetComponent<PrefabIdentifier>();
            var id = prefabIdentifier.Id;

            if (saveData == null)
            {
                saveData = new SaveDataEntry() { Id = id };
            }

            saveData.Id = id;
            saveData.FilterData = currentFilters;
            saveData.Label = label.text;
            saveData.LabelColor = label.color;
            saveData.IconColor = icon.color;
            saveData.OtherTextColor = text.color;
            saveData.ButtonsColor = configureButtonImage.color;

            var meshRenderer = GetComponentInChildren<MeshRenderer>();
            saveData.LockerColor = meshRenderer.material.color;

            saveDataList.Entries.Add(saveData);
        }

        public IEnumerator ShowPlus()
        {
            plus.color = new Color(plus.color.r, plus.color.g, plus.color.b, 1);
            float t = 0;
            float rate = 0.5f;
            while (t < 1.0)
            {
                t += Time.deltaTime * rate;
                plus.color = new Color(plus.color.r, plus.color.g, plus.color.b, Mathf.Lerp(1, 0, t));
                yield return null;
            }
        }

        private void UpdateQuantityText()
        {
            var count = container.container.count;
            quantityText.text = count == 0 ? "empty" : count.ToString();
        }

        #region Prefab

        public static void AddBuildable()
        {
            AddAutosortTargetBuildable();
            AddAutosortStandingTargetBuildable();
        }

        private static void AddAutosortTargetBuildable()
        {
            var info = PrefabInfo
                .WithTechType(
                    "AutosortTarget",
                    "Autosort Receptacle",
                    "Wall locker linked to an Autosorter that receives sorted items.",
                    unlockAtStart: true)
                .WithIcon(ImageUtils.LoadSpriteFromFile(Plugin.GetAssetPath("AutosortTarget.png")));
            var module = new CustomPrefab(info);

            var template = new CloneTemplate(module.Info, TechType.SmallLocker)
            {
                ModifyPrefabAsync = ModifyAutosortTargetPrefabAsync
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
                        new(TechType.Magnetite, 1)
                    }
            });
            module.Register();
        }

        private static void AddAutosortStandingTargetBuildable()
        {
            var info = PrefabInfo
                .WithTechType(
                    "AutosortTargetStanding",
                    "Standing Autosort Receptacle",
                    "Large locker linked to an Autosorter that receives sorted items.",
                    unlockAtStart: true)
                .WithIcon(ImageUtils.LoadSpriteFromFile(Plugin.GetAssetPath("AutosortTargetStanding.png")));
            var module = new CustomPrefab(info);

            var template = new CloneTemplate(module.Info, TechType.Locker)
            {
                ModifyPrefabAsync = ModifyAutosortTargetStandingPrefabAsync
            };

            module.SetGameObject(template);
            module.SetPdaGroupCategory(TechGroup.InteriorModules, TechCategory.InteriorModule);
            module.SetRecipe(new Nautilus.Crafting.RecipeData
            {
                craftAmount = 1,
                Ingredients = Plugin.config.EasyBuild
                    ? new List<Ingredient>
                    {
                        new(TechType.Titanium, 2),
                        new(TechType.Quartz, 1)
                    }
                    : new List<Ingredient>
                    {
                        new(TechType.Titanium, 2),
                        new(TechType.Quartz, 1),
                        new(TechType.Magnetite, 1)
                    }
            });
            module.Register();
        }

        private static IEnumerator ModifyAutosortTargetPrefabAsync(GameObject prefab)
        {
            yield return ModifyPrefabAsync(prefab, false);
            StorageContainer container = prefab.GetComponent<StorageContainer>();
            container.width = Plugin.config.ReceptacleWidth;
            container.height = Plugin.config.ReceptacleHeight;
            container.container.Resize(Plugin.config.ReceptacleWidth, Plugin.config.ReceptacleHeight);
        }

        private static IEnumerator ModifyAutosortTargetStandingPrefabAsync(GameObject prefab)
        {
            yield return ModifyPrefabAsync(prefab, true);
            var container = prefab.GetComponent<StorageContainer>();
            container.width = Plugin.config.StandingReceptacleWidth;
            container.height = Plugin.config.StandingReceptacleHeight;
            container.container.Resize(Plugin.config.StandingReceptacleWidth, Plugin.config.StandingReceptacleHeight);
        }

        private static IEnumerator ModifyPrefabAsync(GameObject prefab, bool isLocker)
        {
            var meshRenderers = prefab.GetComponentsInChildren<MeshRenderer>();
            foreach (var meshRenderer in meshRenderers)
            {
                meshRenderer.material.color = new Color(0.3f, 0.3f, 0.3f);
            }

            var autosortTarget = prefab.AddComponent<AutosortTarget>();

            var smallLockerPrefabTask = GetPrefabForTechTypeAsync(TechType.SmallLocker);
            yield return smallLockerPrefabTask;
            var smallLockerPrefab = smallLockerPrefabTask.GetResult();
            var prefabText = autosortTarget.textPrefab = GameObject.Instantiate(smallLockerPrefab.GetComponentInChildren<TextMeshProUGUI>());

            // NOTE: removing the locker label completely with DestroyImmediate will cause NullRefException in TriggerCull,
            // so we workaround by making the label really small
            var label = prefab.FindChild("Label");
            if (label != null)
            {
                label.transform.localScale = new Vector3(0.00001f, 0.00001f);
            }

            var canvas = LockerPrefabShared.CreateCanvas(prefab.transform);
            if (isLocker)
            {
                canvas.transform.localPosition = new Vector3(0, 1.1f, 0.25f);
            }

            autosortTarget.background = LockerPrefabShared.CreateBackground(canvas.transform);
            autosortTarget.icon = LockerPrefabShared.CreateIcon(autosortTarget.background.transform, prefabText.color, 70);
            autosortTarget.text = LockerPrefabShared.CreateText(autosortTarget.background.transform, prefabText, prefabText.color, -20, 12, "Any");

            autosortTarget.label = LockerPrefabShared.CreateText(autosortTarget.background.transform, prefabText, prefabText.color, 100, 12, "Locker");

            autosortTarget.background.gameObject.SetActive(false);
            autosortTarget.icon.gameObject.SetActive(false);
            autosortTarget.text.gameObject.SetActive(false);

            autosortTarget.plus = LockerPrefabShared.CreateText(autosortTarget.background.transform, prefabText, prefabText.color, 0, 30, "+");
            autosortTarget.plus.color = new Color(prefabText.color.r, prefabText.color.g, prefabText.color.g, 0);
            autosortTarget.plus.rectTransform.anchoredPosition += new Vector2(30, 70);

            autosortTarget.quantityText = LockerPrefabShared.CreateText(autosortTarget.background.transform, prefabText, prefabText.color, 0, 10, "XX");
            autosortTarget.quantityText.rectTransform.anchoredPosition += new Vector2(-35, -104);

            autosortTarget.configureButton = ConfigureButton.Create(autosortTarget.background.transform, prefabText.color, 40);
            autosortTarget.configureButtonImage = autosortTarget.configureButton.GetComponent<Image>();
            autosortTarget.customizeButton = ConfigureButton.Create(autosortTarget.background.transform, prefabText.color, 20);
            autosortTarget.customizeButtonImage = autosortTarget.customizeButton.GetComponent<Image>();
        }

        #endregion Prefab
    }
}
