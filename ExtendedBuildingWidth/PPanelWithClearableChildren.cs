using PeterHan.PLib.UI;

namespace ExtendedBuildingWidth
{
    public class PPanelWithClearableChildren : PPanel
    {
        public PPanelWithClearableChildren(string name) : base(name) { }
        public void ClearChildren() => base.children.Clear();
    }
}