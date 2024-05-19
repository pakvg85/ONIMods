using PeterHan.PLib.Core;
using PeterHan.PLib.UI;
using UnityEngine;

namespace ExtendedBuildingWidth
{
    /// <summary>
    /// Clone of PPanel with ability to flip its image by X
    /// </summary>
    public class PPanelWithFlippableImage : PPanel
    {
        public bool FlipByX { get; set; }

        private GameObject Build(Vector2 size, bool dynamic, bool flipByX)
        {
            GameObject gameObject = PUIElements.CreateUI(null, base.Name);

            SetImage(gameObject);

            var imageChild = gameObject.GetComponent<UnityEngine.UI.Image>();

            if (flipByX)
            {
                var transform = imageChild.rectTransform();
                var scale = Vector3.one;
                scale.x = -1.0f;
                transform.localScale = scale;
                //float rot = 0.0f;
                //transform.Rotate(new Vector3(0.0f, 0.0f, rot));
            }

            foreach (IUIComponent child in children)
            {
                GameObject gameObject2 = child.Build();
                gameObject2.SetParent(gameObject);
                PUIElements.SetAnchors(gameObject2, PUIAnchoring.Stretch, PUIAnchoring.Stretch);
            }

            BoxLayoutGroup boxLayoutGroup = gameObject.AddComponent<BoxLayoutGroup>();
            boxLayoutGroup.Params = new BoxLayoutParams
            {
                Direction = Direction,
                Alignment = Alignment,
                Spacing = Spacing,
                Margin = base.Margin
            };
            if (!dynamic)
            {
                boxLayoutGroup.LockLayout();
                gameObject.SetMinUISize(size);
            }

            boxLayoutGroup.flexibleWidth = base.FlexSize.x;
            boxLayoutGroup.flexibleHeight = base.FlexSize.y;
            InvokeRealize(gameObject);
            return gameObject;
        }

        public override GameObject Build()
        {
            return Build(default(Vector2), DynamicSize, FlipByX);
        }
    }
}