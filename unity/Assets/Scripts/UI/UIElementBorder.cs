using System;
using UnityEngine;
using Assets.Scripts.Content;

namespace Assets.Scripts.UI
{
    public class UIElementBorder
    {
        protected GameObject[] bLine;
        protected UnityEngine.UI.Image frame;

        private const float paperFrameThickness = 0.5f;
        private static Sprite frameSprite;

        public UIElementBorder(UIElement element) : this(element, Color.white)
        {
        }

        public UIElementBorder(UIElement element, Color color, float thickness = 0.05f)
        {
            if (element.CanUsePaperBackground())
            {
                CreatePaperFrame(element, color);
                return;
            }
            CreateBorder(element.GetTransform(), element.GetRectTransform(), element.GetTag(), color, thickness);
        }

        private void CreatePaperFrame(UIElement element, Color color)
        {
            element.SetPaperBackground();
            (element as UIElementScrollVertical)?.SetScrollBarColor(UIElement.paperTextColor);

            GameObject frameObject = new GameObject("BorderFrame");
            frameObject.tag = element.GetTag();
            frame = frameObject.AddComponent<UnityEngine.UI.Image>();
            if (frameSprite == null) frameSprite = Resources.Load<Sprite>("Sprites/UI/Frame_outline_v2");
            frame.sprite = frameSprite;
            frame.type = UnityEngine.UI.Image.Type.Sliced;
            frame.raycastTarget = false;
            frame.color = color;
            frameObject.transform.SetParent(element.GetTransform(), false);
            frameObject.transform.SetAsFirstSibling();

            RectTransform frameTransform = frameObject.GetComponent<RectTransform>();
            frameTransform.anchorMin = Vector2.zero;
            frameTransform.anchorMax = Vector2.one;
            frameTransform.offsetMin = Vector2.zero;
            frameTransform.offsetMax = Vector2.zero;

            Rect rect = element.GetRectTransform().rect;
            float thickness = Mathf.Max(1, Mathf.Min(paperFrameThickness * UIScaler.GetPixelsPerUnit(), 0.35f * Mathf.Min(rect.width, rect.height)));
            frame.pixelsPerUnitMultiplier = frame.sprite.border.x * Game.Get().uICanvas.referencePixelsPerUnit / (frame.sprite.pixelsPerUnit * thickness);
        }

        public UIElementBorder(Transform transform, RectTransform rectTrans, string tag, Color color)
        {
            CreateBorder(transform, rectTrans, tag, color);
        }

        private void CreateBorder(Transform transform, RectTransform rectTrans, string tag, Color color, float thickness = 0.05f)
        {
            bLine = new GameObject[4];

            // create 4 lines
            for (int i = 0; i < 4; i++)
            {
                bLine[i] = new GameObject("Border" + i);
                bLine[i].tag = tag;
                bLine[i].AddComponent<UnityEngine.UI.Image>().color = color;
                bLine[i].transform.SetParent(transform);
            }

            // Set the thickness of the lines
            float thick = thickness * UIScaler.GetPixelsPerUnit();

            // Bottom line
            bLine[0].GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 0, thick);
            bLine[0].GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, -thick, rectTrans.rect.width);
            // Top line
            bLine[1].GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, -thick, thick);
            bLine[1].GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, -thick, rectTrans.rect.width);
            
            // Left line
            bLine[2].GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, -thick, rectTrans.rect.height + thick);
            bLine[2].GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, -thick, thick);
            // Right line
            bLine[3].GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, -thick, rectTrans.rect.height + thick);
            bLine[3].GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 0, thick);

        }

        /// <summary>
        /// Set border color.</summary>
        /// <param name="color">Color to use.</param>
        public void SetColor(Color color)
        {
            if (frame != null)
            {
                frame.color = color;
                return;
            }
            foreach (GameObject line in bLine)
            {
                if (line != null)
                {
                    line.GetComponent<UnityEngine.UI.Image>().color = color;
                }
            }
        }
    }
}
