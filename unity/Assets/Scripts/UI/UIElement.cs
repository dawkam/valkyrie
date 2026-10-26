using UnityEngine;
using Assets.Scripts.Content;
using UnityEditor;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    /// <summary>
    /// UIElement is the base class for drawing UI elements.</summary>
    /// <remarks>
    /// UIElements can display solid colors, images and text.  It can also act as a button.</remarks>
    public class UIElement
    {
        // The button itself, unity object
        protected GameObject text;
        // The background, unity object
        protected GameObject bg;

        protected string tag = Game.DIALOG;

        protected static float textPaddingDefault = 0.5f;

        public static readonly Color defaultBGColor = new Color(0, 0, 0, (float)0.9);
        public static readonly Color paperTextColor = new Color(0.14f, 0.08f, 0.03f);
        public static readonly Color paperLinkColor = new Color(0f, 0.3f, 0.35f);
        public static readonly Color paperGreenColor = new Color(0.12f, 0.38f, 0.1f);
        public static readonly Color paperRedColor = new Color(0.55f, 0.07f, 0.04f);
        public static readonly Color paperRowColor = new Color(0.14f, 0.08f, 0.03f, 0.12f);
        public static readonly Color paperSelectedRowColor = new Color(0.14f, 0.08f, 0.03f, 0.4f);
        protected bool paperText = false;
        protected bool paperBG = false;
        protected bool onPaper = false;
        protected bool keepDarkBackground = false;
        private static Sprite paperSprite;

        protected float textPadding = textPaddingDefault;

        // Used for calculating text size
        protected GameObject textWidthObj;
        protected GameObject textHeightObj;

        protected UnityEngine.Events.UnityAction buttonCall;
        protected UnityEngine.Events.UnityAction<string> buttonCallWithParams;

        /// <summary>
        /// Construct a UI element with options tag name and parent.</summary>
        /// <param name="t">Unity tag name for the element, cannot be changed after construction, defaults to "dialog".</param>
        /// <param name="parent">Parent transform, cannot be changed after construction, defaults to the UI Panel.</param>
        public UIElement(string t = "", Transform parent = null)
        {
            if (t.Length > 0) tag = t;
            CreateBG(parent);
        }

        /// <summary>
        /// Construct a UI element with parent.</summary>
        /// <param name="parent">Parent transform, cannot be changed after construction.</param>
        public UIElement(Transform parent)
        {
            CreateBG(parent);
        }

        /// <summary>
        /// Destroy a UI element.</summary>
        public void Destroy()
        {
            if(bg!=null)
                Object.Destroy(bg);
        }

        /// <summary>
        /// The action if a button is assigned</summary>
        /// <returns>
        /// The action .</returns>
        public UnityEngine.Events.UnityAction GetAction()
        {
            return buttonCall;
        }

        /// <summary>
        /// The tranform that should be used for sub elements.</summary>
        /// <returns>
        /// The background Transform.</returns>
        public Transform GetTransform()
        {
            return bg.transform;
        }

        /// <summary>
        /// The base RectTransform for the UI element.</summary>
        /// <returns>
        /// The background RectTransform.</returns>
        public RectTransform GetRectTransform()
        {
            return bg.GetComponent<RectTransform>();
        }

        /// <summary>
        /// The tag for this UI element.</summary>
        /// <returns>
        /// The Unity tag as a string.</returns>
        public string GetTag()
        {
            return tag;
        }

        /// <summary>
        /// Internal method called by constructor to set up base GameObject.</summary>
        protected virtual void CreateBG(Transform parent)
        {
            bg = new GameObject("UIBG");
            bg.tag = tag;
            UnityEngine.UI.Image uiImage = bg.AddComponent<UnityEngine.UI.Image>();
            // default color
            uiImage.color = defaultBGColor;
            if (parent == null) parent = Game.Get().uICanvas.transform;
            bg.transform.SetParent(parent);
            if (IsOnPaper(parent))
            {
                onPaper = true;
                paperText = true;
                uiImage.color = Color.clear;
            }
        }

        private static bool IsOnPaper(Transform parent)
        {
            if (paperSprite == null) return false;
            for (Transform t = parent; t != null; t = t.parent)
            {
                UnityEngine.UI.Image image = t.GetComponent<UnityEngine.UI.Image>();
                if (image != null && image.sprite == paperSprite) return true;
            }
            return false;
        }

        /// <summary>
        /// Set a background color.</summary>
        /// <param name="c">Color to use.</param>
        /// <remarks>
        /// Can also be used to change image render color, is reset to white when image is set.</remarks>
        public void SetBGColor(Color c)
        {
            if (paperBG && IsDarkBackground(c)) c = Color.white;
            bg.GetComponent<UnityEngine.UI.Image>().color = c;
        }

        public static bool IsDarkBackground(Color c)
        {
            return c.a >= 0.5f && Mathf.Max(c.r, c.g, c.b) < 0.2f;
        }

        /// <summary>
        /// True if the element still has a plain background that can be replaced by paper.</summary>
        public bool CanUsePaperBackground()
        {
            if (keepDarkBackground) return false;
            UnityEngine.UI.Image background = bg.GetComponent<UnityEngine.UI.Image>();
            if (background.sprite != null) return false;
            return IsDarkBackground(background.color) || (onPaper && background.color == Color.clear);
        }

        /// <summary>
        /// Keep the plain dark background and line border, for containers whose content has its own styling.</summary>
        public void KeepDarkBackground()
        {
            keepDarkBackground = true;
        }

        /// <summary>
        /// Use a paper image as background, dark background colors set later are ignored.</summary>
        public void SetPaperBackground()
        {
            paperBG = true;
            SetImage(GetPaperSprite());
            SetPaperText();
        }

        private static Sprite GetPaperSprite()
        {
            if (paperSprite != null) return paperSprite;
            Texture2D paper = Resources.Load<Texture2D>("Sprites/UI/base-paper-1");
            // Crop the dark outer edge of the source image
            Rect inner = new Rect(paper.width * 0.025f, paper.height * 0.025f, paper.width * 0.95f, paper.height * 0.95f);
            paperSprite = Sprite.Create(paper, inner, Vector2.zero, 1);
            return paperSprite;
        }

        /// <summary>
        /// Plain text label placed on top of a paper panel.</summary>
        public void SetPaperLabel()
        {
            SetBGColor(Color.clear);
            SetPaperText();
        }

        /// <summary>
        /// Switch white text to dark brown so it is readable on a paper background.</summary>
        public void SetPaperText()
        {
            paperText = true;
            if (text == null) return;
            UnityEngine.UI.Text uiText = text.GetComponent<UnityEngine.UI.Text>();
            uiText.color = PaperTextColor(uiText.color);
        }

        protected Color PaperTextColor(Color textColor)
        {
            if (!paperText) return textColor;
            if (textColor == Color.white) return paperTextColor;
            if (textColor == Color.cyan) return paperLinkColor;
            if (textColor == Color.green) return paperGreenColor;
            if (textColor == Color.red) return paperRedColor;
            return textColor;
        }

        /// <summary>
        /// Set a background image.</summary>
        /// <param name="texture">Texture2D to use.</param>
        /// <param name="preserveAspect">If the image should preserve the original aspect ratio.</param>
        /// <param name="aspectMode">Applies.</param>
        /// <remarks>
        /// Will also set the background color to white.</remarks>
        public void SetImage(Texture2D texture, 
            bool preserveAspect = false, AspectRatioFitter.AspectMode? aspectMode = null)
        {
            if (texture == null) return;
            SetBGColor(Color.white);
            var component = bg.GetComponent<UnityEngine.UI.Image>();
            component.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero, 1);
            component.preserveAspect = preserveAspect;
            
            var fitter = bg.GetComponent<AspectRatioFitter>();

            if (aspectMode == null)
            {
                if (!(fitter is null))
                {
                    Object.Destroy(fitter);
                }
                return;
            }

            if (fitter is null)
            {
                fitter = bg.AddComponent<AspectRatioFitter>();
            }

            fitter.aspectRatio = (float) texture.width / (float) texture.height;
            fitter.aspectMode = (AspectRatioFitter.AspectMode) aspectMode;
        }

        /// <summary>
        /// Set a background image.</summary>
        /// <param name="sprite">Sprite to use.</param>
        /// <remarks>
        /// Will also set the background color to white.</remarks>
        public void SetImage(Sprite sprite)
        {
            if (sprite == null) return;
            SetBGColor(Color.white);
            bg.GetComponent<UnityEngine.UI.Image>().sprite = sprite;
        }


        /// <summary>
        /// Set UI location in UIscaler units.</summary>
        /// <param name="x">Offset from the left of the screen.</param>
        /// <param name="y">Offset from the top of the screen.</param>
        /// <param name="width">Horizontal size.</param>
        /// <param name="height">Vertical size.</param>
        /// <remarks>
        /// Cannot be changed after ***FIXME***.</remarks>
        public void SetLocation(float x, float y, float width, float height)
        {
            SetLocationPixels(UIScaler.GetPixelsPerUnit() * x, UIScaler.GetPixelsPerUnit() * y, UIScaler.GetPixelsPerUnit() * width, UIScaler.GetPixelsPerUnit() * height);
        }

        /// <summary>
        /// Set UI location in pixels.</summary>
        /// <param name="x">Offset from the left of the screen.</param>
        /// <param name="y">Offset from the top of the screen.</param>
        /// <param name="width">Horizontal size.</param>
        /// <param name="height">Vertical size.</param>
        /// <remarks>
        /// Cannot be changed after ***FIXME***.</remarks>
        public virtual void SetLocationPixels(float x, float y, float width, float height)
        {
            RectTransform transBg = bg.GetComponent<RectTransform>();
            transBg.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, y, height);
            transBg.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, x, width);
        }

        /// <summary>
        /// Set the text Horizontal overflow</summary>
        /// <param name="mode">wrap or overflow</param>
        public virtual void SetTextHorizontalOverflow(HorizontalWrapMode mode)
        {
            if (text == null) return;
            text.GetComponent<UnityEngine.UI.Text>().horizontalOverflow = mode;
        }

        /// <summary>
        /// Add button press event to element.</summary>
        /// <param name="call">Function to call on button press.</param>
        /// <remarks>
        /// Adds button properties to background area and text.</remarks>
        public virtual void SetButton(UnityEngine.Events.UnityAction call)
        {
            UnityEngine.UI.Button uiButton = bg.GetComponent<UnityEngine.UI.Button>();
            if (uiButton == null)
            { 
                uiButton = bg.AddComponent<UnityEngine.UI.Button>();
            }
            else
            {
                // Multiple actions have been set on this button, using only the last one
                uiButton.onClick.RemoveAllListeners();
            }
            uiButton.interactable = true;
            uiButton.onClick.AddListener(call);
            if (text != null)
            {
                uiButton = text.GetComponent<UnityEngine.UI.Button>();
                if(uiButton==null)
                { 
                    uiButton = text.AddComponent<UnityEngine.UI.Button>();
                }
                else
                {
                    // Multiple actions have been set on this text button, using only the last one
                    uiButton.onClick.RemoveAllListeners();
                }
                uiButton.interactable = true;
                uiButton.onClick.AddListener(call);
            }
            buttonCall = call;
        }

        /// <summary>
        /// Add button press event to element.</summary>
        /// <param name="call">Function to call on button press with string parameters.</param>
        /// <remarks>
        /// Adds button properties to background area and text.</remarks>
        public virtual void SetButtonWithParams(UnityEngine.Events.UnityAction<string> call, string value)
        {
            UnityEngine.UI.Button uiButton = bg.AddComponent<UnityEngine.UI.Button>();
            uiButton.interactable = true;
            uiButton.onClick.AddListener(delegate { call(value); });
            if (text != null)
            {
                uiButton = text.AddComponent<UnityEngine.UI.Button>();
                uiButton.interactable = true;
                uiButton.onClick.AddListener(delegate { call(value); } );
            }
            buttonCallWithParams = call;
        }

        /// <summary>
        /// Set horizontal text padding.</summary>
        /// <param name="pad">Padding in UIScaler units.</param>
        /// <remarks>
        /// Must be called before SetText.</remarks>
        public void SetTextPadding(float pad)
        {
            textPadding = pad;
        }

        /// <summary>
        /// Set white element text.</summary>
        /// <param name="content">StringKey to be translated.</param>
        public void SetText(StringKey content)
        {
            SetText(content.Translate(), Color.white);
        }

        /// <summary>
        /// Set element text.</summary>
        /// <param name="content">StringKey to be translated.</param>
        /// <param name="textColor">Color to display text.</param>
        public void SetText(StringKey content, Color textColor)
        {
            SetText(content.Translate(), textColor);
        }

        /// <summary>
        /// Set white element text.</summary>
        /// <param name="content">Text to display.</param>
        public void SetText(string content)
        {
            SetText(content, Color.white);
        }

        /// <summary>
        /// Set element text.</summary>
        /// <param name="content">Text to display.</param>
        /// <param name="textColor">Color to display text.</param>
        public virtual void SetText(string content, Color textColor)
        {
            UnityEngine.UI.Text uiText = null;
            if (text == null)
            {
                text = new GameObject("UIText");
                text.tag = tag;
                uiText = text.AddComponent<UnityEngine.UI.Text>();
                uiText.alignment = TextAnchor.MiddleCenter;
                uiText.verticalOverflow = VerticalWrapMode.Overflow;
                uiText.font = Game.Get().gameType.GetFont();
                uiText.fontSize = UIScaler.GetSmallFont();
                text.transform.SetParent(bg.transform);
                RectTransform transform = text.GetComponent<RectTransform>();
                transform.anchorMin = Vector2.zero;
                transform.anchorMax = Vector2.one;
                transform.localPosition = Vector3.zero;
                transform.localScale = Vector3.one;
                transform.offsetMin = new Vector2(textPadding * UIScaler.GetPixelsPerUnit(), 0);
                transform.offsetMax = new Vector2(-textPadding * UIScaler.GetPixelsPerUnit(), 0);

                if (buttonCall != null)
                {
                    UnityEngine.UI.Button uiButton = text.AddComponent<UnityEngine.UI.Button>();
                    uiButton.interactable = true;
                    uiButton.onClick.AddListener(buttonCall);
                }
                if (buttonCallWithParams != null)
                {
                    UnityEngine.UI.Button uiButton = text.AddComponent<UnityEngine.UI.Button>();
                    uiButton.interactable = true;
                    // use button text as parameters
                    uiButton.onClick.AddListener(delegate { buttonCallWithParams(content); } );
                }
            }
            uiText = text.GetComponent<UnityEngine.UI.Text>();
            uiText.color = PaperTextColor(textColor);
            if (textColor.Equals(Color.black))
            {
                uiText.material = (Material)Resources.Load("Fonts/FontMaterial");
            }
            else
            {
                uiText.material = uiText.font.material;
            }

            if (uiText.font != null && Game.Get() != null && Game.Get().gameType is D2EGameType && uiText.font.name == Game.Get().gameType.GetHeaderFont().name)
            {
                uiText.text = content.ToUpper();
            }
            else
            {
                uiText.text = content;
            }
        }

        /// <summary>
        /// Set Outline color and size.</summary>
        /// <param name="size">Size in pixels.</param>
        /// <param name="color">Outline color.</param>
        /// <remarks>
        /// Must be called after SetText.</remarks>
        public void SetTextOutline(Color color, int size)
        {
            var outline = text.GetComponent<Outline>();
            if (outline == null)
            {
                outline = text.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = new Vector2(size, size);
        }

        /// <summary>
        /// Set font size.</summary>
        /// <param name="size">Size in pixels.</param>
        /// <remarks>
        /// Must be called after SetText.</remarks>
        public virtual void SetFontSize(int size)
        {
            text.GetComponent<UnityEngine.UI.Text>().fontSize = size;
        }

        /// <summary>
        /// Set font style.</summary>
        /// <param name="style">Style of text (bold, italic...) </param>
        /// <remarks>
        /// Must be called after SetText.</remarks>
        public virtual void SetFontStyle(FontStyle style)
        {
            text.GetComponent<UnityEngine.UI.Text>().fontStyle = style;
        }

        /// <summary>
        /// Set font.</summary>
        /// <param name="font">Font to use.</param>
        /// <remarks>
        /// Must be called after SetText.</remarks>
        public virtual void SetFont(Font font)
        {
            if (text == null) { ValkyrieTools.ValkyrieDebug.Log("SetFont called without text"); return; }
            text.GetComponent<UnityEngine.UI.Text>().font = font;

            if (font != null && Game.Get() != null && Game.Get().gameType is D2EGameType && font.name == Game.Get().gameType.GetHeaderFont().name)
            {
                text.GetComponent<UnityEngine.UI.Text>().text = text.GetComponent<UnityEngine.UI.Text>().text.ToUpper();
            }
        }

        /// <summary>
        /// Set text alignment.</summary>
        /// <param name="align">Text alignment.</param>
        /// <remarks>
        /// Must be called after SetText.</remarks>
        public virtual void SetTextAlignment(TextAnchor align)
        {
            if (text == null) { ValkyrieTools.ValkyrieDebug.Log("SetTextAlignment called without text"); return; }
            text.GetComponent<UnityEngine.UI.Text>().alignment = align;
        }

        /// <summary>
        /// Get the UIElement display text.</summary>
        /// <returns>
        /// The display text as a string or "" if not set.</returns>
        public virtual string GetText()
        {
            if (text == null) return "";
            return text.GetComponent<UnityEngine.UI.Text>().text;
        }

        /// <summary>
        /// Get the color of the UIElement display text.</summary>
        /// <returns>
        /// The display text color or Color.clear if text is not set.</returns>
        public virtual Color GetTextColor()
        {
            if (text == null) return Color.clear;
            return text.GetComponent<UnityEngine.UI.Text>().color;
        }

        /// <summary>
        /// Is there any display text?</summary>
        /// <returns>
        /// True if text not set or empty</returns>
        public virtual bool Empty()
        {
            if (text == null) return true;
            return text.GetComponent<UnityEngine.UI.Text>().text.Length == 0;
        }

        /// <summary>
        /// Get the length of a text string at small size with standard font.</summary>
        /// <param name="content">Text to translate.</param>
        /// <returns>
        /// The size of the text in UIScaler units.</returns>
        public float GetStringWidth(StringKey content)
        {
            return GetStringWidth(content.Translate());
        }

        /// <summary>
        /// Get the length of a text string at given size with given font.</summary>
        /// <param name="content">Text to translate.</param>
        /// <param name="fontSize">Size of font.</param>
        /// <returns>
        /// The size of the text in UIScaler units.</returns>
        public float GetStringWidth(StringKey content, int fontSize)
        {
            return GetStringWidth(content.Translate(), fontSize);
        }

        /// <summary>
        /// Get the length of a text string at small size with given font.</summary>
        /// <param name="content">Text to translate.</param>
        /// <param name="fontSize">Size of font.</param>
        /// <param name="fontName">Name of font.</param>
        /// <returns>
        /// The size of the text in UIScaler units.</returns>
        public float GetStringWidth(StringKey content, int fontSize, Font fontName)
        {
            return GetStringWidth(content.Translate(), fontSize, fontName);
        }

        /// <summary>
        /// Get the length of the currently displayed text string.</summary>
        /// <returns>
        /// The size of the text in UIScaler units.</returns>
        public float GetStringWidth()
        {
            return text.GetComponent<UnityEngine.UI.Text>().preferredWidth / UIScaler.GetPixelsPerUnit();
        }

        /// <summary>
        /// Get the length of a text string at small size with standard font.</summary>
        /// <param name="content">Text to measure.</param>
        /// <returns>
        /// The size of the text in UIScaler units.</returns>
        public float GetStringWidth(string content)
        {
            return GetStringWidth(content, UIScaler.GetSmallFont());
        }

        /// <summary>
        /// Get the length of a text string at small size with standard font.</summary>
        /// <param name="content">Text to measure.</param>
        /// <param name="fontSize">Size of font.</param>
        /// <returns>
        /// The size of the text in UIScaler units.</returns>
        public float GetStringWidth(string content, int fontSize)
        {
            return GetStringWidth(content, fontSize, Game.Get().gameType.GetFont());
        }

        /// <summary>
        /// Get the length of a text string at small size with standard font.</summary>
        /// <param name="content">Text to measure.</param>
        /// <param name="fontSize">Size of font.</param>
        /// <param name="fontName">Name of font.</param>
        /// <returns>
        /// The size of the text in UIScaler units.</returns>
        public float GetStringWidth(string content, int fontSize, Font fontName)
        {
            float width = 0f;
            if (textWidthObj == null)
            {
                textWidthObj = new GameObject("TextSizing");
                textWidthObj.AddComponent<UnityEngine.UI.Text>();
                RectTransform transform = textWidthObj.GetComponent<RectTransform>();
                transform.offsetMax = new Vector2(20000, 20000);
                textWidthObj.GetComponent<UnityEngine.UI.Text>().font = fontName;
                textWidthObj.GetComponent<UnityEngine.UI.Text>().fontSize = fontSize;
            }
            textWidthObj.GetComponent<UnityEngine.UI.Text>().text = content;
            width = (textWidthObj.GetComponent<UnityEngine.UI.Text>().preferredWidth / UIScaler.GetPixelsPerUnit()) + (textPaddingDefault * 2);
            Object.Destroy(textWidthObj);
            textWidthObj = null;
            return width;
        }

        /// <summary>
        /// Get the height of a text box of fixed width with a text string at small size with standard font and standard padding.</summary>
        /// <param name="content">Text to translate.</param>
        /// <param name="width">Width of the text box in UIScaler units.</param>
        /// <returns>
        /// The required text box height in UIScaler units.</returns>
        public float GetStringHeight(StringKey content, float width)
        {
            return GetStringHeight(content.Translate(), width);
        }

        /// <summary>
        /// Get the height of a text box of fixed width with a text string at small size with standard font and standard padding.</summary>
        /// <param name="content">Text to measure.</param>
        /// <param name="width">Width of the text box in UIScaler units.</param>
        /// <returns>
        /// The required text box height in UIScaler units.</returns>
        public float GetStringHeight(string content, float width)
        {
            float height = 0f;
            if (textHeightObj == null)
            {
                textHeightObj = new GameObject("TextSizing");
                textHeightObj.AddComponent<UnityEngine.UI.Text>();
                RectTransform transform = textHeightObj.GetComponent<RectTransform>();
                transform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 6000 * UIScaler.GetPixelsPerUnit());
                transform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, (width - (textPaddingDefault * 2)) * UIScaler.GetPixelsPerUnit());
                textHeightObj.GetComponent<UnityEngine.UI.Text>().font = Game.Get().gameType.GetFont();
                textHeightObj.GetComponent<UnityEngine.UI.Text>().fontSize = UIScaler.GetSmallFont();
            }
            textHeightObj.GetComponent<UnityEngine.UI.Text>().text = content;
            height = (textHeightObj.GetComponent<UnityEngine.UI.Text>().preferredHeight / UIScaler.GetPixelsPerUnit()) + (textPaddingDefault * 2);
            Object.Destroy(textHeightObj);
            textHeightObj = null;
            return height;
        }

        /// <summary>
        /// Has the Unity object been destroyed?</summary>
        /// <returns>
        /// If the background Unity object has been destroyed.</returns>
        public bool ObjectDestroyed()
        {
            return bg == null;
        }

        /// <summary>
        /// Is the object at the specified location in UIScaler units</summary>
        /// <param name="x">x location to test</param>
        /// <param name="y">y location to test</param>
        /// <returns>If at that location</returns>
        public bool AtLocation(float x, float y)
        {
            return AtLocationPixels(x * UIScaler.GetPixelsPerUnit(), y * UIScaler.GetPixelsPerUnit());
        }

        /// <summary>
        /// Is the object at the specified location in pixels</summary>
        /// <param name="x">x location to test</param>
        /// <param name="y">y location to test</param>
        /// <returns>If at that location</returns>
        public bool AtLocationPixels(float x, float y)
        {
            RectTransform transBg = bg.GetComponent<RectTransform>();
            if (transBg.anchoredPosition.x - (transBg.sizeDelta.x / 2) > x) return false;
            if (-transBg.anchoredPosition.y - (transBg.sizeDelta.y / 2) > y) return false;
            if (transBg.anchoredPosition.x + (transBg.sizeDelta.x / 2) < x) return false;
            if (-transBg.anchoredPosition.y + (transBg.sizeDelta.y / 2) < y) return false;
            return true;
        }

        /// <summary>
        /// Get the amount of space for additional lines in the box, can be negative.</summary>
        /// <returns>Amount of vertical space, less padding value in UIScaler units</returns>
        protected float GetVerticalTextSpace()
        {
            float gap = text.GetComponent<RectTransform>().rect.height - text.GetComponent<UnityEngine.UI.Text>().preferredHeight;
            gap -= textPaddingDefault * 2f;
            return gap / UIScaler.GetPixelsPerUnit();
        }

        /// <summary>
        /// Is the amount of free vertical space within a specified range?</summary>
        /// <param name="min">Minimum space in UIScaler units</param>
        /// <param name="max">Maximum space in UIScaler units</param>
        protected bool VerticalTextSpaceInRange(float min, float max)
        {
            if (GetVerticalTextSpace() < min) return false;
            if (GetVerticalTextSpace() > max) return false;
            return true;
        }

        /// <summary>
        /// Is the height equal to text size plus padding?</summary>
        /// <param name="space">Additional vertical padding in UIScaler units</param>
        /// <returns>True if height matches</returns>
        public bool HeightAtTextPadding(float space = 0)
        {
            // Allow for floating point errors
            return VerticalTextSpaceInRange(space - 0.01f, space + 0.01f);
        }

        /// <summary>
        /// Set the UIElement to text height plus vertical padding</summary>
        /// <param name="space">Additional vertical padding in UIScaler units</param>
        /// <returns>New UIElement height in UIScaler units</returns>
        public virtual float HeightToTextPadding(float space = 0)
        {
            float newHeight = (text.GetComponent<UnityEngine.UI.Text>().preferredHeight / UIScaler.GetPixelsPerUnit()) + (textPaddingDefault * 2f) + space;
            if (newHeight < 1 + (textPaddingDefault * 2f))
            {
                newHeight = 1 + (textPaddingDefault * 2f);
            }
            float oldHeight = GetRectTransform().sizeDelta.y;
            float newHeightPixels = newHeight * UIScaler.GetPixelsPerUnit();
            GetRectTransform().anchoredPosition = new Vector2(GetRectTransform().anchoredPosition.x, GetRectTransform().anchoredPosition.y - ((newHeightPixels - oldHeight) / 2f));
            GetRectTransform().sizeDelta = new Vector2(GetRectTransform().sizeDelta.x, newHeightPixels);
            return newHeight;
        }
        /// <summary>
        /// Set element text, truncating if it exceeds width.</summary>
        /// <param name="content">Text to display.</param>
        public virtual void SetTextFileName(string content)
        {
            if (content.Length > 20)
            {
                string filename = System.IO.Path.GetFileName(content);
                string directory = System.IO.Path.GetDirectoryName(content);
                
                if (content.Length > 40)
                {
                    if (filename.Length > 20)
                    {
                        content = "..." + System.IO.Path.DirectorySeparatorChar + filename;
                    }
                    else
                    {
                        // keep the last 30 chars
                        content = "..." + content.Substring(content.Length - 30);
                    }
                }
            }
            SetText(content, Color.white);
        }
    }
}
