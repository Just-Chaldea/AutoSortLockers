﻿using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AutosortLockers
{
    class ColorSetting : MonoBehaviour
    {
        public RectTransform rectTransform;
        public Action onClick = delegate { };

        [SerializeField]
        private ColoredIconButton activeButton;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
        }

        private void Initialize(TextMeshProUGUI textPrefab, string label)
        {
            activeButton = ColoredIconButton.Create(transform, CustomizeScreen.ScreenContentColor, textPrefab, label, 100, 15);
            activeButton.text.richText = true;
        }

        internal void SetInitialValue(Color initialColor)
        {
            SetColor(initialColor);
            activeButton.onClick += OnClick;
        }

        internal void SetColor(Color initialColor)
        {
            activeButton.Initialize("Circle.png", initialColor);
        }

        private void OnClick()
        {
            onClick();
        }


        ///////////////////////////////////////////////////////////////////////////////////////////
        public static ColorSetting Create(Transform parent, TextMeshProUGUI textPrefab, string label)
        {
            var colorSetting = new GameObject("ColorSettings", typeof(RectTransform)).AddComponent<ColorSetting>();
            var rt = colorSetting.gameObject.transform as RectTransform;
            RectTransformExtensions.SetParams(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), parent);
            colorSetting.Initialize(textPrefab, label);

            return colorSetting;
        }
    }
}
