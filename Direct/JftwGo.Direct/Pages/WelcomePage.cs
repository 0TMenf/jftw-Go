using System.Drawing;
using System.Windows.Forms;
using JftwGo.UI;
using JftwGo.UI.Wizard;

using JftwGo.Core.Localization;

namespace JftwGo.Direct.Pages
{
    public class WelcomePage : WizardPage
    {
        public override string PageTitle { get { return L.Get("welcome.title"); } }

        public WelcomePage()
        {
            BackColor = UiTheme.Page;
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Surface,
                Padding = new Padding(24)
            };

            var title = new Label
            {
                Text = L.Get("welcome.heading"),
                Font = UiTheme.FontTitle,
                ForeColor = UiTheme.AccentDark,
                Dock = DockStyle.Top,
                Height = 44
            };
            var intro = new Label
            {
                Text = L.Get("welcome.body"),
                Font = UiTheme.FontNormal,
                ForeColor = UiTheme.TextPrimary,
                Dock = DockStyle.Fill
            };
            var tip = new Label
            {
                Text = L.Get("welcome.start"),
                Font = UiTheme.FontSmall,
                ForeColor = UiTheme.TextSecondary,
                Dock = DockStyle.Bottom,
                Height = 28
            };
            card.Controls.Add(intro);
            card.Controls.Add(title);
            card.Controls.Add(tip);
            Controls.Add(card);
        }
    }
}
