using Common.Utility;
using TMPro;
using UnityEngine;

namespace AutosortLockers
{
    class ColorPicker : Picker
    {
        private void Awake()
        {
            rectTransform = transform as RectTransform;
        }

        public void Initialize(Color initialColor)
        {
            base.Initialize();

            var sprite = ImageUtilsCommon.LoadSprite(Plugin.GetAssetPath("Circle.png"), new Vector2(0.5f, 0.5f));
            for (int i = 0; i < buttons.Count; ++i)
            {
                var button = buttons[i];
                var color = Plugin.colors[i];
                button.Initialize(i, color, color == initialColor, sprite);
            }

            onSelect = OnSelect;
        }

        public void OnSelect(int index)
        {
            foreach (var button in buttons)
            {
                button.toggled = false;
            }
            Close();
        }

        public override void Open()
        {
            base.Open();

            var index = 0;
            for (int i = 0; i < buttons.Count; ++i)
            {
                if (buttons[i].toggled)
                {
                    index = i;
                    break;
                }
            }

            int buttonPage = index / ButtonsPerPage;
            ShowPage(buttonPage);
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static ColorPicker Create(Transform parent, TextMeshProUGUI textPrefab)
        {
            var colorPicker = new GameObject("ColorPicker", typeof(RectTransform)).AddComponent<ColorPicker>();

            colorPicker.ButtonSize = 15;
            colorPicker.Spacing = 15;
            colorPicker.ButtonsPerPage = 72;
            colorPicker.ButtonsPerRow = 8;

            Picker.Create(parent, colorPicker, textPrefab, Plugin.colors.Count);

            return colorPicker;
        }
    }
}
