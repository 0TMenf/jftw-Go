using System.Windows.Forms;

namespace JftwGo.UI.Wizard
{
    /// <summary>
    /// 向导页基类。每个页面是 UserControl，由 WizardForm 依次承载。
    /// 子类重写 OnEnter/OnLeave/CanGoNext 参与流程控制。
    /// </summary>
    public class WizardPage : UserControl
    {
        public WizardForm Owner { get; set; }

        /// <summary>页面标题（显示在向导顶部）。</summary>
        public virtual string PageTitle { get { return "步骤"; } }

        /// <summary>进入页面时调用（可异步触发数据加载）。</summary>
        public virtual void OnEnter() { }

        /// <summary>离开页面时调用。</summary>
        public virtual void OnLeave() { }

        /// <summary>是否允许点击"下一步/完成"。默认允许。</summary>
        public virtual bool CanGoNext { get { return true; } }

        /// <summary>点击下一步时的动作，返回 true 表示由本页处理了导航（阻止默认前进）。</summary>
        public virtual bool OnNext() { return false; }

        /// <summary>点击上一步时是否阻止默认后退。</summary>
        public virtual bool OnBack() { return false; }
    }
}
