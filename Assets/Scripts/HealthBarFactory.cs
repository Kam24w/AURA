using UnityEngine;
using UnityEngine.UI;

internal static class HealthBarFactory
{
    public static Slider Create(Transform parent, string name, Vector2 size, Color fillColor)
    {
        GameObject barObject = new GameObject(name, typeof(RectTransform), typeof(Slider));
        RectTransform barRect = (RectTransform)barObject.transform;
        barRect.SetParent(parent, false);
        barRect.sizeDelta = size;

        Image background = CreateImage(barObject.transform, "Background", new Color(0.025f, 0.025f, 0.025f, 0.9f));
        Stretch(background.rectTransform);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(barObject.transform, false);
        Stretch((RectTransform)fillArea.transform);

        Image fill = CreateImage(fillArea.transform, "Fill", fillColor);
        Stretch(fill.rectTransform);

        Slider slider = barObject.GetComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.targetGraphic = fill;
        slider.handleRect = null;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        return slider;
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
