using System;
using System.Drawing;
using System.Windows.Forms;

namespace LauncherGen
{
    /// <summary>编辑单个游戏条目的对话框。</summary>
    public class GameEditForm : Form
    {
        public GameItem Result { get; private set; }
        private readonly TextBox _txtName, _txtPath, _txtArgs, _txtWork;

        public GameEditForm(GameItem item)
        {
            Text = "编辑游戏";
            Size = new Size(560, 300);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Microsoft YaHei", 9F);

            int x2 = 100, y = 20;
            Controls.Add(new Label { Text = "显示名称", Location = new Point(16, y + 4), AutoSize = true });
            _txtName = new TextBox { Location = new Point(x2, y), Size = new Size(420, 26), Text = item.Name };
            Controls.Add(_txtName);

            y += 42;
            Controls.Add(new Label { Text = "文件路径", Location = new Point(16, y + 4), AutoSize = true });
            _txtPath = new TextBox { Location = new Point(x2, y), Size = new Size(420, 26), Text = item.RelativePath };
            Controls.Add(_txtPath);
            var btnBrowse = new Button { Text = "…", Location = new Point(528, y - 2), Size = new Size(30, 28) };
            btnBrowse.Click += (s, e) =>
            {
                var ofd = new OpenFileDialog { Filter = "游戏程序|*.exe;*.bat;*.cmd;*.sh|所有文件|*.*" };
                if (ofd.ShowDialog(this) == DialogResult.OK) _txtPath.Text = ofd.FileName;
            };
            Controls.Add(btnBrowse);

            y += 42;
            Controls.Add(new Label { Text = "启动参数", Location = new Point(16, y + 4), AutoSize = true });
            _txtArgs = new TextBox { Location = new Point(x2, y), Size = new Size(420, 26), Text = item.Arguments };
            Controls.Add(_txtArgs);

            y += 42;
            Controls.Add(new Label { Text = "工作目录", Location = new Point(16, y + 4), AutoSize = true });
            _txtWork = new TextBox { Location = new Point(x2, y), Size = new Size(420, 26), Text = item.WorkDir };
            Controls.Add(_txtWork);

            y += 50;
            var btnOk = new Button { Text = "确定", Location = new Point(360, y), Size = new Size(90, 32), DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "取消", Location = new Point(460, y), Size = new Size(90, 32), DialogResult = DialogResult.Cancel };
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            btnOk.Click += (s, e) =>
            {
                Result = new GameItem
                {
                    Name = _txtName.Text.Trim(),
                    RelativePath = _txtPath.Text.Trim(),
                    Arguments = _txtArgs.Text.Trim(),
                    WorkDir = _txtWork.Text.Trim()
                };
                if (string.IsNullOrEmpty(Result.Name) || string.IsNullOrEmpty(Result.RelativePath))
                {
                    MessageBox.Show(this, "名称与路径不能为空。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                }
            };
        }
    }
}
